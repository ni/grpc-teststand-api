namespace ExampleClient
{
    internal static class Constants
    {
        public const string SequentialModelFilename = "SequentialModel.seq";
        public const string ModelOptionsFileSectionName = "ModelOptions";
        public const string NumberOfTestSocketsPropertyName = "NumTestSockets";
        public const string RunRemoteSequenceFile = "Run Remote Sequence File";
        public const string RunningRemoteSequenceFile = "Running Remote Sequence File";
        public const string ConnectionStatusConnected = "Connected";
        public const string ConnectionStatusDisconnected = "Disconnected";
        public const string ExecutionStateAborted = "Aborted";
        public const string ExecutionStateError = "Error";
        public const string ExecutionStateFailed = "Failed";
        public const string ExecutionStatePassed = "Passed";
        public const string ExecutionStatePaused = "Paused";
        public const string ExecutionStateRunning = "Running...";
        public const string ExecutionStateTerminated = "Terminated";
        public const string ExecutionStateTerminating = "Terminating...";
        public const string StepResultDone = "Done";
        public const string StepResultSkipped = "Skipped";
        public const string NotExecutedSequenceFile = "Not Executed";
        public const string AddGlobal = "Add Global";
        public const string DeleteGlobal = "Delete Global";
        public const string NotConnected = "NotConnected";
        public const string SecureConnection = "SecureConnection";
        public const string NotSecureConnection = "NotSecureConnection";

        public const int ColorBoxWidth = 3;

        // This is the default TestStand uses in process models.
        public const int DefaultNumberOfTestSockets = 4;

        public const int StatusLength = 7;
        public const int IndentOffsetForOneLevel = 4;
    }
}