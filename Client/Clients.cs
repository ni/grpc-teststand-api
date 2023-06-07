using System.Threading.Tasks;
using Grpc.Net.Client;
using NationalInstruments.TestStand.API.Grpc;
using NationalInstruments.TestStand.Grpc.Client.Utilities;
using NationalInstruments.TestStand.Grpc.Net.Client.OO;
using NationalInstruments.TestStand.UI.Grpc;

namespace ExampleClient
{
    public class Clients
    {
        private GrpcChannel _gRPCChannel = null;
        private readonly CreateChannelHelper _channelHelper = new();

        public Clients()
        {
        }

        public bool OpenChannel(string serverAddress, ClientOptions options, out bool connectionIsSecured, out string connectionErrors)
        {
            _gRPCChannel = _channelHelper.OpenChannel(serverAddress, options, out string _, out connectionIsSecured, out connectionErrors);
            if (HasChannel)
            {
                InitializeClients();
            }
            return HasChannel;
        }

        internal async Task ShutdownAsync()
        {
            await _gRPCChannel.ShutdownAsync();
            _gRPCChannel = null;
        }

        public bool HasChannel => _gRPCChannel is not null;

        public InstanceLifetime.InstanceLifetimeClient InstanceLifetimeClient { get; private set; }
        public Engine.EngineClient EngineClient { get; private set; }
        public Step.StepClient StepClient { get; private set; }
        public Execution.ExecutionClient ExecutionClient { get; private set; }
        public Report.ReportClient ReportClient { get; private set; }
        public Thread.ThreadClient ThreadClient { get; private set; }
        public SequenceContext.SequenceContextClient SequenceContextClient { get; private set; }
        public PropertyObject.PropertyObjectClient PropertyObjectClient { get; private set; }
        public PropertyObjectFile.PropertyObjectFileClient PropertyObjectFileClient { get; private set; }
        public StationOptions.StationOptionsClient StationOptionsClient { get; private set; }
        public ApplicationMgr.ApplicationMgrClient ApplicationMgrClient { get; private set; }
        public Executions.ExecutionsClient ExecutionsClient { get; private set; }
        public UIMessage.UIMessageClient UiMessageClient { get; private set; }
        public StepProperties.StepPropertiesClient StepPropertiesClient { get; private set; }

        private void InitializeClients()
        {
            // clients for TestStand API interfaces we want to use
            EngineClient = new Engine.EngineClient(_gRPCChannel);
            StepClient = new Step.StepClient(_gRPCChannel);
            ExecutionClient = new Execution.ExecutionClient(_gRPCChannel);
            ReportClient = new Report.ReportClient(_gRPCChannel);
            ThreadClient = new Thread.ThreadClient(_gRPCChannel);
            SequenceContextClient = new SequenceContext.SequenceContextClient(_gRPCChannel);
            PropertyObjectClient = new PropertyObject.PropertyObjectClient(_gRPCChannel);
            PropertyObjectFileClient = new PropertyObjectFile.PropertyObjectFileClient(_gRPCChannel);
            StationOptionsClient = new StationOptions.StationOptionsClient(_gRPCChannel);
            ApplicationMgrClient = new ApplicationMgr.ApplicationMgrClient(_gRPCChannel);
            ExecutionsClient = new Executions.ExecutionsClient(_gRPCChannel);
            UiMessageClient = new UIMessage.UIMessageClient(_gRPCChannel);
            StepPropertiesClient = new StepProperties.StepPropertiesClient(_gRPCChannel);

            // client for the Instance Lifetime API, which lets you tell the server when your client doesn't need specific objects on the server any longer
            InstanceLifetimeClient = new InstanceLifetime.InstanceLifetimeClient(_gRPCChannel);
        }
    }
}
