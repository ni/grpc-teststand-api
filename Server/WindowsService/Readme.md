# TestStand gRPC Windows Service

This example creates a TestStand gRPC server that runs as a Windows service.

To run the service, you need to first publish it and then install it. You must have .NET installed on the server with `dotnet` available as a command. If you completed the [Prerequisites](../../README.md#Prerequisites) section of the other Readme, this command is available already. Otherwise, install the [.NET SDK](https://dotnet.microsoft.com/en-us/download).

---

## Publishing the Windows Service

Publish the service using one of the following methods:
* Open a command prompt in `Examples/Server/WindowsService/` and run the following command:  
`dotnet publish -c Release -r win-x64 --sc -o <Output Directory>`
* Publish in Visual Studio
    1. Open `Examples/Server/WindowsService/TestExecWindowsService.sln` in Visual Studio. Open the .sln file. Do not select Open Folder.
    2. In the **Solution Configurations** drop-down menu, select **Release**.
    3. Click **Build>Publish Selection**.
    4. In the **Publish** tab, click **Publish**.
    5. When building completes, in the **Publish** tab, click the **Target location** link to find the executable file `TestExecWindowsService.exe`. Note the location of this file, as you will need it in future steps.
    

For more options for "donet publish", see https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish.

---

## Installing the Windows Service
To install the service, run the following command with admin priviledges:  
`sc.exe create "NI TestStand gRPC Service" binpath= "<PathToExecutable> --contentRoot <DirectoryOfExecutable>"`

To set the description of the service, run the following command with admin priviledges:  
`sc.exe description "NI TestStand gRPC Service" "Allows remote gRPC clients to run and monitor test sequences."`

Note: The service can take some time to initialize and accept client connections. Any errors that occurred during startup are logged to the Windows Event Log. You can see the log using [Windows Event Viewer](https://learn.microsoft.com/en-us/shows/inside/event-viewer).

---

## Starting the Windows Service

Complete the following steps to start the service:

1. Click **Start**, then type **Services** to open the Services app.
2. Right-click **NI TestStand gRPC Service**, and select **Start**.

You can now connect to the service using the example client. Refer to [Launch the Client and Connect to the Server](../../README.md#launch-the-client-and-connect-to-the-server) and [Example Sequence Files](../../README.md#example-sequence-files) for more information.

---

## Removing the Windows Service

To delete the service, run the following command with admin priviledges:  
`sc.exe delete "NI TestStand gRPC Service"`


---

## See Also

[sc.exe create](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/sc-create)

[Creating a Windows Service using BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)

[Creating a Windows Service with .NET 6](https://csharp.christiannagel.com/2022/03/22/windowsservice-2/)