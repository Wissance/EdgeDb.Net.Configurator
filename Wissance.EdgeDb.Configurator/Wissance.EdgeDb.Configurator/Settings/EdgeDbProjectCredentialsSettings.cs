using System.Text.Json.Serialization;

namespace Wissance.EdgeDb.Configurator.Settings
{
    public class EdgeDbProjectCredentialsSettings
    {
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string Database { get; set; }
        [JsonPropertyName("tls_cert_data")]
        public string TlsCertData { get; set; }
        [JsonPropertyName("tls_ca")]
        public string TlsCa { get; set; }
        [JsonPropertyName("tls_security")]
        public string TlsSecurity { get; set; }
    }
}