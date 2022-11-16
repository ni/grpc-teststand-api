using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grpc.Core;
using Grpc.Net.Client;
using NationalInstruments.TestStand.Grpc.Net.Client.OO;  // instance lifetime API
using NationalInstruments.TestStand.API.Grpc; // TestStand Engine API
using NationalInstruments.TestStand.UI.Grpc;
using static System.FormattableString;
using static ExampleClient.Win32Interop;

namespace ExampleClient
{
	public partial class Example : Form
	{
		public enum ExecutionAction
        {
			Break,
			Resume,
			Terminate
        }

		const string SequentialModelFilename = "SequentialModel.seq";
		const string ModelOptionsFileSectionName = "ModelOptions";
		const string NumberOfTestSocketsPropertyName = "NumTestSockets";
		const string RunRemoteSequenceFile = "Run Remote Sequence File";
		const string RunningRemoteSequenceFile = "Running Remote Sequence File";
		const string ConnectionStatusConnected = "Connected";
		const string ConnectionStatusDisconnected = "Disconnected";
		const string ExecutionStateAborted = "Aborted";
		const string ExecutionStateError = "Error";
		const string ExecutionStateFailed = "Failed";
		const string ExecutionStatePassed = "Passed";
		const string ExecutionStatePaused = "Paused";
		const string ExecutionStateRunning = "Running...";
		const string ExecutionStateTerminated = "Terminated";
		const string ExecutionStateTerminating = "Terminating...";
		const string StepResultDone = "Done";
		const string StepResultSkipped = "Skipped";
		const string NotExecutedSequenceFile = "Not Executed";
		const string AddGlobal = "Add Global";
		const string DeleteGlobal = "Delete Global";
		const string NotConnected = "NotConnected";
		const string SecureConnection = "SecureConnection";
		const string NotSecureConnection = "NotSecureConnection";

        const int ColorBoxWidth = 3;
        // For a step result, we need to add a color box before the status. To do this,
        // we need to add the space to the result to draw the color box. ColorBoxSpace
        // defines the space to insert. We nee to add one character to ColorBoxWidth to
        // allow one space between the box and the actual result text.
        readonly string ColorBoxSpace = string.Format("{0," + (ColorBoxWidth + 1) + "}", "");

        const int StatusLength = 7;
		const int IndentOffsetForOneLevel = 4;
        readonly string IndentSpace = string.Format("{0," + IndentOffsetForOneLevel + "}", "");

		private readonly object _dataLock = new();
		private int _busyCount = 0;
		private Cursor _previousCursor;

		private GrpcChannel _gRPCChannel = null;

		// service clients for the interfaces we might want to use
		private InstanceLifetime.InstanceLifetimeClient _instanceLifetimeClient;
		private Engine.EngineClient _engineClient;
		private Step.StepClient _stepClient;
		private Execution.ExecutionClient _executionClient;
		private Thread.ThreadClient _threadClient;
		private SequenceContext.SequenceContextClient _sequenceContextClient;
		private PropertyObject.PropertyObjectClient _propertyObjectClient;
		private PropertyObjectFile.PropertyObjectFileClient _propertyObjectFileClient;
		private SearchDirectories.SearchDirectoriesClient _searchDirectoriesClient;
		private SearchDirectory.SearchDirectoryClient _searchDirectoryClient;
		private StationOptions.StationOptionsClient _stationOptionsClient;
		private ApplicationMgr.ApplicationMgrClient _applicationMgrClient;
		private Executions.ExecutionsClient _executionsClient;
		private UIMessage.UIMessageClient _uiMessageClient = null;
		private StepProperties.StepPropertiesClient _stepPropertiesClient = null;

		private readonly Dictionary<string, Stream> _imageList = new();

		private bool _isConnected = false;
		private string _nonSequentialProcessModelName = null;

		// remember some objects we create on the server for later use
		private EngineInstance _engine = null;
		private ExecutionInstance _activeExecution;

		private int _valueForSelectedItemIndex = -1;
		private ClientOptions _clientOptions;
		private bool _connectionIsSecured;
		private CreateChannelHelper _channelHelper;

		private ToolTipEx _numTestSocketsNumericUpDownToolTip;

		public Example(ClientOptions options)
		{
			InitializeComponent();

			InitializeImageList();
			SetMonospacedFontInTraceAndLogControls();
			UpdateTraceMessagesControls();

			_clientOptions = options;

			_connectionTypePictureBox.Image = new Bitmap(_imageList[NotConnected]);
			_connectionStatusDescriptionLabel.Text = ConnectionStatusDisconnected;
			_connectionStatusPictureBox.Image = new Bitmap(_imageList[ConnectionStatusDisconnected]);
			_executionStateDescriptionLabel.Text = string.Empty;

			_addGlobalButton.Image = new Bitmap(_imageList[AddGlobal]);
			_deleteGlobalButton.Image = new Bitmap(_imageList[DeleteGlobal]);

			_channelHelper = new CreateChannelHelper();
		}

		private void InitializeImageList()
		{
			_imageList[ConnectionStatusConnected] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.StatusConnected.png");
			_imageList[ConnectionStatusDisconnected] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.StatusDisconnected.png");
			_imageList[ExecutionStateAborted] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionAborted.ico");
			_imageList[ExecutionStateError] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionError.ico");
			_imageList[ExecutionStateFailed] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionFailed.ico");
			_imageList[ExecutionStatePassed] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionPassed.ico");
			_imageList[ExecutionStatePaused] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionPaused.ico");
			_imageList[ExecutionStateRunning] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionRunning.ico");
			_imageList[ExecutionStateTerminated] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionTerminated.ico");
			_imageList[ExecutionStateTerminating] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.ExecutionTerminating.ico");
			_imageList[AddGlobal] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.Add.ico");
			_imageList[DeleteGlobal] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.Delete.ico");
			_imageList[NotConnected] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.NotConnected.png");
			_imageList[SecureConnection] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.SecuredConnection.png");
			_imageList[NotSecureConnection] = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleClient.Resources.NotSecuredConnection.png");
		}

		private void SetMonospacedFontInTraceAndLogControls()
		{
            // These fonts are documented here: https://docs.microsoft.com/en-us/typography/font-list/
            const string PreferredMonospacedFontName = "Lucida Sans Typewriter";
			const string BackupMonospacedFontName = "Courier New";

            // Since "Lucida Sans Typewriter" is not installed by default in Windows,
            // we need to check if it is installed before trying to use it. If not, we
            // need to use the backup font "Courier New" which is installed by default.
            string fontToUse = BackupMonospacedFontName;
            var installedFontCollection = new InstalledFontCollection();

            foreach (FontFamily family in installedFontCollection.Families)
			{
				if (family.Name == PreferredMonospacedFontName)
				{
					fontToUse = PreferredMonospacedFontName;
					break;
				}
			}

			_logTextBox.Font = new Font(fontToUse, Font.Size);
			_executionTraceMessagesTextBox.Font = new Font(fontToUse, Font.Size);
        }

        private void SetConnectionStatus(bool isConnected)
		{
			// Control values need to be set in the UI thread. Also, running in the
			// UI thread, removes the need to add a lock when updating _isConnected.
			_connectionStatusDescriptionLabel.Invoke((Action)(() =>
			{
				SetConnectionStatusOnUIThread(isConnected);
			}));
		}

		private void SetConnectionStatusOnUIThread(bool isConnected)
        {
			Debug.Assert(!_connectionStatusDescriptionLabel.InvokeRequired);

			if (_isConnected != isConnected)
			{
				_isConnected = isConnected;
				if (_isConnected)
				{
					_serverHeartbeatTimer.Start();
				}
				else
				{
					// Stop heartbeat since we know we are no longer connected to the server
					_serverHeartbeatTimer.Stop();
				}

				string connectionStatusString, connectionStatusImageName, connectionType;
				if (_isConnected)
				{
					connectionStatusString = ConnectionStatusConnected;
					connectionStatusImageName = ConnectionStatusConnected;
					connectionType = _connectionIsSecured ? SecureConnection : NotSecureConnection;
				}
				else
				{
					connectionStatusString = ConnectionStatusDisconnected;
					connectionStatusImageName = ConnectionStatusDisconnected;
					connectionType = NotConnected;

					// Reset the execution status
					SetExecutionStatus(NotExecutedSequenceFile);

					_addGlobalButton.Enabled = false;
					_deleteGlobalButton.Enabled = false;
					_commitGlobalsToDiskButton.Enabled = false;
					_stationGlobalsListView.Enabled = false;
					_valueTextBox.Enabled = false;
				}

				_enableTracingCheckBox.Enabled = _isConnected;

				_connectionTypePictureBox.Image = new Bitmap(_imageList[connectionType]);
				_connectionStatusPictureBox.Image = new Bitmap(_imageList[connectionStatusImageName]);
				_connectionStatusDescriptionLabel.Text = connectionStatusString;
			}
		}

		// pass false to onlyIfNeeded if the server address might have changed
		private void Setup(bool onlyIfNeeded)
		{
			if (!onlyIfNeeded || _gRPCChannel == null)
			{
				Cleanup();

				_gRPCChannel = _channelHelper.OpenChannel(_serverAddressTextBox.Text, _clientOptions, out _connectionIsSecured, out string connectionErrors);
				if (_gRPCChannel != null)
				{
					// create the service clients for the interfaces we want to use 
					SetupServiceClients();

					// the engine is used a lot, make sure we have a reference handy
					GetEngineReference();

					// Add station globals to the list view
					RefreshStationGlobals();

					InitializeProcessModelInformation();

					HandleUIMessages();

					InitializeEnableTracingOption();

					LogLine("Connection Succeeded.");
					SetConnectionStatus(true);
				}
				else
                {
					LogLine(connectionErrors);
					SetConnectionStatus(false);
				}
			}
		}

		private void Cleanup()
		{	
			// if a channel already exists, dispose it
			if (_gRPCChannel != null)
			{
				if (_isConnected)
				{
					// let the server know this connection and all its instance ids and event streams are no longer needed
					_instanceLifetimeClient?.Clear(new InstanceLifetime_ClearRequest { DiscardConnection = true });
				}

				Task.Run(async () => await _gRPCChannel.ShutdownAsync()).Wait(); // call async task in thread pool thread so that Wait() can't prevent the continuation from completing 
				_gRPCChannel = null;
			}

			_engine = null; // all instance ids are now invalid

		}

