REM This example assumes you have openssl installed in your machine. See https://wiki.openssl.org/index.php/Binaries.

SET mypass=password123
SET NumberOfDaysCertificateIsValid=3650
SET RootSubject="/C=US/ST=Texas/L=Austin/O=NI/OU=TestStand/CN=localhost.root"
SET ServerSubject="/C=US/ST=Texas/L=Austin/O=NI/OU=TestStand/CN=localhost.Server"
SET ServerFriendlyName="TestStand gRPC Server"
SET ClientSubject="/C=US/ST=Texas/L=Austin/O=NI/OU=TestStand/CN=localhost.Client"
SET ClientFriendlyName="TestStand gRPC Client"
SET SizeOfKeyInBits=4096

REM Generate Certificate Authority (CA) key and certificate
openssl genrsa -passout pass:%mypass% -des3 -out ca.key %SizeOfKeyInBits%
openssl req -passin pass:%mypass% -new -x509 -days %NumberOfDaysCertificateIsValid% -key ca.key -out ca.crt -subj %RootSubject%

REM Generate Server Certificate
openssl genrsa -passout pass:%mypass% -des3 -out server_privatekey.pem %SizeOfKeyInBits%

REM Generate server certificate signing request (csr)
openssl req -passin pass:%mypass% -new -key server_privatekey.pem -out server_csr.pem -subj %ServerSubject%

REM Signed server certificate using Certificate Authority
REM Need to provide alternate name when using self-signed certificate - https://stackoverflow.com/questions/60341743/why-do-i-get-remotecertificatenamemismatch
openssl x509 -req -passin pass:%mypass% -days %NumberOfDaysCertificateIsValid% -extfile extensions.conf -extensions alt_names -in server_csr.pem -CA ca.crt -CAkey ca.key -CAcreateserial -out server_self_signed_crt.pem

REM Remove passphrase from server key
openssl rsa -passin pass:%mypass% -in server_privatekey.pem -out server_privatekey.pem

REM Create pfx for ASP.NET Core server
openssl pkcs12 -password pass:%mypass% -export -name %ServerFriendlyName% -out server.pfx -inkey server_privatekey.pem -in server_self_signed_crt.pem -certfile ca.crt


REM Generate client Certificate
openssl genrsa -passout pass:%mypass% -des3 -out client_privatekey.pem %SizeOfKeyInBits%

REM Generate client certificate signing request (csr)
openssl req -passin pass:%mypass% -new -key client_privatekey.pem -out client_csr.pem -subj %ClientSubject%

REM Signed client certificate using Certificate Authority
openssl x509 -passin pass:%mypass% -req -days %NumberOfDaysCertificateIsValid% -in client_csr.pem -CA ca.crt -CAkey ca.key -CAcreateserial -out client_self_signed_crt.pem

REM Remove passphrase from client key
openssl rsa -passin pass:%mypass% -in client_privatekey.pem -out client_privatekey.pem

REM Create pfx for client
openssl pkcs12 -password pass:%mypass% -export -name %ClientFriendlyName% -out client.pfx -inkey client_privatekey.pem -in client_self_signed_crt.pem -certfile ca.crt

openssl verify -CAfile ca.crt server_self_signed_crt.pem
openssl verify -CAfile ca.crt client_self_signed_crt.pem

del server_csr.pem
del client_csr.pem