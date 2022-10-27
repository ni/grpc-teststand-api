using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestStandGrpcApi
{
    /// <summary>
    /// This parser will read the server json configuration file and will load any specified certificates.
    /// It expects the names and locations specified in this page: https://github.com/ni/grpc-device/wiki/Server-Security-Support.
    /// </summary>
    public class ServerConfigurationParser
    {
        private const string CertificatesFolderName = "certs";
        private const string DefaultConfigFileName = "server_config.json";

        public ServerConfigurationParser(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath))
            {
                configFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), DefaultConfigFileName);
            }

            if (File.Exists(configFilePath))
            {
                var input = new StreamReader(configFilePath);
                var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                Options = JsonSerializer.Deserialize<ServerOptions>(input.ReadToEnd(), serializerOptions);

                string certificatesFolder = Path.Combine(Path.GetDirectoryName(configFilePath), CertificatesFolderName);
                Options.SetCertificatePaths(certificatesFolder);
            }
            else
            {
                // If no config file exists, assume server does not want to use certificates
                Options = new ServerOptions();
            }
        }

        public ServerOptions Options { get; private set; }
    }

    public class ServerOptions
    {
        // 5020 is used to avoid conflicting with NI Services
        public int Port { get; set; } = 5020;

        public Security Security { get; set; } = new Security();

        public Cors Cors { get; set; } = new Cors();

        public string ServerCertificatePath { get; set; } = null;

        public string ServerKeyPath { get; set; } = null;

        public string ServerCertificateFriendlyName { get; set; } = null;

        public string ServerCertificatePFXPath { get; set; } = null;

        public string ServerCertificatePFXPassword => Security.ServerCertificatePFXFilePassword;

        public string ClientCertificatePath { get; set; } = null;

        public void SetCertificatePaths(string certificatesFolder)
        {
            if (!string.IsNullOrEmpty(Security.ServerCertificateFilename))
            {
                if (Path.HasExtension(Security.ServerCertificateFilename))
                {
                    ServerCertificatePath = Path.Combine(certificatesFolder, Security.ServerCertificateFilename);
                }
                else
                {
                    ServerCertificateFriendlyName = Security.ServerCertificateFilename;
                }
            }
            if (!string.IsNullOrEmpty(Security.ServerKeyFilename))
            {
                ServerKeyPath = Path.Combine(certificatesFolder, Security.ServerKeyFilename);
            }
            if (!string.IsNullOrEmpty(Security.ClientCertificateFilename))
            {
                ClientCertificatePath = Path.Combine(certificatesFolder, Security.ClientCertificateFilename);
            }
            if (!string.IsNullOrEmpty(Security.ServerCertificagePFXFilename))
            {
                ServerCertificatePFXPath = Path.Combine(certificatesFolder, Security.ServerCertificagePFXFilename);
            }
        }
    }
    
    public class Security
    {
        [JsonPropertyName("server_cert")]
        public string ServerCertificateFilename { get; set; } = string.Empty;

        [JsonPropertyName("server_key")]
        public string ServerKeyFilename { get; set; } = string.Empty;

        [JsonPropertyName("root_cert")]
        public string ClientCertificateFilename { get; set; } = string.Empty;

        [JsonPropertyName("server_cert_pfx")]
        public string ServerCertificagePFXFilename { get; set; } = string.Empty;

        [JsonPropertyName("server_cert_pfx_password")]
        public string ServerCertificatePFXFilePassword { get; set; } = string.Empty;
    }

    // Cors stands for "Cross-Origin Resource Sharing"
    public class Cors
    {
        public bool Enable { get; set; }

        public string[] Origins { get; set; }

        public bool IsEnabled
        {
            get => Enable && (Origins?.Length > 0);
        }
    }
}
