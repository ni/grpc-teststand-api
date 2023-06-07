using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NationalInstruments.TestStand.Grpc.Client.Utilities
{
    /// <summary>
    /// The ClientConfiguration reads the client json configuration file and will load any specified certificates.
    /// It expects the names and locations specified in this page: TBD.
    /// </summary>
    public class ClientConfiguration
    {
        private static readonly string assemblyDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static string DefaultConfigFilepath { get; } = Path.Combine(assemblyDirectoryPath, "client_config.json");
        public static string DefaultCertificatesFolderpath { get; } = Path.Combine(assemblyDirectoryPath, "certs");

        public ClientConfiguration(bool useSecureConnection)
        {
            InitializeClientConfiguration(useSecureConnection, DefaultConfigFilepath, DefaultCertificatesFolderpath);
        }

        public ClientConfiguration(bool useSecureConnection, string configFilePath, string certificatesDirectoryPath)
        {
            InitializeClientConfiguration(useSecureConnection, configFilePath, certificatesDirectoryPath);
        }

        private void InitializeClientConfiguration(bool useSecureConnection, string configFilePath, string certificatesDirectoryPath)
        {
            if (!useSecureConnection)
            {
                Options = new ClientOptions();
            }
            else
            {
                if (!File.Exists(configFilePath))
                {
                    throw new ArgumentException(configFilePath, nameof(configFilePath));
                }

                if (!Directory.Exists(certificatesDirectoryPath)) 
                {
                    throw new ArgumentException(certificatesDirectoryPath, nameof(certificatesDirectoryPath));
                }
                
                var input = new StreamReader(configFilePath);
                var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                Options = JsonSerializer.Deserialize<ClientOptions>(input.ReadToEnd(), serializerOptions);
                Options.SetCertificatePaths(certificatesDirectoryPath);
            }
        }

        public ClientOptions Options { get; private set; }
    }

    public class ClientOptions
    {
        // 5020 is used to avoid conflicting with NI Services
        public int Port { get; set; } = 5020;

        public Security Security { get; set; } = new Security();

        [JsonPropertyName("example_files")]
        public string[] ExampleFiles { get; set; }

        public bool UseSecureConnection { get; set; }

        public string ServerCertificatePath { get; set; } = null;

        public string ServerCertificateFriendlyName { get; set; } = null;

        public string ClientCertificatePath { get; set; } = null;

        public string ClientKeyPath { get; set; } = null;

        public string ClientCertificatePFXPath { get; set; } = null;

        public string ClientCertificatePFXPassword => Security.ClientCertificatePFXFilePassword;

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

                UseSecureConnection = true;
            }
            if (!string.IsNullOrEmpty(Security.ClientCertificateFilename))
            {
                ClientCertificatePath = Path.Combine(certificatesFolder, Security.ClientCertificateFilename);
            }
            if (!string.IsNullOrEmpty(Security.ClientKeyFilename))
            {
                ClientKeyPath = Path.Combine(certificatesFolder, Security.ClientKeyFilename);
            }
            if (!string.IsNullOrEmpty(Security.ClientCertificagePFXFilename))
            {
                ClientCertificatePFXPath = Path.Combine(certificatesFolder, Security.ClientCertificagePFXFilename);
            }
        }
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
