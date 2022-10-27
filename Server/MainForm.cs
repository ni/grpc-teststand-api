// Note:	This application has a manifest file in the project. This manifest file includes the Microsoft.Windows.Common-Controls which 
//			enables the application to display controls using the XP theme that the operating system selects.
//			A post build event embeds this manifest file into the executable.
//			In order for the manifest file to enable the executable to display with the XP theme:
//			1. The manifest file must have the same name as the executable. For example, if your executable is named MyExecutable.exe, your manifest file is required to have the name MyExecutable.exe.manifest.
//			2. The manifest file must include the Microsoft.Windows.Common-Controls.
//			3. The manifest file must reside in the same directory as the executable.
//			Also note that if you enable the Project Properties>>Debug>>Enable Visual Studio Hosting Process option, the XP theme adaption does not occur when debugging the executable
//			because the Visual Studio environment creates the process and does not allow the manifest file to be embedded into the executable.

// Note:	TestStand installs the source code files for the default user interfaces in the <TestStand>\UserInterfaces and <TestStand Public>\UserInterfaces directories. 
//			To modify the installed user interfaces or to create new user interfaces, modify the files in the <TestStand Public>\UserInterfaces directory. 
//			You can use the read-only source files for the default user interfaces in the <TestStand>\UserInterfaces directory as a reference. 
//			National Instruments recommends that you track the changes you make to the user interface source code files so you can integrate the changes with any enhancements in future versions of the TestStand User Interfaces.

using System;
using System.Windows.Forms;
using NationalInstruments.TestStand.Grpc.Net.Server.OO;
using System.Text.RegularExpressions;

// TestStand Core API 
using NationalInstruments.TestStand.Interop.API;

// TestStand User Interface Controls
using NationalInstruments.TestStand.Interop.UI;
using NationalInstruments.TestStand.Interop.UI.Support;

// .net specific functions for use with TestStand APIs (TSUtil)
using NationalInstruments.TestStand.Utility;

namespace TestExecServer
{
	/// <summary>
	/// Summary description for MainForm.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		private const string WindowTitle = "Simple Test Executive Operator Interface Example - with gRPC server";

		private NationalInstruments.TestStand.Interop.UI.Ax.AxComboBox axFilesComboBox;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxComboBox axSequencesComboBox;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axOpenFileButton;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axCloseFileButton;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxApplicationMgr axApplicationMgr;
		private System.Windows.Forms.Timer GCTimer;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axEntryPoint1Button;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axEntryPoint2Button;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axRunSelectedButton;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axCloseExecutionButton;
		private System.Windows.Forms.Label sequenceFileLabel;
		private System.Windows.Forms.Label sequenceLabel;
		private System.Windows.Forms.Label executionLabel;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxSequenceView axSequenceView;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxExecutionViewMgr axExecutionViewMgr;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxSequenceFileViewMgr axSequenceFileViewMgr;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxComboBox axExecutionsComboBox;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxReportView axReportView;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axBreakResumeButton;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axTerminateRestartButton;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axLoginLogoutButton;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axExitButton;
		private NationalInstruments.TestStand.Interop.UI.Ax.AxButton axTerminateAllButton;
		private System.ComponentModel.IContainer components;
        
		private const int WM_QUERYENDSESSION = 0x11;
		private System.Windows.Forms.NotifyIcon notifyIcon;

		// flag that will be set to true if the user tries to shut down windows
		private bool sessionEnding = false;

