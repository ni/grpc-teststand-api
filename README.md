# TestStand gRPC API Example

This example demonstrates how to use gRPC to communicate from a client to a server and run a sequence file in a TestStand C# user interface. This example consists of two C# .NET projects, a client and a server, which you can run on separate machines to remotely execute TestStand sequence files. 

## Prerequisites

Complete the following steps to configure both the client and server machines.

1. Install the following software:
    | Software | Server | Client |
    | -------- | -------------- | -------------- |
    | Visual Studio 2015, 2017, 2019, or 2022 | Yes | Yes |
    | Git | Yes | Yes |
    | TestStand 2021 SP1 or later | Yes | No |
2. Clone the TestStand gRPC API Example repository on both the client and server machines.

## Configure and Run the Server

The server is a simple TestStand C# User Interface that uses a .NET Generic Host in ASP.NET Core. The server supports secure connections using server-side TLS or mutual TLS, and insecure connections.

Complete the following steps to prepare the server to accept requests from the client and run TestStand sequence files.
1. Configure TestStand Station Options to prevent dialogs from appearing during execution.
    1. Open the TestStand Sequence Editor, then click **Configure>Station Options** to open the Station Options dialog box.
    2. In **User Manager**, deselect **Check User Privileges**.
    3. In **Execution**, set **On Run-Time Error** to **Run Cleanup**.
    4. In **Time Limits**, ensure that none of the properties are set to **Prompt for Action**.
    5. In **Preferences**, deselect **Prompt to Find Files**.
2. [in work] (Optional) For secure connections, set the necessary values in `Server/server_config.json`.
    1. (Substeps and details as needed.)
3. In Visual Studio, open `Server/TestExecServer.sln`.
4. Build and run the solution.

## Launch the Client and Connect to the Server

The client is a C# GUI application where you control execution of the sequence file on the server.

Complete the following steps to connect the client to the server.
1. [in work] (Optional) For secure connections, set the necessary values in `Client/client_config.json`.
    1. (Substeps and details as needed.)
2. In Visual Studio, open `Client/ExampleClient.sln`.
2. Build and run the solution.
3. In the Example TestStand API Client Application window, enter the server IP address in **Server Address**, then click **Connect**. The Status field indicates whether a connection is established. If you have successfully established a secure connection, the IP address field will display a green shield icon. 

## Run a Sequence File

Complete the following steps in the client to run a sequence file on the server. 
1. In the client application window, select the sequence file you want to run. The list of sequence files is hard coded, and any sequence file in the list needs to be present on the server.
2. Select the Model, Station Model, and Entry Point you want to use.
3. Click **Run Remote Sequence File**. The sequence file runs on the server. 
4. While the sequence file is running, you can break, resume, and terminate the execution. 

## Postrequisites

After running the sequence file on a server, you can use the TestMonitor plugin to upload test data to the cloud.

## Known Issues and Limitations of the gRPC API

- Events are not currently supported.

## See Also
[Overview for gRPC on .NET](https://docs.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-5.0)

[.NET Generic Host in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0)

[Server Security Support](https://github.com/ni/grpc-device/wiki/Server-Security-Support)
>>>>>>> 282e642e8fc4802cfca268bb3844f64065edb59b
