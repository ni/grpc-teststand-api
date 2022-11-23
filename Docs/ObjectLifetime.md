# Object Lifetime

When a server returns or sends an object reference to a client, the reference is returned as an instance handle
that contains a string identifier.  The client uses the instance handle refer to the object when it makes
subsequent calls to the server.

An instance handle must be released at some point, or the object it refers to will never be eligible
to be garbage collected on the server. However, an instance handle must not be released until the client
is done using it. The InstanceLifetime service enables a client to manage the lifetime of the 
instances handles it receives.

To use the InstanceLifetime Service from a C# project that uses the TestStand API Nuget package, add a using 
statement that specifies the namespace NationalInstruments.TestStand.Grpc.Net.Client.OO;  
Then create an InstanceLifetime.InstanceLifetimeClient and use it to call methods in the service. An examples
of doing this can be found in Client\Example.cs.

The service and methods have intellisense documentation in C#. If you are not using C#, the definition
of the InstanceLifetime Service and the corresponding documentation can be found in the
instance_lifetime_api.proto file that is included with the other api proto files.

#InstanceLifetime Service Methods
You can explicitly release or specify the lifetime of specific objects by calling:
Release(), SetLifespan(), and SetDefaultLifespan().

You can release all instance handles that were allocated for a client between markers by calling:
GetMarker() and ClearFromMarker().

You can release all instance handles for a client by calling:
Clear().

You can obtain information about object lifetimes by calling:
NumberOfInstances(), GetLifespan(), and GetDefaultLifespan().

The structure and function of your client application will determine whether it is more convenient
for you to manage the lifetime of object references individually, or to periodically
release all object references or a subset that were allocated between markers.
 
Note that lifetimes of object instance handles returned in an event stream are also governed by
the parameters you specific when establishing the event stream. Refer to the request parameter to
a GetEvents_[EventName]() method for more information. 

