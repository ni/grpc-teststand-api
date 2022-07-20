using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationalInstruments.TestStand.gRPC.Server;
using static System.FormattableString;

namespace TestStandGrpcApi
{
    public class Server
    {
        private static IHost _host;
        private static ServerOptions _serverOptions;
        private static X509Certificate2 _serverCertificate;
        private static X509Certificate2 _clientCertificate;

        public static void Start(string[] args)
		{
            // From the command line, a configuration file path is specified by using the "-Config" option
            // followed by a file path.
            string configurationFile = GetConfigFilePathFromCommandLineIfSpecified(args);
            var serverConfiguration = new ServerConfigurationParser(configurationFile);
            _serverOptions = serverConfiguration.Options;

            _host = CreateHostBuilder(args).Build();
            Task.Run(() => _host.Run());
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

        public static bool UsesHttps { get; private set; }

        // Has error messages generated when starting gRPC service. It will be displayed by the Application Manager
        // after it is created by MainForm.
        public static string ErrorMessage { get; private set; } = string.Empty;

        public static void Shutdown()
        {
            _clientCertificate?.Dispose();
            _serverCertificate?.Dispose();

            Task task = Task.Run(async () => await _host.StopAsync());
            Task.WaitAll(task);
            _host.Dispose();
            _host = null;
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
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

                    // To make a connection secure, certificates filenames must be provided in the config file.
                    // If the filenames were left blank or no config file was specified, the connection will
                    // not be secured.
                    UsesHttps = !string.IsNullOrEmpty(_serverOptions.ServerCertificatePFXPath)
                        || !string.IsNullOrEmpty(_serverOptions.ServerCertificateFriendlyName)
                        || (!string.IsNullOrEmpty(_serverOptions.ServerCertificatePath)
                        && !string.IsNullOrEmpty(_serverOptions.ServerKeyPath));

                    options.Listen(IPAddress.Any, _serverOptions.Port, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        if (UsesHttps)
                        {
                            try
                            {
                                listenOptions.UseHttps();
                            }
                            catch (Exception e)
                            {
                                ErrorMessage += Invariant($"Error starting port '{_serverOptions.Port}'.\nError: {e.Message}\n\n");
                            }
                        }
                    });

                    // 5002 is just for testing
                    const int TestingPort = 5002;
                    options.Listen(IPAddress.Any, TestingPort, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                        if (UsesHttps)
                        {
                            try
                            {
                                listenOptions.UseHttps();
                            }
                            catch (Exception e)
                            {
                                ErrorMessage += Invariant($"Error starting port '{TestingPort}'.\nError: {e.Message}\n\n");
                            }
                        }
                    });
                });

                webBuilder.UseStartup<Server>();
            });

        private static void ConfigureSecureConnectionIfRequired(HttpsConnectionAdapterOptions configureOptions)
        {
            // Since there are two channels, this method will be called twice. So, only load the server certificate once.
            if (_serverCertificate == null)
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
            }

            if (_serverCertificate != null)
            {
                configureOptions.ServerCertificate = _serverCertificate;

                // If a client certificate has been specified, we need to validate the client. This is effectively
                // supporting mutual TLS.
                if (!string.IsNullOrEmpty(_serverOptions.ClientCertificatePath))
                {
                    if (_clientCertificate == null)
                    {
                        _clientCertificate = new X509Certificate2(_serverOptions.ClientCertificatePath);
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

        public Server(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
            .AddCertificate(options =>
            {
                // Accept chained and self-signed certificates
                // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/certauth?view=aspnetcore-6.0#allowedcertificatetypes--chained-selfsigned-or-all-chained--selfsigned
                options.AllowedCertificateTypes = CertificateTypes.All;
            });

            // Enable gRPC services
            // https://docs.microsoft.com/en-us/aspnet/core/grpc/aspnetcore?view=aspnetcore-6.0&tabs=visual-studio#add-grpc-services-to-an-aspnet-core-app
            services.AddGrpc(options =>
            {
                // Set message sizes to unlimited
                options.MaxReceiveMessageSize = null;
                options.MaxSendMessageSize = null;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment _)
        {
            // Microsoft recommends using HTTPS Redirection Middleware.
            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-6.0&tabs=visual-studio#require-https
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Add gRPC-Web middleware after routing and before endpoints.  https://devblogs.microsoft.com/aspnet/grpc-web-experiment/
            // app.UseGrpcWeb();  need to test this. also in Server.RegisterServices, it needs endPoints.MapGrpcService<T>().EnableGrpcWeb() for each service, also need package Grpc.AspNetCore.Web 

            app.UseEndpoints(endpoints =>
            {
                // Call RegisterServices for each Grpc api server side assembly you have generated and want to make available
                ServicesRegistration.RegisterServices(endpoints);

                // BTS: Register the ExecutionTraceEvents service to make available
                BTS.ExecutionTraceEvents.Server.RegisterServices(endpoints);
            });
        }
    }
}
