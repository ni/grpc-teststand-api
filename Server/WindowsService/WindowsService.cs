using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NationalInstruments.TestStand.Utility;

namespace TestExecWindowsService
{
	public class WindowsService : BackgroundService
	{
		private Thread _staThreadForRunningTSServer;
		private MainForm _serverMainForm;
		private ManualResetEvent _waitForFormToClose;
		private bool _shuttingDown;

		// This method starts the TestStand headless server as a background service.
		// For more information about the method see:
		// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio#ihostedservice-interface
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_waitForFormToClose = new ManualResetEvent(false);

			_staThreadForRunningTSServer = new Thread(StartTestStand);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// MainForm requires an STA thread to run.
				_staThreadForRunningTSServer.SetApartmentState(ApartmentState.STA);
			}
			_staThreadForRunningTSServer.Start();

			return Task.CompletedTask;
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				_shuttingDown = true;

				if (_serverMainForm.Created)
				{
					_serverMainForm.BeginInvoke(_serverMainForm.Close);
					_waitForFormToClose.WaitOne(Timeout.Infinite);
				}
			}
			catch (Exception e)
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					MainForm.WriteErrorToEventLog("Error occurred while stopping service.\nError: " + e.Message);
				}
			}

			return Task.CompletedTask;
		}

		public void StartTestStand(object state)
		{
			_serverMainForm = new MainForm();
			ApplicationWrapper.Run(_serverMainForm);

			// When shutting down the Windows service, the method StopAsync is called. StopAsync shutdowns the
			// TestStand server by calling Close on MainForm. StopAsync then waits for TestStand to finish
			// shutting down. This is done by waiting for the event _waitForFormToClose to be signaled.
			// So, we need to signal _waitForFormToClose here to let StopAsync finish shutting down the service.
			_waitForFormToClose.Set();

			// If an error occurs while starting the service, the MainForm will close without
			// shutting down the gRPC service. When that happens, we need to shutdown the 
			// service here.  We cannot shutdown the service in MainForm because it will result
			// in a deadlock because MainForm will wait for the service to shutdown, but the
			// service cannot shutdown because it is waiting for MainForm to close.
			if (!_shuttingDown)
			{
				GrpcService.Shutdown();
			}
		}
	}
}