		private static string _errorResultStatusConstant;

		private void HandleUIMessages()
		{
			bool demoEventMessages = false;

			if (String.IsNullOrEmpty(_errorResultStatusConstant))
				 _errorResultStatusConstant = _stepPropertiesClient.Get_ResultStatus_Error(new ConstantValueRequest()).ReturnValue;

			// get stream of UIMessage events
			var call = _engineClient.GetEvents_UIMessageEvent(new Engine_GetEvents_UIMessageEventRequest
			{
				Instance = _engine,
				DiscardMultipleEventsWithinPeriod = 0.0,
				ReplyTimeout = 20.0,
				TimeoutCancelsEvents = true
			});

			var uiMessageEventStream = call.ResponseStream;

			// read the message stream from a separate thread. Otherwise the asynchronous message reading loop would block whenever the thread in which it is established
			// blocks in a synchronous call, including synchronous gRPC calls. Because some TestStand gRPC API calls can generate events that require replies before completing
			// the call, event loops should not be in a thread that might make non-async calls to the TestStand API, or any other calls that might block for an unbounded period.
			Task.Run(async () =>
			{
				const int IndentOffset = 4;
				const int StatusLength = 7;

				try
				{
					await foreach (var uiMessageEvent in uiMessageEventStream.ReadAllAsync())
					{
						var now = DateTime.Now;

						if (demoEventMessages)
							LogLine($"received msg id {uiMessageEvent.Msg.Id}  eventId: {uiMessageEvent.EventId}");

						var uiMessageCode = _uiMessageClient.Get_Event(new UIMessage_Get_EventRequest { Instance = uiMessageEvent.Msg }).ReturnValue;

						switch (uiMessageCode)
						{
							case UIMessageCodes.UimsgEndExecution:
								{
									var executionInstance = (await _uiMessageClient.Get_ExecutionAsync(new UIMessage_Get_ExecutionRequest { Instance = uiMessageEvent.Msg })).ReturnValue;
									var executionId = (await _executionClient.Get_IdAsync(new Execution_Get_IdRequest { Instance = executionInstance })).ReturnValue;

									// Log the end of an execution only if tracing is enabled.
									if (_enableTracingCheckBox.Checked)
									{
										LogTraceMessage(Invariant($"Execution with id '{executionId}' is done running.") + Environment.NewLine);
									}
								}
								break;
							case UIMessageCodes.UimsgTrace:
								{
									string message = string.Empty;

									var threadInstance = _uiMessageClient.Get_Thread(new UIMessage_Get_ThreadRequest { Instance = uiMessageEvent.Msg }).ReturnValue;
									var sequenceContextInstance = _threadClient.GetSequenceContext(new Thread_GetSequenceContextRequest { Instance =  threadInstance, CallStackIndex = 0 }).ReturnValue;
									var sequenceContextPropertyObjectInstance = new PropertyObjectInstance { Id = sequenceContextInstance.Id };
									var previousStepIndex = _sequenceContextClient.Get_PreviousStepIndex(new SequenceContext_Get_PreviousStepIndexRequest { Instance = sequenceContextInstance }).ReturnValue;

									if (previousStepIndex >= 0)
									{
										int numberOfSockets = (int)_propertyObjectClient.GetValNumber(new PropertyObject_GetValNumberRequest
										{ 
											Instance = sequenceContextPropertyObjectInstance,
											 LookupString = "Runstate.TestSockets.Count",
											 Options = PropertyOptions.PropOptionNoOptions
										}).ReturnValue;

										if (numberOfSockets > 1)
										{
											int socketNumber = (int)_propertyObjectClient.GetValNumber(new PropertyObject_GetValNumberRequest
											{
												Instance = sequenceContextPropertyObjectInstance,
												LookupString = "Runstate.TestSockets.MyIndex",
												Options = PropertyOptions.PropOptionNoOptions
											}).ReturnValue;

											// Make socket two characters long and left aligned it
											message = Invariant($"Socket {socketNumber,-2}  ");
										}

										var previousStepInstance = (await _sequenceContextClient.Get_PreviousStepAsync(new SequenceContext_Get_PreviousStepRequest { Instance = sequenceContextInstance })).ReturnValue;
										var stepName = (await _stepClient.Get_NameAsync(new Step_Get_NameRequest { Instance = previousStepInstance })).ReturnValue;
										var status = (await _stepClient.Get_ResultStatusAsync(new Step_Get_ResultStatusRequest { Instance = previousStepInstance })).ReturnValue;

										// Make status 7 characters long and left aligned it.
										string statusFormatted = string.Format("{0,-" + StatusLength + "}", status);
										message += Invariant($"{statusFormatted}  Step {stepName}");

										if (status == _errorResultStatusConstant)
										{
											var stepObj = new PropertyObjectInstance { Id = previousStepInstance.Id }; // no need to call AsPropertyObject, just use the same Id and save a round trip
											var errorCode = (await _propertyObjectClient.GetValNumberAsync(new PropertyObject_GetValNumberRequest { Instance = stepObj, LookupString = "Result.Error.Code", Options = PropertyOptions.PropOptionNoOptions })).ReturnValue;
											var errorMessage = (await _propertyObjectClient.GetValStringAsync(new PropertyObject_GetValStringRequest { Instance = stepObj, LookupString = "Result.Error.Msg", Options = PropertyOptions.PropOptionNoOptions })).ReturnValue;

											// Indent the Code label below the Step label
											int codeStartingIndex = message.IndexOf("Step") + IndentOffset;
											string indentedCodeLabel = string.Format("\n{0," + codeStartingIndex + "}Code", "");

											message += Invariant($"{indentedCodeLabel} {errorCode}  Message {errorMessage}");
										}

										LogTraceMessage(message + Environment.NewLine);
									}
								}
								break;
						}

						_ = _engineClient.ReplyToEvent_UIMessageEventAsync(new Engine_ReplyToEvent_UIMessageEventRequest { EventId = uiMessageEvent.EventId });

						if (demoEventMessages)
						{
							var elapsed = DateTime.Now - now;
							LogLine("UIMessage event: " + uiMessageCode.ToString() + ", Processing Time = " + elapsed.TotalSeconds.ToString());
						}
					}

					LogLine("The UIMessage event stream exited without an error.");

				}
				catch (Exception ex)
				{
					LogLine("The UIMessage event stream exited with an error: " + ex.Message);
				}

				call.Dispose(); // cancels the call, in case we exited with an error
			});
		}

		private void SetupServiceClients()
		{
			// with gRPC you need a separate 'client' to talk to each 'service'

			// clients for TestStand API interfaces we want to use
			_engineClient = new Engine.EngineClient(_gRPCChannel);
			_stepClient = new Step.StepClient(_gRPCChannel);
			_executionClient = new Execution.ExecutionClient(_gRPCChannel);
			_threadClient = new Thread.ThreadClient(_gRPCChannel);
			_sequenceContextClient = new SequenceContext.SequenceContextClient(_gRPCChannel);
			_propertyObjectClient = new PropertyObject.PropertyObjectClient(_gRPCChannel);
			_propertyObjectFileClient = new PropertyObjectFile.PropertyObjectFileClient(_gRPCChannel);
			_searchDirectoriesClient = new SearchDirectories.SearchDirectoriesClient(_gRPCChannel);
			_searchDirectoryClient = new SearchDirectory.SearchDirectoryClient(_gRPCChannel);
			_stationOptionsClient = new StationOptions.StationOptionsClient(_gRPCChannel);
			_applicationMgrClient = new ApplicationMgr.ApplicationMgrClient(_gRPCChannel);
			_executionsClient = new Executions.ExecutionsClient(_gRPCChannel);
			_uiMessageClient = new UIMessage.UIMessageClient(_gRPCChannel);
			_stepPropertiesClient = new StepProperties.StepPropertiesClient(_gRPCChannel);

			// client for the Instance Lifetime API, which lets you tell the server when your client doesn't need specific objects on the server any longer
			_instanceLifetimeClient = new InstanceLifetime.InstanceLifetimeClient(_gRPCChannel);
		}

		private void GetEngineReference()
		{
			if (_engine == null)
			{
				_engine = _engineClient.Engine(new Engine_EngineRequest()).ReturnValue;
				
				// in case someone changes the default lifespan, always make the engine have an unlimited lifespan
				_instanceLifetimeClient.SetLifespan(new InstanceLifetime_SetLifespanRequest
				{
					Value = new ObjectInstance() { Id = _engine.Id },
					LifeSpan = _instanceLifetimeClient.Get_InfiniteLifetime(new InstanceLifetime_Get_InfiniteLifetimeRequest()).ReturnValue
				});
			}
		}

		private void InitializeProcessModelInformation()
		{
			// Initialize the process model MRU list
			PropertyObjectFileInstance configFile = _engineClient.GetEngineConfigFile(
				new Engine_GetEngineConfigFileRequest
				{
					Instance = _engine,
					ConfigFileType = PropertyObjectFileTypes.FileTypeGeneralEngineConfigFile
				}).ReturnValue;

			PropertyObjectInstance data = _propertyObjectFileClient.Get_Data(new PropertyObjectFile_Get_DataRequest { Instance = configFile }).ReturnValue;
			string mruList = _propertyObjectClient.GetValString(
				new PropertyObject_GetValStringRequest
				{
					Instance = data,
					LookupString = "ModelsMRUList",
					Options = PropertyOptions.PropOptionNoOptions
				}).ReturnValue;
			string[] processModels = mruList.Split('|');

			_stationModelComboBox.Items.Clear();
			_stationModelComboBox.Items.AddRange(processModels);

			// Get the active process model and the number of test sockets
			StationOptionsInstance stationOptions = GetStationOptions();
			string modelPath = _stationOptionsClient.Get_StationModelSequenceFilePath(
				new StationOptions_Get_StationModelSequenceFilePathRequest
				{
					Instance = stationOptions
				}).ReturnValue;

			_stationModelComboBox.Text = Path.GetFileName(modelPath);

			// Always dispose the tooltip. It will be recreated below if needed again.
			_numTestSocketsNumericUpDownToolTip?.Dispose();
			_numTestSocketsNumericUpDownToolTip = null;

			_numTestSocketsNumericUpDown.Value = GetMultipleUUTSettingsNumberOfTestSocketsOption();
			if (_numTestSocketsNumericUpDown.Value == 0)
            {
				_numTestSocketsNumericUpDown.Enabled = false;

				string tooltip = "This option is not available because the model options file is not found on the server.\n" +
                    "Change a model option on the server to create the file.\n" +
                    "Reconnect to the server to enable this option when using Batch and Parallel models.";
				_numTestSocketsNumericUpDownToolTip = new ToolTipEx(this, _numTestSocketsNumericUpDown, tooltip);
			}
		}

