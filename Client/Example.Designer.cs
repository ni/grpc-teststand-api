
namespace ExampleClient
{
	partial class Example
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
            if (disposing)
            {
                components?.Dispose();
                components = null;

                _numTestSocketsNumericUpDownToolTip?.Dispose();
                _numTestSocketsNumericUpDownToolTip = null;
            }
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this._logTextBox = new System.Windows.Forms.RichTextBox();
            this._logLabel = new System.Windows.Forms.Label();
            this._runSequenceFileButton = new System.Windows.Forms.Button();
            this._processModelComboBox = new System.Windows.Forms.ComboBox();
            this._serverAddressTextBox = new System.Windows.Forms.TextBox();
            this._serverAddressLabel = new System.Windows.Forms.Label();
            this._connectButton = new System.Windows.Forms.Button();
            this._sequenceFileNameLabel = new System.Windows.Forms.Label();
            this._entryPointComboBox = new System.Windows.Forms.ComboBox();
            this._modelLabel = new System.Windows.Forms.Label();
            this._entryPointLabel = new System.Windows.Forms.Label();
            this._sequenceFileNameComboBox = new System.Windows.Forms.ComboBox();
            this._breakButton = new System.Windows.Forms.Button();
            this._resumeButton = new System.Windows.Forms.Button();
            this._clearOutputButton = new System.Windows.Forms.Button();
            this._updateExecutionOptionsStateTimer = new System.Windows.Forms.Timer(this.components);
            this._serverHeartbeatTimer = new System.Windows.Forms.Timer(this.components);
            this._suspendedAtStepTextBox = new System.Windows.Forms.TextBox();
            this._suspendedAtStepLabel = new System.Windows.Forms.Label();
            this._listThreadsButton = new System.Windows.Forms.Button();
            this._terminateButton = new System.Windows.Forms.Button();
            this._stationGlobalsListView = new System.Windows.Forms.ListView();
            this._nameColumn = new System.Windows.Forms.ColumnHeader();
            this._valueColumn = new System.Windows.Forms.ColumnHeader();
            this._typeColumn = new System.Windows.Forms.ColumnHeader();
            this._selectedItemValue = new System.Windows.Forms.Label();
            this._valueTextBox = new System.Windows.Forms.TextBox();
            this._addGlobalButton = new System.Windows.Forms.Button();
            this._deleteGlobalButton = new System.Windows.Forms.Button();
            this._commitGlobalsToDiskButton = new System.Windows.Forms.Button();
            this._booleanValueComboBox = new System.Windows.Forms.ComboBox();
            this._activeProcessModelLabel = new System.Windows.Forms.Label();
            this._stationModelComboBox = new System.Windows.Forms.ComboBox();
            this._numberOfTestSocketsLabel = new System.Windows.Forms.Label();
            this._numTestSocketsNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this._executionTraceMessagesTextBox = new System.Windows.Forms.RichTextBox();
            this._clearExecutionTraceMessagesButton = new System.Windows.Forms.Button();
            this._executionTraceMessagesLabel = new System.Windows.Forms.Label();
            this._showTracingCheckBox = new System.Windows.Forms.CheckBox();
            this._connectionStatusLabel = new System.Windows.Forms.Label();
            this._connectionStatusPictureBox = new System.Windows.Forms.PictureBox();
            this._connectionStatusDescriptionLabel = new System.Windows.Forms.Label();
            this._executionStatePictureBox = new System.Windows.Forms.PictureBox();
            this._executionStateDescriptionLabel = new System.Windows.Forms.Label();
            this._remoteStationLabel = new System.Windows.Forms.Label();
            this._stationGlobalsSectionLabel = new System.Windows.Forms.Label();
            this._verticalLine = new ExampleClient.Line();
            this._outputLabel = new System.Windows.Forms.Label();
            this._horizontalLine = new ExampleClient.Line();
            this._serverAddressPanel = new System.Windows.Forms.Panel();
            this._connectionTypePictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this._numTestSocketsNumericUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._connectionStatusPictureBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._executionStatePictureBox)).BeginInit();
            this._serverAddressPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._connectionTypePictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // _logTextBox
            // 
            this._logTextBox.BackColor = System.Drawing.SystemColors.Window;
            this._logTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._logTextBox.Location = new System.Drawing.Point(388, 374);
            this._logTextBox.MaxLength = 0;
            this._logTextBox.Name = "_logTextBox";
            this._logTextBox.ReadOnly = true;
            this._logTextBox.Size = new System.Drawing.Size(721, 197);
            this._logTextBox.TabIndex = 42;
            this._logTextBox.Text = "";
            // 
            // _logLabel
            // 
            this._logLabel.AutoSize = true;
            this._logLabel.Location = new System.Drawing.Point(388, 346);
            this._logLabel.Name = "_logLabel";
            this._logLabel.Size = new System.Drawing.Size(27, 15);
            this._logLabel.TabIndex = 40;
            this._logLabel.Text = "Log";
            // 
            // _runSequenceFileButton
            // 
            this._runSequenceFileButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._runSequenceFileButton.Location = new System.Drawing.Point(388, 20);
            this._runSequenceFileButton.Name = "_runSequenceFileButton";
            this._runSequenceFileButton.Size = new System.Drawing.Size(191, 25);
            this._runSequenceFileButton.TabIndex = 25;
            this._runSequenceFileButton.Text = "Run Remote Sequence File";
            this._runSequenceFileButton.UseVisualStyleBackColor = true;
            this._runSequenceFileButton.Click += new System.EventHandler(this.OnRunSequenceFileButtonClick);
            // 
            // _processModelComboBox
            // 
            this._processModelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._processModelComboBox.FormattingEnabled = true;
            this._processModelComboBox.ItemHeight = 15;
            this._processModelComboBox.Items.AddRange(new object[] {
            "Use Station Model",
            "Sequential",
            "Batch",
            "Parallel",
            "None"});
            this._processModelComboBox.Location = new System.Drawing.Point(173, 163);
            this._processModelComboBox.Name = "_processModelComboBox";
            this._processModelComboBox.Size = new System.Drawing.Size(174, 23);
            this._processModelComboBox.TabIndex = 8;
            this._processModelComboBox.SelectedIndexChanged += OnProcessModelComboBoxSelectedIndexChanged;
            // 
            // _serverAddressTextBox
            // 
            this._serverAddressTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._serverAddressTextBox.Location = new System.Drawing.Point(23, 3);
            this._serverAddressTextBox.Name = "_serverAddressTextBox";
            this._serverAddressTextBox.PlaceholderText = "Server Name/IP";
            this._serverAddressTextBox.Size = new System.Drawing.Size(127, 16);
            this._serverAddressTextBox.TabIndex = 3;
            this._serverAddressTextBox.Tag = "";
            this._serverAddressTextBox.Text = "127.0.0.1";
            // 
            // _serverAddressLabel
            // 
            this._serverAddressLabel.AutoSize = true;
            this._serverAddressLabel.Location = new System.Drawing.Point(20, 52);
            this._serverAddressLabel.Name = "_serverAddressLabel";
            this._serverAddressLabel.Size = new System.Drawing.Size(84, 15);
            this._serverAddressLabel.TabIndex = 2;
            this._serverAddressLabel.Text = "Server Address";
            // 
            // _connectButton
            // 
            this._connectButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._connectButton.Location = new System.Drawing.Point(270, 51);
            this._connectButton.Name = "_connectButton";
            this._connectButton.Size = new System.Drawing.Size(77, 25);
            this._connectButton.TabIndex = 4;
            this._connectButton.Text = "Connect";
            this._connectButton.UseVisualStyleBackColor = true;
            this._connectButton.Click += new System.EventHandler(this.OnConnectButtonClick);
            // 
            // _sequenceFileNameLabel
            // 
            this._sequenceFileNameLabel.AutoSize = true;
            this._sequenceFileNameLabel.Location = new System.Drawing.Point(20, 127);
            this._sequenceFileNameLabel.Name = "_sequenceFileNameLabel";
            this._sequenceFileNameLabel.Size = new System.Drawing.Size(79, 15);
            this._sequenceFileNameLabel.TabIndex = 11;
            this._sequenceFileNameLabel.Text = "Sequence File";
            // 
            // _entryPointComboBox
            // 
            this._entryPointComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._entryPointComboBox.FormattingEnabled = true;
            this._entryPointComboBox.ItemHeight = 15;
            this._entryPointComboBox.Items.AddRange(new object[] {
            "Single Pass",
            "Test UUTs"});
            this._entryPointComboBox.Location = new System.Drawing.Point(173, 241);
            this._entryPointComboBox.Name = "_entryPointComboBox";
            this._entryPointComboBox.Size = new System.Drawing.Size(174, 23);
            this._entryPointComboBox.TabIndex = 10;
            // 
            // _modelLabel
            // 
            this._modelLabel.AutoSize = true;
            this._modelLabel.Location = new System.Drawing.Point(20, 166);
            this._modelLabel.Name = "_modelLabel";
            this._modelLabel.Size = new System.Drawing.Size(41, 15);
            this._modelLabel.TabIndex = 7;
            this._modelLabel.Text = "Model";
            // 
            // _entryPointLabel
            // 
            this._entryPointLabel.AutoSize = true;
            this._entryPointLabel.Location = new System.Drawing.Point(20, 244);
            this._entryPointLabel.Name = "_entryPointLabel";
            this._entryPointLabel.Size = new System.Drawing.Size(65, 15);
            this._entryPointLabel.TabIndex = 9;
            this._entryPointLabel.Text = "Entry Point";
            // 
            // _sequenceFileNameComboBox
            // 
            this._sequenceFileNameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._sequenceFileNameComboBox.FormattingEnabled = true;
            this._sequenceFileNameComboBox.ItemHeight = 15;
            this._sequenceFileNameComboBox.Items.AddRange(new object[] {
            "test.seq",
            "LoopForever.seq",
            "TraceExecution.seq",
            "LoadError.seq",
            "RunTimeError.seq",
            "StepResults.seq"});
            this._sequenceFileNameComboBox.Location = new System.Drawing.Point(173, 124);
            this._sequenceFileNameComboBox.Name = "_sequenceFileNameComboBox";
            this._sequenceFileNameComboBox.Size = new System.Drawing.Size(174, 23);
            this._sequenceFileNameComboBox.TabIndex = 12;
            this._sequenceFileNameComboBox.SelectedIndexChanged += new System.EventHandler(this.OnSequenceFileNameComboBoxSelectedIndexChanged);
            // 
            // _breakButton
            // 
            this._breakButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._breakButton.Location = new System.Drawing.Point(388, 61);
            this._breakButton.Name = "_breakButton";
            this._breakButton.Size = new System.Drawing.Size(75, 25);
            this._breakButton.TabIndex = 28;
            this._breakButton.Text = "Break";
            this._breakButton.UseVisualStyleBackColor = true;
            this._breakButton.Click += new System.EventHandler(this.OnBreakButtonClick);
            // 
            // _resumeButton
            // 
            this._resumeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._resumeButton.Location = new System.Drawing.Point(479, 61);
            this._resumeButton.Name = "_resumeButton";
            this._resumeButton.Size = new System.Drawing.Size(75, 25);
            this._resumeButton.TabIndex = 29;
            this._resumeButton.Text = "Resume";
            this._resumeButton.UseVisualStyleBackColor = true;
            this._resumeButton.Click += new System.EventHandler(this.OnResumeButtonClick);
            // 
            // _clearOutputButton
            // 
            this._clearOutputButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._clearOutputButton.Location = new System.Drawing.Point(987, 341);
            this._clearOutputButton.Name = "_clearOutputButton";
            this._clearOutputButton.Size = new System.Drawing.Size(122, 25);
            this._clearOutputButton.TabIndex = 41;
            this._clearOutputButton.Text = "Clear Log";
            this._clearOutputButton.UseVisualStyleBackColor = true;
            this._clearOutputButton.Click += new System.EventHandler(this.OnClearOutputButtonClick);
            // 
            // _updateExecutionOptionsStateTimer
            // 
            this._updateExecutionOptionsStateTimer.Enabled = true;
            this._updateExecutionOptionsStateTimer.Interval = 400;
            this._updateExecutionOptionsStateTimer.Tick += new System.EventHandler(this.OnUpdateExecutionOptionsStateTimerTick);
            // 
            // _serverHeartbeatTimer
            // 
            this._serverHeartbeatTimer.Interval = 1000;
            this._serverHeartbeatTimer.Tick += new System.EventHandler(this.OnServerHeartbeatTimerTick);
            // 
            // _suspendedAtStepTextBox
            // 
            this._suspendedAtStepTextBox.BackColor = System.Drawing.SystemColors.Window;
            this._suspendedAtStepTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._suspendedAtStepTextBox.Location = new System.Drawing.Point(771, 61);
            this._suspendedAtStepTextBox.Multiline = true;
            this._suspendedAtStepTextBox.Name = "_suspendedAtStepTextBox";
            this._suspendedAtStepTextBox.ReadOnly = true;
            this._suspendedAtStepTextBox.Size = new System.Drawing.Size(200, 25);
            this._suspendedAtStepTextBox.TabIndex = 32;
            this._suspendedAtStepTextBox.TabStop = false;
            this._suspendedAtStepTextBox.Tag = "1";
            // 
            // _suspendedAtStepLabel
            // 
            this._suspendedAtStepLabel.AutoSize = true;
            this._suspendedAtStepLabel.Location = new System.Drawing.Point(661, 63);
            this._suspendedAtStepLabel.Name = "_suspendedAtStepLabel";
            this._suspendedAtStepLabel.Size = new System.Drawing.Size(104, 15);
            this._suspendedAtStepLabel.TabIndex = 31;
            this._suspendedAtStepLabel.Text = "Suspended at Step";
            // 
            // _listThreadsButton
            // 
            this._listThreadsButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._listThreadsButton.Location = new System.Drawing.Point(987, 61);
            this._listThreadsButton.Name = "_listThreadsButton";
            this._listThreadsButton.Size = new System.Drawing.Size(122, 25);
            this._listThreadsButton.TabIndex = 33;
            this._listThreadsButton.Text = "List Threads";
            this._listThreadsButton.UseVisualStyleBackColor = true;
            this._listThreadsButton.Click += new System.EventHandler(this.OnListThreadsButtonClick);
            // 
            // _terminateButton
            // 
            this._terminateButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._terminateButton.Location = new System.Drawing.Point(570, 61);
            this._terminateButton.Name = "_terminateButton";
            this._terminateButton.Size = new System.Drawing.Size(75, 25);
            this._terminateButton.TabIndex = 30;
            this._terminateButton.Text = "Terminate";
            this._terminateButton.UseVisualStyleBackColor = true;
            this._terminateButton.Click += new System.EventHandler(this.OnTerminateButtonClick);
            // 
            // _stationGlobalsListView
            // 
            this._stationGlobalsListView.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._stationGlobalsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._nameColumn,
            this._valueColumn,
            this._typeColumn});
            this._stationGlobalsListView.Enabled = false;
            this._stationGlobalsListView.FullRowSelect = true;
            this._stationGlobalsListView.GridLines = true;
            this._stationGlobalsListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._stationGlobalsListView.HideSelection = false;
            this._stationGlobalsListView.LabelWrap = false;
            this._stationGlobalsListView.Location = new System.Drawing.Point(20, 374);
            this._stationGlobalsListView.MultiSelect = false;
            this._stationGlobalsListView.Name = "_stationGlobalsListView";
            this._stationGlobalsListView.ShowGroups = false;
            this._stationGlobalsListView.Size = new System.Drawing.Size(327, 119);
            this._stationGlobalsListView.TabIndex = 20;
            this._stationGlobalsListView.UseCompatibleStateImageBehavior = false;
            this._stationGlobalsListView.View = System.Windows.Forms.View.Details;
            this._stationGlobalsListView.SelectedIndexChanged += new System.EventHandler(this.OnStationGlobalsListViewSelectedIndexChanged);
            // 
            // _nameColumn
            // 
            this._nameColumn.Name = "_nameColumn";
            this._nameColumn.Text = "NAME";
            this._nameColumn.Width = 140;
            // 
            // _valueColumn
            // 
            this._valueColumn.Name = "_valueColumn";
            this._valueColumn.Text = "VALUE";
            this._valueColumn.Width = 100;
            // 
            // _typeColumn
            // 
            this._typeColumn.Name = "_typeColumn";
            this._typeColumn.Text = "TYPE";
            this._typeColumn.Width = 85;
            // 
            // _selectedItemValue
            // 
            this._selectedItemValue.AutoSize = true;
            this._selectedItemValue.Location = new System.Drawing.Point(20, 512);
            this._selectedItemValue.Name = "_selectedItemValue";
            this._selectedItemValue.Size = new System.Drawing.Size(109, 15);
            this._selectedItemValue.TabIndex = 21;
            this._selectedItemValue.Text = "Selected Item Value";
            // 
            // _valueTextBox
            // 
            this._valueTextBox.Enabled = false;
            this._valueTextBox.Location = new System.Drawing.Point(173, 509);
            this._valueTextBox.Name = "_valueTextBox";
            this._valueTextBox.Size = new System.Drawing.Size(174, 23);
            this._valueTextBox.TabIndex = 23;
            this._valueTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnValueTextBoxKeyDown);
            this._valueTextBox.Leave += new System.EventHandler(this.OnValueTextBoxLeave);
            // 
            // _addGlobalButton
            // 
            this._addGlobalButton.Enabled = false;
            this._addGlobalButton.FlatAppearance.BorderSize = 0;
            this._addGlobalButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._addGlobalButton.Location = new System.Drawing.Point(20, 350);
            this._addGlobalButton.Name = "_addGlobalButton";
            this._addGlobalButton.Size = new System.Drawing.Size(16, 16);
            this._addGlobalButton.TabIndex = 18;
            this._addGlobalButton.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this._addGlobalButton.UseVisualStyleBackColor = true;
            this._addGlobalButton.Click += new System.EventHandler(this.OnAddStationGlobalClick);
            // 
            // _deleteGlobalButton
            // 
            this._deleteGlobalButton.Enabled = false;
            this._deleteGlobalButton.FlatAppearance.BorderSize = 0;
            this._deleteGlobalButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._deleteGlobalButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._deleteGlobalButton.Location = new System.Drawing.Point(46, 350);
            this._deleteGlobalButton.Name = "_deleteGlobalButton";
            this._deleteGlobalButton.Size = new System.Drawing.Size(16, 16);
            this._deleteGlobalButton.TabIndex = 19;
            this._deleteGlobalButton.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this._deleteGlobalButton.UseVisualStyleBackColor = true;
            this._deleteGlobalButton.Click += new System.EventHandler(this.OnDeleteStationGlobalClick);
            // 
            // _commitGlobalsToDiskButton
            // 
            this._commitGlobalsToDiskButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._commitGlobalsToDiskButton.Location = new System.Drawing.Point(20, 548);
            this._commitGlobalsToDiskButton.Name = "_commitGlobalsToDiskButton";
            this._commitGlobalsToDiskButton.Size = new System.Drawing.Size(327, 23);
            this._commitGlobalsToDiskButton.TabIndex = 23;
            this._commitGlobalsToDiskButton.Text = "Commit Globals To Disk";
            this._commitGlobalsToDiskButton.UseVisualStyleBackColor = true;
            this._commitGlobalsToDiskButton.Click += new System.EventHandler(this.OnCommitGlobalsToDiskClick);
            // 
            // _booleanValueComboBox
            // 
            this._booleanValueComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._booleanValueComboBox.FormattingEnabled = true;
            this._booleanValueComboBox.Items.AddRange(new object[] {
            "False",
            "True"});
            this._booleanValueComboBox.Location = new System.Drawing.Point(173, 509);
            this._booleanValueComboBox.Name = "_booleanValueComboBox";
            this._booleanValueComboBox.Size = new System.Drawing.Size(174, 23);
            this._booleanValueComboBox.TabIndex = 22;
            this._booleanValueComboBox.Visible = false;
            this._booleanValueComboBox.SelectionChangeCommitted += new System.EventHandler(this.OnBooleanValueComboBoxSelectionChangedCommitted);
            // 
            // _activeProcessModelLabel
            // 
            this._activeProcessModelLabel.AutoSize = true;
            this._activeProcessModelLabel.Enabled = false;
            this._activeProcessModelLabel.Location = new System.Drawing.Point(22, 205);
            this._activeProcessModelLabel.Name = "_activeProcessModelLabel";
            this._activeProcessModelLabel.Size = new System.Drawing.Size(81, 15);
            this._activeProcessModelLabel.TabIndex = 13;
            this._activeProcessModelLabel.Text = "Station Model";
            // 
            // _stationModelComboBox
            // 
            this._stationModelComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._stationModelComboBox.Enabled = false;
            this._stationModelComboBox.FormattingEnabled = true;
            this._stationModelComboBox.Location = new System.Drawing.Point(173, 202);
            this._stationModelComboBox.Name = "_stationModelComboBox";
            this._stationModelComboBox.Size = new System.Drawing.Size(174, 23);
            this._stationModelComboBox.TabIndex = 14;
            this._stationModelComboBox.SelectedIndexChanged += new System.EventHandler(this.OnStationModelComboBoxSelectedIndexChanged);
            // 
            // _numberOfTestSocketsLabel
            // 
            this._numberOfTestSocketsLabel.AutoSize = true;
            this._numberOfTestSocketsLabel.Enabled = false;
            this._numberOfTestSocketsLabel.Location = new System.Drawing.Point(20, 282);
            this._numberOfTestSocketsLabel.Name = "_numberOfTestSocketsLabel";
            this._numberOfTestSocketsLabel.Size = new System.Drawing.Size(133, 15);
            this._numberOfTestSocketsLabel.TabIndex = 15;
            this._numberOfTestSocketsLabel.Text = "Number Of Test Sockets";
            // 
            // _numTestSocketsNumericUpDown
            // 
            this._numTestSocketsNumericUpDown.Enabled = false;
            this._numTestSocketsNumericUpDown.Location = new System.Drawing.Point(173, 280);
            this._numTestSocketsNumericUpDown.Name = "_numTestSocketsNumericUpDown";
            this._numTestSocketsNumericUpDown.Size = new System.Drawing.Size(174, 23);
            this._numTestSocketsNumericUpDown.TabIndex = 16;
            this._numTestSocketsNumericUpDown.ValueChanged += new System.EventHandler(this.OnNumTestSocketNumericUpDownValueChanged);
            // 
            // _executionTraceMessagesTextBox
            // 
            this._executionTraceMessagesTextBox.BackColor = System.Drawing.SystemColors.Window;
            this._executionTraceMessagesTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._executionTraceMessagesTextBox.Location = new System.Drawing.Point(388, 176);
            this._executionTraceMessagesTextBox.MaxLength = 0;
            this._executionTraceMessagesTextBox.Name = "_executionTraceMessagesTextBox";
            this._executionTraceMessagesTextBox.ReadOnly = true;
            this._executionTraceMessagesTextBox.Size = new System.Drawing.Size(721, 153);
            this._executionTraceMessagesTextBox.TabIndex = 39;
            this._executionTraceMessagesTextBox.Text = "";
            // 
            // _clearExecutionTraceMessagesButton
            // 
            this._clearExecutionTraceMessagesButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._clearExecutionTraceMessagesButton.Location = new System.Drawing.Point(987, 143);
            this._clearExecutionTraceMessagesButton.Name = "_clearExecutionTraceMessagesButton";
            this._clearExecutionTraceMessagesButton.Size = new System.Drawing.Size(122, 25);
            this._clearExecutionTraceMessagesButton.TabIndex = 38;
            this._clearExecutionTraceMessagesButton.Text = "Clear Messages";
            this._clearExecutionTraceMessagesButton.UseVisualStyleBackColor = true;
            this._clearExecutionTraceMessagesButton.Click += new System.EventHandler(this.OnClearExecutionTraceMessagesButtonClick);
            // 
            // _executionTraceMessagesLabel
            // 
            this._executionTraceMessagesLabel.AutoSize = true;
            this._executionTraceMessagesLabel.Location = new System.Drawing.Point(388, 148);
            this._executionTraceMessagesLabel.Name = "_executionTraceMessagesLabel";
            this._executionTraceMessagesLabel.Size = new System.Drawing.Size(143, 15);
            this._executionTraceMessagesLabel.TabIndex = 36;
            this._executionTraceMessagesLabel.Text = "Execution Trace Messages";
            // 
            // _showTracingCheckBox
            // 
            this._showTracingCheckBox.AutoSize = true;
            this._showTracingCheckBox.Checked = true;
            this._showTracingCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this._showTracingCheckBox.Location = new System.Drawing.Point(875, 147);
            this._showTracingCheckBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this._showTracingCheckBox.Name = "_showTracingCheckBox";
            this._showTracingCheckBox.Size = new System.Drawing.Size(96, 19);
            this._showTracingCheckBox.TabIndex = 37;
            this._showTracingCheckBox.Text = "Show Tracing";
            this._showTracingCheckBox.UseVisualStyleBackColor = true;
            this._showTracingCheckBox.CheckStateChanged += new System.EventHandler(this.OnShowTracingCheckBoxCheckStateChanged);
            // 
            // _connectionStatusLabel
            // 
            this._connectionStatusLabel.AutoSize = true;
            this._connectionStatusLabel.Location = new System.Drawing.Point(20, 92);
            this._connectionStatusLabel.Name = "_connectionStatusLabel";
            this._connectionStatusLabel.Size = new System.Drawing.Size(39, 15);
            this._connectionStatusLabel.TabIndex = 5;
            this._connectionStatusLabel.Text = "Status";
            // 
            // _connectionStatusPictureBox
            // 
            this._connectionStatusPictureBox.Location = new System.Drawing.Point(173, 92);
            this._connectionStatusPictureBox.Name = "_connectionStatusPictureBox";
            this._connectionStatusPictureBox.Size = new System.Drawing.Size(16, 16);
            this._connectionStatusPictureBox.TabIndex = 37;
            this._connectionStatusPictureBox.TabStop = false;
            // 
            // _connectionStatusDescriptionLabel
            // 
            this._connectionStatusDescriptionLabel.AutoSize = true;
            this._connectionStatusDescriptionLabel.Location = new System.Drawing.Point(192, 93);
            this._connectionStatusDescriptionLabel.Name = "_connectionStatusDescriptionLabel";
            this._connectionStatusDescriptionLabel.Size = new System.Drawing.Size(88, 15);
            this._connectionStatusDescriptionLabel.TabIndex = 6;
            this._connectionStatusDescriptionLabel.Text = "Not Connected";
            // 
            // _executionStatePictureBox
            // 
            this._executionStatePictureBox.Location = new System.Drawing.Point(597, 25);
            this._executionStatePictureBox.Name = "_executionStatePictureBox";
            this._executionStatePictureBox.Size = new System.Drawing.Size(16, 16);
            this._executionStatePictureBox.TabIndex = 39;
            this._executionStatePictureBox.TabStop = false;
            // 
            // _executionStateDescriptionLabel
            // 
            this._executionStateDescriptionLabel.Location = new System.Drawing.Point(617, 25);
            this._executionStateDescriptionLabel.Name = "_executionStateDescriptionLabel";
            this._executionStateDescriptionLabel.Size = new System.Drawing.Size(191, 15);
            this._executionStateDescriptionLabel.TabIndex = 27;
            this._executionStateDescriptionLabel.Text = "Not Executed";
            // 
            // _remoteStationLabel
            // 
            this._remoteStationLabel.AutoSize = true;
            this._remoteStationLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this._remoteStationLabel.Location = new System.Drawing.Point(20, 20);
            this._remoteStationLabel.Name = "_remoteStationLabel";
            this._remoteStationLabel.Size = new System.Drawing.Size(95, 15);
            this._remoteStationLabel.TabIndex = 1;
            this._remoteStationLabel.Text = "Remote Station";
            // 
            // _stationGlobalsSectionLabel
            // 
            this._stationGlobalsSectionLabel.AutoSize = true;
            this._stationGlobalsSectionLabel.Location = new System.Drawing.Point(20, 319);
            this._stationGlobalsSectionLabel.Name = "_stationGlobalsSectionLabel";
            this._stationGlobalsSectionLabel.Size = new System.Drawing.Size(86, 15);
            this._stationGlobalsSectionLabel.TabIndex = 17;
            this._stationGlobalsSectionLabel.Text = "Station Globals";
            // 
            // _verticalLine
            // 
            this._verticalLine.IsVertical = true;
            this._verticalLine.Location = new System.Drawing.Point(367, 20);
            this._verticalLine.Name = "_verticalLine";
            this._verticalLine.Size = new System.Drawing.Size(1, 551);
            this._verticalLine.TabIndex = 35;
            this._verticalLine.TabStop = false;
            this._verticalLine.Thickness = 1;
            // 
            // _outputLabel
            // 
            this._outputLabel.AutoSize = true;
            this._outputLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this._outputLabel.Location = new System.Drawing.Point(388, 122);
            this._outputLabel.Name = "_outputLabel";
            this._outputLabel.Size = new System.Drawing.Size(47, 15);
            this._outputLabel.TabIndex = 24;
            this._outputLabel.Text = "Output";
            // 
            // _horizontalLine
            // 
            this._horizontalLine.IsVertical = false;
            this._horizontalLine.Location = new System.Drawing.Point(388, 106);
            this._horizontalLine.Name = "_horizontalLine";
            this._horizontalLine.Size = new System.Drawing.Size(721, 1);
            this._horizontalLine.TabIndex = 34;
            this._horizontalLine.TabStop = false;
            this._horizontalLine.Thickness = 1;
            // 
            // _serverAddressPanel
            // 
            this._serverAddressPanel.BackColor = System.Drawing.SystemColors.Window;
            this._serverAddressPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._serverAddressPanel.Controls.Add(this._connectionTypePictureBox);
            this._serverAddressPanel.Controls.Add(this._serverAddressTextBox);
            this._serverAddressPanel.Location = new System.Drawing.Point(110, 51);
            this._serverAddressPanel.Name = "_serverAddressPanel";
            this._serverAddressPanel.Size = new System.Drawing.Size(154, 25);
            this._serverAddressPanel.TabIndex = 43;
            // 
            // _connectionTypePictureBox
            // 
            this._connectionTypePictureBox.Location = new System.Drawing.Point(4, 3);
            this._connectionTypePictureBox.Name = "_connectionTypePictureBox";
            this._connectionTypePictureBox.Size = new System.Drawing.Size(16, 16);
            this._connectionTypePictureBox.TabIndex = 4;
            this._connectionTypePictureBox.TabStop = false;
            // 
            // Example
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1129, 591);
            this.Controls.Add(this._serverAddressPanel);
            this.Controls.Add(this._horizontalLine);
            this.Controls.Add(this._outputLabel);
            this.Controls.Add(this._verticalLine);
            this.Controls.Add(this._stationGlobalsSectionLabel);
            this.Controls.Add(this._remoteStationLabel);
            this.Controls.Add(this._listThreadsButton);
            this.Controls.Add(this._deleteGlobalButton);
            this.Controls.Add(this._suspendedAtStepTextBox);
            this.Controls.Add(this._suspendedAtStepLabel);
            this.Controls.Add(this._booleanValueComboBox);
            this.Controls.Add(this._addGlobalButton);
            this.Controls.Add(this._terminateButton);
            this.Controls.Add(this._commitGlobalsToDiskButton);
            this.Controls.Add(this._resumeButton);
            this.Controls.Add(this._stationGlobalsListView);
            this.Controls.Add(this._breakButton);
            this.Controls.Add(this._valueTextBox);
            this.Controls.Add(this._executionStateDescriptionLabel);
            this.Controls.Add(this._executionStatePictureBox);
            this.Controls.Add(this._selectedItemValue);
            this.Controls.Add(this._connectionStatusDescriptionLabel);
            this.Controls.Add(this._connectionStatusPictureBox);
            this.Controls.Add(this._connectionStatusLabel);
            this.Controls.Add(this._showTracingCheckBox);
            this.Controls.Add(this._clearExecutionTraceMessagesButton);
            this.Controls.Add(this._executionTraceMessagesLabel);
            this.Controls.Add(this._executionTraceMessagesTextBox);
            this.Controls.Add(this._numTestSocketsNumericUpDown);
            this.Controls.Add(this._numberOfTestSocketsLabel);
            this.Controls.Add(this._stationModelComboBox);
            this.Controls.Add(this._activeProcessModelLabel);
            this.Controls.Add(this._clearOutputButton);
            this.Controls.Add(this._sequenceFileNameComboBox);
            this.Controls.Add(this._entryPointLabel);
            this.Controls.Add(this._modelLabel);
            this.Controls.Add(this._entryPointComboBox);
            this.Controls.Add(this._sequenceFileNameLabel);
            this.Controls.Add(this._connectButton);
            this.Controls.Add(this._serverAddressLabel);
            this.Controls.Add(this._processModelComboBox);
            this.Controls.Add(this._runSequenceFileButton);
            this.Controls.Add(this._logLabel);
            this.Controls.Add(this._logTextBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Example";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Example TestStand API Client Application";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.OnClosing);
            this.Load += new System.EventHandler(this.OnLoad);
            ((System.ComponentModel.ISupportInitialize)(this._numTestSocketsNumericUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._connectionStatusPictureBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._executionStatePictureBox)).EndInit();
            this._serverAddressPanel.ResumeLayout(false);
            this._serverAddressPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._connectionTypePictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

        #endregion

        private System.Windows.Forms.RichTextBox _logTextBox;
		private System.Windows.Forms.Label _logLabel;
		private System.Windows.Forms.Button _runSequenceFileButton;
		private System.Windows.Forms.ComboBox _processModelComboBox;
		private System.Windows.Forms.TextBox _serverAddressTextBox;
		private System.Windows.Forms.Label _serverAddressLabel;
		private System.Windows.Forms.Button _connectButton;
		private System.Windows.Forms.Label _sequenceFileNameLabel;
		private System.Windows.Forms.ComboBox _entryPointComboBox;
		private System.Windows.Forms.Label _modelLabel;
		private System.Windows.Forms.Label _entryPointLabel;
		private System.Windows.Forms.ComboBox _sequenceFileNameComboBox;
		private System.Windows.Forms.Button _breakButton;
		private System.Windows.Forms.Button _resumeButton;
		private System.Windows.Forms.Button _clearOutputButton;
		private System.Windows.Forms.Timer _updateExecutionOptionsStateTimer;
        private System.Windows.Forms.Timer _serverHeartbeatTimer;
        private System.Windows.Forms.TextBox _suspendedAtStepTextBox;
		private System.Windows.Forms.Label _suspendedAtStepLabel;
        private System.Windows.Forms.ListView _stationGlobalsListView;
        private System.Windows.Forms.ColumnHeader _nameColumn;
        private System.Windows.Forms.ColumnHeader _valueColumn;
        private System.Windows.Forms.Label _selectedItemValue;
        private System.Windows.Forms.TextBox _valueTextBox;
        private System.Windows.Forms.Button _addGlobalButton;
        private System.Windows.Forms.Button _deleteGlobalButton;
        private System.Windows.Forms.ColumnHeader _typeColumn;
        private System.Windows.Forms.ComboBox _booleanValueComboBox;
        private System.Windows.Forms.Button _commitGlobalsToDiskButton;
        private System.Windows.Forms.Label _activeProcessModelLabel;
        private System.Windows.Forms.ComboBox _stationModelComboBox;
        private System.Windows.Forms.Label _numberOfTestSocketsLabel;
        private System.Windows.Forms.NumericUpDown _numTestSocketsNumericUpDown;
        private System.Windows.Forms.RichTextBox _executionTraceMessagesTextBox;
        private System.Windows.Forms.Button _clearExecutionTraceMessagesButton;
        private System.Windows.Forms.Label _executionTraceMessagesLabel;
		private System.Windows.Forms.Button _listThreadsButton;
		private System.Windows.Forms.Button _terminateButton;
        private System.Windows.Forms.CheckBox _showTracingCheckBox;
        private System.Windows.Forms.Label _connectionStatusLabel;
        private System.Windows.Forms.PictureBox _connectionStatusPictureBox;
        private System.Windows.Forms.Label _connectionStatusDescriptionLabel;
        private System.Windows.Forms.PictureBox _executionStatePictureBox;
        private System.Windows.Forms.Label _executionStateDescriptionLabel;
        private System.Windows.Forms.Label _remoteStationLabel;
        private System.Windows.Forms.Label _stationGlobalsSectionLabel;
        private Line _verticalLine;
        private System.Windows.Forms.Label _outputLabel;
        private Line _horizontalLine;
        private System.Windows.Forms.Panel _serverAddressPanel;
        private System.Windows.Forms.PictureBox _connectionTypePictureBox;
    }
}

