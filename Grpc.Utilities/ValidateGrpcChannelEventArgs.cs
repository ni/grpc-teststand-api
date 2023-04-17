using Grpc.Net.Client;

namespace NationalInstruments.TestStand.Grpc.Client.Utilities
{
	public class ValidateGrpcChannelEventArgs
	{
		public GrpcChannel GrpcChannel { get; }
		public bool UseHttps { get; }
		internal string ErrorMessage { get; private set; }

		public ValidateGrpcChannelEventArgs(GrpcChannel grpcChannel, bool useHttps)
		{
			GrpcChannel = grpcChannel;
			UseHttps = useHttps;
		}

		public void SetErrorMessage(string grpcChannelErrorMessage) 
		{
			ErrorMessage = grpcChannelErrorMessage;
		}
	}
}