		private void InitializeEnableTracingOption()
		{
			StationOptionsInstance stationOptions = GetStationOptions();
			_enableTracingCheckBox.Checked = _stationOptionsClient.Get_TracingEnabled(
				new StationOptions_Get_TracingEnabledRequest
				{
					Instance = stationOptions
				}).ReturnValue;

			UpdateTraceMessagesControls();
		}

		private void UpdateTraceMessagesControls()
		{
			bool tracingIsEnabled = _enableTracingCheckBox.Checked;
			_executionTraceMessagesLabel.Enabled = tracingIsEnabled;
			_executionTraceMessagesTextBox.Enabled = tracingIsEnabled;
		}

		private StationOptionsInstance GetStationOptions()
		{
			return _engineClient.Get_StationOptions(new Engine_Get_StationOptionsRequest { Instance = _engine }).ReturnValue;
		}

		private int GetMultipleUUTSettingsNumberOfTestSocketsOption()
		{
			// Zero is not a valid value for number of test sockets. I cannot return -1 since the value is set on
			// _numTestSocketsNumericUpDown and that control does not accept negative values.
			int numberOfTestSockets = 0;

			PropertyObjectInstance modelOptions = GetProcessModelOptions();
			if (modelOptions != null)
			{
				// Get number of test sockets
				numberOfTestSockets = (int)_propertyObjectClient.GetValNumber(
					new PropertyObject_GetValNumberRequest
					{
						Instance = modelOptions,
						LookupString = NumberOfTestSocketsPropertyName,
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;
			}

			return numberOfTestSockets;
		}

		private void SetMultipleUUTSettingsNumberOfTestSocketsOption(int numberOfSockets)
		{
			// Always get the model options from the server before setting the new number of test sockets.
			PropertyObjectInstance modelOptions = GetProcessModelOptions();
			if (modelOptions != null)
			{
				// Set the number of test sockets
				_propertyObjectClient.SetValNumber(
					new PropertyObject_SetValNumberRequest
					{
						Instance = modelOptions,
						LookupString = NumberOfTestSocketsPropertyName,
						NewValue = numberOfSockets,
						Options = PropertyOptions.PropOptionNoOptions
					});

				// Persist the new value
				string modelOptionsFilePath = GetModelOptionsFilePath();
				_propertyObjectClient.Write(
					new PropertyObject_WriteRequest
					{
						Instance = modelOptions,
						PathString = modelOptionsFilePath,
						ObjectName = ModelOptionsFileSectionName,
						RWoptions = ReadWriteOptions.RwoptionEraseAll
					});
			}
		}

		private PropertyObjectInstance GetProcessModelOptions()
		{
			const string ModelOptionsTypeName = "ModelOptions";

			string modelOptionsFilePath = GetModelOptionsFilePath();

			// Create object to store the model options
			PropertyObjectInstance modelOptions = _engineClient.NewPropertyObject(
				new Engine_NewPropertyObjectRequest
				{
					Instance = _engine,
					ValueType = PropertyValueTypes.PropValTypeNamedType,
					AsArray = false,
					TypeNameParam = ModelOptionsTypeName,
					Options = PropertyOptions.PropOptionNoOptions
				}).ReturnValue;

			try
			{
				// Read the model options
				_propertyObjectClient.ReadEx(
					new PropertyObject_ReadExRequest
					{
						Instance = modelOptions,
						PathString = modelOptionsFilePath,
						ObjectName = ModelOptionsFileSectionName,
						RWoptions = ReadWriteOptions.RwoptionNoOptions,
						HandlerType = TypeConflictHandlerTypes.ConflictHandlerUseGlobalType
					});
			}
			catch (RpcException rpcException)
            {
				TSError errorCode = GetTSErrorCode(rpcException, out _);
				if (errorCode == TSError.TsErrFileWasNotFound || errorCode == TSError.TsErrUnableToOpenFile)
                {
					// File does not exist on the server. Return a null object to let the caller know we
					// cannot get the model options.
					modelOptions = null;
                }
				else
                {
					throw;
                }
			}

			return modelOptions;
		}

		private TSError GetTSErrorCode(RpcException rpcException, out string description)
        {
			description = null;

			string errorCodeString = rpcException.Trailers.GetValue("tserrorcode");
			if (!string.IsNullOrEmpty(errorCodeString))
            {
				if (int.TryParse(errorCodeString, out int errorCode))
                {
					description = _engineClient.GetErrorString(new Engine_GetErrorStringRequest { Instance = _engine, ErrorCode = (TSError)errorCode }).ErrorString;
					return (TSError)errorCode;
                }
            }

			return TSError.TsErrNoError;
		}

		private string GetModelOptionsFilePath()
		{
			const string ModelOptionsFilename = "TestStandModelModelOptions.ini";

			string modelOptionsFilePath = _engineClient.GetTestStandPath(
				new Engine_GetTestStandPathRequest
				{
					Instance = _engine,
					TestStandPath = TestStandPaths.TestStandPathConfig
				}).ReturnValue;
			modelOptionsFilePath = Path.Combine(modelOptionsFilePath, ModelOptionsFilename);

			return modelOptionsFilePath;
		}

		private void RefreshStationGlobals()
        {
			_stationGlobalsListView.BeginUpdate();
			_stationGlobalsListView.Items.Clear();

			// Always refresh the station global by getting them directly from the server
			PropertyObjectInstance stationGlobals = _engineClient.Get_Globals(new Engine_Get_GlobalsRequest { Instance = _engine }).ReturnValue;
			int numberOfGlobals = _propertyObjectClient.GetNumSubProperties(
				new PropertyObject_GetNumSubPropertiesRequest
				{
					Instance = stationGlobals,
					LookupString = string.Empty
				}).ReturnValue;

			List<ListViewItem> globalVariables = new(numberOfGlobals);
			for (int index = 0; index < numberOfGlobals; index++)
			{
				// Get the station global
				PropertyObjectInstance global = _propertyObjectClient.GetNthSubProperty(
					new PropertyObject_GetNthSubPropertyRequest
					{
						Instance = stationGlobals,
						Index = index,
						LookupString = string.Empty,
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;

				// Get name, value, and type information
				string name = _propertyObjectClient.Get_Name(new PropertyObject_Get_NameRequest { Instance = global }).ReturnValue;
				string value = _propertyObjectClient.GetValString(
					new PropertyObject_GetValStringRequest
					{
						Instance = global,
						LookupString = string.Empty,
						Options = PropertyOptions.PropOptionCoerce
					}).ReturnValue;
				string displayType = _propertyObjectClient.GetTypeDisplayString(
					new PropertyObject_GetTypeDisplayStringRequest
					{
						Instance = global,
						LookupString = string.Empty,
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;

				globalVariables.Add(new ListViewItem(new string[] { name, value, displayType }));
			}

			if (numberOfGlobals > 0)
			{
				_stationGlobalsListView.Items.AddRange(globalVariables.ToArray());
				_stationGlobalsListView.SelectedIndices.Add(0);
			}

			_stationGlobalsListView.EndUpdate();

			_stationGlobalsListView.Enabled = true;
			_addGlobalButton.Enabled = true;
        }

		private void OnConnectButtonClick(object sender, EventArgs e)
		{			
			TryAction(() =>
			{
				Setup(false);
			}, "Connect to server.");
		}

		private async Task TryActionAsync(Func<Task> action, string stringToLog)
		{
			bool logAction = !string.IsNullOrEmpty(stringToLog);
			if (logAction)
			{
				LogBold("Started: ");
				LogLine(stringToLog);
			}

			try
			{
				await action();
			}
			catch (Exception exception)
			{
				ReportException(exception);
			}
            finally
            {
				if (logAction)
				{
					LogBold("Completed: ");
					LogLine(stringToLog);
				}
			}
		}

		private void TryAction(Action action, string stringToLog)
		{
			_ = TryActionAsync(async Task () =>
			{
				action();
				await Task.CompletedTask;
			}, stringToLog);
		}

		private void ReportException(Exception exception)
		{
			if (exception is RpcException rpcException)
			{
				// The grpc exceptions for some cases (like a bad server address) contain the stack trace in the Message, so using the Detail instead
				LogLine("gRPC EXCEPTION: " + rpcException.Status.Detail);

				TSError errorCode = GetTSErrorCode(rpcException, out string description);
				if (errorCode != TSError.TsErrNoError)
				{
					LogFaded("\tError ");
					Log(((int)errorCode).ToString());
					LogFaded("  Message ");
					LogLine(description);
				}

				if (rpcException.StatusCode == StatusCode.Unavailable)
				{
					SetConnectionStatus(false);
				}
			}
			else
			{
				LogLine("EXCEPTION: " + exception.Message);
			}
		}

		private void EnableApplicationDirectorySearchPath()
		{
			var searchDirectories = _engineClient.Get_SearchDirectories(new Engine_Get_SearchDirectoriesRequest { Instance = _engine }).ReturnValue;

			var count = _searchDirectoriesClient.Get_Count(new SearchDirectories_Get_CountRequest { Instance = searchDirectories }).ReturnValue;

			for (int index = 0; index < count; index++)
			{
				var searchDirectory = _searchDirectoriesClient.Get_Item(new SearchDirectories_Get_ItemRequest { Instance = searchDirectories, Index = index }).ReturnValue;

				var searchDirectoryType = _searchDirectoryClient.Get_Type(new SearchDirectory_Get_TypeRequest { Instance = searchDirectory }).ReturnValue;

				if (searchDirectoryType == SearchDirectoryTypes.SearchDirectoryTypeApplicationDir)
				{
					_searchDirectoryClient.Set_Disabled(new SearchDirectory_Set_DisabledRequest { Instance = searchDirectory, Val = false });
				}
			}
		}

		private void OnProcessModelComboBoxSelectedIndexChanged(object sender, System.EventArgs e)
		{
			EnableOrDisableProcessModelOptionAndNumberOfTestSockets();
		}

		private void EnableOrDisableProcessModelOptionAndNumberOfTestSockets()
		{
			bool usingModel = true;
			var selectedModel = (string)_processModelComboBox.SelectedItem;
			bool usingStationModel = string.Compare(selectedModel, "Use Station Model", StringComparison.OrdinalIgnoreCase) == 0;

			_nonSequentialProcessModelName = null;
			if (usingStationModel)
			{
				var stationModelFile = (string)_stationModelComboBox.SelectedItem;
				if (string.Compare(stationModelFile, "BatchModel.seq", StringComparison.OrdinalIgnoreCase) == 0)
				{
					_nonSequentialProcessModelName = "Batch";
				}
				else if (string.Compare(stationModelFile, "ParallelModel.seq", StringComparison.OrdinalIgnoreCase) == 0)
				{
					_nonSequentialProcessModelName = "Parallel";
				}
			}
			else if (string.Compare(selectedModel, "Batch", StringComparison.OrdinalIgnoreCase) == 0
				|| string.Compare(selectedModel, "Parallel", StringComparison.OrdinalIgnoreCase) == 0)
			{
				_nonSequentialProcessModelName = selectedModel;

			}
			else
            {
				usingModel = string.Compare(selectedModel, "None", StringComparison.OrdinalIgnoreCase) != 0;
            }

			_activeProcessModelLabel.Enabled = usingStationModel;
			_stationModelComboBox.Enabled = usingStationModel;
			_entryPointLabel.Enabled = usingModel;
			_entryPointComboBox.Enabled = usingModel;

			bool enableNumberOfTestSocketsOption = _nonSequentialProcessModelName != null && _numTestSocketsNumericUpDown.Value != 0;
			_numberOfTestSocketsLabel.Enabled = enableNumberOfTestSocketsOption;
			_numTestSocketsNumericUpDown.Enabled = enableNumberOfTestSocketsOption;
		}

		private void OnSequenceFileNameComboBoxSelectedIndexChanged(object sender, EventArgs e)
		{
			SetExecutionStatus(NotExecutedSequenceFile);
		}

		private async void OnRunSequenceFileButtonClick(object sender, EventArgs e)
		{
			Debug.Assert(_activeExecution == null);

			try
			{
				// Since only one execution can be run at a time, disable the run button when starting a new execution.
				_runSequenceFileButton.Enabled = false;
				_runSequenceFileButton.Text = RunningRemoteSequenceFile;

				await TryActionAsync(async () =>
				{
					Setup(true);

					// Don't run sequence if connection fails
					if (!_isConnected)
                    {
						return;
                    }

					EnableApplicationDirectorySearchPath(); // the test.seq file is next to the example server executable

					// get the sequence file to run
					var sequenceFile = _engineClient.GetSequenceFileEx(new Engine_GetSequenceFileExRequest
					{
						Instance = _engine,
						SeqFilePath = _sequenceFileNameComboBox.Text,
						GetSeqFileFlags = GetSeqFileOptions.GetSeqFileFindFile
					}).ReturnValue;

					SequenceFileInstance processModel = null;

					try
					{
						processModel = GetSelectedProcessModel(out string modelName);
						string sequenceName = GetSelectedSequenceName(processModel);

                        // The process models store the step results in a local variable called "ModelData". We need to get a 
                        // reference to ModelData to keep the local alive so we can get the step results after the execution ends.
                        PropertyObjectInstance modelData = await RunSequenceFileAsync(sequenceFile, sequenceName, processModel, modelName);

						var resultStatus = _executionClient.Get_ResultStatus(new Execution_Get_ResultStatusRequest { Instance = _activeExecution }).ReturnValue;
						SetExecutionStatus(resultStatus);

						LogExecutionResults(resultStatus, processModel, modelName, modelData);
					}
					finally
					{
						// release file references we no longer need (files require explicit release)
						if (processModel != null)
						{
							_engineClient.ReleaseSequenceFileEx(new Engine_ReleaseSequenceFileExRequest
							{
								Instance = _engine,
								SequenceFileToRelease = processModel,
								Options = ReleaseSeqFileOptions.ReleaseSeqFileNoOptions
							});
						}

						_engineClient.ReleaseSequenceFileEx(new Engine_ReleaseSequenceFileExRequest
						{
							Instance = _engine,
							SequenceFileToRelease = sequenceFile,
							Options = ReleaseSeqFileOptions.ReleaseSeqFileNoOptions
						});
					}
				}, "Run remote execution.");
			}
			finally
			{
				lock (_dataLock)
				{
					// If _activeExecution is null, it means execution failed to run or server crashed in the middle
					// of execution. So, reset state to not executed since any errors will appear in the log control.
					if (_activeExecution == null)
					{
						SetExecutionStatus(NotExecutedSequenceFile);
					}

					_activeExecution = null;
				}

				_runSequenceFileButton.Enabled = true;
				_runSequenceFileButton.Text = RunRemoteSequenceFile;
			}
		}

		private SequenceFileInstance GetSelectedProcessModel(out string modelName)
		{
			SequenceFileInstance processModel = null;
			modelName = (string)_processModelComboBox.SelectedItem;

			if (modelName != "None")
			{
				if (modelName == "Use Station Model")
				{
					StationOptionsInstance stationOptions = GetStationOptions();
					modelName = _stationOptionsClient.Get_StationModelSequenceFilePath(
						new StationOptions_Get_StationModelSequenceFilePathRequest
						{
							Instance = stationOptions
						}).ReturnValue;
				}
				else
				{
					modelName += "Model.seq";
				}

				processModel = _engineClient.GetSequenceFileEx(new Engine_GetSequenceFileExRequest
				{
					Instance = _engine,
					SeqFilePath = modelName,
					GetSeqFileFlags = GetSeqFileOptions.GetSeqFileFindFile
				}).ReturnValue;
			}

			return processModel;
		}

		private string GetSelectedSequenceName(SequenceFileInstance processModel)
		{
			return processModel == null ? "MainSequence" : _entryPointComboBox.Text;
		}

		private async Task<PropertyObjectInstance> RunSequenceFileAsync(
			SequenceFileInstance sequenceFile,
			string sequenceName,
			SequenceFileInstance processModel,
			string modelName)
		{
			var newExecutionRequest = new Engine_NewExecutionRequest
			{
				Instance = _engine,
				SequenceFileParam = sequenceFile,
				SequenceNameParam = sequenceName,
				BreakAtFirstStep = false,
				ExecutionTypeMaskParam = ExecutionTypeMask.ExecTypeMaskCloseWindowWhenDone
			};

			if (processModel != null)
			{
				newExecutionRequest.ProcessModelParam = processModel;
			}

			try
			{
				lock (_dataLock)
				{
					_activeExecution = _engineClient.NewExecution(newExecutionRequest).ReturnValue;
				}
			}
			catch (RpcException rpcException)
            {
				TSError errorCode = GetTSErrorCode(rpcException, out string description);
				if (errorCode != TSError.TsErrNoError)
				{
					// Some error messages include additional information that we don't want to display.  The additional
					// information appears between {}. So, remove all instances of " {<any number of characters>}".
					string errorMessage = Regex.Replace(rpcException.Status.Detail, @"\s\{[^}]+\}", string.Empty);
					var status = new Status(rpcException.Status.StatusCode, errorMessage, rpcException.Status.DebugException);

					throw new RpcException(status, rpcException.Trailers, errorMessage);
				}

				throw;
            }

			PropertyObjectInstance modelData = GetProcessModelModelData(_activeExecution, modelName);

			SetExecutionStatus(ExecutionStateRunning);

			try
			{
				await _executionClient.WaitForEndExAsync(new Execution_WaitForEndExRequest
				{
					Instance = _activeExecution,
					MillisecondTimeOut = -1,
					ProcessWindowsMsgs = false
				});
			}
			catch
			{
				lock (_dataLock)
				{
					_activeExecution = null;
				}
				throw;
			}

			return modelData;
		}

		private PropertyObjectInstance GetProcessModelModelData(ExecutionInstance execution, string modelName)
        {
			if (string.IsNullOrEmpty(modelName) || string.Compare(modelName, "None", StringComparison.OrdinalIgnoreCase) == 0)
            {
				return null;
            }

			// ModelData is a local variable in the root context.
			ThreadInstance thread = _executionClient.GetThread(new Execution_GetThreadRequest { Instance = execution, Index = 0 }).ReturnValue;
			SequenceContextInstance currentContext = _threadClient.GetSequenceContext(new Thread_GetSequenceContextRequest { Instance = thread, CallStackIndex = 0 }).ReturnValue;
			SequenceContextInstance rootContext = _sequenceContextClient.Get_Root(new SequenceContext_Get_RootRequest { Instance = currentContext }).ReturnValue;
			PropertyObjectInstance locals = _sequenceContextClient.Get_Locals(new SequenceContext_Get_LocalsRequest { Instance = rootContext }).ReturnValue;
				
			PropertyObjectInstance modelData = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
			{
				Instance = locals,
				LookupString = "ModelData",
				Options = PropertyOptions.PropOptionNoOptions
			}).ReturnValue;

			return modelData;
        }

		private void LogExecutionResults(string resultStatus, SequenceFileInstance processModel, string modelName, PropertyObjectInstance modelData)
		{
			int numberOfResults = 0;

			if (processModel != null && !IsSequentialModelName(modelName))
			{
				numberOfResults = DisplayResultsForBatchOrParallelModelRuns(modelData, modelName);
			}
			else
			{
				bool hasResults = true;
				PropertyObjectInstance executionResults = _executionClient.Get_ResultObject(new Execution_Get_ResultObjectRequest { Instance = _activeExecution }).ReturnValue;

				if (processModel != null)
				{
					if (IsSequentialModelName(modelName))
					{
						var resultList = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
						{
							Instance = executionResults,
							LookupString = "ResultList",
							Options = PropertyOptions.PropOptionNoOptions
						}).ReturnValue;

						var elementCount = _propertyObjectClient.GetNumElements(new PropertyObject_GetNumElementsRequest
						{
							Instance = resultList,
						}).ReturnValue;

						hasResults = elementCount == 1;
						if (hasResults)
						{
							executionResults = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
							{
								Instance = executionResults,
								LookupString = "ResultList[0].TS.SequenceCall",
								Options = PropertyOptions.PropOptionNoOptions
							}).ReturnValue;

							string entryPointName = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
							{
								Instance = modelData,
								LookupString = "EntryPoint",
								Options = PropertyOptions.PropOptionNoOptions
							}).ReturnValue;

							string sequenceName = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
							{
								Instance = executionResults,
								LookupString = "Sequence",
								Options = PropertyOptions.PropOptionNoOptions
							}).ReturnValue;

							LogLine(Invariant($"Results for '{_sequenceFileNameComboBox.Text}' using '{modelName}: {entryPointName}'"));
						}
					}
				}
				else
				{
					string sequenceName = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
					{
						Instance = executionResults,
						LookupString = "Sequence",
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;

					LogLine(Invariant($"Results for '{_sequenceFileNameComboBox.Text}: {sequenceName}'"));
				}

				if (hasResults)
				{
					numberOfResults = DisplayResults(executionResults, indentationLevel: 0);
				}
			}

			Log("Execution Complete. Status: ");
			Log(resultStatus, GetResultBackgroundColor(resultStatus));
			LogLine(", Number of Results = " + numberOfResults.ToString(CultureInfo.InvariantCulture));
		}

		private int DisplayResultsForBatchOrParallelModelRuns(PropertyObjectInstance modelData, string modelName)
        {
			int numberOfResults = 0;

			Debug.Assert(modelData != null);

			string entryPointName = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
			{
				Instance = modelData,
				LookupString = "EntryPoint",
				Options = PropertyOptions.PropOptionNoOptions
			}).ReturnValue;

			LogLine(Invariant($"Results for '{_sequenceFileNameComboBox.Text}' using '{modelName}: {entryPointName}'"));

            // The results are under ModelData.TestSockets.
            PropertyObjectInstance testSockets = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
			{
				Instance = modelData,
				LookupString = "TestSockets",
				Options = PropertyOptions.PropOptionNoOptions
			}).ReturnValue;

			int numberOfTestSockets = _propertyObjectClient.GetNumElements(new PropertyObject_GetNumElementsRequest
			{
				Instance = testSockets,
			}).ReturnValue;

			for (int socketIndex = 0; socketIndex < numberOfTestSockets; socketIndex++)
			{
				string socketResultLookupString = Invariant($"[{socketIndex}].MainSequenceResults.TS.SequenceCall");
				PropertyObjectInstance socketResults = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
				{
					Instance = testSockets,
					LookupString = socketResultLookupString,
					Options = PropertyOptions.PropOptionNoOptions
				}).ReturnValue;

				var sequenceCallStatus = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
				{
					Instance = socketResults,
					LookupString = "Status",
					Options = PropertyOptions.PropOptionNoOptions
				}).ReturnValue;

