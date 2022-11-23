# Encrypting Connections to the Server
The TestStand gRPC server supports both server-side Transport Layer Security (TLS) and mutual TLS connections. If you do not configure an encrypted connection, the connection from the client to the server is not secure. When you want to provide your own certificates for a secure connection, either use a trusted certificate from a certified authority, or create a self-signed certificate with OpenSSL or another tool. See `SelfSignedServerCertificates\GenerateSelfSignedServerAndClientCertificates.bat` for an example of how to create self-signed certificates using OpenSSL.

NI recommends using mutual TLS connections for production environments, as they are the most secure type of connection. Server-side TLS connections may be acceptable for testing environments depending on your security requirements.

---

## Configuring a Server-Side TLS Connection
In a server-side TLS connection, only the server provides a certificate file. The client has a copy of the server certificate file, and uses it to authenticate the server identity and enable an encrypted connection.

1. Review the folder structure of the server and client files. Both the server and the client expect the certificate files listed in the configuration file to exist in a `certs` folder located in the same directory as the configuration file.

    The default case, used in this example, requires the `certs` folder to be in the same folder as the executable for both the server and client as illustrated below:
    ```
    installation_folder/
    ├── certs/
    │   ├── <server or client>_self_signed_crt.pem
    │   ├── <server or client>_privatekey.pem
    │   └── <server or client>_self_signed_crt.pem
    ├── <server or client_executable file>
    └── <server or client>_config.json
    ```

    If you specify a path to the configuration file when starting the server or client, then you must ensure the `certs` folder is present in the same location as the configuration file you specify. 
    ```
    installation_folder/
    └── <server or client_executable file>

    config_file_folder/
    ├── certs/
    │   ├── <server or client>_self_signed_crt.pem
    │   ├── <server or client>_privatekey.pem
    │   └── <server or client>_self_signed_crt.pem
    └── <server or client>_config.json
    ```


2. On the server, open `Server\certs` and review the contents. A server-side TLS connection uses the following files:
    - `server_self_signed_crt.pem` - The server certificate file. In this example, the file is a self-signed certificate. The client uses the public key embedded in this file to authenticate the server.
    - `server_privatekey.pem` - The server private key.
    
    You can save your own certificates in this folder using either `.pem` or `.pfx` file formats.

2. Open `Server\server_config.json`. This configuration file provides the server application with certificate information. Review the following key/value pairs:
    - `server_cert` - Name of the server certificate file, `server_self_signed_crt.pem`. Alternatively, you can use the friendly name of the certificate specified in the Windows Certificate Store. If you use this configuration, the `server_key` field is not used and you can leave it empty.
    - `server_key` - Name of the server private key, `server_privatekey.pem`. 
    
    If you add your own certificates in `Server\certs`, change the values to the names of your certificate files.
    
    If you add a `.pfx` certificate in `Server\certs`, enter values for the following keys:
    - `server_cert_pfx` - Name of the certificate file.
    - `server_cert_pfx_password` - Password of the certificate file.

    When the server reads the config file, it first looks for a `.pfx` file. If it does not find one, it then looks for values for `server_cert` and `server_key`. 

3. On the client, open `Client\certs` and review the contents. A server-side TLS connection uses the following file:
    - `server_self_signed_crt.pem` - A copy of the server certificate file.

4. Open `Client\client_config.json`. This configuration file provides the client application with certificate information. Review the following key/value pair:
    - `server_cert` - Name of the server certificate file, `server_self_signed_crt.pem`. 

---

## Configuring a Mutual TLS Connection
In a mutual TLS connection, both the server and client have their own certificates. The client has a copy of the server certificate file, and vice versa. In this scenario, the client and server both use the certificate from the opposite machine to authenticate each others' identity and send encrypted data. 

1. Complete the server-side portion of the steps described in [Configuring a Server-Side TLS Connection](#configuring-a-server-side-tls-connection). 

2. Include the following additional server-side components required for mutual TLS: 

    - `client_self_signed_crt.pem` - A copy of the client certificate file in `Server\certs`. The server uses this file to authenticate the client.

    - `root_cert` - Name of the client certificate file, `client_self_signed_crt.pem`. Ensure this key/value pair is defined in `Server\server_config.json`.

2. On the client, open `Client\certs` and review the contents. A mutual TLS connection uses the following files:
    - `server_self_signed_crt.pem` - A copy of the server certificate file.
    - `client_self_signed_crt.pem` - The client certificate file. In this example, the file is a self-signed certificate. The server uses the public key embedded in this file to authenticate the client.
    - `client_privatekey.pem` - The client private key.
    
    You can save your own certificates in this folder using either `.pem` or `.pfx` file formats.

3. Open `Client\client_config.json`. This configuration file provides the client application with certificate information. Review the values for the following key/value pairs:
    - `client_cert` - Name of the client certificate file, `client_self_signed_crt.pem`.
    - `client_key` - Name of the client private key, `client_privatekey.pem`.
    - `server_cert` - Name of the server certificate file, `server_self_signed_crt.pem`.
    
    If you add your own certificates in `Client\certs`, change the values to the names of your certificate files.

    If you replace this file with a `.pfx` certificate in `Client\certs`, enter values for the following keys:
    - `client_cert_pfx` - Name of the certificate file.
    - `client_cert_pfx_password` - Password of the certificate file.

    When the client reads the config file, it first looks for a `.pfx` file. If it does not find one, it then looks for values for `client_cert` and `client_key`. 

---

## See Also
[OpenSSL](https://www.openssl.org/)