		public MainForm()
		{
			// Required for Windows Form Designer support
			InitializeComponent();

			Text = WindowTitle;

			// NOTE: Add any constructor code after InitializeComponent call
			this.Icon = Properties.Resources.App;
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

				TestStandGrpcApi.GrpcServer.Shutdown();
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
			this.axApplicationMgr = new NationalInstruments.TestStand.Interop.UI.Ax.AxApplicationMgr();
			this.axFilesComboBox = new NationalInstruments.TestStand.Interop.UI.Ax.AxComboBox();
			this.axSequencesComboBox = new NationalInstruments.TestStand.Interop.UI.Ax.AxComboBox();
			this.axOpenFileButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axCloseFileButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.GCTimer = new System.Windows.Forms.Timer(this.components);
			this.axExecutionViewMgr = new NationalInstruments.TestStand.Interop.UI.Ax.AxExecutionViewMgr();
			this.axSequenceFileViewMgr = new NationalInstruments.TestStand.Interop.UI.Ax.AxSequenceFileViewMgr();
			this.axEntryPoint1Button = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axEntryPoint2Button = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axRunSelectedButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axExecutionsComboBox = new NationalInstruments.TestStand.Interop.UI.Ax.AxComboBox();
			this.axCloseExecutionButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.sequenceFileLabel = new System.Windows.Forms.Label();
			this.sequenceLabel = new System.Windows.Forms.Label();
			this.executionLabel = new System.Windows.Forms.Label();
			this.axSequenceView = new NationalInstruments.TestStand.Interop.UI.Ax.AxSequenceView();
			this.axReportView = new NationalInstruments.TestStand.Interop.UI.Ax.AxReportView();
			this.axBreakResumeButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axTerminateRestartButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axLoginLogoutButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axExitButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.axTerminateAllButton = new NationalInstruments.TestStand.Interop.UI.Ax.AxButton();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			((System.ComponentModel.ISupportInitialize)(this.axApplicationMgr)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axFilesComboBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axSequencesComboBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axOpenFileButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axCloseFileButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axExecutionViewMgr)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axSequenceFileViewMgr)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axEntryPoint1Button)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axEntryPoint2Button)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axRunSelectedButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axExecutionsComboBox)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axCloseExecutionButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axSequenceView)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axReportView)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axBreakResumeButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axTerminateRestartButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axLoginLogoutButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axExitButton)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.axTerminateAllButton)).BeginInit();
			this.SuspendLayout();
			// 
			// axApplicationMgr
			// 
			this.axApplicationMgr.Location = new System.Drawing.Point(644, 332);
			this.axApplicationMgr.Name = "axApplicationMgr";
			this.axApplicationMgr.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axApplicationMgr.OcxState")));
			this.axApplicationMgr.Size = new System.Drawing.Size(32, 32);
			this.axApplicationMgr.TabIndex = 16;
			this.axApplicationMgr.ExitApplication += new System.EventHandler(this.ApplicationMgr_ExitApplication);
			this.axApplicationMgr.ReportError += new NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_ReportErrorEventHandler(this.ApplicationMgr_ReportError);
			this.axApplicationMgr.DisplaySequenceFile += new NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_DisplaySequenceFileEventHandler(this.ApplicationMgr_DisplaySequenceFile);
			this.axApplicationMgr.DisplayExecution += new NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_DisplayExecutionEventHandler(this.ApplicationMgr_DisplayExecution);
			// 
			// axFilesComboBox
			// 
			this.axFilesComboBox.Location = new System.Drawing.Point(112, 8);
			this.axFilesComboBox.Name = "axFilesComboBox";
			this.axFilesComboBox.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axFilesComboBox.OcxState")));
			this.axFilesComboBox.Size = new System.Drawing.Size(564, 22);
			this.axFilesComboBox.TabIndex = 1;
			// 
			// axSequencesComboBox
			// 
			this.axSequencesComboBox.Location = new System.Drawing.Point(112, 37);
			this.axSequencesComboBox.Name = "axSequencesComboBox";
			this.axSequencesComboBox.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axSequencesComboBox.OcxState")));
			this.axSequencesComboBox.Size = new System.Drawing.Size(564, 22);
			this.axSequencesComboBox.TabIndex = 4;
			// 
			// axOpenFileButton
			// 
			this.axOpenFileButton.Location = new System.Drawing.Point(709, 5);
			this.axOpenFileButton.Name = "axOpenFileButton";
			this.axOpenFileButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axOpenFileButton.OcxState")));
			this.axOpenFileButton.Size = new System.Drawing.Size(167, 26);
			this.axOpenFileButton.TabIndex = 2;
			// 
			// axCloseFileButton
			// 
			this.axCloseFileButton.Location = new System.Drawing.Point(709, 35);
			this.axCloseFileButton.Name = "axCloseFileButton";
			this.axCloseFileButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axCloseFileButton.OcxState")));
			this.axCloseFileButton.Size = new System.Drawing.Size(167, 26);
			this.axCloseFileButton.TabIndex = 5;
			// 
			// GCTimer
			// 
			this.GCTimer.Interval = 3000;
			this.GCTimer.Tick += new System.EventHandler(this.GCTimerTick);
			// 
			// axExecutionViewMgr
			// 
			this.axExecutionViewMgr.Location = new System.Drawing.Point(720, 331);
			this.axExecutionViewMgr.Name = "axExecutionViewMgr";
			this.axExecutionViewMgr.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axExecutionViewMgr.OcxState")));
			this.axExecutionViewMgr.Size = new System.Drawing.Size(32, 32);
			this.axExecutionViewMgr.TabIndex = 18;
			// 
			// axSequenceFileViewMgr
			// 
			this.axSequenceFileViewMgr.Location = new System.Drawing.Point(682, 331);
			this.axSequenceFileViewMgr.Name = "axSequenceFileViewMgr";
			this.axSequenceFileViewMgr.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axSequenceFileViewMgr.OcxState")));
			this.axSequenceFileViewMgr.Size = new System.Drawing.Size(32, 32);
			this.axSequenceFileViewMgr.TabIndex = 17;
			// 
			// axEntryPoint1Button
			// 
			this.axEntryPoint1Button.Location = new System.Drawing.Point(112, 63);
			this.axEntryPoint1Button.Name = "axEntryPoint1Button";
			this.axEntryPoint1Button.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axEntryPoint1Button.OcxState")));
			this.axEntryPoint1Button.Size = new System.Drawing.Size(187, 26);
			this.axEntryPoint1Button.TabIndex = 6;
			// 
			// axEntryPoint2Button
			// 
			this.axEntryPoint2Button.Location = new System.Drawing.Point(300, 63);
			this.axEntryPoint2Button.Name = "axEntryPoint2Button";
			this.axEntryPoint2Button.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axEntryPoint2Button.OcxState")));
			this.axEntryPoint2Button.Size = new System.Drawing.Size(187, 26);
			this.axEntryPoint2Button.TabIndex = 7;
			// 
			// axRunSelectedButton
			// 
			this.axRunSelectedButton.Location = new System.Drawing.Point(488, 63);
			this.axRunSelectedButton.Name = "axRunSelectedButton";
			this.axRunSelectedButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axRunSelectedButton.OcxState")));
			this.axRunSelectedButton.Size = new System.Drawing.Size(187, 26);
			this.axRunSelectedButton.TabIndex = 8;
			// 
			// axExecutionsComboBox
			// 
			this.axExecutionsComboBox.Location = new System.Drawing.Point(112, 95);
			this.axExecutionsComboBox.Name = "axExecutionsComboBox";
			this.axExecutionsComboBox.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axExecutionsComboBox.OcxState")));
			this.axExecutionsComboBox.Size = new System.Drawing.Size(564, 22);
			this.axExecutionsComboBox.TabIndex = 10;
			// 
			// axCloseExecutionButton
			// 
			this.axCloseExecutionButton.Location = new System.Drawing.Point(709, 92);
			this.axCloseExecutionButton.Name = "axCloseExecutionButton";
			this.axCloseExecutionButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axCloseExecutionButton.OcxState")));
			this.axCloseExecutionButton.Size = new System.Drawing.Size(167, 26);
			this.axCloseExecutionButton.TabIndex = 11;
			// 
			// sequenceFileLabel
			// 
			this.sequenceFileLabel.Location = new System.Drawing.Point(5, 12);
			this.sequenceFileLabel.Name = "sequenceFileLabel";
			this.sequenceFileLabel.Size = new System.Drawing.Size(96, 16);
			this.sequenceFileLabel.TabIndex = 0;
			this.sequenceFileLabel.Text = "Sequence Files:";
			// 
			// sequenceLabel
			// 
			this.sequenceLabel.Location = new System.Drawing.Point(5, 40);
			this.sequenceLabel.Name = "sequenceLabel";
			this.sequenceLabel.Size = new System.Drawing.Size(96, 16);
			this.sequenceLabel.TabIndex = 3;
			this.sequenceLabel.Text = "Sequences:";
			// 
			// executionLabel
			// 
			this.executionLabel.Location = new System.Drawing.Point(5, 100);
			this.executionLabel.Name = "executionLabel";
			this.executionLabel.Size = new System.Drawing.Size(96, 16);
			this.executionLabel.TabIndex = 9;
			this.executionLabel.Text = "Executions:";
			// 
			// axSequenceView
			// 
			this.axSequenceView.Location = new System.Drawing.Point(5, 125);
			this.axSequenceView.Name = "axSequenceView";
			this.axSequenceView.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axSequenceView.OcxState")));
			this.axSequenceView.Size = new System.Drawing.Size(871, 200);
			this.axSequenceView.TabIndex = 12;
			// 
			// axReportView
			// 
			this.axReportView.Location = new System.Drawing.Point(5, 361);
			this.axReportView.Name = "axReportView";
			this.axReportView.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axReportView.OcxState")));
			this.axReportView.Size = new System.Drawing.Size(871, 205);
			this.axReportView.TabIndex = 19;
			// 
			// axBreakResumeButton
			// 
			this.axBreakResumeButton.Location = new System.Drawing.Point(5, 330);
			this.axBreakResumeButton.Name = "axBreakResumeButton";
			this.axBreakResumeButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axBreakResumeButton.OcxState")));
			this.axBreakResumeButton.Size = new System.Drawing.Size(167, 26);
			this.axBreakResumeButton.TabIndex = 13;
			// 
			// axTerminateRestartButton
			// 
			this.axTerminateRestartButton.Location = new System.Drawing.Point(176, 330);
			this.axTerminateRestartButton.Name = "axTerminateRestartButton";
			this.axTerminateRestartButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axTerminateRestartButton.OcxState")));
			this.axTerminateRestartButton.Size = new System.Drawing.Size(167, 26);
			this.axTerminateRestartButton.TabIndex = 14;
			// 
			// axLoginLogoutButton
			// 
			this.axLoginLogoutButton.Location = new System.Drawing.Point(539, 571);
			this.axLoginLogoutButton.Name = "axLoginLogoutButton";
			this.axLoginLogoutButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axLoginLogoutButton.OcxState")));
			this.axLoginLogoutButton.Size = new System.Drawing.Size(167, 26);
			this.axLoginLogoutButton.TabIndex = 20;
			// 
			// axExitButton
			// 
			this.axExitButton.Location = new System.Drawing.Point(710, 571);
			this.axExitButton.Name = "axExitButton";
			this.axExitButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axExitButton.OcxState")));
			this.axExitButton.Size = new System.Drawing.Size(167, 26);
			this.axExitButton.TabIndex = 21;
			// 
			// axTerminateAllButton
			// 
			this.axTerminateAllButton.Location = new System.Drawing.Point(347, 330);
			this.axTerminateAllButton.Name = "axTerminateAllButton";
			this.axTerminateAllButton.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axTerminateAllButton.OcxState")));
			this.axTerminateAllButton.Size = new System.Drawing.Size(167, 26);
			this.axTerminateAllButton.TabIndex = 15;
			// 
			// notifyIcon
			// 
			this.notifyIcon.BalloonTipText = "TestExecServer";
			this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
			this.notifyIcon.Text = "TestExecServer";
			this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.NotifyIcon_MouseDoubleClick);
			// 
			// MainForm
			// 
			this.ClientSize = new System.Drawing.Size(882, 604);
			this.Controls.Add(this.axTerminateAllButton);
			this.Controls.Add(this.axExitButton);
			this.Controls.Add(this.axSequenceView);
			this.Controls.Add(this.axLoginLogoutButton);
			this.Controls.Add(this.axTerminateRestartButton);
			this.Controls.Add(this.axBreakResumeButton);
			this.Controls.Add(this.axReportView);
			this.Controls.Add(this.executionLabel);
			this.Controls.Add(this.sequenceLabel);
			this.Controls.Add(this.sequenceFileLabel);
			this.Controls.Add(this.axExecutionsComboBox);
			this.Controls.Add(this.axRunSelectedButton);
			this.Controls.Add(this.axEntryPoint2Button);
			this.Controls.Add(this.axEntryPoint1Button);
			this.Controls.Add(this.axSequenceFileViewMgr);
			this.Controls.Add(this.axExecutionViewMgr);
			this.Controls.Add(this.axCloseFileButton);
			this.Controls.Add(this.axOpenFileButton);
			this.Controls.Add(this.axCloseExecutionButton);
			this.Controls.Add(this.axSequencesComboBox);
			this.Controls.Add(this.axFilesComboBox);
			this.Controls.Add(this.axApplicationMgr);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Closing += new System.ComponentModel.CancelEventHandler(this.MainForm_Closing);
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Resize += new System.EventHandler(this.MainForm_Resize);
			((System.ComponentModel.ISupportInitialize)(this.axApplicationMgr)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axFilesComboBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axSequencesComboBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axOpenFileButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axCloseFileButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axExecutionViewMgr)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axSequenceFileViewMgr)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axEntryPoint1Button)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axEntryPoint2Button)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axRunSelectedButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axExecutionsComboBox)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axCloseExecutionButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axSequenceView)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axReportView)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axBreakResumeButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axTerminateRestartButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axLoginLogoutButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axExitButton)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.axTerminateAllButton)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.SetHighDpiMode(HighDpiMode.DpiUnaware);

			var form = new MainForm();

			// specify an action to perform if waiting on a client event reply in the GUI thread. This is optional. It will improve UI responsiveness
			// of the server app when clients go unresponsive and the title bar message will inform a user who is interacting with the server app
			// when the progress of the app is necessarily impeeded due to waiting for client event responses or for the corresponding timeouts to expire.
			ConnectionFactory.WaitLoopDelegate = (WaitLoopStates waitLoopState, double elapsedWaitTime) =>
			{
				if (!form.InvokeRequired)  // if we are waiting for a grpc event reply in the gui thread...
				{					
					if (waitLoopState == WaitLoopStates.Ended)
					{
						form.Text = Regex.Replace(form.Text, @" \[Waiting For Client ?\d+[\.?\d+]*\]", String.Empty); // clear wait message
						// form.Enabled = true;  // see comment on form.Enabled = false
					}
					else
					{
						// To avoid event-loop reentrancy, disable the UI before calling DoEvents, just like a Modal dialog disables a
						// parent window  before running its nested event loop
						// form.Enabled = false; // actually not doing this because disabling the form prevents dragging and makes the UI look hung even if it isn't. A more targeted disabling would be better.

						if (waitLoopState == WaitLoopStates.Starting)
							form.Text += $" [Waiting For Client {elapsedWaitTime:F1}]";
						else
							form.Text = Regex.Replace(form.Text, @" \[Waiting For Client ?\d+[\.?\d+]*\]", $" [Waiting For Client {elapsedWaitTime:F1}]");

						Application.DoEvents(); // process events so the gui thread isn't hung while waiting
					}
				}
			};

			TestStandGrpcApi.GrpcServer.Start(args);  // start grpc server

			ApplicationWrapper.Run(form);
		}

		private void MainForm_Load(object sender, System.EventArgs e)
		{			
			try
			{
				// If this UI is running in a CLR other than the one TestStand uses,
				// then it needs its own GCTimer for that version of the CLR. If it's running in the
				// same CLR as TestStand then the engine's gctimer enabled by the ApplicationMgr
				// is sufficient. See the API help for Engine.DotNetGarbageCollectionInterval for more details.
				if (System.Environment.Version.ToString() != axApplicationMgr.GetEngine().DotNetCLRVersion)
					this.GCTimer.Enabled = true;
				
				// connect TestStand comboboxes 
				axSequenceFileViewMgr.ConnectSequenceFileList(axFilesComboBox.GetOcx(), true);
				axSequenceFileViewMgr.ConnectSequenceList(axSequencesComboBox.GetOcx());

				// specify what information to display in each execution list combobox entry (the expression string looks extra complicated here because we have to escape the quotes for the C# compiler.)
				axExecutionViewMgr.ConnectExecutionList(axExecutionsComboBox.GetOcx()).DisplayExpression = @"""%CurrentExecution% - "" + (""%UUTSerialNumber%"" == """" ? """" : (ResStr(""TSUI_OI_MAIN_PANEL"",""SERIAL_NUMBER"") + "" %UUTSerialNumber% - "")) + (""%TestSocketIndex%"" == """" ? """" : (ResStr(""TSUI_OI_MAIN_PANEL"",""SOCKET_NUMBER"") + "" %TestSocketIndex% - "")) + ""%ModelState%""";

				// connect sequence view to execution view manager									  
				axExecutionViewMgr.ConnectExecutionView(axSequenceView.GetOcx(), ExecutionViewOptions.ExecutionViewConnection_NoOptions);

				// connect report view to execution view manager									  
				axExecutionViewMgr.ConnectReportView(axReportView.GetOcx());

				// connect TestStand buttons to commands
				axExecutionViewMgr.ConnectCommand(axCloseExecutionButton.GetOcx(), CommandKinds.CommandKind_Close, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axApplicationMgr.ConnectCommand(axTerminateAllButton.GetOcx(), CommandKinds.CommandKind_TerminateAll, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axApplicationMgr.ConnectCommand(axLoginLogoutButton.GetOcx(), CommandKinds.CommandKind_LoginLogout, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axApplicationMgr.ConnectCommand(axExitButton.GetOcx(), CommandKinds.CommandKind_Exit, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axSequenceFileViewMgr.ConnectCommand(axOpenFileButton.GetOcx(), CommandKinds.CommandKind_OpenSequenceFiles, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axSequenceFileViewMgr.ConnectCommand(axCloseFileButton.GetOcx(), CommandKinds.CommandKind_Close, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axSequenceFileViewMgr.ConnectCommand(axEntryPoint1Button.GetOcx(), CommandKinds.CommandKind_ExecutionEntryPoints_Set, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axSequenceFileViewMgr.ConnectCommand(axEntryPoint2Button.GetOcx(), CommandKinds.CommandKind_ExecutionEntryPoints_Set, 1, CommandConnectionOptions.CommandConnection_EnableImage);
				axSequenceFileViewMgr.ConnectCommand(axRunSelectedButton.GetOcx(), CommandKinds.CommandKind_RunCurrentSequence, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axExecutionViewMgr.ConnectCommand(axBreakResumeButton.GetOcx(), CommandKinds.CommandKind_BreakResume, 0, CommandConnectionOptions.CommandConnection_EnableImage);
				axExecutionViewMgr.ConnectCommand(axTerminateRestartButton.GetOcx(), CommandKinds.CommandKind_TerminateRestart, 0, CommandConnectionOptions.CommandConnection_EnableImage);

				// show all step groups at once in the sequence view
				axExecutionViewMgr.StepGroupMode = StepGroupModes.StepGroupMode_AllGroups;

				// this is a server, no sense in prompting to log in a local user
				axApplicationMgr.GetEngine().StationOptions.LoginOnStart = false;
				axApplicationMgr.GetEngine().StationOptions.EnableUserPrivilegeChecking = false;

				// make the managers available as named grpc globals
				ConnectionFactory.ServerGlobalScope.ToInstanceId(axApplicationMgr.GetOcx(), true, "ApplicationMgr");
				ConnectionFactory.ServerGlobalScope.ToInstanceId(axSequenceFileViewMgr.GetOcx(), true, "SequenceFileViewMgr");
				ConnectionFactory.ServerGlobalScope.ToInstanceId(axExecutionViewMgr.GetOcx(), true, "ExecutionFileViewMgr");
				
				axApplicationMgr.Start();   // start up the TestStand User Interface Components.
			}
			catch (Exception theException)
			{
				MessageBox.Show(this, theException.Message, "Error");
				Application.Exit();
			}

			if (string.IsNullOrEmpty(TestStandGrpcApi.GrpcServer.ErrorMessage))
			{
				Text += TestStandGrpcApi.GrpcServer.UsesHttps ? " [Secure]" : " [Not secure]";
			}
			else
			{
				Text += " [No gRPC Service]";

				// Try to show the error dialog after the MainForm becomes visible. It is not always guarantee that 
				// MainForm will show before the error dialog when using BeginInvoke, however, in most cases it does.
				BeginInvoke(() =>
				{
					string message = "Failed to initialize gRPC service with following error(s):\n\n" + TestStandGrpcApi.GrpcServer.ErrorMessage;
					axApplicationMgr.RaiseError((int)TSError.TS_Err_OperationFailed, message);
				});
            }
		}

		// handle request to close form (via Windows close box, for example)
		private void MainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Don't set e.Cancel to true if windows is shutting down.
			// Doing so would prevent windows from shutting down or logging out.
			if (!sessionEnding)
			{
				// initiate shutdown and cancel close if shutdown is not complete.  The applicationMgr will
				// send the ExitApplication event when shutdown is complete and we can close then
				if (axApplicationMgr.Shutdown() == false)
					e.Cancel = true;		
			}

			if (!e.Cancel)
            {
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
				sessionEnding = true;
				Application.Exit();
			}

			base.WndProc(ref msg);
		}

		// It is now ok to exit, close the form
		private void ApplicationMgr_ExitApplication(object sender, System.EventArgs e)
		{
			Environment.ExitCode = this.axApplicationMgr.ExitCode;
			Close();

			TSHelper.DoSynchronousGCForCOMObjectDestruction();
		}

		// ApplicationMgr sends this event to handle any errors it detects.  For example, if a TestStand menu command
		// generates an error, this handler displays it
		private void ApplicationMgr_ReportError(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_ReportErrorEvent e)
		{
			MessageBox.Show(this, ErrorMessage.AppendCodeAndDescription(this.axApplicationMgr, e.errorMessage, e.errorCode), "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop); 		
		}

		// the ApplicationMgr sends this event to request that the UI display a particular execution
		private void ApplicationMgr_DisplayExecution(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_DisplayExecutionEvent e)
		{
			// bring application to front if we hit a breakpoint
			if (e.reason == ExecutionDisplayReasons.ExecutionDisplayReason_Breakpoint || e.reason == ExecutionDisplayReasons.ExecutionDisplayReason_BreakOnRunTimeError)
				this.Activate();

			axExecutionViewMgr.Execution = e.exec;
		}

		// the ApplicationMgr sends this event to request that the UI display a particular sequence file
		private void ApplicationMgr_DisplaySequenceFile(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_DisplaySequenceFileEvent e)
		{
			axSequenceFileViewMgr.SequenceFile = e.file;
		}

		// Release all objects periodically.  .NET lets COM objects pile up on the managed heap, seemingly even objects you don't know about such
		// as parameters to unhandled ActiveX events.  This timer ensures that all COM objects are released in a timely manner,
		// thus preventing the performance hiccup that could occur when .NET finally decides to collect garbage. Also, this timer
		// ensures that actions triggered by object destruction run in a timely manner. For example: sequence file unload callbacks.
		private void GCTimerTick(object sender, System.EventArgs e)
		{
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false); // force .net garbage collection
		}

		// Check whether the form has been minimized and if so, hide it from the Taskbar
		// and set the NotifyIcon's visibility to True to display it in the system tray.
		private void MainForm_Resize(object sender, EventArgs e)
		{
			if (this.WindowState == FormWindowState.Minimized)
			{
				Hide();
				notifyIcon.Visible = true;
			}
		}

		// Handle NotifyIcon's MouseDoubleClick event and in its response we will
		// show the app on the Taskbar, un-minimize it, and hide the system tray icon.
		private void NotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)  
		{
			Show();
			this.WindowState = FormWindowState.Normal;
			notifyIcon.Visible = false;
		}
	}
}