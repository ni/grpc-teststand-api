# About This Example
This example works with a TestStand gRPC server to demonstrate making remote TestStand API calls on a server.

Note that this example depends on a nuget that contains the `.proto` files for the API and the client side assembly
the [protoc compiler](https://protobuf.dev/getting-started/cpptutorial) generates for those `.proto` files. If you intend to make a non-c# client and need the `.proto` 
files, you can download the nuget package, open it as a `.zip` file, and access the `.proto` files from the 
folder `ni/teststand/api/grpc/techpreview` located under `content/ProtoFiles` in the nuget package.  Simply copy
the `ni` folder to your project directory.

Here is the link to download the [NationalInstruments.TestStand.gRPC.Client](https://www.nuget.org/packages/NationalInstruments.TestStand.Grpc.Client) nuget package.


## Building and running this example:

Open `ExampleClient.csproj` in a version of Visual Studio that includes support for .NET 6. Build and run the
project. In the **Server Address** control, specify the hostname or ip address of the computer that is running the
example server. If it is the same computer, you can specify an ip address of `127.0.0.1`. Click **Connect** and
then use the various controls to demonstrate invoking the TestStand API in the server process.


# To Do Items:

- If the project is not finding its nuget dependency of `NationalInstruments.TestStand.gRPC.Client (0.5.0-*)`,
add https://api.nuget.org/v3/index.json as a nuget package source,

- Use the TestStand API to implement your desired functionality

- Use the `InstanceLifetimeClient` service to make sure the server knows when it is ok to release objects it
has returned to the client.