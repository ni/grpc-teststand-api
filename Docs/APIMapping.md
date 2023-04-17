# Mapping
These mappings give a high level overview of how TestStand API objects, methods, and parameters are mapped to the gRPC API.

- API Objects such as `Engine` and `PropertyObject` are exposed as gRPC services.
- Methods are mapped to RPC calls.
- Properties are mapped to RPC calls with prepend *Get_* and *Set_* followed by the property name.
- Input parameters are mapped to request messages.
- Output parameters and return values are mapped to reply messages.
- Events are mapped to server side streams of reply messages.
- Different APIs are mapped to different .proto files.
- Namespaces are mapped to corresponding Grpc namespaces.

# API Objects
The TestStand gRPC API exposes all TestStand API objects as gRPC services. Here is an example of how the `PropertyObject` is mapped to a gRPC .proto (Protobuf) file:

```
class PropertyObject {
	Double GetValNumber(String lookupString, Long options)
	void SetValNumber (String lookupString, Long options, Double newValue)
	PropertyObject GetPropertyObject (String lookupString, Long options)

         ||
        \||/
         \/

service PropertyObject {
	rpc GetValNumber(PropertyObject_GetValNumberRequest) returns (PropertyObject_GetValNumberResponse);
    rpc SetValNumber(PropertyObject_SetValNumberRequest) returns (PropertyObject_SetValNumberResponse);
    rpc GetPropertyObject(PropertyObject_GetPropertyObjectRequest) returns (PropertyObject_GetPropertyObjectResponse);
 ```

## Object Instances
Each service has an object instance message defined.  The instance message definition has the name *\<ServiceName\>Instance*.

Since gRPC has no concept of object instances, these instance messages enable you to explicitly provide the object instance to each method. It also allows type checking.

Here is an example of how a `PropertyObject` instance is defined and used as a field in the request message when calling `PropertyObject.GetValNumber`:

```
message PropertyObjectInstance {
	string id = 1;
}

message PropertyObject_GetValNumberRequest {
	PropertyObjectInstance instance = 1;
    ...
}
```

An object instance has a string id associated with it.  The string id specifies the handle to the TestStand object instance in the server. The lifetime of the TestStand object is managed by the server.  See [Object Lifetime](ObjectLifetime.md) for more details.

Object instances are obtained from a constructor RPC call. The following example shows how to construct an `EngineInstance`:

```
var engineClient = new EngineClass.EngineClassClient(gRPCChannel);
EngineClassInstance engine = engineClient.EngineClass(new EngineClass_EngineClassRequest()).ReturnValue;
```

Also, return values or response message fields from an RPC call can provide object instances. Here is an example of getting a `TypeUsageListInstance`:

```
EngineClass_UnserializeObjectsAndTypesResponse response = engineClient.UnserializeObjectsAndTypes(unserializeObjectsAndTypesRequest);
TypeUsageListInstance usageList = response.typesUsed;
```

# Methods/Properties and Parameters
The method names between the gRPC API and the TestStand API remain the same. For properties, the gRPC API prepends *Set_* and *Get_* to the property names.  For example, the property `PropertyObject.Name` is mapped as follows:

```
rpc Set_Name(PropertyObject_Set_NameRequest) returns (PropertyObject_Set_NameResponse);
rpc Get_Name(PropertyObject_Get_NameRequest) returns (PropertyObject_Get_NameResponse);
```

All inputs and out parameters are mapped to request and response messages respectively. The messages have the name *\<ServiceName\>_\<MethodName\>[Request|Response]*. Here is how the request and response for `PropertyObject.GetValNumber` are defined:

```
message PropertyObject_GetValNumberRequest {
	PropertyObjectInstance instance = 1;
	string lookupString = 2;
	PropertyOptions options = 3;
}

message PropertyObject_GetValNumberResponse {
	double returnValue = 1;
}
```

Each request requires an object instance followed by the input parameter values, if any.

## Arrays

Some arrays in the TestStand API are optional. Since arrays cannot be optional in proto files, a wrapper is created for each array type. Each wrapper has the name *\<ObjectType\>Collection* and has a `repeated` field named *items*.  For example, the collections for an array of PropertyObjectFileInstances and strings are defined as follows
```
message PropertyObjectFileInstanceCollection {
  repeated PropertyObjectFileInstance items = 1;
}

message stringCollection {
	repeated string items = 1;
}
```

The example below shows how they are used in the call to `Engine.SearchFiles`.

