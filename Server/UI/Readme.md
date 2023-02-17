# About This Example
This example is a copy of `<TestStand>\UserInterfaces\Simple\CSharp` that has been extended to provide a gRPC server for the TestStand API.

This example can run as a headless server. To enable the headless option, pass the command line argument `/headless` when running the server.
If an error occurs while starting the gRPC service, the server will automatically shutdown and the error will be logged in the Windows Event Log.
When running as a headless server, a tray icon will appear on the taskbar that allows you to close the server. To close the server, right-click
on the tray icon and select Exit.

# To Do Items:

- Modify `axApplicationMgr_ReportError` to do whatever is appropriate when the server detects an error. Displaying a message box is probably not a good idea for a server.

- Modify `StartupTestStandGrpc.cs` to setup your grpc settings, including port selection, certificate settings, and any additional services you want to publish.

- Arrange to hide the main window if that is needed for the application. Otherwise leave it visible to aid with debugging. Here is how you can [hide a WinForms](https://stackoverflow.com/questions/683896/any-way-to-create-a-hidden-main-window-in-c/4913580) window.

- If the project is not finding its nuget dependency of `NationalInstruments.TestStand.gRPC.Server (0.5.0-*)`, add `https://api.nuget.org/v3/index.json` as a package source. Here is the link to [download the NationalInstruments.TestStand.gRPC.Server](https://www.nuget.org/packages?q=NationalInstruments.TestStand.gRPC.Server) nuget package.