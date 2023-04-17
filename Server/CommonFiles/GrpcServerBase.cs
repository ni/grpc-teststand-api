using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationalInstruments.TestStand.Grpc.Server;
using NationalInstruments.TestStand.Grpc.Server.Utilities;
using static System.FormattableString;

namespace TestStandGrpcApi
{
    public class GrpcServerBase
    {
        private const string AllowedOrigins = "AllowedOrigins";

        private static X509Certificate2 _serverCertificate;
        private static X509Certificate2 _clientCertificate;

        protected static void InitializeServerOptions(string[] args)
		{
            // From the command line, a configuration file path is specified by using the "-Config" option
            // followed by a file path.
            string configurationFile = GetConfigFilePathFromCommandLineIfSpecified(args);
            var serverConfiguration = new ServerConfiguration(configurationFile);
            ServerOptions = serverConfiguration.Options;

            // To make a connection secure (server-side TLS), the server certificate information needs
            // to be provided in the config file. If the information is left blank or no config file is
            // specified, the connection will not be secured.
            UsesHttps = ServerOptions.UseSecureConnection;
        }

        private static string GetConfigFilePathFromCommandLineIfSpecified(string[] args)
        {
            for (int index = 0; index < args.Length; index++)
            {
                string argument = args[index];
                if (string.Equals(argument, "-Config", StringComparison.OrdinalIgnoreCase))
                {
                    // The next argument is the file path
                    index++;
                    if (index < args.Length)
                    {
                        return args[index];
                    }

                    throw new Exception("No config json file path specified.");
                }
            }

            return null;
        }

        protected static IHost ServerHost { get; set; }

        protected static ServerOptions ServerOptions { get; private set; }

        public static bool UsesHttps { get; private set; }

        public static int Port => ServerOptions.Port;

        // Has error messages generated when starting gRPC service. It will be displayed by the Application Manager
        // after it is created by MainForm.
        public static string ErrorMessage { get; private set; } = string.Empty;

        public static void Shutdown()
        {
            _clientCertificate?.Dispose();
            _serverCertificate?.Dispose();

            Task task = Task.Run(async () => await ServerHost.StopAsync());
            Task.WaitAll(task);
            ServerHost.Dispose();
            ServerHost = null;
        }

        protected static IHostBuilder CreateHostBuilder<T>(string[] args) where T : class =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureLogging(logging =>
                {
                    // Showing logging messages slowdown the benchmarks so disable them.                  
                    FilterLoggingBuilderExtensions.AddFilter(logging, "Microsoft", LogLevel.None);
                    FilterLoggingBuilderExtensions.AddFilter(logging, "Grpc", LogLevel.None);
                }).ConfigureKestrel(options =>
                {
                    options.ConfigureHttpsDefaults(configureOptions =>
                    {
                        ConfigureSecureConnectionIfRequired(configureOptions);
                    });

                    options.Listen(IPAddress.Any, ServerOptions.Port, listenOptions =>
                    {
                        // HTTP2 is faster than 1 and should be preferred. Browsers only support HTTP1 for insecure communication,
                        // so we need to listen on that too to support requests coming from insecure browsers.
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        if (UsesHttps)
                        {
                            try
                            {
                                listenOptions.UseHttps();
                            }
                            catch (Exception e)
                            {
                                ErrorMessage += Invariant($"Error starting port '{ServerOptions.Port}'.\nError: {e.Message}\n\n");
                            }
                        }
                    });

                    // Make the testing port be the next port
                    int testingPort = ServerOptions.Port + 1; // Port=5021
                    options.Listen(IPAddress.Any, testingPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        if (UsesHttps)
                        {
                            try
                            {
                                listenOptions.UseHttps();
                            }
                            catch (Exception e)
                            {
                                ErrorMessage += Invariant($"Error starting port '{testingPort}'.\nError: {e.Message}\n\n");
                            }
                        }
                    });
                });

