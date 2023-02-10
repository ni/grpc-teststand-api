using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestStandGrpcApi;

namespace TestExecWindowsService
{
	internal class GrpcService : GrpcServerBase
	{
		public static void Start(string[] args)
		{
			InitializeServerOptions(args);

			IHostBuilder hostBuilder = CreateHostBuilder<GrpcService>(args)
				// This call builds the Windows Service that will run the TestStand gRPC service
				.UseWindowsService(options =>
				{
					string connectionType = UsesHttps ? " [Secure]" : " [Not secure]";
					options.ServiceName = "NI TestStand gRPC Windows Service" + connectionType + ". Listening at port number '" + ServerOptions.Port + "'.";
				});

			ServerHost = hostBuilder.Build();
			ServerHost.Run();
		}

		public GrpcService(IConfiguration configuration) : base(configuration)
		{
		}

		public static void ConfigureServices(IServiceCollection services)
		{
			ConfigureServicesCore(services);

			// This call registers the background service that starts TestStand.
			services.AddHostedService<WindowsService>();
		}

		public static void Configure(IApplicationBuilder app, IWebHostEnvironment webHostEnvironment)
		{
			ConfigureCore(app, webHostEnvironment);
		}
	}
}