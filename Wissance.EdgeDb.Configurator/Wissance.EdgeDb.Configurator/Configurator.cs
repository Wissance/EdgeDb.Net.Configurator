using System.Net;
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
        public static void ConfigureLocalEdgeDbDatabase(this IServiceCollection services, string projectName, EdgeDBClientPoolConfig poolCfg)
        {
            ConfigureEdgeDbDatabaseImpl(services, IPAddress.Loopback.ToString(), projectName, poolCfg);
        }
        
        public static void ConfigureEdgeDbDatabase(this IServiceCollection services, string hostName, string projectName, EdgeDBClientPoolConfig poolCfg,
            string[] configSearchDirs, bool includeDefault)
        {
            ConfigureEdgeDbDatabaseImpl(services, hostName, projectName, poolCfg, configSearchDirs, includeDefault);
        }

        private static void ConfigureEdgeDbDatabaseImpl(IServiceCollection services, string hostName, string projectName,
            EdgeDBClientPoolConfig poolCfg, string[] configSearchDirs = null, bool includeDefault = true)
        {
            ILoggerFactory loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
            ILogger<object> logger = loggerFactory?.CreateLogger<object>();
            IList<string> configSearchDirectories = GetEdgeDbConfigSearchDirectories(logger, configSearchDirs, includeDefault);
            Tuple<string, EdgeDbProjectCredentialsSettings> connOptions = GetEdgeDbConnStrByProjectName(logger, hostName, projectName, configSearchDirectories);
            if (string.IsNullOrEmpty(connOptions.Item1))
            {
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

        private static Tuple<string, EdgeDbProjectCredentialsSettings> GetEdgeDbConnStrByProjectName(ILogger<object> logger, string hostName, string projectName, 
            IList<string> configSearchDirectories)
        {
            string projectCredentialsFile = string.Empty;
            bool configFound = false;
            foreach (string searchDirectory in configSearchDirectories)
            {
                projectCredentialsFile = GetEdgeDbProjectCredentialFile(searchDirectory, projectName);
                if (File.Exists(projectCredentialsFile))
                {
                    configFound = true;
                    break;
                }
            }

            if (!configFound)
            {
                logger?.LogWarning($"Edgedb project \"{projectName}\" credentials file wasn't found in search directories, ensure that edgedb credentials file exists");
                return new Tuple<string, EdgeDbProjectCredentialsSettings>(null, null);
            }

            string content = File.ReadAllText(projectCredentialsFile);
            EdgeDbProjectCredentialsSettings credentials = JsonSerializer.Deserialize<EdgeDbProjectCredentialsSettings>(content);
            if (credentials == null)
                return new Tuple<string, EdgeDbProjectCredentialsSettings>(null, null);
            return new Tuple<string, EdgeDbProjectCredentialsSettings>(string.Format(EdgeDbConnStrTemplate, credentials.User, credentials.Password, 
                hostName, credentials.Port, credentials.Database), credentials);
        }
        
        private static string GetEdgeDbProjectCredentialFile(string directory, string projectName)
        {
            string projectCredentialsFile = Path.Combine(directory, $"{projectName}.json");
            return projectCredentialsFile;
        }

        private static IList<string> GetEdgeDbConfigSearchDirectories(ILogger<object> logger, string[] configSearchDirs, bool includeDefault)
        {
            // Config search order - 1. directories in configSearchDirs, 2 - default dirs\
            List<string> dirs = new List<string>();
            if (configSearchDirs != null && configSearchDirs.Any())
            {
                dirs.AddRange(configSearchDirs);
            }

            if (includeDefault)
            {
                if (OperatingSystem.IsWindows())
                {
                    // default project credentials files is located in dir ~AppData\Local\EdgeDB\config\credentials\{projectName}.json
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string projectCredentialsDir = Path.Combine(new string[]
                    {
                        appData,
                        "EdgeDB", "config", "credentials",
                    });
                    dirs.Add(projectCredentialsDir);
                }
                else
                {
                    if (OperatingSystem.IsLinux())
                    {
                        // linux - /.config/edgedb/credentials/{projectName}.json relative to home dir
                        string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                        string projectCredentialsDir = Path.Combine(new string[]
                        {
                            home,
                            ".config", "edgedb", "credentials"
                        });
                        dirs.Add(projectCredentialsDir);
                    }
                    else
                    {
                        logger?.LogWarning("Unsupported platform for EdbeDB configuration");
                        // todo(UMV): other platform don't not supported now, please add issue here: https://github.com/Wissance/EdgeDb.Net.Configurator/issues
                    }
                }
            }

            return dirs;
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