```
Class Engine {
    SearchResults SearchFiles(String searchString, Long searchOptions, Long filterOptions,
                              Long elementsToSearch, StringArray limitToAdapters,
                              StringArray limitToNamedProps, StringArray imitToPropsOfNamedTypes,
                              ObjectArray openFilesToSearch, String Array directoriesAndFilePaths)
}

         ||
        \||/
         \/

service EngineClass {
	rpc SearchFiles(EngineClass_SearchFilesRequest) returns (EngineClass_SearchFilesResponse);
}

message Engine_SearchFilesRequest {
  EngineInstance instance = 1;
  string searchString = 2;
  SearchOptions SearchOptions = 3;
  SearchFilterOptions filterOptions = 4;
  SearchElements elementsToSearch = 5;
  stringCollection limitToAdapters = 6;
  stringCollection limitToNamedProps = 7;
  stringCollection limitToPropsOfNamedTypes = 8;
  PropertyObjectFileInstanceCollection openFilesToSearch = 9;
  stringCollection directoriesAndFilePaths = 10;
}
```

Since all collections are null by default, an instance of the collection needs to be created and populate if a non-null array is needed for the gRPC API call.  For example, to create instances of limitToAdapters and openFilesToSearch from the example above, the following needs to be done
```
searchFilesRequest.LimitToAdapters = new stringCollection();
searchFilesRequest.LimitToAdapters.Items.Add(adapters);

searchFilesRequest.OpenFilesToSearch = new PropertyObjectFileInstanceCollection();
searchFilesRequest.OpenFilesToSearch.Items.Add(files);
```

