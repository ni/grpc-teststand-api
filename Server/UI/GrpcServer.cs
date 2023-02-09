using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestStandGrpcApi;

namespace TestExecServer
{
	internal class GrpcServer : GrpcServerBase
	{
		public static event EventHandler<Exception> StartupException;

        public static void Start(string[] args)
		{
			InitializeServerOptions(args);

			ServerHost = CreateHostBuilder<GrpcServer>(args).Build();
			Task.Run(() =>
			{ 
				try
				{
					ServerHost.Run();
				}
				catch (Exception exception)
				{
					StartupException?.Invoke(null, exception);
                }
			});
		}

		public GrpcServer(IConfiguration configuration) : base(configuration)
		{
		}

		public static void ConfigureServices(IServiceCollection services)
		{
			ConfigureServicesCore(services);
		}

		public static void Configure(IApplicationBuilder app, IWebHostEnvironment webHostEnvironment)
		{
            ConfigureCore(app, webHostEnvironment);
		}
	}
}
