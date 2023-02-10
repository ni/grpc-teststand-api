// Note:	This application has a manifest file in the project. This manifest file includes the Microsoft.Windows.Common-Controls which 
//			enables the application to display controls using the XP theme that the operating system selects.
//			The manifest file is specified in the project settings.
//			In order for the manifest file to enable the executable to display with the XP theme:
//			1. The manifest file must have the same name as the executable. For example, if your executable is named MyExecutable.exe, your manifest file is required to have the name MyExecutable.exe.manifest.
//			2. The manifest file must include the Microsoft.Windows.Common-Controls.
//			3. The manifest file must reside in the same directory as the executable.
//			Also note that if you enable the Project Properties>>Debug>>Enable Visual Studio Hosting Process option, the XP theme adaption does not occur when debugging the executable
//			because the Visual Studio environment creates the process and does not allow the manifest file to be embedded into the executable.
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Forms;
using NationalInstruments.TestStand.Grpc.Net.Server.OO;
using NationalInstruments.TestStand.Interop.API;

// TestStand User Interface Controls
using NationalInstruments.TestStand.Interop.UI;
using NationalInstruments.TestStand.Interop.UI.Ax;

// .net specific functions for use with TestStand APIs (TSUtil)
using NationalInstruments.TestStand.Utility;

