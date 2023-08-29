using System.Text.Json.Serialization;

namespace Wissance.EdgeDb.Configurator.Settings
{
    public class EdgeDbProjectCredentialsSettings
    {
        [JsonPropertyName("port")]
        public int Port { get; set; }
        [JsonPropertyName("user")]
        public string User { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
        [JsonPropertyName("database")]
        public string Database { get; set; }
        [JsonPropertyName("tls_cert_data")]
        public string TlsCertData { get; set; }
        [JsonPropertyName("tls_ca")]
        public string TlsCa { get; set; }
        [JsonPropertyName("tls_security")]
        public string TlsSecurity { get; set; }
    }
}