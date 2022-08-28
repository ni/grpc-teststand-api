# Encrypting Connections to the Server
The TestStand gRPC server supports both server-side TLS and mutual TLS connections. If you do not configure an encrypted connection, by default the connection from the client to the server is not secure. When you want to provide your own certificates for a secure connection, either use a trusted certificate from a certified authority, or create a self-signed certificate with OpenSSL or another tool. 

NI recommends using mutual TLS connections for production environments, as they are the most secure type of connection. Server-side TLS connections may be acceptable for testing environments depending on your security requirements.

---

## Configuring a Server-Side TLS Connection
In a server-side TLS connection, only the server provides a certificate file. The client has a copy of the server certificate file, and uses it to validate the server and send encrypted data to the server. The server returns data that is not encrypted to the client.

1. On the server, open `Server\certs` and review the contents. A server-side TLS connection uses the following files:
    - `server_self_signed_crt.pem` - The server certificate file. In this example, the file is a self-signed certificate. The client uses the public key embedded in this file to authenticate the server and encrypt data it sends to the server.
    - `server_privatekey.pem` - The server private key. The server uses this key to decrypt data sent by the client.
    
    You can save your own certificates in this folder using either `.pem` or `.pfx` file formats.

2. Open `Server\server_config.json`. This configuration file provides the server application with certificate information. Review the following key/value pairs:
    - `server_cert` - Name of the server certificate file, `server_self_signed_crt.pem`.
    - `server_key` - Name of the server private key, `server_privatekey.pem`. 
    
    If you add your own certificates in `Server\certs`, change the values to the names of your certificate files.
    
    If you add a `.pfx` certificate in `Server\certs`, enter values for the following keys:
    - `server_cert_pfx` - Name of the certificate file.
    - `server_cert_pfx_password` - Name of the certificate key.

3. On the client, open `Client\certs` and review the contents. A server-side TLS connection uses the following file:
    - `server_self_signed_crt.pem` - A copy of the server certificate file.

4. Open `Client\client_config.json`. This configuration file provides the client application with certificate information. Review the following key/value pair:
    - `server_cert` - Name of the server certificate file, `server_self_signed_crt.pem`. 

---

## Configuring a Mutual TLS Connection
In a mutual TLS connection, both the server and client have their own certificates. The client has a copy of the server certificate file, and vice versa. In this scenario, the client and server both use the certificate from the opposite machine to validate the connection and send encrypted data. 

1. On the server, open `Server\certs` and review the contents. A mutual TLS connection uses the following files:
    - `server_self_signed_crt.pem` - The server certificate file. In this example, the file is a self-signed certificate. The client uses the public key embedded in this file to authenticate the server and encrypt data it sends to the server.
    - `server_privatekey.pem` - The server private key. The server uses this key to decrypt data sent by the client.
    - `client_self_signed_crt.pem` - A copy of the client certificate file. The server uses this file to authenticate the client and encrypt data it sends to the client.
    
    You can save your own certificates in this folder using either `.pem` or `.pfx` file formats.

2. Open `Server\server_config.json`. This configuration file provides the server application with certificate information. Review the values for the following key/value pairs:
    - `server_cert` - Name of the server certificate file, `server_self_signed_crt.pem`.
    - `server_key` - Name of the server private key, `server_privatekey.pem`.
    - `root_cert` - Name of the client certificate file, `client_self_signed_crt.pem`.
    
    If you add your own certificates in `Server\certs`, change the values to the names of your certificate files.
    
    If you replace this file with a `.pfx` certificate in `Server\certs`, enter values for the following keys:
    - `server_cert_pfx` - Name of the certificate file.
    - `server_cert_pfx_password` - Name of the certificate key.

3. On the client, open `Client\certs` and review the contents. A mutual TLS connection uses the following files:
    - `server_self_signed_crt.pem` - A copy of the server certificate file.
    - `client_self_signed_crt.pem` - The client certificate file. In this example, the file is a self-signed certificate. The server uses the public key embedded in this file to authenticate the client and encrypt data it sends to the client.
    - `client_privatekey.pem` - The client private key. The client uses this key to decrypt data sent by the server.
    
    You can save your own certificates in this folder using either `.pem` or `.pfx` file formats.

4. Open `Client\client_config.json`. This configuration file provides the client application with certificate information. Review the values for the following key/value pairs:
    - `client_cert` - Name of the client certificate file, `client_self_signed_crt.pem`.
    - `client_key` - Name of the client private key, `client_privatekey.pem`.
    - `server_cert` - Name of the server certificate file, `server_self_signed_crt.pem`.
    
    If you add your own certificates in `Client\certs`, change the values to the names of your certificate files.

    If you replace this file with a `.pfx` certificate in `Client\certs`, enter values for the following keys:
    - `client_cert_pfx` - Name of the certificate file.
    - `client_cert_pfx_password` - Name of the certificate key.

---

## See Also
[OpenSSL](https://www.openssl.org/)