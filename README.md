# TestStand gRPC API Example Technology Preview

This example demonstrates how to use the TestStand gRPC API from a client application to get status, and control a TestStand user interface (UI) or a TestStand Windows service running a gRPC service on a local or remote machine. This example consists of three C# .NET projects: a client, a UI server, and a Windows service, which you can run on separate machines to remotely execute TestStand sequence files. 

Subject to change without notice.

---

## Prerequisites

Complete the following steps to configure both the client and server machines.

1. Install the following software:
    | Software | Server | Client |
    | -------- | -------------- | -------------- |
    | Visual Studio 2019, or 2022 with the .NET and C++ desktop development workloads | Yes | Yes |
    | Git | Yes | Yes |
    | TestStand 2021 SP1 or later. Earlier versions of TestStand are untested. | Yes | No |

2. Clone the TestStand gRPC API Example Git repository on both the client and server machines.

---

## Configure and Run the UI Server

The UI server is a simple TestStand C# User Interface that uses a gRPC service using ASP.NET Core. The server supports secure connections using server-side TLS or mutual TLS, and non-secure connections.

Complete the following steps to prepare the server to accept requests from the client and run TestStand sequence files.
1. Activate the TestStand license to prevent NI License Manager dialogs upon starting the server.
    1. Open the TestStand Sequence Editor, and wait for the NI License Manager dialog to appear. If no dialog appears, then the TestStand license is activated and you may continue to the next procedure.
    2. Select **Log in to Activate** or **Begin 7 Day Trial** options to activate the TestStand License.
    3. Close and reopen the TestStand Sequence Editor.
    4. You may continue to the next procedure if no NI License Manger dialog appeared after reopening the Sequence Editor. If however a dialog appeared, continue to the next step.
    5. If you selected **Begin 7 Day Trial**, then in the License Activation dialog select the **Remind Me When They Expire** option.
    6. Close and reopen the TestStand Sequence Editor and ensure no NI License Manager dialog appears.
2. Configure TestStand Station Options to prevent dialogs from appearing during execution.
    1. Open the TestStand Sequence Editor, then click **Configure>Station Options** to open the Station Options dialog box.
    2. In **User Manager**, deselect **Check User Privileges**.
    3. In **Execution**, set **On Run-Time Error** to **Run Cleanup**.
    4. In **Time Limits**, ensure that none of the properties are set to **Prompt for Action**.
    5. In **Preferences**, deselect **Prompt to Find Files**.
3. (Optional) Configure a server-side TLS connection or mutual TLS connection. Refer to [Encrypting Connections to the Server](Docs/Encrypt_Connection.md) for more information. If you do not configure a secure connection, the client and server connection is not secure.
4. In Visual Studio, open `Server/UI/TestExecServer.sln`.  Open the .sln file. Do not select Open Folder.
5. Build and run the solution. Type F5 or select the Start Debugging item from the DEBUG menu to run the solution. If you are using 32-bit TestStand, select the x86 Solution Platform before building.

---

## Configure and Run the Windows Service

The Windows service is a .NET Worker Service application that enables gRPC and runs TestStand as a background service. The server supports secure connections using server-side TLS or mutual TLS, and non-secure connections.

Complete the following steps to prepare the server to accept requests from the client and run TestStand sequence files.
1. Follow step 1 from section [Configure and Run the UI Server](#configure-and-run-the-ui-server)
2. (Optional) Configure a server-side TLS connection or mutual TLS connection. Refer to [Encrypting Connections to the Server](Docs/Encrypt_Connection.md) for more information. If you do not configure a secure connection, the client and server connection is not secure.
3. Build and install the Windows service using instructions in [Readme.md](Server/WindowsService/Readme.md).

---

## Launch the Client and Connect to the Server

The client is a C# GUI application that demonstrates using the TestStand gRPC API to remotely execute sequence files and perform other operations.

Complete the following steps to connect the client to the server.
1. (Optional) Configure a server-side TLS connection or mutual TLS connection. Refer to [Encrypting Connections to the Server](Docs/Encrypt_Connection.md) for more information. If you do not configure a secure connection, the client and server connection is not secure.
2. Build and run the solution in a new instance of Visual Studio. Open the `Client/ExampleClient.sln` file. Do not select Open Folder. Type F5 or select the Start Debugging item from the DEBUG menu to run the solution.
3. In the Example TestStand API Client Application window, enter the server IP address in **Server Address**, then click **Connect**. The Status field indicates whether a connection is established. If you have successfully established a secure connection, the IP address field displays a green shield icon. 

---

## Example Sequence Files

#### Server
The server application has example files that the client can run. When building the server project, all the files under `Server/ExampleFiles` are distributed with the executable.  

To add additional example files, do one or all of the following:
- Add files to the `Server\ExampleFiles` directory and rebuild the server.
- Copy files to the `<server executable>\ExampleFiles` directory.
- Add files to a directory that is part of TestStand's search directories.

#### Client
The client application has a list of files that can be run on the server. The list of files is configured in the `client_config.json` file.

To add additional example files that can be run on the server, modify the property `example_files` in the `client_config.json` file.

### Run a Sequence File

Complete the following steps in the client to run a sequence file on the server. 
1. In the client application window, select the sequence file you want to run. The list of sequence files is hard coded, and any sequence file in the list needs to be present on the server.
2. Select the Model, Station Model, and Entry Point you want to use.
3. Click **Run Remote Sequence File**. The sequence file runs on the server. 
4. While the sequence file is running, you can break, resume, and terminate the execution. 

After running the sequence file on a server, you can optionally use the TestMonitor plugin to upload test data to the cloud.

---


## Incompatibilities

| Nuget Package Version | Comment |
| -- | -- |
| 15 | The namespace changed from NationalInstruments.TestStand.gRPC.Server to NationalInstruments.TestStand.Grpc.Server|
| 16 | None |
| 17 | All services with suffix "Class" had that suffix removed. Their corresponding interfaces were also deleted. For example, EngineClass has been renamed to Engine and IEngine has been deleted. |

---

## See Also
### Example Projects: 
- [TS gRPC Server Example](Server/UI/Readme.md)
- [TS gRPC Server Windows Service Example](Server/WindowsService/Readme.md)
- [TS gRPC Client Example](Client/Readme.md)

### References
- [Overview for gRPC on .NET](https://docs.microsoft.com/en-us/aspnet/core/grpc/?view=aspnetcore-5.0)
- [.NET Generic Host in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-5.0)
- [Docs Folder](Docs)