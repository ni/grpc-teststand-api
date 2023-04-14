using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NationalInstruments.TestStand.Grpc.Client.Utilities;

namespace ExampleClient
{
    /// <summary>
    /// This parser will read the server json configuration file and will load any specified certificates.
    /// It expects the names and locations specified in this page: TBD.
    /// </summary>
    public class ClientConfigurationParser
    {
        private const string CertificatesFolderName = "certs";
        private const string DefaultConfigFileName = "client_config.json";

        public ClientConfigurationParser(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath))
            {
                configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DefaultConfigFileName);
            }

            if (File.Exists(configFilePath))
            {
                var input = new StreamReader(configFilePath);
                var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                Options = JsonSerializer.Deserialize<ClientOptions>(input.ReadToEnd(), serializerOptions);

                string certificatesFolder = Path.Combine(Path.GetDirectoryName(configFilePath), CertificatesFolderName);
                Options.SetCertificatePaths(certificatesFolder);
            }
            else
            {
                // If no config file exists, assume client does not want to use certificates.
                Options = new ClientOptions();
            }
        }

        public ClientOptions Options { get; private set; }
    }
    
    public class Security
    {
        [JsonPropertyName("server_cert")]
        public string ServerCertificateFilename { get; set; } = string.Empty;

        [JsonPropertyName("client_cert")]
        public string ClientCertificateFilename { get; set; } = string.Empty;

        [JsonPropertyName("client_key")]
        public string ClientKeyFilename { get; set; } = string.Empty;

        [JsonPropertyName("client_cert_pfx")]
        public string ClientCertificagePFXFilename { get; set; } = string.Empty;

        [JsonPropertyName("client_cert_pfx_password")]
        public string ClientCertificatePFXFilePassword { get; set; } = string.Empty;
    }
}
