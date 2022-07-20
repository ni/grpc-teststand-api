using System.Collections.Generic;
using NationalInstruments.TestStand.API.Grpc;

namespace ExampleClient
{
    public class ExecutionInfo
    {
        public int ExecutionId { get; set; }

        public ExecutionInstance Instance { get; }

        public string Name { get; set; }

        public ExecutionRunStates RunState { get; set; }

        public List<ThreadInfo> Threads { get; } = new();

        public ExecutionInfo(ExecutionInstance instance)
        {
            Instance = instance;
        }
    }

    public class ThreadInfo
    {
        public bool IsController { get; set; } = false;

        public bool IsTestSocket { get; set; } = false;

        public string Name { get; set; }

        public int ParentControllerThreadId { get; set; } = 0;

        public int ParentControllerExecutionId { get; set; } = 0;

        public int SocketIndex { get; set; } = -1;
    }
}
