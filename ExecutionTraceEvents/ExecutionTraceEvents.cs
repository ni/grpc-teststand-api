using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Grpc.Core;

using static System.FormattableString;

// This file contains a custom grpc service that returns trace event information in a form suitable for use in a specific battery test system application.
// This service is currently necessary because the current TestStand gRPC api does not yet support events.
// Even after the TestStand gRPC provides event support, some applications may need to implement trace events in a
// custom manner for performance reasons in order to avoid additional round trip queries to the server to
// obtain the desired details of each trace event.

namespace BTS.ExecutionTraceEvents
{
    public class ExecutionTraceEventsService : ExecutionTraceEvents.ExecutionTraceEventsBase
    {
        private static readonly object _dataLock = new();

        // Holds all the traces messages for executions that don't have open server streams. 
        // This can be deleted if we don't care about any trace messages that occured before opening a stream.
        //
        // Note: This will lead to memory increase and can crash the server if there are too many
        // cached messages. We need to cap the size of the trace messages or we don't cache them.
        private static readonly Dictionary<long, string> _cachedTraceMessages = new();

        // This map contains all the open server streams. For every execution, there is a list of actions.
        // Each action represents a client connection.  This allows multiple clients to trace the same execution.
        private static readonly Dictionary<long, List<StreamCallContext>> _traceExecutionStreamCallContexts = new();

        // This list is used when tracing all executions. Using a list allows us to manage multiple clients.
        private static readonly List<StreamCallContext> _traceAllExecutionsStreamCallContexts = new();

        private static readonly Dictionary<ServerCallContext, ManualResetEvent> _listenForServerShutdownEvents = new();

        // This timer will check for any stream cancelling requests. This is needed because the streaming
        // methods don't have a loop.  They wait for an event to be signal before completing. The timer
        // will allows us to periodically check for cancelling requests so we can signal the streaming
        // method to complete.
        private System.Timers.Timer _checkForCancelRequestsTimer;

        private void StartTimerForMonitoringCancellationRequestsIfNecessary()
        {
            lock (_dataLock)
            {
                if (_checkForCancelRequestsTimer == null)
                {
                    _checkForCancelRequestsTimer = new System.Timers.Timer(500);
                    _checkForCancelRequestsTimer.Elapsed += OnCheckForCancelRequestsTimerElapsed;
                    _checkForCancelRequestsTimer.AutoReset = true;
                    _checkForCancelRequestsTimer.Enabled = true;  // Start timer
                }
            }
        }

        private void OnCheckForCancelRequestsTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_dataLock)
            {
                CheckIfStreamsNeedToBeCancelled(_traceAllExecutionsStreamCallContexts);

                foreach (List<StreamCallContext> streamCallContexts in _traceExecutionStreamCallContexts.Values)
                {
                    CheckIfStreamsNeedToBeCancelled(streamCallContexts);
                }

                foreach (var pair in _listenForServerShutdownEvents)
                {
                    ServerCallContext callContext = pair.Key;
                    if (callContext.CancellationToken.CanBeCanceled && callContext.CancellationToken.IsCancellationRequested)
                    {
                        // Client request to cancel streaming so lets cancel.
                        pair.Value.Set();
                    }
                }
            }
        }

        private static void CheckIfStreamsNeedToBeCancelled(List<StreamCallContext> streamCallContexts)
        {
            foreach (StreamCallContext streamCallContext in streamCallContexts)
            {
                if (streamCallContext.CallContext.CancellationToken.CanBeCanceled
                    && streamCallContext.CallContext.CancellationToken.IsCancellationRequested)
                {
                    // Client request to cancel streaming so lets cancel.
                    streamCallContext.DoneStreamingEvent.Set();
                }
            }
        }

        public static void AddTraceMessage(int executionId, string newMessage)
        {
            string message = newMessage + Environment.NewLine;

            // If there is a stream for all trace messages, write to it here.
            CallTraceAllExecutionsCallbacks(_traceAllExecutionsStreamCallContexts, message);

            // If there is a stream open for a specific execution, write directly to that stream.
            if (_traceExecutionStreamCallContexts.TryGetValue(executionId, out List<StreamCallContext> writeMessageToStreamFunctions))
            {
                CallTraceAllExecutionsCallbacks(writeMessageToStreamFunctions, message);
            }

            // If there are no streams open, cached the messages. When a stream is opened for a specific
            // execution, all the cached strings for that execution will be written to the stream.  This
            // can be deleted if we don't care about any previous trace messages before opening a stream.
            else if (_traceAllExecutionsStreamCallContexts.Count == 0)
            {
                lock (_dataLock)
                {
                    if (_cachedTraceMessages.TryGetValue(executionId, out string messages))
                    {
                        _cachedTraceMessages[executionId] = messages + message;
                    }
                    else
                    {
                        _cachedTraceMessages[executionId] = message;
                    }
                }
            }
        }

        private static void CallTraceAllExecutionsCallbacks(List<StreamCallContext> streamCallContexts, string message)
        {
            lock (_dataLock)
            {
                foreach (StreamCallContext streamCallContext in streamCallContexts)
                {
                    if (!streamCallContext.CallContext.CancellationToken.CanBeCanceled
                        || !streamCallContext.CallContext.CancellationToken.IsCancellationRequested)
                    {
                        streamCallContext.WriteToStreamCallback.Invoke(message);
                    }
                    else
                    {
                        // Client request to cancel streaming so lets cancel.
                        streamCallContext.DoneStreamingEvent.Set();
                    }
                }
            }
        }

        public static void ExecutionEnded(int executionId)
        {
            // Inform client that execution ended if necessary
            AddTraceMessage(executionId, Invariant($"Execution with id '{executionId}' is done running."));

            lock (_dataLock)
            {
                // Remove all cached trace messages if any
                if (_cachedTraceMessages.ContainsKey(executionId))
                {
                    _cachedTraceMessages.Remove(executionId);
                }
            }
        }

        public static void ServerShuttingDown()
        {
            // Close any open streams since server is shutting down
            lock (_dataLock)
            {
                foreach (var streamCallContexts in _traceExecutionStreamCallContexts.Values)
                {
                    foreach (var streamCallContext in streamCallContexts)
                    {
                        streamCallContext.DoneStreamingEvent.Set();
                    }
                }

                foreach (StreamCallContext streamCallContext in _traceAllExecutionsStreamCallContexts)
                {
                    streamCallContext.DoneStreamingEvent.Set();
                }

                foreach (var signalingEvent in _listenForServerShutdownEvents.Values)
                {
                    signalingEvent.Set();
                }
            }
        }

        public override async Task GetTraceEventMessages(
            ExecutionTraceEvents_GetTraceEventMessagesRequest request,
            IServerStreamWriter<ExecutionTraceEvents_GetTraceEventMessagesResponse> responseStream,
            ServerCallContext context)
        {
            string messages = string.Empty;

            StartTimerForMonitoringCancellationRequestsIfNecessary();

            // Cache stream and other necessary information so we can write trace messages to the steam
            // when handling execution trace event for the execution with the given id.
            StreamCallContext callContext = AddStreamCallContextForExecution(request.ExecutionId, responseStream, context);

            // If there are any cached trace messages, we need to send them.
            if (_cachedTraceMessages.ContainsKey(request.ExecutionId))
            {
                messages = _cachedTraceMessages[request.ExecutionId];
            }

            // Send any cached messages now.
            if (!string.IsNullOrEmpty(messages))
            {
                var response = new ExecutionTraceEvents_GetTraceEventMessagesResponse { Messages = messages };
                await responseStream.WriteAsync(response);
            }

            // We need to wait until execution is done. Returning from this method will signal the client that there
            // are no more trace messages. So, we need to make sure the stream is open while the execution is running.
            callContext.DoneStreamingEvent.WaitOne(Timeout.Infinite);

            RemoveCallbackForExecutionFromList(request.ExecutionId, callContext);
        }

        public override Task GetTraceEventMessagesForAllExecutions(
            ExecutionTraceEvents_GetTraceEventMessagesForAllExecutionsRequest request,
            IServerStreamWriter<ExecutionTraceEvents_GetTraceEventMessagesForAllExecutionsResponse> responseStream,
            ServerCallContext context)
        {
            StartTimerForMonitoringCancellationRequestsIfNecessary();

            // Cache stream and other information so we can write all execution trace messages.
            StreamCallContext streamCallContext = AddStreamCallContextForAllExecutions(responseStream, context);

            // Note: Code can be added here to get all cached messages

            // We need to wait until all executions are done. Returning from this method will signal the client that there
            // are no more trace messages. So, we need to make sure the stream is open while the execution is running.
            streamCallContext.DoneStreamingEvent.WaitOne(Timeout.Infinite);

            lock (_dataLock)
            {
                _traceAllExecutionsStreamCallContexts.Remove(streamCallContext);
            }

            return Task.CompletedTask;
        }

        public override Task ListenForServerShutdown(
            ExecutionTraceEvents_ListenForServerShutdownRequest request,
            IServerStreamWriter<ExecutionTraceEvents_ListenForServerShutdownResponse> responseStream,
            ServerCallContext context)
        {
            StartTimerForMonitoringCancellationRequestsIfNecessary();

            var signalingEvent = new ManualResetEvent(false);
            lock (_dataLock)
            {
                _listenForServerShutdownEvents[context] = signalingEvent;
            }

            signalingEvent.WaitOne(Timeout.Infinite);

            lock (_dataLock)
            {
                _listenForServerShutdownEvents.Remove(context);
            }

            return responseStream.WriteAsync(new ExecutionTraceEvents_ListenForServerShutdownResponse { ShuttingDown = true });
        }

        private static StreamCallContext AddStreamCallContextForExecution(
            long executionId,
            IServerStreamWriter<ExecutionTraceEvents_GetTraceEventMessagesResponse> responseStream,
            ServerCallContext context)
        {
            List<StreamCallContext> writeMessageToStreamFunctions;

            lock (_dataLock)
            {
                if (!_traceExecutionStreamCallContexts.TryGetValue(executionId, out writeMessageToStreamFunctions))
                {
                    writeMessageToStreamFunctions = new();
                    _traceExecutionStreamCallContexts[executionId] = writeMessageToStreamFunctions;
                }
            }

			void writeToStreamCallback(string message)
			{
				var response = new ExecutionTraceEvents_GetTraceEventMessagesResponse { Messages = message };
				Task task = responseStream.WriteAsync(response);
				task.Wait();
			}

			var streamCallContext = new StreamCallContext(writeToStreamCallback, context);
            lock (_dataLock)
            {
                writeMessageToStreamFunctions.Add(streamCallContext);
            }
            return streamCallContext;
        }

        private static void RemoveCallbackForExecutionFromList(long executionId, StreamCallContext callContext)
        {
            lock (_dataLock)
            {
                _traceExecutionStreamCallContexts[executionId].Remove(callContext);
                if (_traceExecutionStreamCallContexts[executionId].Count == 0)
                {
                    _traceExecutionStreamCallContexts.Remove(executionId);
                }
            }
        }

        private static StreamCallContext AddStreamCallContextForAllExecutions(
            IServerStreamWriter<ExecutionTraceEvents_GetTraceEventMessagesForAllExecutionsResponse> responseStream,
            ServerCallContext context)
        {
			void writeToStreamCallback(string message)
			{
				var response = new ExecutionTraceEvents_GetTraceEventMessagesForAllExecutionsResponse { Messages = message };
				Task task = responseStream.WriteAsync(response);
				task.Wait();
			}

			var streamCallContext = new StreamCallContext(writeToStreamCallback, context);
            lock (_dataLock)
            {
                _traceAllExecutionsStreamCallContexts.Add(streamCallContext);
            }
            return streamCallContext;
        }

        private class StreamCallContext
        {
            public Action<string> WriteToStreamCallback { get; }

            public ServerCallContext CallContext { get; }

            public ManualResetEvent DoneStreamingEvent { get; }

            public StreamCallContext(Action<string> writeToStreamCallback, ServerCallContext callContext)
            {
                WriteToStreamCallback = writeToStreamCallback;
                CallContext = callContext;

                // This will allow the client to cancel streaming trace message. It also allows stream to remain
                // open until executions are done if client does not cancel the streaming.
                DoneStreamingEvent = new ManualResetEvent(false);
            }
        }
    }

    public class Server
    {
        public static void RegisterServices(IEndpointRouteBuilder endPoints)
        {
            endPoints.MapGrpcService<ExecutionTraceEventsService>();
        }
    }
}
