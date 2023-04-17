using System.Security.Cryptography.X509Certificates;
using Grpc.Core;
using Grpc.Net.Client;
using static System.FormattableString;

namespace NationalInstruments.TestStand.Grpc.Client.Utilities
{
    public class CreateChannelHelper : IDisposable
    {
		public static event EventHandler<ValidateGrpcChannelEventArgs> ValidateGrpcChannel;
		private X509Certificate2 _clientCertificate;
		private X509Certificate2 _serverCertificate;
        private bool _disposedValue;

		public GrpcChannel OpenChannel(
			string serverAddress,
			ClientOptions clientOptions,
			out string fullServerAddress,
			out bool isSecured,
			out string connectionErrors)
		{
			// Start with the connection being secured with no errors.
			isSecured = true;
			connectionErrors = null;

			// The TestStand service we are connecting needs to distinguish each connection.  This is needed
			// to keep track of the lifetime of TestStand objects. For this reason, we need to give the channel
			// a unique connection id. Using a GUID will guarantee a unique connection id.
			string connectionId = Guid.NewGuid().ToString();

			var httpHandler = new HttpClientHandler();
			var httpClient = new HttpClient(httpHandler);

			// The default timeout is 100 seconds (see
			// https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=net-5.0).
			// This will cause gRPC calls to timeout after 100 seconds. This is not acceptable for TestStand.
			// When asynchronously waiting for an execution to complete using WaitForEndExAsync for example,
			// the execution can easily take more than 100 seconds which will cause a timeout error. Also,
			// this app asynchronously listens for a server shutdown event. This will also timeout since the
			// server and the client can run for a long time.  
			//
			// To simplify this example, we are going to have an infinite timeout for all calls.
			httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

			// We determine if the connection is secured based on the existence of certificate files.
			// Alternatively, we can based it on the whether the server address has https or not. However,
			// this will required adding a new option to the client.
			GrpcChannel channel = ConnectUsingHttp(true, httpClient, httpHandler, connectionId, clientOptions, serverAddress, out fullServerAddress, out string httpsErrors);
			if (channel == null)
			{
				isSecured = false;
				channel = ConnectUsingHttp(false, httpClient, httpHandler, connectionId, clientOptions, serverAddress, out fullServerAddress, out string httpErrors);
				if (channel == null)
				{
					connectionErrors = "Connection to server failed with the following errors:\n\n";
					connectionErrors += Invariant($"{httpsErrors}\n\n{httpErrors}");
				}
			}

			return channel;
		}

		private GrpcChannel ConnectUsingHttp(
			bool useHttps,
			HttpClient httpClient,
			HttpClientHandler httpHandler,
			string connectionId,
			ClientOptions clientOptions,
			string serverAddress,
			out string fullServerAddress,
			out string errorMessage)
		{
			ChannelCredentials channelCredentials;
			if (useHttps)
			{
				channelCredentials = CreateSecureChannelCredentials(httpHandler, connectionId, clientOptions);
			}
			else
			{
				channelCredentials = ChannelCredentials.Insecure;

				// Add the "connection-id" http header to every request.
				httpClient.DefaultRequestHeaders.Add("connection-id", connectionId);
			}

			fullServerAddress = useHttps ? "https://" : "http://";
			fullServerAddress += serverAddress + ":" + clientOptions.Port.ToString();
	
			var grpcChannel = GrpcChannel.ForAddress(fullServerAddress, new GrpcChannelOptions
			{
				Credentials = channelCredentials,
				HttpClient = httpClient
			});

			var eventArgs = new ValidateGrpcChannelEventArgs(grpcChannel, useHttps);
			ValidateGrpcChannel?.Invoke(this, eventArgs);
			errorMessage = eventArgs.ErrorMessage;

			if (string.IsNullOrWhiteSpace(eventArgs.ErrorMessage))
			{
				return grpcChannel;
			}

			return null;
		}

