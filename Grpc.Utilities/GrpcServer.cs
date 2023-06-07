using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NationalInstruments.TestStand.Grpc.Server.Utilities
{
    public class GrpcServer
    {
        public static event EventHandler<IEndpointRouteBuilder> RegisterService;
        private static IHost _host;
        private static ServerOptions _serverOptions;
        private static X509Certificate2 _serverCertificate;
        private static X509Certificate2 _clientCertificate;
        private static ManualResetEvent sServiceInitialized;

        public static void Start(string[] args, bool useSecureConnection)
        {
            string configFilePath = ServerConfiguration.GetDefaultConfigFilePath();
            string certificateDirectoryPath = ServerConfiguration.GetCertificateDirectoryPath();

            Start(args, new ServerConfiguration(useSecureConnection, configFilePath, certificateDirectoryPath));
        }

        public static void Start(string[] args, ServerConfiguration serverConfiguration)
        {
            sServiceInitialized = new ManualResetEvent(false);

            _serverOptions = serverConfiguration.Options;
            _host = CreateHostBuilder(args).Build();
            Task.Run(() => _host.Run());

            // Wait until the server is initialized before returning.
            var seconds = 5;
            if (!sServiceInitialized.WaitOne(1000 * seconds))
            {
                throw new Exception($"Server failed to initialized within {seconds} seconds.");
            }
        }

        public static Process Start(string grpcServiceExecutablePath, bool useSecureConnection)
        {
            if (!File.Exists(grpcServiceExecutablePath))
            {
                throw new FileNotFoundException($"File not found: \"{grpcServiceExecutablePath}\"");
            }

            string arguments = !useSecureConnection ? "-NotSecure" : string.Empty;
            var process = Process.Start(grpcServiceExecutablePath, arguments);
            return process;
        }

        public static void Shutdown()
        {
            _clientCertificate?.Dispose();
            _serverCertificate?.Dispose();

            _host?.StopAsync().Wait();
            _host?.Dispose();
            _host = null;
        }
        public static void Shutdown(Process serverProcess)
        {
            Shutdown();

            serverProcess?.Kill();
            serverProcess?.Dispose();
        }


        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureLogging(logging =>
            {
                // Showing logging messages slowdown the benchmarks so disable them.
                FilterLoggingBuilderExtensions.AddFilter(logging, "Microsoft", LogLevel.None);
                FilterLoggingBuilderExtensions.AddFilter(logging, "Grpc", LogLevel.None);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureKestrel(options =>
                {
                    options.ConfigureHttpsDefaults(configureOptions =>
                    {
                        ConfigureSecureConnectionIfRequired(configureOptions);
                    });

                    options.Listen(IPAddress.Loopback, _serverOptions.Port, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        if (_serverOptions.UseSecureConnection)
                        {
                            listenOptions.UseHttps();
                        }
                    });
                });

                webBuilder.UseStartup<GrpcServer>();
            });

        private static void ConfigureSecureConnectionIfRequired(HttpsConnectionAdapterOptions configureOptions)
        {
            if (!string.IsNullOrEmpty(_serverOptions.ServerCertificateFriendlyName))
            {
                _serverCertificate = FindCertificateInCertificatesStore(_serverOptions.ServerCertificateFriendlyName);
            }
            else if (!string.IsNullOrEmpty(_serverOptions.ServerCertificatePFXPath))
            {
                _serverCertificate = new X509Certificate2(_serverOptions.ServerCertificatePFXPath, _serverOptions.ServerCertificatePFXPassword);
            }
            else if (!string.IsNullOrEmpty(_serverOptions.ServerCertificatePath) && !string.IsNullOrEmpty(_serverOptions.ServerKeyPath))
            {
                var serverCertificate = X509Certificate2.CreateFromPemFile(_serverOptions.ServerCertificatePath, _serverOptions.ServerKeyPath);

                // ASP.NET Core apps expect pfx certificates so we need to create one.
                _serverCertificate = new X509Certificate2(serverCertificate.Export(X509ContentType.Pfx));
            }

            if (_serverCertificate != null)
            {
                // This is where we set the server certificate to authenticate HTTPS connections.
                configureOptions.ServerCertificate = _serverCertificate;

                // If a client certificate has been specified, we need to validate the client. The server needs
                // to verify who is connecting.  This is effectively implementing mutual TLS.
                if (!string.IsNullOrEmpty(_serverOptions.ClientCertificatePath))
                {
                    if (_clientCertificate == null)
                    {
                        _clientCertificate = new X509Certificate2(_serverOptions.ClientCertificatePath);
                        if (_clientCertificate == null)
                        {
                            throw new Exception("Failed to load client certificate for mutual TLS. Verify certificate exists on disk.");
                        }
                    }

                    configureOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;

                    // When using a self-signed certificate for the client and not installing it in the server's
                    // Trusted Root Certification Authorities certificate store, we need to validate the client
                    // certificate ourselves. To do this, we need to provide a certificate validation
                    // callback method.  For production code, it is recommended to use trusted certificates.
                    configureOptions.ClientCertificateValidation = (certificate, chain, sslPolicyErrors) =>
                    {
                        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        {
                            return true;
                        }

                        return certificate.Thumbprint == _clientCertificate.Thumbprint;
                    };
                }
            }
            else
            {
                throw new Exception("Failed to load server's certificate. Make sure certificate exists on disk or it has been added to the machine's certificate store if using a friendly name.");
            }
        }

        private static X509Certificate2 FindCertificateInCertificatesStore(string friendlyName)
        {
            foreach (var storeLocation in (StoreLocation[])System.Enum.GetValues(typeof(StoreLocation)))
            {
                foreach (StoreName storeName in (StoreName[])System.Enum.GetValues(typeof(StoreName)))
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

        public GrpcServer(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                // Accept chained and self-signed certificates
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth?view=aspnetcore-6.0#allowedcertificatetypes--chained-selfsigned-or-all-chained--selfsigned
                options.AllowedCertificateTypes = CertificateTypes.All;
            });

            services.AddGrpc(options =>
            {
                // Set message sizes to unlimited
                options.MaxReceiveMessageSize = null;
                options.MaxSendMessageSize = null;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
        {
            // Microsoft recommends using HTTPS Redirection Middleware.
            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-6.0&tabs=visual-studio#require-https
            app.UseHttpsRedirection();

            app.UseRouting();

            // To enable client certificate authentication, the following two calls are needed.
            // https://learn.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz?view=aspnetcore-6.0
            app.UseAuthentication();
            app.UseAuthorization();

            // Allow user to customize the middleware.

            app.UseEndpoints(endpoints =>
            {
                RegisterService?.Invoke(this, endpoints);
                sServiceInitialized.Set();
            });
        }

        /// <summary>
        /// Validates that the gRPC service identified by the <paramref name="processId"/> is running at the specified <paramref name="port"/>
        /// It checks if port is been used up to 5 times waiting the number of milliseconds specified in <paramref name="waitTimeoutInMilliSeconds"/>
        /// between checks.
        /// </summary>
        /// <param name="port">Port number at which the gRPC service is listening at.</param>
        /// <param name="processId">Process ID of the gRPC service.</param>
        /// <param name="waitTimeoutInMilliSeconds">Wait time to allow gRPC service to start before validation.</param>
        /// <exception cref="Exception"></exception>
        public static void ValidateGrpcServiceIsRunning(int port, int processId, int waitTimeoutInMilliSeconds = 5000)
        {
            // If constant value is changed, make sure to update the summary above.
            const int NumberOfTimesToTryToCheckServerIsRunning = 10;

            var command = "cmd";
            var startInfo = new ProcessStartInfo()
            {
                FileName = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                // Expecting netstat to return: [Proto] [Local Address] [Foreign Address] [State] [PID]
                // Find mathing strings for port, listening and process id.  
                Arguments = $"/C netstat -ano | findstr /l \":{port} \" | findstr /i /l \" LISTENING \"" + ((processId != 0) ? $" | findstr /e /l \"{processId}\"" : string.Empty)
            };

            // need to search for LISTENING or ESTABLISHED. I tried searching for both with findstr, but it seemed to not work as described in the docs. 
            // rather than investigate into that, I search for each separately
            var startInfo2 = new ProcessStartInfo()
            {
                FileName = command,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                // Expecting netstat to return: [Proto] [Local Address] [Foreign Address] [State] [PID]
                // Find mathing strings for port, established and process id.  
                Arguments = $"/C netstat -ano | findstr /l \":{port} \" | findstr /i /l \" ESTABLISHED \"" + ((processId != 0) ? $" | findstr /e /l \"{processId}\"" : string.Empty)
            };


            for (int i = 0; i < NumberOfTimesToTryToCheckServerIsRunning; i++)
            {                
                if (RunAndCheckForOutput(startInfo) || RunAndCheckForOutput(startInfo2))
                {
                    return; // Terminate loop early if port is found.
                }

                Thread.Sleep(waitTimeoutInMilliSeconds);
            }

            throw new Exception($"Specified process [{processId}] is not listening or established at port [{port}]. Validated with commands: [{startInfo.Arguments}] and [{startInfo2.Arguments}]");


            bool RunAndCheckForOutput(ProcessStartInfo processStartInfo)
			{
                // This validation method verifies that the server is up and listening at known port numbers by looking at the netstat command ouptut.
                var netstatProcess = Process.Start(processStartInfo);
                string output = netstatProcess.StandardOutput.ReadToEnd();

                return !string.IsNullOrEmpty(output);
            }
        }
    }
}
