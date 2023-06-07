using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NationalInstruments.TestStand.Grpc.Server.Utilities
{
    /// <summary>
    /// The ServerConfiguration reads the server json configuration file and will load any specified certificates.
    /// It expects the names and locations specified in this page: https://github.com/ni/grpc-device/wiki/Server-Security-Support.
    /// </summary>
    public class ServerConfiguration
	{
		internal const string DefaultCertificatesFolderName = "certs";
		private const string DefaultConfigFileName = "server_config.json";

        internal static string GetDefaultConfigFilePath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultConfigFileName);
		internal static string GetCertificateDirectoryPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultCertificatesFolderName);

        public ServerConfiguration(bool useSecureConnection, string configFilePath)
        : this(useSecureConnection, configFilePath, GetCertificateDirectoryPath())
        {
        }

        public ServerConfiguration(bool useSecureConnection, string configFilePath, string certificateDirectoryPath)
        {
            InitializeServerConfiguration(useSecureConnection, configFilePath ?? GetDefaultConfigFilePath(), certificateDirectoryPath);
		}

		private void InitializeServerConfiguration(bool useSecureConnection, string configFilePath, string certificateDirectoryPath)
		{
            if (!useSecureConnection)
            {
                Options = new ServerOptions();
            }
            else
            {
                if (!File.Exists(configFilePath))
                {
                    throw new ArgumentException(configFilePath, nameof(configFilePath));
                }

                if (!Directory.Exists(certificateDirectoryPath))
                {
                    throw new ArgumentException(certificateDirectoryPath, nameof(certificateDirectoryPath));
                }

                var input = new StreamReader(configFilePath);
                var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                Options = JsonSerializer.Deserialize<ServerOptions>(input.ReadToEnd(), serializerOptions);
                Options.SetCertificatePaths(certificateDirectoryPath);
            }
        }

		public ServerOptions Options { get; private set; }
    }

    public class ServerOptions
    {
        // 5020 is used to avoid conflicting with NI Services
        public int Port { get; set; } = 5020;

        public ServerSecurity Security { get; set; } = new ServerSecurity();

        public Cors Cors { get; set; } = new Cors();

        public bool UseSecureConnection { get; set; }

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

            UseSecureConnection = !string.IsNullOrEmpty(ServerCertificatePFXPath)
                || !string.IsNullOrEmpty(ServerCertificateFriendlyName)
                || (!string.IsNullOrEmpty(ServerCertificatePath) && !string.IsNullOrEmpty(ServerKeyPath));
        }
    }
    
    public class ServerSecurity
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