		private ChannelCredentials CreateSecureChannelCredentials(HttpClientHandler httpHandler, string connectionId, ClientOptions clientOptions)
		{
			// Only allow calls without a trusted certificate during app development. Production apps should always use trusted certificates.
			// https://docs.microsoft.com/en-us/aspnet/core/grpc/troubleshoot?view=aspnetcore-5.0#call-a-grpc-service-with-an-untrustedinvalid-certificate-1should
			if (!string.IsNullOrEmpty(clientOptions.ServerCertificateFriendlyName))
			{
				_serverCertificate = FindCertificateInCertificatesStore(clientOptions.ServerCertificateFriendlyName);
			}
			else if (!string.IsNullOrEmpty(clientOptions.ServerCertificatePath))
			{
				_serverCertificate = new X509Certificate2(clientOptions.ServerCertificatePath);
			}

			// If no server certificate has been specified, we assume the certificate has been installed in the certificate store.
			// In that case, there is nothing else to do since ASP.NET will do the validation for us. However, if a certificate has
			// been specified, we need to do custom validation in case the certificate is self-signed.
			if (_serverCertificate != null)
			{
				// When using a self-signed certificate and not installing it in the client's Trusted Root Certification
				// Authorities certificate store, we need to validate the server certificate ourselves.  To do this, we
				// need to provide a certificate validation callback method to the HttpClientHandler.
				httpHandler.ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, sslPolicyErrors) =>
				{
					// If there are no errors, there is nothing to do. This is the case when the certifice is trusted.
					if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
					{
						return true;
					}

					// For this example, we use our copy of the server certificate to compare the thumbprint againts the server
					// provided certificate.  If they match, we allow the connection.  For production code, this is not 
					// required when using trusted certificates.
					return certificate.Thumbprint == _serverCertificate.Thumbprint;
				};
			}

			// When doing mutual TLS, a client certificate needs to be provided to server. If there is one, load it and
			// add it to the client certificates.
			bool usesMutualTLS = !string.IsNullOrEmpty(clientOptions.ClientCertificatePFXPath)
				|| (!string.IsNullOrEmpty(clientOptions.ClientCertificatePath) && !string.IsNullOrEmpty(clientOptions.ClientKeyPath));
			if (!string.IsNullOrEmpty(clientOptions.ClientCertificatePFXPath))
			{
				_clientCertificate = new X509Certificate2(clientOptions.ClientCertificatePFXPath, clientOptions.ClientCertificatePFXPassword);
			}
			else if (!string.IsNullOrEmpty(clientOptions.ClientCertificatePath) && !string.IsNullOrEmpty(clientOptions.ClientKeyPath))
			{
				var clientCertificate = X509Certificate2.CreateFromPemFile(clientOptions.ClientCertificatePath, clientOptions.ClientKeyPath);

				// ASP.NET Core apps expect pfx certificates so we need to create one.
				_clientCertificate = new X509Certificate2(clientCertificate.Export(X509ContentType.Pfx));
			}

			if (_clientCertificate != null)
			{
				httpHandler.ClientCertificates.Add(_clientCertificate);
			}
			else if (usesMutualTLS)
			{
				throw new Exception("Client certificate cannot be loaded. Verify certificate exists on disk.");
			}

			// Add the "connection-id" http header to every request.
			var callCredentials = CallCredentials.FromInterceptor(((context, metadata) =>
			{
				metadata.Add("connection-id", connectionId);
				return Task.CompletedTask;
			}));

			return ChannelCredentials.Create(new SslCredentials(), callCredentials);
		}

		private X509Certificate2 FindCertificateInCertificatesStore(string friendlyName)
		{
			foreach (var storeLocation in (StoreLocation[])Enum.GetValues(typeof(StoreLocation)))
			{
				foreach (StoreName storeName in (StoreName[])Enum.GetValues(typeof(StoreName)))
				{
					var store = new X509Store(storeName, storeLocation);

					try
					{
						store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

						foreach (var certificate in store.Certificates)
						{
							// For our example, we are using the friendly name to match the cerfiticate in the certificate store.
							// Other properties can be used to find the certificate.
							if (string.Equals(certificate.FriendlyName, friendlyName, StringComparison.OrdinalIgnoreCase))
							{
								return certificate;
							}
						}
					}
					catch
					{
						// If the store does not exist, an exception is thrown. Ignore the error.
					}
				}
			}

			return null;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_clientCertificate?.Dispose();
					_serverCertificate?.Dispose();
				}

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}