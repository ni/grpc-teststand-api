using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grpc.Core;
using Grpc.Net.Client;
using NationalInstruments.TestStand.Grpc.Net.Client.OO;  // instance lifetime API
using NationalInstruments.TestStand.API.Grpc; // TestStand Engine API
using NationalInstruments.TestStand.UI.Grpc;
using static System.FormattableString;

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

		const long IdForStreamForTracingAllExecutions = -1;

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

		private readonly Dictionary<string, Stream> _imageList = new();

		private bool _isConnected = false;
		private string _nonSequentialProcessModelName = null;

		// BTS client for getting execution trace messages
		private BTS.ExecutionTraceEvents.ExecutionTraceEvents.ExecutionTraceEventsClient _btsTraceEventsClient;
		private IDisposable _listenForServerShutdownStream;

		// This map is used to cancel all the calls that are streaming trace messages.
		private readonly Dictionary<long, IDisposable> _traceMessagesStreams = new();

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
				}

				_connectionTypePictureBox.Image = new Bitmap(_imageList[connectionType]);
				_connectionStatusPictureBox.Image = new Bitmap(_imageList[connectionStatusImageName]);
				_connectionStatusDescriptionLabel.Text = connectionStatusString;
			}
		}

		// pass false to onlyIfNeeded if the server address might have changed
		private async Task Setup(bool onlyIfNeeded)
		{
			if (!onlyIfNeeded || _gRPCChannel == null)
			{
				// if a channel already exists, dispose it
				if (_gRPCChannel != null)
				{
					ReleaseEngineReference();

					await _gRPCChannel.ShutdownAsync();
					_gRPCChannel = null;
				}

				_gRPCChannel = _channelHelper.OpenChannel(_serverAddressTextBox.Text, _clientOptions, out _connectionIsSecured, out string connectionErrors);
				if (_gRPCChannel != null)
				{
					// create the service clients for the interfaces we want to use 
					SetupServiceClients();

					// Start worker thread to listen for server shutdown
					_ = Task.Run(async () => await StartListeningForServerShutdownAsync());

					// start clean in case this is a reconnect from the same machine as prior run.  might not be necessary if we distinguish connection by process id in the future as noted in USER STORY 1631323
					_instanceLifetimeClient.Clear(new InstanceLifetime_ClearRequest { DiscardConnection = true });

					// the engine is used a lot, make sure we have a reference handy
					GetEngineReference();

					// Add station globals to the list view
					RefreshStationGlobals();

					InitializeProcessModelInformation();

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

			// client for the Instance Lifetime API, which lets you tell the server when your client doesn't need specific objects on the server any longer
			_instanceLifetimeClient = new InstanceLifetime.InstanceLifetimeClient(_gRPCChannel);

			// BTS client for getting execution trace messages
			_btsTraceEventsClient = new BTS.ExecutionTraceEvents.ExecutionTraceEvents.ExecutionTraceEventsClient(_gRPCChannel);
		}

		private async Task StartListeningForServerShutdownAsync()
		{
			var request = new BTS.ExecutionTraceEvents.ExecutionTraceEvents_ListenForServerShutdownRequest();
			var listenForServerShutdownStream = _btsTraceEventsClient.ListenForServerShutdown(request);

			// Save it so client can close stream when client shutdowns
			_listenForServerShutdownStream = listenForServerShutdownStream;

			// Wait for a message from the server. If a message is received, it means the server is shutting down.
			await listenForServerShutdownStream.ResponseStream.MoveNext();

			SetConnectionStatus(false);

			CloseAllStreams();
			ReleaseEngineReference();
		}

		private void ReleaseEngineReference()
		{
			// tell the server we don't need the current engine reference anymore
			if (_engine != null)
			{
				try
				{
					_instanceLifetimeClient.Release(new InstanceLifetime_ReleaseRequest { Value = new ObjectInstance { Id = _engine.Id } });
				}
				catch { }

				_engine = null;
			}
		}

		private void GetEngineReference()
		{
			if (_engine == null)
			{
				_engine = _engineClient.Engine(new Engine_EngineRequest()).ReturnValue;

				// in case someone changes the default lifespan, always make the engine have unlimited lifespan
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
				if (rpcException.Status.Detail.Contains("Unable to open file"))
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

		private async void OnConnectButtonClick(object sender, EventArgs e)
		{
			await TryActionAsync(async () =>
			{
				await Setup(false);
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

		private void ReportException(Exception exception)
		{
			if (exception is RpcException rpcException)
			{
				// The grpc exceptions for some cases (like a bad server address) contain the stack trace in the Message, so using the Detail instead
				LogLine("gRPC EXCEPTION: " + rpcException.Status.Detail);

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
					await Setup(true);

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

						await RunSequenceFileAsync(sequenceFile, sequenceName, processModel);

						var resultStatus = _executionClient.Get_ResultStatus(new Execution_Get_ResultStatusRequest { Instance = _activeExecution }).ReturnValue;
						SetExecutionStatus(resultStatus);

						LogExecutionResults(resultStatus, processModel, modelName);
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

		private async Task RunSequenceFileAsync(SequenceFileInstance sequenceFile, string sequenceName, SequenceFileInstance processModel)
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
				if (rpcException.Status.Detail.Contains("Error loading step"))
                {
					// Some error messages include additional information that we don't want to display.  The additional
					// information appears between {}. So, remove all instances of " {<any number of characters>}".
					string errorMessage = Regex.Replace(rpcException.Status.Detail, @"\s\{[^}]+\}", string.Empty);
					errorMessage = "Load Error:" + Environment.NewLine + errorMessage;

					throw new Exception(errorMessage);
				}

				throw;
            }

			SetExecutionStatus(ExecutionStateRunning);

			if (_showTracingCheckBox.Checked)
			{
				StartListeningForExecutionTraceMessages(null);
			}

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
			finally
			{
				CloseStreamForExecution(_activeExecution);
			}
		}

		private void LogExecutionResults(string resultStatus, SequenceFileInstance processModel, string modelName)
		{
			int numberOfResults = 0;
			if (processModel != null && !IsSequentialModelName(modelName))
			{
				LogLine("This example is not currently retrieving results for Parallel and Batch socket executions.");
			}
			else
			{
				bool hasResults = true;

				var executionResults = _executionClient.Get_ResultObject(new Execution_Get_ResultObjectRequest { Instance = _activeExecution }).ReturnValue;
				var resultList = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
				{
					Instance = executionResults,
					LookupString = "ResultList",
					Options = PropertyOptions.PropOptionNoOptions
				}).ReturnValue;

				// When running using the Sequential model, the results are stored in ResultList[0].TS.SequenceCall.ResultList.
				// The top level result list is from the model. Its first and only result is the result of the call to main sequence.
				//
				// If the execution terminates before any steps in the sequence file are executed, ResultList will be empty since
				// the call the MainSequence of the sequence file is never done. Therefore, we need to check if ResultList has one
				// element before trying to get the results for the sequence file.
				if (processModel != null && IsSequentialModelName(modelName))
				{
					var elementCount = _propertyObjectClient.GetNumElements(new PropertyObject_GetNumElementsRequest
					{
						Instance = resultList,
					}).ReturnValue;

					hasResults = elementCount == 1;
					if (hasResults)
					{
						resultList = _propertyObjectClient.GetPropertyObject(new PropertyObject_GetPropertyObjectRequest
						{
							Instance = executionResults,
							LookupString = "ResultList[0].TS.SequenceCall.ResultList",
							Options = PropertyOptions.PropOptionNoOptions
						}).ReturnValue;
					}
				}

				if (hasResults)
				{
					numberOfResults = ListExecutionResults(resultList);
				}
			}

			Log("Execution Complete. Status: ");
			Log(resultStatus, GetResultBackgroundColor(resultStatus));
			LogLine(", Number of Results = " + numberOfResults.ToString(CultureInfo.InvariantCulture));
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

		private void StartListeningForExecutionTraceMessages(ExecutionInstance execution)
        {
			if (execution != null)
			{
				// When specifying an execution, we will only show traces messages for that particular execution.
				long executionId = _executionClient.Get_Id(new Execution_Get_IdRequest { Instance = execution }).ReturnValue;

				// Read trace messages in a background thread
				_ = Task.Run(async() => await TraceSingleExecutionAsync(executionId));
			}

			// There should only be one open stream for getting all execution trace messages
			else if (!_traceMessagesStreams.ContainsKey(IdForStreamForTracingAllExecutions))
            {
				// Read trace messages in a background thread
				_ = Task.Run(async () => await TraceAllExecutionsAsync());
            }
		}

		private async Task TraceSingleExecutionAsync(long executionId)
        {
			var request = new BTS.ExecutionTraceEvents.ExecutionTraceEvents_GetTraceEventMessagesRequest { ExecutionId = executionId };

			// Open the stream. The stream will remain open until the server tells us there are no more messages or streaming is cancelled.
			var messagesStream = _btsTraceEventsClient.GetTraceEventMessages(request);

			// Track all streams so they can be cancelled if requested
			TrackTraceMessageCallStream(executionId, messagesStream);

			// ReadAllAsync creates an IAsyncEnumerable<out T> that enables reading all of the data from the stream reader.
			await foreach (var traceMessage in messagesStream.ResponseStream.ReadAllAsync())
			{
				_executionTraceMessagesTextBox.Invoke((Action)(() => LogTraceMessage(traceMessage.Messages)));
			}
		}

		private async Task TraceAllExecutionsAsync()
		{
			var request = new BTS.ExecutionTraceEvents.ExecutionTraceEvents_GetTraceEventMessagesForAllExecutionsRequest();

			// Open the stream. The stream will remain open until the server tells us there are no more messages.
			var messagesStream = _btsTraceEventsClient.GetTraceEventMessagesForAllExecutions(request);

			// Add stream to list so we can cancel it if requested
			TrackTraceMessageCallStream(IdForStreamForTracingAllExecutions, messagesStream);

			// ReadAllAsync creates an IAsyncEnumerable<out T> that enables reading all of the data from the stream reader.
			await foreach (var traceMessage in messagesStream.ResponseStream.ReadAllAsync())
			{
				_executionTraceMessagesTextBox.Invoke((Action)(() => LogTraceMessage(traceMessage.Messages)));
			}
		}

		private void TrackTraceMessageCallStream(long executionId, IDisposable stream)
        {
			_traceMessagesStreams[executionId] = stream;
		}

		private void CloseTraceMessagesStream(long executionId)
        {
			if (_traceMessagesStreams.TryGetValue(executionId, out IDisposable messageStream))
			{
				_traceMessagesStreams.Remove(executionId);

				// Disposing the stream will close it or cancelled if it is still streaming.
				messageStream.Dispose();
			}
		}

		private void CloseStreamForExecution(ExecutionInstance execution)
		{
			long executionId = _executionClient.Get_Id(new Execution_Get_IdRequest { Instance = execution }).ReturnValue;
			CloseTraceMessagesStream(executionId);
		}

		private int ListExecutionResults(PropertyObjectInstance mainSequenceResults)
		{
			var numberOfResults = _propertyObjectClient.GetNumElements(new PropertyObject_GetNumElementsRequest
			{
				Instance = mainSequenceResults
			}).ReturnValue;

			if (numberOfResults == 0)
			{
				LogLine("No results.");
			}
			else
			{
				for (int index = 0; index < numberOfResults; index++)
				{
					var nthResult = _propertyObjectClient.GetPropertyObjectByOffset(new PropertyObject_GetPropertyObjectByOffsetRequest
					{
						Instance = mainSequenceResults,
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

					Log(Invariant($"Result #{index}, Step Name: {stepName}, Status = "));
					LogLine(stepStatus, GetResultBackgroundColor(stepStatus));

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

						LogLine(Invariant($"    Code = {code}, Message = {message}"));
					}
				}
			}

			return numberOfResults;
		}

		private void OnLoad(object sender, EventArgs e)
		{
			_processModelComboBox.SelectedIndex = 0;
			_entryPointComboBox.SelectedIndex = 0;
			_sequenceFileNameComboBox.SelectedIndex = 0;
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			CloseAllStreams();
			_listenForServerShutdownStream?.Dispose();
			ReleaseEngineReference();
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
			// We cannot completly rely on StartListeningForServerShutdownAsync for determining if the server is
			// down because we will not get a message if the server crashes. This heartbeat complements 
			// StartListeningForServerShutdownAsync.
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

		private void CloseAllStreams()
        {
			long[] executionIds = _traceMessagesStreams.Keys.ToArray();

			for (int index = 0; index < executionIds.Length; index++)
			{
				CloseTraceMessagesStream(executionIds[index]);
			}
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
			var applicationMgrReference = _engineClient.GetInternalOption(new Engine_GetInternalOptionRequest { Instance = _engine, Option = InternalOptions.InternalOptionApplicationManager }).Reference;
			var applicationMgr = new ApplicationMgrInstance { Id = applicationMgrReference.Id };

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

        private void OnShowTracingCheckBoxCheckStateChanged(object sender, EventArgs e)
        {
			if (!_showTracingCheckBox.Checked)
            {
				CloseAllStreams();
			}
			else
            {
				// If there are no executions running, there is nothing to do.
				lock (_dataLock)
				{
					if (_activeExecution != null)
					{
						StartListeningForExecutionTraceMessages(null);
					}
				}
			}

			LogTraceMessage("Tracing is " + (_showTracingCheckBox.Checked ? "enabled." : "disabled.") + Environment.NewLine);
		}

		private static Color GetResultBackgroundColor(string stepStatus)
		{
			return stepStatus switch
			{
				StepResultDone => Color.FromArgb(217, 208, 229),				
				ExecutionStateError or ExecutionStateFailed => Color.FromArgb(241, 178, 185),
				ExecutionStatePassed => Color.FromArgb(187, 237, 196),
				StepResultSkipped => Color.FromArgb(192, 192, 192),
				_ => SystemColors.Window,
			};
		}

		private void LogLine(string lineToLog)
        {
			Log(lineToLog + Environment.NewLine);
		}
		private void Log(string stringToLog)
		{
			_logTextBox.AppendText(stringToLog);
			ScrollToBottomOfText(_logTextBox);
		}

		private void LogLine(string lineToLog, Color textBackgroundColor)
        {
			Log(lineToLog + Environment.NewLine, textBackgroundColor);
        }

		private void Log(string stringToLog, Color textBackgroundColor)
        {
			int startSelection = _logTextBox.TextLength;
			Color originalSelectionBackgroundColor = _logTextBox.SelectionBackColor;

			_logTextBox.AppendText(stringToLog);

			_logTextBox.Select(startSelection, stringToLog.Length);
			_logTextBox.SelectionBackColor = textBackgroundColor;

			// Reset selection and color
			_logTextBox.Select(startSelection + stringToLog.Length, 0);
			_logTextBox.SelectionBackColor = originalSelectionBackgroundColor;

			ScrollToBottomOfText(_logTextBox);
        }

		private void LogBold(string stringToLog)
        {
			int startSelection = _logTextBox.TextLength;
			Font originalSelectionFont = _logTextBox.SelectionFont;

			_logTextBox.AppendText(stringToLog);
			_logTextBox.Select(startSelection, stringToLog.Length);
			_logTextBox.SelectionFont = new Font(_logTextBox.Font, FontStyle.Bold);

			// Reset selection and font
			_logTextBox.Select(startSelection + stringToLog.Length, 0);
			_logTextBox.SelectionFont = originalSelectionFont;

			ScrollToBottomOfText(_logTextBox);
		}

		private static void ScrollToBottomOfText(RichTextBox richTextBox)
        {
			richTextBox.SelectionStart = richTextBox.Text.Length;
			richTextBox.ScrollToCaret();
		}

		private void LogTraceMessage(string traceMessage)
        {
			const string StatusString = "Status: ";

			_executionTraceMessagesTextBox.AppendText(traceMessage);

			// Set color on step result
			int startSelection = _executionTraceMessagesTextBox.Text.LastIndexOf(StatusString);
			if (startSelection != -1)
			{
				Color originalSelectionBackgroundColor = _executionTraceMessagesTextBox.SelectionBackColor;

				startSelection += StatusString.Length;

				// The status is at the end of the first line.
				int endSelection = _executionTraceMessagesTextBox.Text.IndexOf('\n', startSelection);
				string status = _executionTraceMessagesTextBox.Text[startSelection..endSelection];

				_executionTraceMessagesTextBox.Select(startSelection, status.Length); 
				_executionTraceMessagesTextBox.SelectionBackColor = GetResultBackgroundColor(status);

				// Reset selection and color
				_executionTraceMessagesTextBox.Select(startSelection + status.Length, 0);
				_executionTraceMessagesTextBox.SelectionBackColor = originalSelectionBackgroundColor;
			}

			ScrollToBottomOfText(_executionTraceMessagesTextBox);
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