# Enums
Some of the long parameters defined in the TestStand API take enum values like the *options* parameter in many of the methods. For those long parameters, the gRPC API maps them to the enum data types they expect. For example, the TestStand API defines the *options* parameter as a `long` in `PropertyObject.GetValNumber` (see [API Objects](#api-objects)). The gRPC API maps `long` to `PropertyOptions` to simplify specifying the different options.

Other examples include using the following:
- `PropertyFlags` for the *Flags* parameter of `PropertyObject.SetFlags`
- `LoadPrototypeOptions` for the *options* parameter of `Module.LoadPrototype`
- `OpenFileDialogOptions` for the *openFileDialogFlags* of `Engine.DisplayFileDialog`

# Constants

TestStand API constants like AdapterKeyNames, DefaultModelCallbacks, StepProperties and StepTypes are mapped to gRPC services. The service has an RPC get method for each constant that returns the value for that constant. For example, AdapterKeyNames is defined and used as follows:

```
service AdapterKeyNames {
	rpc Get_StdCVIAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_FlexCAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_LVAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_GAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_SequenceAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_AutomationAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_NoneAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_HTBasicAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_FlexLVAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_FlexCVIAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_DotNetAdapterKeyname(ConstantValueRequest) returns (stringResponse);
	rpc Get_LabVIEWNXGAdapterKeyName(ConstantValueRequest) returns (stringResponse);
	rpc Get_PythonAdapterKeyName(ConstantValueRequest) returns (stringResponse);
}

var adapterKeyNamesClient = new AdapterKeyNames.AdapterKeyNamesClient(gRPCChannel);
string lvAdapterName = adapterKeyNamesClient.Get_LVAdapterKeyName(new ConstantValueRequest()).ReturnValue;
```

# Variant mapping to Protobuf Data Type
Parameters that are optional, accept more than one type of object, or can return different value types are defined as `VARIANT` data types in the TestStand API.

Since the gRPC API knows what data type each `VARIANT` parameter expects, it maps each parameter to a specific Protobuf data type. For example, `Engine.NewExecution` has three optional parameters: `sequenceArgsParam`, `editArgsParam`, and `interactiveArgsParam`.  All of them expect an instance of `PropertyObject`, so the gRPC API maps all three parameters to the `PropertyObjectInstance` Protobuf data type.

```
message IEngine_NewExecutionRequest {
	IEngineInstance instance = 1;  
	SequenceFileInstance sequenceFileParam = 2;  
	string sequenceNameParam = 3;  
	SequenceFileInstance processModelParam = 4; 
	bool breakAtFirstStep = 5;
	ExecutionTypeMask executionTypeMaskParam = 6;
	PropertyObjectInstance sequenceArgsParam = 7;
	PropertyObjectInstance editArgsParam = 8;
	PropertyObjectInstance InteractiveArgsParam = 9;
}
```

Protobuf does not have a variant data type so, for parameters that are truly dynamic, like the `newValue` parameter of `Engine.SetInternalOption`, the gRPC API uses the `oneof` keyword. As mentioned earlier, the gRPC API knows what data type each `VARIANT` data type expects so it is able to map dynamic data types to specific Protobuf types. This is how the gRPC API maps the `value` parameter of `Engine.SetInternalOption`:

```
message IEngine_SetInternalOptionRequest {
	IEngineInstance instance = 1;
	InternalOptions option = 2;
	oneof value { bool boolean = 3; int32 integer = 4; double double = 5; string string = 6; ObjectInstance reference = 7; }
}
```

Using `oneof` simplifies passing a boolean value like this:

```
var internalOptionRequest = new EngineClass_SetInternalOptionRequest
	{
		Instance = engineInstance,
		Option = InternalOptions.InternalOptionWarnOnApicallThroughDispatchInterface,
		Boolean = true
	}
```

# Events

Each event maps to a registration method and a reply method. The registration method is named 
GetEvents_[EventName]. This method returns a ResponseStream field. A client typically creates a loop
that reads from the stream asynchronously, such that the loop does not block the client while it waits for
the next item in the stream. Each new item read from the stream represents an event that
occurred on the server. 

When you register for an event, you pass options that determine whether the server
waits for you to respond before allowing the event to complete. To respond, you call
the corresponding ReplyToEvent_[EventName] method. If the event has output parameters, you can provide
the output values to the server when you call ReplyToEvent_[EventName]. 

Documentation for each pair of GetEvents_ and ReplyToEvent_ methods is provided in C# with intellisense and in
the .proto file that defines them.

Refer to the HandleUIMessages() method in the Client\Example.cs file for an example of subscribing to
and handling an event.

# .proto files

The gRPC version of the TestStand API consists of multiple .proto files that contain different
portions of the API. The files and their contents are as follows:

NationalInstruments.TestStand.API.proto - The core TestStand API, including interfaces/services such
as Engine, SequenceFile, and Step.

NationalInstruments.TestStand.AdapterAPI.proto - Interfaces for configuring module adapters 
and module calls.

TSSync.proto - Interfaces for Locks, Queues, Notifications, Rendezvous, and other TestStand 
synchronization objects.

NationalInstruments.TestStand.UI.proto - The TestStand UI controls, including visible controls such
as the SequenceView and ExpressionEdit, and non-visible controls such as the ApplicationMgr,
SequenceFileViewMgr, and ExecutionViewMgr.

NationalInstruments.TestStand.UI.Support.proto - Interfaces for connecting functionality to 
the TestStand UI controls, including SelectedSteps, Command, and CommandConnection.

NationalInstruments.TestStand.SequenceAnalyzer.proto - Interfaces for analyzing sequence files.

instance_lifetime_api.proto - An interface for controlling the lifetime of object instance handles
from a TestStand gRPC client.

common_types_api.proto - Some common message types that are used by multiple .proto files.

# Namespaces

The C# namespaces of the gRPC APIs are similar to the namespaces of their corresponding local APIs. 
You can find the namespace at the top of each .proto file. For example, 
NationalInstruments.TestStand.API.proto contains the following statement:  

	option csharp_namespace = "NationalInstruments.TestStand.API.Grpc";

Here is a list with the gRPC namespace and the original local API namespace for each .protofile:  

**NationalInstruments.TestStand.API.proto**  
	gRPC:		NationalInstruments.TestStand.API.Grpc  
	Local:		NationalInstruments.TestStand.Interop.API  

**NationalInstruments.TestStand.AdapterAPI.proto**  
	gRPC:		NationalInstruments.TestStand.AdapterAPI.Grpc  
	Local:	 	NationalInstruments.TestStand.Interop.AdapterAPI  

**TSSync.proto**  
	gRPC:		TSSync.Grpc  
	Local:		TSSyncLib  

**NationalInstruments.TestStand.UI.proto**  
	gRPC:		NationalInstruments.TestStand.UI.Grpc  
	Local:	 	NationalInstruments.TestStand.Interop.UI  

**NationalInstruments.TestStand.UI.Support.proto**  
	gRPC:		NationalInstruments.TestStand.UI.Support.Grpc  
	Local:	 	NationalInstruments.TestStand.Interop.UI.Support  

**NationalInstruments.TestStand.SequenceAnalyzer.proto**  
	gRPC: 		NationalInstruments.TestStand.SequenceAnalyzer.Grpc  
	Local:		NationalInstruments.TestStand.Interop.SequenceAnalyzer  

**instance_lifetime_api.proto**  
	gRPC: 		NationalInstruments.TestStand.Grpc.Net.Client.OO  
	Local: 		NationalInstruments.TestStand.Grpc.Net.Server.OO  
		
**common_types_api.proto**   
	gRPC: 		NationalInstruments.TestStand.API.Grpc  
	Local:		Not Applicable  
