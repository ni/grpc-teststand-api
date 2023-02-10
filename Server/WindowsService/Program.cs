namespace TestExecWindowsService
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			GrpcService.Start(args);
		}
	}
}
