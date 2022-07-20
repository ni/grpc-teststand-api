This example is a copy of <TestStand>\UserInterfaces\Simple\CSharp that has been extended to provide a gRPC server for the TestStand API

To Do Items:

- Modify axApplicationMgr_ReportError to do whatever is appropriate when the server detects an error. Displaying a message box is probably not a good idea for a server.

- Modify StartupTestStandGrpc.cs to setup your grpc settings, including port selection, certificate settings, and any additional services you want to publish

- Arrange to hide the main window if that is needed for the application. Otherwise leave it visible to aid with debugging. For hiding a winforms app main windows, maybe start here? https://stackoverflow.com/questions/683896/any-way-to-create-a-hidden-main-window-in-c/4913580

- If the project is not finding its nuget dependency of NationalInstruments.TestStand.gRPC.Server (0.5.0-*), add http://ninugetsvr/nuget/packages-prerelease/ as a package source (while connected to VPN) and install it from that source.

- if you get error 'Unable to configure HTTPS endpoint.', refer to https://ni.visualstudio.com/DevCentral/_workitems/edit/1710839. 
