using System.Text.Json;
using System.Text.Json.Serialization;
using EdgeDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wissance.EdgeDb.Configurator.Settings;

namespace Wissance.EdgeDb.Configurator
{
    public static class Configurator
    {
        public static void ConfigureEdgeDbDatabase(this IServiceCollection services, string projectName, EdgeDBClientPoolConfig poolCfg)
        {
            ILoggerFactory loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
            // todo(UMV): Resolve security settings using Project Name: that is why important to have same project name for all Backend developers
            // use ~AppData\Local\EdgeDB\config\credentials
            Tuple<string, EdgeDbProjectCredentialsSettings> connOptions = GetEdgeDbConnStrByProjectName(projectName);
            if (string.IsNullOrEmpty(connOptions.Item1))
            {
                ILogger<object> logger = loggerFactory?.CreateLogger<object>();
                logger?.LogError("Provider Project name or path to credentials file doesn't exists or your system is not supported (allowed: Windows, Linux)");
            }

            EdgeDBConnection conn = EdgeDBConnection.FromDSN(connOptions.Item1);
            
            // additionally set security mode
            conn.TLSSecurity = SecurityModes[connOptions.Item2.TlsSecurity];
            conn.TLSCertificateAuthority = connOptions.Item2.TlsCa;
            conn.SecretKey = connOptions.Item2.TlsCertData;

            services.AddEdgeDB(conn, cfg =>
            {
                poolCfg.Logger = loggerFactory?.CreateLogger<EdgeDBClient>();
            });
        }

        private static Tuple<string, EdgeDbProjectCredentialsSettings> GetEdgeDbConnStrByProjectName(string projectName)
        {
            string projectCredentialsFile = GetEdgeDbProjectCredentialFile(projectName);
            string content = File.ReadAllText(projectCredentialsFile);
            EdgeDbProjectCredentialsSettings credentials = JsonSerializer.Deserialize<EdgeDbProjectCredentialsSettings>(content);
            if (credentials == null)
                return new Tuple<string, EdgeDbProjectCredentialsSettings>(null, null);
            return new Tuple<string, EdgeDbProjectCredentialsSettings>(string.Format(EdgeDbConnStrTemplate, credentials.User, credentials.Password, 
                "localhost", credentials.Port, credentials.Database), credentials);
        }


        private static string GetEdgeDbProjectCredentialFile(string projectName)
        {
            string projectCredentialsFile = string.Empty;
            if (OperatingSystem.IsWindows())
            {
                // should be using ~AppData\Local\EdgeDB\config\credentials\{projectName}.json as a path
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                projectCredentialsFile = Path.Combine(new string[]
                {
                    appData,
                    "EdgeDB", "config", "credentials",
                    projectName + ".json"
                });
            }
            else
            {
                if (OperatingSystem.IsLinux())
                {
                    // linux - /.config/edgedb/credentials/{projectName}.json relative to home dir
                    string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    projectCredentialsFile = Path.Combine(new string[]
                    {
                        home,
                        ".config", "edgedb", "credentials",
                        projectName + ".json"
                    });
                }
                else
                {
                    throw new NotImplementedException(
                        "Other systems are not implemented, if you would like to contribute visit: \"https://github.com/Wissance/EdgeDb.Net.Configurator\"");
                }
            }

            return projectCredentialsFile;
        }

        private static readonly IDictionary<string, TLSSecurityMode> SecurityModes =
            new Dictionary<string, TLSSecurityMode>()
            {
                {"default", TLSSecurityMode.Default},
                {"insecure", TLSSecurityMode.Insecure},
                {"strict", TLSSecurityMode.Strict},
                {"no_hostname_verification", TLSSecurityMode.NoHostnameVerification}
            };
        private const string EdgeDbConnStrTemplate = "edgedb://{0}:{1}@{2}:{3}/{4}";
    }
}