				LogBold(Invariant($"Socket {socketIndex}: "));
				LogLine(sequenceCallStatus, GetResultBackgroundColor(sequenceCallStatus));

				numberOfResults += DisplayResults(socketResults, indentationLevel: 0);
			}

			return numberOfResults;
		}

		private static bool IsSequentialModelName(string processModelName)
        {
			return string.Compare(processModelName, SequentialModelFilename, StringComparison.OrdinalIgnoreCase) == 0;
        }

		private void SetExecutionStatus(string executionStatus)
        {
			if (executionStatus == NotExecutedSequenceFile)
			{
				_executionStatePictureBox.Image = null;
				_executionStateDescriptionLabel.Text = string.Empty;
			}
			else
			{
				_executionStatePictureBox.Image = new Bitmap(_imageList[executionStatus]);
				_executionStateDescriptionLabel.Text = executionStatus;
			}
		}

		private int DisplayResults(PropertyObjectInstance resultObject, int indentationLevel)
		{
			var resultList = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
			{
				Instance = resultObject,
				LookupString = "ResultList",
				Options = PropertyOptions.PropOptionNoOptions
			}).ReturnValue;

			var numberOfResults = _propertyObjectClient.GetNumElements(new PropertyObject_GetNumElementsRequest
			{
				Instance = resultList
			}).ReturnValue;

			int totalNumberOfResults = numberOfResults;
			if (numberOfResults == 0)
			{
				LogLine("No results.", indentationLevel);
			}
			else
			{
				for (int index = 0; index < numberOfResults; index++)
				{
					var nthResult = _propertyObjectClient.GetPropertyObjectByOffset(new PropertyObject_GetPropertyObjectByOffsetRequest
					{
						Instance = resultList,
						ArrayOffset = index,
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;

					var stepName = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
					{
						Instance = nthResult,
						LookupString = "TS.StepName",
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;

					var stepStatus = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
					{
						Instance = nthResult,
						LookupString = "Status",
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;
					string formattedStepStatus = string.Format("{0, -7}", stepStatus);

					Log(formattedStepStatus, GetResultBackgroundColor(stepStatus), indentationLevel: 0, overlayColor: false);
					LogFaded("  Step ", indentationLevel);
					LogLine(stepName);

					if (stepStatus == ExecutionStateError)
					{
						double code = _propertyObjectClient.GetValNumber(new PropertyObject_GetValNumberRequest
						{
							Instance = nthResult,
							LookupString = "Error.Code",
							Options = PropertyOptions.PropOptionNoOptions
						}).ReturnValue;

						var message = _propertyObjectClient.GetValString(new PropertyObject_GetValStringRequest
						{
							Instance = nthResult,
							LookupString = "Error.Msg",
							Options = PropertyOptions.PropOptionNoOptions
						}).ReturnValue;

						// Indent the Code label below the Step label.
						// 7 for status column + 2 for empty space before "Step" label + IndentOffsetForOneLevel
						int labelIndentOffset = IndentOffsetForOneLevel + 9;
						string indentedCodeLabel = string.Format("{0," + labelIndentOffset + "}Code ", "");

						LogFaded(indentedCodeLabel, indentationLevel + 1);
						Log(code.ToString());
						LogFaded("  Message ");
						LogLine(message);
					}

					// If the property TS.SequenceCall exists in the current result, it means it is a 
					// sequence call. So, recurse to get those results.
					bool isSequenceCall = _propertyObjectClient.Exists(new PropertyObject_ExistsRequest
					{
						Instance = nthResult,
						LookupString = "TS.SequenceCall",
						Options = PropertyOptions.PropOptionNoOptions
					}).ReturnValue;
					if (isSequenceCall)
					{
						var sequenceCall = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
						{
							Instance = nthResult,
							LookupString = "TS.SequenceCall",
							Options = PropertyOptions.PropOptionNoOptions
						}).ReturnValue;

						totalNumberOfResults += DisplayResults(sequenceCall, indentationLevel + 1);
					}
				}
			}

			return totalNumberOfResults;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			// Add some space between the lines in the log and trace messages text.
			// The space will make the lines more readable.
            SetLineSpacing(_logTextBox);
			SetLineSpacing(_executionTraceMessagesTextBox);

            _processModelComboBox.SelectedIndex = 0;
			_entryPointComboBox.SelectedIndex = 0;
			_sequenceFileNameComboBox.SelectedIndex = 0;
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Cleanup();
		}

		private void OnClearOutputButtonClick(object sender, EventArgs e)
		{
			_logTextBox.Text = string.Empty;
		}

		private async void OnBreakButtonClick(object sender, EventArgs e)
		{
			await TryActionAsync(async () =>
			{
				using (new AutoWaitCursor(this))
				{
					await ApplyActionOnOpenExecutionsAsync(ExecutionAction.Break);
					await UpdateExecutionOptionsStateAsync();
				}
			}, "Breaking execution");
		}

		private async void OnResumeButtonClick(object sender, EventArgs e)
		{
			await TryActionAsync(async () =>
			{
				using (new AutoWaitCursor(this))
				{
					await ApplyActionOnOpenExecutionsAsync(ExecutionAction.Resume);
					await UpdateExecutionOptionsStateAsync();
				}
			}, "Resuming execution");
		}

		private async void OnTerminateButtonClick(object sender, EventArgs e)
		{
			await TryActionAsync(async () =>
			{
				using (new AutoWaitCursor(this))
				{
					await ApplyActionOnOpenExecutionsAsync(ExecutionAction.Terminate);
					SetExecutionStatus(ExecutionStateTerminating);
					await UpdateExecutionOptionsStateAsync();
				}
			}, "Terminating execution.");
		}
		
		private async Task ApplyActionOnOpenExecutionsAsync(ExecutionAction action)
		{
			List<ExecutionInfo> executionInfos = GetOpenExecutionsAssociatedWithActiveExecution(getThreadInfo: false);

			switch (action)
			{
				case ExecutionAction.Break:
					{
						var tasksToWaitOn = new List<AsyncUnaryCall<Execution_BreakResponse>>(executionInfos.Count);
						foreach (ExecutionInfo executionInfo in executionInfos)
						{
							if (executionInfo.RunState == ExecutionRunStates.ExecRunStateRunning)
							{
								tasksToWaitOn.Add(_executionClient.BreakAsync(new Execution_BreakRequest { Instance = executionInfo.Instance }));
							}
						}
						await Task.WhenAll(tasksToWaitOn.Select(asynCall => asynCall.ResponseAsync));
					}
					break;
				case ExecutionAction.Resume:
					{
						var tasksToWaitOn = new List<AsyncUnaryCall<Execution_ResumeResponse>>(executionInfos.Count);
						foreach (ExecutionInfo executionInfo in executionInfos)
						{
							if (executionInfo.RunState == ExecutionRunStates.ExecRunStatePaused)
							{
								tasksToWaitOn.Add(_executionClient.ResumeAsync(new Execution_ResumeRequest { Instance = executionInfo.Instance }));
							}
						}
						await Task.WhenAll(tasksToWaitOn.Select(asynCall => asynCall.ResponseAsync));
					}
					break;
				case ExecutionAction.Terminate:
					{
						var tasksToWaitOn = new List<AsyncUnaryCall<Execution_TerminateResponse>>(executionInfos.Count);
						foreach (ExecutionInfo executionInfo in executionInfos)
						{
							if (executionInfo.RunState != ExecutionRunStates.ExecRunStateStopped)
							{
								tasksToWaitOn.Add(_executionClient.TerminateAsync(new Execution_TerminateRequest { Instance = executionInfo.Instance }));
							}
						}
						await Task.WhenAll(tasksToWaitOn.Select(asynCall => asynCall.ResponseAsync));
					}
					break;
			}
		}

		private async Task UpdateExecutionOptionsStateAsync()
		{
			bool breakEnabled = false;
			bool resumeEnabled = false;
			bool terminateEnabled = false;
			string suspendedAtStepName = string.Empty;
			ExecutionInstance activeExecution;

			// Since an await cannot be inside a lock, we need to copy the executions to make sure
			// the list is not updated while refreshing the button states.
			lock (_dataLock)
			{
				activeExecution = _activeExecution;
			}

			if (activeExecution != null)
			{
				var states = await _executionClient.GetStatesAsync(new Execution_GetStatesRequest { Instance = activeExecution });

				if (_executionStateDescriptionLabel.Text != ExecutionStateTerminating)
				{
					if (states.RunState == ExecutionRunStates.ExecRunStatePaused)
					{
						SetExecutionStatus(ExecutionStatePaused);
					}
					else if (states.RunState == ExecutionRunStates.ExecRunStateRunning)
					{
						SetExecutionStatus(ExecutionStateRunning);
					}
				}

				breakEnabled = states.RunState == ExecutionRunStates.ExecRunStateRunning;
				resumeEnabled = states.RunState == ExecutionRunStates.ExecRunStatePaused;
				terminateEnabled = states.RunState != ExecutionRunStates.ExecRunStateStopped;

				if (resumeEnabled)
				{
					// When using Batch or Parallel models, we need to show the model name for the suspended step
					// since the executions can be stopped at different steps.
					if (_nonSequentialProcessModelName != null)
					{
						suspendedAtStepName ="<" + _nonSequentialProcessModelName + ">";
					}
					else
					{
					var mainExecutionThread = _executionClient.Get_ForegroundThread(new Execution_Get_ForegroundThreadRequest { Instance = activeExecution }).ReturnValue;
					var sequenceContext = _threadClient.GetSequenceContext(new Thread_GetSequenceContextRequest { Instance = mainExecutionThread, CallStackIndex = 0 }).ReturnValue;
					var nextStepIndex = _sequenceContextClient.Get_NextStepIndex(new SequenceContext_Get_NextStepIndexRequest { Instance = sequenceContext }).ReturnValue;
					if (nextStepIndex >= 0)
					{
						var step = _sequenceContextClient.Get_NextStep(new SequenceContext_Get_NextStepRequest { Instance = sequenceContext }).ReturnValue;
						suspendedAtStepName = _stepClient.Get_Name(new Step_Get_NameRequest { Instance = step }).ReturnValue;
					}
				}
			}
			}

			_suspendedAtStepTextBox.Text = suspendedAtStepName;
			_suspendedAtStepTextBox.Enabled = !string.IsNullOrEmpty(suspendedAtStepName);

			_breakButton.Enabled = breakEnabled;
			_resumeButton.Enabled = resumeEnabled;
			_terminateButton.Enabled = terminateEnabled;
		}

		private void OnUpdateExecutionOptionsStateTimerTick(object sender, EventArgs e)
		{
            _ = TryActionAsync(async () => await UpdateExecutionOptionsStateAsync(), null);
		}

		private void OnServerHeartbeatTimerTick(object sender, EventArgs e)
        {
			// To check the connection to the server, we need to do a simple-non-updating Engine call. If the call
			// fails with the status code set to Unavailable, we know the connection to the server has been lost.
			if (_isConnected)
			{
				try
				{
					_engineClient.Get_MajorVersion(new Engine_Get_MajorVersionRequest { Instance = _engine });
				}
				catch (Exception exception)
				{
					if (exception is RpcException rpcException && rpcException.StatusCode == StatusCode.Unavailable)
					{
						LogLine(Environment.NewLine + "ERROR: Lost connection to the server.");
						SetConnectionStatus(false);
					}
					else
					{
						ReportException(exception);
					}
				}
			}
		}

		// This method gets called when changing the selection in the list view
		private void OnStationGlobalsListViewSelectedIndexChanged(object sender, EventArgs e)
		{
			bool canEditValue = false;

			if (_stationGlobalsListView.SelectedIndices.Count > 0)
            {
				_deleteGlobalButton.Enabled = true;
				_booleanValueComboBox.Visible = false;
				_valueTextBox.Visible = true;

				_valueForSelectedItemIndex = _stationGlobalsListView.SelectedIndices[0];
				ListViewItem selectedItem = _stationGlobalsListView.Items[_valueForSelectedItemIndex];
				string typeString = selectedItem.SubItems[2].Text;

				// Populate the value text box if a string or number is selected.
				if (typeString == "Number" || typeString == "String")
				{
					_valueTextBox.Text = selectedItem.SubItems[1].Text;
					_valueTextBox.Enabled = true;
					canEditValue = true;
				}
				// Else, show the combobox and set it to the boolean property value
				else if (typeString == "Boolean")
                {
					_booleanValueComboBox.Visible = true;
					_valueTextBox.Visible = false;
					_booleanValueComboBox.SelectedIndex = selectedItem.SubItems[1].Text == "True" ? 1 : 0;
					canEditValue = true;
				}
			}
			
			if (!canEditValue)
            {
				_deleteGlobalButton.Enabled = false;
				_valueTextBox.Text = string.Empty;
				_valueTextBox.Enabled = false;
				_booleanValueComboBox.Visible = false;
				_valueForSelectedItemIndex = -1;
			}
		}

		// Will commit the value of the text box when focus changes
		private void OnValueTextBoxLeave(object sender, EventArgs e)
		{
			if (_valueTextBox.Enabled)
			{
				SetValueOnGlobalVariable(_valueTextBox.Text);
			}
		}

		private void OnValueTextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				SetValueOnGlobalVariable(_valueTextBox.Text);
			}
		}

		private void OnAddStationGlobalClick(object sender, EventArgs e)
        {
			PropertyObjectInstance stationGlobals = _engineClient.Get_Globals(new Engine_Get_GlobalsRequest { Instance = _engine }).ReturnValue;
			int numberOfGlobals = _propertyObjectClient.GetNumSubProperties(
				new PropertyObject_GetNumSubPropertiesRequest
				{
					Instance = stationGlobals,
					LookupString = string.Empty
				}).ReturnValue;

			string newGlobalName = "NumericGlobal_" + numberOfGlobals;

			_propertyObjectClient.NewSubProperty(
				new PropertyObject_NewSubPropertyRequest
				{
					Instance = stationGlobals,
					LookupString = newGlobalName,
					ValueType = PropertyValueTypes.PropValTypeNumber,
					AsArray = false,
					TypeNameParam = string.Empty,
					Options = PropertyOptions.PropOptionNoOptions
				});

			RefreshStationGlobals();
			_commitGlobalsToDiskButton.Enabled = true;
		}

		private void OnDeleteStationGlobalClick(object sender, EventArgs e)
        {
			ListViewItem selectedItem = _stationGlobalsListView.Items[_stationGlobalsListView.SelectedIndices[0]];
			PropertyObjectInstance stationGlobals = _engineClient.Get_Globals(new Engine_Get_GlobalsRequest { Instance = _engine }).ReturnValue;
			_propertyObjectClient.DeleteSubProperty(new PropertyObject_DeleteSubPropertyRequest
			{
				Instance = stationGlobals,
				LookupString = selectedItem.Text,
				Options = PropertyOptions.PropOptionNoOptions
			});

			RefreshStationGlobals();
			_commitGlobalsToDiskButton.Enabled = true;
		}

        private void OnBooleanValueComboBoxSelectionChangedCommitted(object sender, EventArgs e)
        {
			SetValueOnGlobalVariable((string)_booleanValueComboBox.SelectedItem);
		}

		private void OnCommitGlobalsToDiskClick(object sender, EventArgs e)
		{
			_engineClient.CommitGlobalsToDisk(new Engine_CommitGlobalsToDiskRequest { Instance = _engine, PromptOnSaveConflicts = true });
			_commitGlobalsToDiskButton.Enabled = false;
		}

		private void OnStationModelComboBoxSelectedIndexChanged(object sender, EventArgs e)
		{
			StationOptionsInstance stationOptions = GetStationOptions();
			_stationOptionsClient.Set_StationModelSequenceFilePath(
				new StationOptions_Set_StationModelSequenceFilePathRequest
				{
					Instance = stationOptions,
					ModelPath = _stationModelComboBox.Text
				});

			EnableOrDisableProcessModelOptionAndNumberOfTestSockets();
		}

		private void OnNumTestSocketNumericUpDownValueChanged(object sender, EventArgs e)
		{
			SetMultipleUUTSettingsNumberOfTestSocketsOption((int)_numTestSocketsNumericUpDown.Value);
		}

		private void SetValueOnGlobalVariable(string newValue)
        {
			ListViewItem selectedItem = _stationGlobalsListView.Items[_valueForSelectedItemIndex];
			PropertyObjectInstance stationGlobals = _engineClient.Get_Globals(new Engine_Get_GlobalsRequest { Instance = _engine }).ReturnValue;
			PropertyObjectInstance globalObject = _propertyObjectClient.GetPropertyObject(
				new PropertyObject_GetPropertyObjectRequest
				{
					Instance = stationGlobals,
					LookupString = selectedItem.Text,
					Options = PropertyOptions.PropOptionNoOptions
				}).ReturnValue;

			_propertyObjectClient.SetValString(
				new PropertyObject_SetValStringRequest
				{
					Instance = globalObject,
					LookupString = string.Empty,
					NewValue = newValue,
					Options = PropertyOptions.PropOptionCoerce
				});

			RefreshStationGlobals();
			_commitGlobalsToDiskButton.Enabled = true;
		}

        private void OnClearExecutionTraceMessagesButtonClick(object sender, EventArgs e)
        {
			ClearTraceMessages();
        }

		private void IdentifyThread(ThreadInstance thread, out ThreadInfo threadInfo)
		{
			SequenceContextInstance rootContext;

			threadInfo = new ThreadInfo();

			do
			{
				var currentContext = _threadClient.GetSequenceContext(new Thread_GetSequenceContextRequest { Instance = thread, CallStackIndex = 0 }).ReturnValue;
				rootContext = _sequenceContextClient.Get_Root(new SequenceContext_Get_RootRequest { Instance = currentContext }).ReturnValue;
			} while (rootContext.Id == "0");  // if we ask at the wrong time (frame just ended), we might get zero (null). Not sure of a way to get the root 100% without retries if execution is not paused.

			var locals = _sequenceContextClient.Get_Locals(new SequenceContext_Get_LocalsRequest { Instance = rootContext }).ReturnValue;
			var modelThreadTypeLocalExists = _propertyObjectClient.Exists(new PropertyObject_ExistsRequest { Instance = locals, LookupString = "ModelThreadType", Options = PropertyOptions.PropOptionNoOptions }).ReturnValue;

			if (modelThreadTypeLocalExists)
			{
				threadInfo.IsController = _propertyObjectClient.GetValBoolean(new PropertyObject_GetValBooleanRequest { Instance = locals, LookupString = "ModelThreadType.IsController", Options = PropertyOptions.PropOptionNoOptions }).ReturnValue;
				threadInfo.IsTestSocket = _propertyObjectClient.GetValBoolean(new PropertyObject_GetValBooleanRequest { Instance = locals, LookupString = "ModelThreadType.IsTestSocket", Options = PropertyOptions.PropOptionNoOptions }).ReturnValue;

				var propertyObjectInstance = new PropertyObjectInstance() { Id = rootContext.Id };
				threadInfo.SocketIndex = (int)_propertyObjectClient.GetValNumber(new PropertyObject_GetValNumberRequest { Instance = propertyObjectInstance, LookupString = "Runstate.TestSockets.MyIndex", Options = PropertyOptions.PropOptionNoOptions }).ReturnValue;
			}

			if (threadInfo.IsTestSocket && !threadInfo.IsController)
			{
				var parameters = _sequenceContextClient.Get_Parameters(new SequenceContext_Get_ParametersRequest { Instance = rootContext }).ReturnValue;
				var parentControllerThread = _propertyObjectClient.GetValInterface(new PropertyObject_GetValInterfaceRequest { Instance = parameters, LookupString = "ParentThread", Options = PropertyOptions.PropOptionNoOptions }).ReturnValue;
				var threadInstance = new ThreadInstance { Id = parentControllerThread.Id };
				var execution = _threadClient.Get_Execution(new Thread_Get_ExecutionRequest { Instance = threadInstance }).ReturnValue;
				threadInfo.ParentControllerThreadId = _threadClient.Get_Id(new Thread_Get_IdRequest { Instance = threadInstance }).ReturnValue;
				threadInfo.ParentControllerExecutionId = _executionClient.Get_Id(new Execution_Get_IdRequest { Instance = execution }).ReturnValue;
			}				
		}

		private void ListExecutionsAndThreads()
		{
			if (_engineClient == null)
		{
				LogLine("NOT CONNECTED");
			}

			if (_activeExecution == null)
			{
				LogLine("NO EXECUTIONS");
			}

			List<ExecutionInfo> executionInfos = GetOpenExecutionsAssociatedWithActiveExecution(getThreadInfo: true);

			for (int executionIndex = 0; executionIndex < executionInfos.Count; executionIndex++)
			{
				ExecutionInfo executionInfo = executionInfos[executionIndex];

				LogLine(Invariant($"Execution #{executionIndex} - {executionInfo.Name}"));

				for (int threadIndex = 0; threadIndex < executionInfo.Threads.Count; threadIndex++)
				{
					ThreadInfo threadInfo = executionInfo.Threads[threadIndex];

					Log(Invariant($"    Thread #{threadIndex} - {threadInfo.Name} [Controller = {threadInfo.IsController}, Socket = {threadInfo.IsTestSocket}, Socket Index = {threadInfo.SocketIndex}"));
					if (threadInfo.ParentControllerThreadId != 0)
					{
						Log(Invariant($", Parent (Controller) Thread Id = {threadInfo.ParentControllerThreadId}"));
					}
					LogLine(string.Empty);
				}
			}
		}

		private List<ExecutionInfo> GetOpenExecutionsAssociatedWithActiveExecution(bool getThreadInfo)
        {
			var executionInstances = new List<ExecutionInfo>();

			if (_activeExecution == null)
			{
				return executionInstances;
			}

			ExecutionsInstance executions = GetAllOpenExecutions();

			var numberOfExecutions = _executionsClient.Get_Count(new Executions_Get_CountRequest { Instance = executions }).ReturnValue;
			int activeExecutionId = _executionClient.Get_Id(new Execution_Get_IdRequest { Instance = _activeExecution }).ReturnValue;

			// For each execution, check if the id or the parent execution id matches the active execution.
			for (int executionIndex = 0; executionIndex < numberOfExecutions; executionIndex++)
			{
				var execution = _executionsClient.Get_Item(new Executions_Get_ItemRequest { Instance = executions, ItemIdx = executionIndex }).ReturnValue;
				var numberOfThreads = _executionClient.Get_NumThreads(new Execution_Get_NumThreadsRequest { Instance = execution }).ReturnValue;

				// If the number of threads is zero, it means the execution has completed so skip it.
				if (numberOfThreads == 0)
				{
					continue;
				}

				var executionInfo = new ExecutionInfo(execution)
				{
					ExecutionId = _executionClient.Get_Id(new Execution_Get_IdRequest { Instance = execution }).ReturnValue
				};

				bool executionStartedByClient = executionInfo.ExecutionId == activeExecutionId;
				if (!executionStartedByClient)
				{
					// Get the thread info which includes the execution parent id
					var thread = _executionClient.GetThread(new Execution_GetThreadRequest { Instance = execution, Index = 0 }).ReturnValue;
					IdentifyThread(thread, out ThreadInfo threadInfo);

					executionStartedByClient = threadInfo.ParentControllerExecutionId == activeExecutionId;
					if (executionStartedByClient && getThreadInfo)
				{
						threadInfo.Name = _threadClient.Get_DisplayName(new Thread_Get_DisplayNameRequest { Instance = thread }).ReturnValue;
						executionInfo.Threads.Add(threadInfo);
					}
				}

				if (executionStartedByClient)
                {
					executionInfo.Name = _executionClient.Get_DisplayName(new Execution_Get_DisplayNameRequest { Instance = execution }).ReturnValue;
					executionInfo.RunState = _executionClient.GetStates(new Execution_GetStatesRequest { Instance = execution }).RunState;

					if (getThreadInfo)
					{
						for (int threadIndex = 1; threadIndex < numberOfThreads; threadIndex++)
					{
							var thread = _executionClient.GetThread(new Execution_GetThreadRequest { Instance = execution, Index = threadIndex }).ReturnValue;
							IdentifyThread(thread, out ThreadInfo threadInfo);
							threadInfo.Name = _threadClient.Get_DisplayName(new Thread_Get_DisplayNameRequest { Instance = thread }).ReturnValue;
							executionInfo.Threads.Add(threadInfo);
						}
					}

					executionInstances.Add(executionInfo);
				}
			}

			return executionInstances;
		}

		private ExecutionsInstance GetAllOpenExecutions()
        {
			var applicationMgr = new ApplicationMgrInstance { Id = "ApplicationMgr" };

			return _applicationMgrClient.Get_Executions(new ApplicationMgr_Get_ExecutionsRequest { Instance = applicationMgr }).ReturnValue;
		}

		private void OnListThreadsButtonClick(object sender, EventArgs e)
		{
			try
			{
				ListExecutionsAndThreads();
			}
			catch (Exception exception)
			{
				ReportException(exception);
			}
		}

        private void OnEnableTracingCheckBoxCheckStateChanged(object sender, EventArgs e)
        {
			StationOptionsInstance stationOptions = GetStationOptions();
			_stationOptionsClient.Set_TracingEnabled(
				new StationOptions_Set_TracingEnabledRequest
				{
					Instance = stationOptions,
					IsEnabled = _enableTracingCheckBox.Checked
				});

			UpdateTraceMessagesControls();

			if (!_enableTracingCheckBox.Checked)
            {


			}

			Log("Tracing on the server is " + (_enableTracingCheckBox.Checked ? "enabled." : "disabled.") + Environment.NewLine);
		}

		private static Color GetResultBackgroundColor(string stepStatus)
		{
			return stepStatus.Trim() switch
			{
				ExecutionStateError or ExecutionStateFailed => Color.FromArgb(241, 178, 185),
				ExecutionStatePassed => Color.FromArgb(187, 237, 196),
				StepResultSkipped => Color.FromArgb(192, 192, 192),
				_ => SystemColors.Window,
			};
		}

		private void LogLine(string lineToLog, int indentationLevel = 0)
        {
			Log(lineToLog + Environment.NewLine, indentationLevel);
		}
		private void Log(string stringToLog, int indentationLevel = 0)
        {
			stringToLog = GetIndentSpace(indentationLevel) + stringToLog;
			this.BeginInvoke(() =>  // BeginInvoke, so this can be called from any thread without blocking
			{ 
				_logTextBox.AppendText(stringToLog);
				ScrollToBottomOfText(_logTextBox);
			});
		}

        /// <summary>
        /// Appends the given line to the log control
        /// </summary>
        /// <param name="lineToLog">The line of text to append</param>
        /// <param name="textBackgroundColor">The background color to use to highlight the text</param>
        /// <param name="indentationLevel">The indentation level of the text</param>
        /// <param name="overlayColor">When true, the text is highlighted. When false, a color box will be added to the left of the text.</param>
        private void LogLine(string lineToLog, Color textBackgroundColor, int indentationLevel = 0, bool overlayColor = true)
        {
			Log(lineToLog + Environment.NewLine, textBackgroundColor, indentationLevel, overlayColor);
        }

		/// <summary>
		/// Appends the given string to the log control
		/// </summary>
		/// <param name="lineToLog">The line of text to append</param>
		/// <param name="textBackgroundColor">The background color to use to highlight the text</param>
		/// <param name="indentationLevel">The indentation level of the text</param>
		/// <param name="overlayColor">When true, the text is highlighted. When false, a color box will be added to the left of the text.</param>
		private void Log(string stringToLog, Color textBackgroundColor, int indentationLevel = 0, bool overlayColor = true)
		{
			// BeginInvoke, so this can be called from any thread without blocking
			this.BeginInvoke(() => LogImpl(stringToLog, textBackgroundColor, indentationLevel, overlayColor));
		}

		private void LogImpl(string stringToLog, Color textBackgroundColor, int indentationLevel = 0, bool overlayColor = true)
        {
			int indentOffset = indentationLevel * IndentOffsetForOneLevel;
            int startSelection = _logTextBox.TextLength + indentOffset;
			Color originalSelectionBackgroundColor = _logTextBox.SelectionBackColor;

            stringToLog = GetIndentSpace(indentationLevel) + stringToLog;

            int selectionLength;
            if (overlayColor)
			{
				selectionLength = stringToLog.Length;
			}
			else
			{
                // When not overlaying the color, we need to add a box with the given color before the text.
                selectionLength = ColorBoxWidth;
                stringToLog = stringToLog.Insert(indentOffset, ColorBoxSpace);
			}

			_logTextBox.AppendText(stringToLog);

			_logTextBox.Select(startSelection, selectionLength);
			_logTextBox.SelectionBackColor = textBackgroundColor;

			// Reset selection and color
			_logTextBox.Select(startSelection + selectionLength, 0);
			_logTextBox.SelectionBackColor = originalSelectionBackgroundColor;

			ScrollToBottomOfText(_logTextBox);
        }

		private void LogBold(string stringToLog, int indentationLevel = 0)
		{
			// BeginInvoke, so this can be called from any thread without blocking
			BeginInvoke(() => LogBoldImpl(stringToLog, indentationLevel));
		}

		private void LogBoldImpl(string stringToLog, int indentationLevel = 0)
        {
			int startSelection = _logTextBox.TextLength;
			Font originalSelectionFont = _logTextBox.SelectionFont;

            stringToLog = GetIndentSpace(indentationLevel) + stringToLog;

            _logTextBox.AppendText(stringToLog);
			_logTextBox.Select(startSelection, stringToLog.Length);
			_logTextBox.SelectionFont = new Font(_logTextBox.Font, FontStyle.Bold);

			// Reset selection and font
			_logTextBox.Select(startSelection + stringToLog.Length, 0);
			_logTextBox.SelectionFont = originalSelectionFont;

			ScrollToBottomOfText(_logTextBox);
		}

		private void LogFaded(string stringToLog, int indentationLevel = 0)
		{
			// BeginInvoke, so this can be called from any thread without blocking
			BeginInvoke(() => LogFadedImpl(stringToLog, indentationLevel));
		}

		private void LogFadedImpl(string stringToLog, int indentationLevel = 0)
		{
            int startSelection = _logTextBox.TextLength;
            Color originalSelectionColor = _logTextBox.SelectionColor;

            stringToLog = GetIndentSpace(indentationLevel) + stringToLog;

            _logTextBox.AppendText(stringToLog);
            _logTextBox.Select(startSelection, stringToLog.Length);
            _logTextBox.SelectionColor = Color.FromArgb(129, 131, 134); // Light gray

            // Reset selection and color
            _logTextBox.Select(startSelection + stringToLog.Length, 0);
            _logTextBox.SelectionColor = originalSelectionColor;
        }

        private static void ScrollToBottomOfText(RichTextBox richTextBox)
        {
			richTextBox.SelectionStart = richTextBox.Text.Length;
			richTextBox.ScrollToCaret();
		}

		private void LogTraceMessage(string traceMessage) 
		{
			// BeginInvoke, so this can be called from any thread without blocking
			Debug.Assert(_enableTracingCheckBox.Checked);
			BeginInvoke(() => LogTraceMessageImpl(traceMessage));
		}


		private void LogTraceMessageImpl(string traceMessage)
		{
			int startSearchOffset = _executionTraceMessagesTextBox.Text.Length;

			traceMessage = InsertColorBoxSpaceToTraceMessage(traceMessage);

			_executionTraceMessagesTextBox.AppendText(traceMessage);

			FadeLabel("Socket", startSearchOffset);
			AddColorBoxToTraceResult(startSearchOffset);
			FadeLabel("Step", startSearchOffset);

			// If trace message has an error, fade the error labels
			if (traceMessage.IndexOf("Code") != -1)
			{
				// Start searching on the error message line
				int indexOfNewLine = traceMessage.IndexOf('\n');
				startSearchOffset += indexOfNewLine;

				FadeLabel("Code", startSearchOffset);
				FadeLabel("Message", startSearchOffset);
			}

			ScrollToBottomOfText(_executionTraceMessagesTextBox);
		}

		private string InsertColorBoxSpaceToTraceMessage(string traceMessage)
		{
            int startIndex = traceMessage.IndexOf("Step");
			if (startIndex != -1)
			{
				// Insertion starts at the beginning of status.
				// Add 2 additional spaces to include space between status and "Step" label;
				startIndex -= (StatusLength + 2);

				// Insert status box color space
                traceMessage = traceMessage.Insert(startIndex, ColorBoxSpace);

                // If trace message has an error, we need to indent the error line as well.
                if (traceMessage.IndexOf("Code") != -1)
				{
					int indexOfNewLine = traceMessage.IndexOf('\n');
					if (indexOfNewLine != -1)
					{
						traceMessage = traceMessage.Insert(indexOfNewLine + 1, ColorBoxSpace);
					}
				}
            }

            return traceMessage;
        }

        private void FadeLabel(string label, int startSearchOffset)
		{
			int startSelection = _executionTraceMessagesTextBox.Text.IndexOf(label, startSearchOffset);
            if (startSelection != -1)
            {
                Color originalSelectionColor = _executionTraceMessagesTextBox.SelectionColor;

                _executionTraceMessagesTextBox.Select(startSelection, label.Length);
                _executionTraceMessagesTextBox.SelectionColor = Color.FromArgb(129, 131, 134);

                // Reset selection and color
                _executionTraceMessagesTextBox.Select(startSelection + label.Length, 0);
                _executionTraceMessagesTextBox.SelectionColor = originalSelectionColor;
            }
        }

		private void AddColorBoxToTraceResult(int startSearchOffset)
		{
            int startSelection = _executionTraceMessagesTextBox.Text.IndexOf("Step", startSearchOffset);
            if (startSelection != -1)
            {
                // Selection starts at the beginning of the colorbox space.
                // Add 2 additional spaces to include space between status and "Step" label;
                startSelection -= (StatusLength + ColorBoxSpace.Length + 2); 

                Color originalSelectionBackgroundColor = _executionTraceMessagesTextBox.SelectionBackColor;
                string status = _executionTraceMessagesTextBox.Text.Substring(startSelection + ColorBoxSpace.Length, StatusLength);

                _executionTraceMessagesTextBox.Select(startSelection, ColorBoxWidth);
                _executionTraceMessagesTextBox.SelectionBackColor = GetResultBackgroundColor(status);

                // Reset selection and color
                _executionTraceMessagesTextBox.Select(startSelection + ColorBoxWidth, 0);
                _executionTraceMessagesTextBox.SelectionBackColor = originalSelectionBackgroundColor;
            }
        }

        private string GetIndentSpace(int indentationLevel)
        {
            string indentSpace = string.Empty;
            while (indentationLevel > 0)
            {
                indentSpace = IndentSpace + indentSpace;
                indentationLevel--;
            }

            return indentSpace;
        }

        private void ClearTraceMessages()
        {
			_executionTraceMessagesTextBox.Text = string.Empty;
		}

		private void SetBusy()
        {
			lock(_dataLock)
            {
				_busyCount++;
				if (_busyCount == 1)
                {
					_previousCursor = Cursor.Current;
					Cursor.Current = Cursors.WaitCursor;
				}
            }
        }

		private void UnsetBusy()
        {
			lock (_dataLock)
			{
				Debug.Assert(_busyCount > 0, "Busy count is not greater than zero.");
				_busyCount--;

				if (_busyCount == 0)
                {
					Cursor.Current = _previousCursor;
				}
			}
        }

        private void SetLineSpacing(RichTextBox richTextBox)
        {
            // The only way to set line spacing on a RichTextBox is
            // through a EM_SETPARAFORMAT message.

            var paraformat = new PARAFORMAT2();
            paraformat.cbSize = (uint)Marshal.SizeOf(paraformat);
            paraformat.wReserved = 0;
            paraformat.dwMask = (uint)RichTextBoxOptions.PFM_LINESPACING;
			paraformat.dyLineSpacing = 25;  // 1.25 line spacing

            // The value of dyLineSpacing/20 is the spacing, in lines, from one line to the next.
			// Thus, setting dyLineSpacing to 20 produces single-spaced text, 40 is double spaced,
			// 60 is triple spaced, and so on.
            paraformat.bLineSpacingRule = 5;

            IntPtr lParam = IntPtr.Zero;
			try
			{
				lParam = Marshal.AllocHGlobal(Marshal.SizeOf(paraformat));
				Marshal.StructureToPtr(paraformat, lParam, false);

				SendMessage(
					new HandleRef(richTextBox, richTextBox.Handle),
					(int)WindowsMessage.EM_SETPARAFORMAT,
					new IntPtr((int)RichTextBoxOptions.SCF_SELECTION),
					lParam);
			}
			finally
			{
				if (lParam != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(lParam);
				}
			}
        }

        private class AutoWaitCursor : IDisposable
		{
			private readonly Example _exampleApplication;
			private bool _disposedValue = false;

			public AutoWaitCursor(Example exampleApplication)
			{
				_exampleApplication = exampleApplication;
				exampleApplication.SetBusy();
			}

			protected virtual void Dispose(bool disposing)
			{
				if (!_disposedValue)
				{
					if (disposing)
					{
						_exampleApplication.UnsetBusy();
					}

					_disposedValue = true;
				}
			}

			public void Dispose()
			{
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}
		}
	}
}