                webBuilder.UseStartup<T>();
            });

        private static void ConfigureSecureConnectionIfRequired(HttpsConnectionAdapterOptions configureOptions)
        {
            // Since there are two channels, this method will be called twice. So, only load the server certificate once.
            if (_serverCertificate == null)
            {
                if (!string.IsNullOrEmpty(ServerOptions.ServerCertificateFriendlyName))
                {
                    _serverCertificate = FindCertificateInCertificatesStore(ServerOptions.ServerCertificateFriendlyName);
                }
                else if (!string.IsNullOrEmpty(ServerOptions.ServerCertificatePFXPath))
                {
                    _serverCertificate = new X509Certificate2(ServerOptions.ServerCertificatePFXPath, ServerOptions.ServerCertificatePFXPassword);
                }
                else if (!string.IsNullOrEmpty(ServerOptions.ServerCertificatePath) && !string.IsNullOrEmpty(ServerOptions.ServerKeyPath))
                {
                    var serverCertificate = X509Certificate2.CreateFromPemFile(ServerOptions.ServerCertificatePath, ServerOptions.ServerKeyPath);

                    // ASP.NET Core apps expect pfx certificates so we need to create one.
                    _serverCertificate = new X509Certificate2(serverCertificate.Export(X509ContentType.Pfx));
                }
            }

            if (_serverCertificate != null)
            {
                // This is where we set the server certificate to authenticate HTTPS connections.
                configureOptions.ServerCertificate = _serverCertificate;

                // If a client certificate has been specified, we need to validate the client. The server needs
                // to verify who is connecting.  This is effectively implementing mutual TLS.
                if (!string.IsNullOrEmpty(ServerOptions.ClientCertificatePath))
                {
                    if (_clientCertificate == null)
                    {
                        _clientCertificate = new X509Certificate2(ServerOptions.ClientCertificatePath);
                        if(_clientCertificate == null)
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

        public GrpcServerBase(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public static void ConfigureServicesCore(IServiceCollection services)
        {
            services.AddControllers();

            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                // Accept chained and self-signed certificates
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth?view=aspnetcore-6.0#allowedcertificatetypes--chained-selfsigned-or-all-chained--selfsigned
                options.AllowedCertificateTypes = CertificateTypes.All;

                if (_clientCertificate != null)
                {
                    // Since we are using self-signed certificates for the client, turned off revocation checking to
                    // avoid getting warnings when making gRPC calls to this service.
                    options.RevocationMode = X509RevocationMode.NoCheck;
                }
            });

            // Enable gRPC services
            // https://docs.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-6.0&tabs=visual-studio#add-grpc-services-to-an-aspnet-core-app
            services.AddGrpc(options =>
            {
                // Set message sizes to unlimited
                options.MaxReceiveMessageSize = null;
                options.MaxSendMessageSize = null;
            });

            // Browser security prevents a web page from making requests to a different domain (origin) than the one
            // that served the web page. This restriction is called the same-origin policy. Browser apps that want to
            // use this gRPC service might be running in a different domain. To allow those browser apps to use this
            // gRPC service, we need to enable Cross-Origin Resource Sharing (CORS) which is done in the Configure method
            // below. We also need to add a CORS policy (as done below) to allow specific origins (domains) to use this
            // service. For more information as to how to configure all the different options, see
            // https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
            if (ServerOptions.Cors.IsEnabled)
            {
                services.AddCors(options =>
                {
                    options.AddPolicy(name: AllowedOrigins,
                        policy =>
                        {
                            policy.WithOrigins(ServerOptions.Cors.Origins)
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        });
                });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void ConfigureCore(IApplicationBuilder app, IWebHostEnvironment _)
        {
            // Microsoft recommends using HTTPS Redirection Middleware.
            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-6.0&tabs=visual-studio#require-https
            app.UseHttpsRedirection();

            app.UseRouting();

            // To enable client certificate authentication, the following two calls are needed.
            // https://learn.microsoft.com/en-us/aspnet/core/grpc/authn-and-authz?view=aspnetcore-6.0
            app.UseAuthentication();
            app.UseAuthorization();

            // To allow browser apps to use this gRPC service, we need to enable the gRPC-Web protocol.
            // gRPC-Web middleware needs to be enabled after routing and before endpoints.
            // https://docs.microsoft.com/en-us/aspnet/core/grpc/grpcweb?view=aspnetcore-6.0#configure-grpc-web-in-aspnet-core
            // Configure so that all services support gRPC-Web by default. This will remove the
            // requirement to call EnableGrpcWeb in all services. 
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

            // Enabled CORS if option is enabled. Here we only enable the CORS middleware. In the method
            // ConfigureServices above, we add a CORS policy to enable browser apps to connect.
            // https://learn.microsoft.com/en-us/aspnet/core/grpc/grpcweb?view=aspnetcore-6.0#grpc-web-and-cors
            if (ServerOptions.Cors.IsEnabled)
            {
                app.UseCors(AllowedOrigins);
            }

            app.UseEndpoints(endpoints =>
            {
                // Call RegisterServices for each Grpc api server side assembly you have generated and want to make available
                ServicesRegistration.RegisterServices(endpoints);
            });
        }
    }
}
