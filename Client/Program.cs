using System;
using System.Windows.Forms;

namespace ExampleClient;

static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// A path to a config file can be specified in the command line. If an argument 
			// is specified, we will assume it is the path to the config file since no 
			// other command line arguments are supported.
			string configurationFile = GetConfigFilePathFromCommandLineIfSpecified(args);
			var clientConfiguration = new ClientConfigurationParser(configurationFile);
			ClientOptions options = clientConfiguration.Options;

			Application.Run(new Example(options));
		}

		private static string GetConfigFilePathFromCommandLineIfSpecified(string[] args)
		{
			return args.Length == 1 ? args[0] : null;
		}
	}