namespace TestExecWindowsService
{
	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public class MainForm : Form
	{
		private const int WM_QUERYENDSESSION = 0x11;

		private Timer _gcTimer;
		private AxApplicationMgr _axApplicationMgr;
		private AxExecutionViewMgr _axExecutionViewMgr;
		private AxSequenceFileViewMgr _axSequenceFileViewMgr;
		private PreviousServerConfigurationOptions _previousServerOptions;
		private IContainer components;

		// Flag that will be set to true if the user tries to shut down windows
		private bool _sessionEnding = false;
		private bool _calledOnLoad;

		public MainForm()
		{
			// Required for Windows Form Designer support
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (disposing)
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this._axApplicationMgr = new NationalInstruments.TestStand.Interop.UI.Ax.AxApplicationMgr();
			this._gcTimer = new Timer(this.components);
			this._axExecutionViewMgr = new NationalInstruments.TestStand.Interop.UI.Ax.AxExecutionViewMgr();
			this._axSequenceFileViewMgr = new NationalInstruments.TestStand.Interop.UI.Ax.AxSequenceFileViewMgr();
			((System.ComponentModel.ISupportInitialize)(this._axApplicationMgr)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._axExecutionViewMgr)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._axSequenceFileViewMgr)).BeginInit();
			this.SuspendLayout();
			// 
			// axApplicationMgr
			// 
			this._axApplicationMgr.Location = new System.Drawing.Point(644, 332);
			this._axApplicationMgr.Name = "axApplicationMgr";
			this._axApplicationMgr.OcxState = (AxHost.State)resources.GetObject("axApplicationMgr.OcxState");
			this._axApplicationMgr.Size = new System.Drawing.Size(32, 32);
			this._axApplicationMgr.TabIndex = 16;
			this._axApplicationMgr.DisplaySequenceFile += ApplicationMgr_DisplaySequenceFile;
			this._axApplicationMgr.DisplayExecution += ApplicationMgr_DisplayExecution;
			this._axApplicationMgr.ExitApplication += ApplicationMgr_ExitApplication;
			this._axApplicationMgr.QueryShutdown += AxApplicationMgr_QueryShutdown;
			this._axApplicationMgr.QueryCloseExecution += AxApplicationMgr_QueryCloseExecution;
			this._axApplicationMgr.QueryReloadSequenceFile += AxApplicationMgr_QueryReloadSequenceFile;
			this._axApplicationMgr.ReportError += ApplicationMgr_ReportError;
			// 
			// GCTimer
			// 
			this._gcTimer.Interval = 3000;
			this._gcTimer.Tick += new EventHandler(this.GCTimerTick);
			// 
			// axExecutionViewMgr
			// 
			this._axExecutionViewMgr.Location = new System.Drawing.Point(720, 331);
			this._axExecutionViewMgr.Name = "axExecutionViewMgr";
			this._axExecutionViewMgr.OcxState = (AxHost.State)resources.GetObject("axExecutionViewMgr.OcxState");
			this._axExecutionViewMgr.Size = new System.Drawing.Size(32, 32);
			this._axExecutionViewMgr.TabIndex = 18;
			// 
			// axSequenceFileViewMgr
			// 
			this._axSequenceFileViewMgr.Location = new System.Drawing.Point(682, 331);
			this._axSequenceFileViewMgr.Name = "axSequenceFileViewMgr";
			this._axSequenceFileViewMgr.OcxState = (AxHost.State)resources.GetObject("axSequenceFileViewMgr.OcxState");
			this._axSequenceFileViewMgr.Size = new System.Drawing.Size(32, 32);
			this._axSequenceFileViewMgr.TabIndex = 17;
			// 
			// MainForm
			// 
			this.ClientSize = new System.Drawing.Size(882, 604);
			this.Controls.Add(this._axSequenceFileViewMgr);
			this.Controls.Add(this._axExecutionViewMgr);
			this.Controls.Add(this._axApplicationMgr);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Closing += MainForm_Closing;
			this.Load += MainForm_Load;
			((System.ComponentModel.ISupportInitialize)(this._axApplicationMgr)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._axExecutionViewMgr)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._axSequenceFileViewMgr)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion

		protected override void SetVisibleCore(bool value)
		{
			// Don't show the window, but load the form
			if (!_calledOnLoad)
			{
				_calledOnLoad = true;
				OnLoad(EventArgs.Empty);
			}
		}

		private void MainForm_Load(object sender, EventArgs e)
		{			
			try
			{
				// If this UI is running in a CLR other than the one TestStand uses,
				// then it needs its own GCTimer for that version of the CLR. If it's running in the
				// same CLR as TestStand then the engine's gctimer enabled by the ApplicationMgr
				// is sufficient. See the API help for Engine.DotNetGarbageCollectionInterval for more details.
				if (Environment.Version.ToString() != _axApplicationMgr.GetEngine().DotNetCLRVersion)
					_gcTimer.Enabled = true;

				ConfigureServer();

				// make the managers available as named grpc globals
				ConnectionFactory.ServerGlobalScope.ToInstanceId(_axApplicationMgr.GetOcx(), true, "ApplicationMgr");
				ConnectionFactory.ServerGlobalScope.ToInstanceId(_axSequenceFileViewMgr.GetOcx(), true, "SequenceFileViewMgr");
				ConnectionFactory.ServerGlobalScope.ToInstanceId(_axExecutionViewMgr.GetOcx(), true, "ExecutionFileViewMgr");

				_axApplicationMgr.Start();   // start up the TestStand User Interface Components.

				// Enable finding example sequence files in the location of the executable.
				EnableApplicationDirectorySearchPath();
			}
			catch (Exception theException)
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					WriteErrorToEventLog(theException.Message);
				}
				Close();
			}

			if (!string.IsNullOrEmpty(GrpcService.ErrorMessage))
			{
				string message = "Failed to initialize gRPC service with following error(s):\n\n" + GrpcService.ErrorMessage;
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					WriteErrorToEventLog(message);
				}
				Close();
			}
		}

		private void EnableApplicationDirectorySearchPath()
		{
			SearchDirectories searchDirectories = _axApplicationMgr.GetEngine().SearchDirectories;

			foreach (SearchDirectory searchDirectory in searchDirectories)
			{
				if (searchDirectory.Type == SearchDirectoryTypes.SearchDirectoryType_ApplicationDir)
				{
					searchDirectory.Disabled = false;
					break;
				}
			}
		}

		// Configure server to avoid showing popups. This only applies to any popups that TestStand might show
		// when running an execution. It does not prevent popups shown by user code modules.
		private void ConfigureServer()
		{
			StationOptions stationOptions = _axApplicationMgr.GetEngine().StationOptions;

			// First, save current settings of all options that are going to be modified.
			_previousServerOptions = new PreviousServerConfigurationOptions();
			_previousServerOptions._loginOnStart = stationOptions.LoginOnStart;
			_previousServerOptions._enableUserPrivilegeChecking = stationOptions.EnableUserPrivilegeChecking;
			_previousServerOptions._rteOption = stationOptions.RTEOption;
			_previousServerOptions._executingAbortingLimitAction = stationOptions.GetTimeLimitAction(TimeLimitTypes.TimeLimitType_NormalExecution, TimeLimitOperations.TimeLimitOperation_Aborting);
			_previousServerOptions._exitingAbortingTimeLimit = stationOptions.GetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Aborting);
			_previousServerOptions._exitingExecutingTimeLimit = stationOptions.GetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Executing);
			_previousServerOptions._exitingTerminatingTimeLimit = stationOptions.GetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Terminating);
			_previousServerOptions._exitingAbortingLimitAction = stationOptions.GetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Aborting);
			_previousServerOptions._exitingExecutingLimitAction = stationOptions.GetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Executing);
			_previousServerOptions._exitingTerminatingLimitAction = stationOptions.GetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Terminating);

			// this is a server, no sense in prompting to log in a local user
			stationOptions.LoginOnStart = false;
			stationOptions.EnableUserPrivilegeChecking = false;

			// Don't show any dialogs when an error occurs
			stationOptions.RTEOption = RTEOptions.RTEOption_Continue;

			// Don't prompt to abort an execution
			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_NormalExecution, TimeLimitOperations.TimeLimitOperation_Aborting, TimeLimitActions.TimeLimitAction_KillThreads);

			// Make sure server does not show any dialogs when shutting down.
			stationOptions.SetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Aborting, 0);
			stationOptions.SetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Executing, 0);
			stationOptions.SetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Terminating, 0);
			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Aborting, TimeLimitActions.TimeLimitAction_KillThreads);
			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Executing, TimeLimitActions.TimeLimitAction_Abort);
			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Terminating, TimeLimitActions.TimeLimitAction_Abort);
		}

		// Restore all the settings modified service.
		private void RestoreServerSettings()
		{
			StationOptions stationOptions = _axApplicationMgr.GetEngine().StationOptions;

			stationOptions.LoginOnStart = _previousServerOptions._loginOnStart;
			stationOptions.EnableUserPrivilegeChecking = _previousServerOptions._enableUserPrivilegeChecking;
			stationOptions.RTEOption = _previousServerOptions._rteOption;

			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_NormalExecution, TimeLimitOperations.TimeLimitOperation_Aborting, _previousServerOptions._executingAbortingLimitAction);

			stationOptions.SetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Aborting, _previousServerOptions._exitingAbortingTimeLimit);
			stationOptions.SetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Executing, _previousServerOptions._exitingExecutingTimeLimit);
			stationOptions.SetTimeLimit(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Terminating, _previousServerOptions._exitingTerminatingTimeLimit);
			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Aborting, _previousServerOptions._exitingAbortingLimitAction);
			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Executing, _previousServerOptions._exitingExecutingLimitAction);
			stationOptions.SetTimeLimitAction(TimeLimitTypes.TimeLimitType_Exiting, TimeLimitOperations.TimeLimitOperation_Terminating, _previousServerOptions._exitingTerminatingLimitAction);
		}

		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Don't set e.Cancel to true if windows is shutting down.
			// Doing so would prevent windows from shutting down or logging out.
			if (!_sessionEnding)
			{
				// initiate shutdown and cancel close if shutdown is not complete.  The applicationMgr will
				// send the ExitApplication event when shutdown is complete and we can close then
				if (_axApplicationMgr.Shutdown() == false)
				{
					e.Cancel = true;
				}
			}

			if (!e.Cancel)
            {
				RestoreServerSettings();

				// Since server is shutting down, discard all connections to make sure
				// all objects in those connections are also released.
				ConnectionFactory.DiscardAllConnections();
			}
		}

		protected override void WndProc(ref Message msg)
		{
			// set the sessionEnding flag so I will know the form is closing because the user
			// is trying to shutdown, restart, or logoff windows
			if (msg.Msg == WM_QUERYENDSESSION)
			{
				_sessionEnding = true;
				Close();
			}

			base.WndProc(ref msg);
		}

		// It is now ok to exit, close the form
		private void ApplicationMgr_ExitApplication(object sender, EventArgs e)
		{
			Environment.ExitCode = _axApplicationMgr.ExitCode;
			Close();
			TSHelper.DoSynchronousGCForCOMObjectDestruction();
		}

		private void AxApplicationMgr_QueryShutdown(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_QueryShutdownEvent e)
		{
			e.opt = QueryShutdownOptions.QueryShutdown_Continue;
		}

		private void AxApplicationMgr_QueryReloadSequenceFile(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_QueryReloadSequenceFileEvent e)
		{
			e.opt = QueryReloadSequenceFileOptions.QueryReloadSequenceFile_Cancel;
		}

		private void AxApplicationMgr_QueryCloseExecution(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_QueryCloseExecutionEvent e)
		{
			e.opt = QueryCloseExecutionOptions.QueryCloseExecution_Terminate;
		}

		// ApplicationMgr sends this event to handle any errors it detects.  For example, if a TestStand menu command
		// generates an error, this handler displays it
		private void ApplicationMgr_ReportError(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_ReportErrorEvent e)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				WriteErrorToEventLog(ErrorMessage.AppendCodeAndDescription(_axApplicationMgr, e.errorMessage, e.errorCode));
			}
		}

		// the ApplicationMgr sends this event to request that the UI display a particular execution
		private void ApplicationMgr_DisplayExecution(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_DisplayExecutionEvent e)
		{
			_axExecutionViewMgr.Execution = e.exec;
		}

		// the ApplicationMgr sends this event to request that the UI display a particular sequence file
		private void ApplicationMgr_DisplaySequenceFile(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_DisplaySequenceFileEvent e)
		{
			_axSequenceFileViewMgr.SequenceFile = e.file;
		}

		// Release all objects periodically.  .NET lets COM objects pile up on the managed heap, seemingly even objects you don't know about such
		// as parameters to unhandled ActiveX events.  This timer ensures that all COM objects are released in a timely manner,
		// thus preventing the performance hiccup that could occur when .NET finally decides to collect garbage. Also, this timer
		// ensures that actions triggered by object destruction run in a timely manner. For example: sequence file unload callbacks.
		private void GCTimerTick(object sender, EventArgs e)
		{
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false); // force .net garbage collection
		}

		[SupportedOSPlatform("windows")]  // Added this to remove warning CA1416
		public static void WriteErrorToEventLog(string textToLog)
		{
			WriteToEventLog(textToLog, EventLogEntryType.Error);
		}

		[SupportedOSPlatform("windows")]  // Added this to remove warning CA1416
		public static void WriteInformationToEventLog(string textToLog)
		{
			WriteToEventLog(textToLog, EventLogEntryType.Information);
		}

		[SupportedOSPlatform("windows")]  // Added this to remove warning CA1416
		private static void WriteToEventLog(string textToLog, EventLogEntryType type)
		{
			const string logSource = "TestStand gRPC Server";

			if (!EventLog.SourceExists(logSource))
			{
				EventLog.CreateEventSource(logSource, "Application");
			}

			EventLog.WriteEntry(logSource, textToLog, type);
		}

		private struct PreviousServerConfigurationOptions
		{
			public bool _loginOnStart;
			public bool _enableUserPrivilegeChecking;

			public RTEOptions _rteOption;

			public TimeLimitActions _executingAbortingLimitAction;

			public double _exitingAbortingTimeLimit;
			public double _exitingExecutingTimeLimit;
			public double _exitingTerminatingTimeLimit;

			public TimeLimitActions _exitingAbortingLimitAction;
			public TimeLimitActions _exitingExecutingLimitAction;
			public TimeLimitActions _exitingTerminatingLimitAction;
		}
	}
}