using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PipServices3.Commons.Config;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Components.Auth;
using PipServices3.Components.Connect;

namespace PipServices3.Postgres.Connect
{
    /// <summary>
    /// Helper class that resolves PostgreSQL connection and credential parameters,
    /// validates them and generates a connection URI.
    /// 
    /// It is able to process multiple connections to PostgreSQL cluster nodes.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// connection(s):
    /// - discovery_key:               (optional) a key to retrieve the connection from <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
    /// - host:                        host name or IP address
    /// - port:                        port number (default: 27017)
    /// - database:                    database name
    /// - uri:                         resource URI or connection string with all parameters in it 
    /// 
    /// credential(s):
    /// - store_key:                   (optional) a key to retrieve the credentials from <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_auth_1_1_i_credential_store.html">ICredentialStore</a>
    /// - username:                    user name
    /// - password:                    user password
    /// 
    /// ### References ###
    /// 
    /// - *:discovery:*:*:1.0          (optional) <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services
    /// - *:credential-store:*:*:1.0   (optional) Credential stores to resolve credentials
    /// </summary>
    public class PostgresConnectionResolver: IReferenceable, IConfigurable
    {
        /// <summary>
        /// The connections resolver.
        /// </summary>
        protected ConnectionResolver _connectionResolver = new ConnectionResolver();
        /// <summary>
        /// The credentials resolver.
        /// </summary>
        protected CredentialResolver _credentialResolver = new CredentialResolver();

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public void SetReferences(IReferences references)
        {
            _connectionResolver.SetReferences(references);
            _credentialResolver.SetReferences(references);
        }

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public void Configure(ConfigParams config)
        {
            _connectionResolver.Configure(config, false);
            _credentialResolver.Configure(config, false);
        }

        private void ValidateConnection(string correlationId, ConnectionParams connection)
        {
            var uri = connection.Uri;
            if (uri != null) return;

            var host = connection.Host;
            if (host == null)
                throw new ConfigException(correlationId, "NO_HOST", "Connection host is not set");

            var port = connection.Port;
            if (port == 0)
                throw new ConfigException(correlationId, "NO_PORT", "Connection port is not set");

            var database = connection.GetAsNullableString("database");
            if (database == null)
                throw new ConfigException(correlationId, "NO_DATABASE", "Connection database is not set");
        }

        private void ValidateConnections(string correlationId, List<ConnectionParams> connections)
        {
            if (connections == null || connections.Count == 0)
                throw new ConfigException(correlationId, "NO_CONNECTION", "Database connection is not set");

            foreach (var connection in connections)
                ValidateConnection(correlationId, connection);
        }

        private string ComposeConfig(List<ConnectionParams> connections, CredentialParams credential)
        {
            // Define connection part
            var connectionConfig = new ConfigParams();
            string connectionString = null;
            foreach (var connection in connections)
            {
                var uri = connection.Uri;
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    connectionString = uri;
                }

                var host = connection.Host;
                if (!string.IsNullOrWhiteSpace(host)) connectionConfig["Host"] = host;

                var port = connection.Port;
                if (port != default) connectionConfig["Port"] = port.ToString();

                var database = connection.GetAsNullableString("database");
                if (!string.IsNullOrWhiteSpace(database)) connectionConfig["Database"] = database;
            }

            // Define authentication part
            var credentialConfig = new ConfigParams();
            if (credential != null)
            {
                var username = credential.Username;
                if (!string.IsNullOrWhiteSpace(username)) credentialConfig["Username"] = username;

                var password = credential.Password;
                if (!string.IsNullOrWhiteSpace(password)) credentialConfig["Password"] = password;
            }

            return string.Join(";", new[] 
            {
                connectionString != null ? connectionString.TrimEnd(';') : JoinParams(connectionConfig), 
                JoinParams(credentialConfig) 
            });
        }

        /// <summary>
        /// Resolves PostgreSQL connection string from connection and credential parameters.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <returns>resolved connection string.</returns>
        public async Task<string> ResolveAsync(string correlationId)
        {
            var connections = await _connectionResolver.ResolveAllAsync(correlationId);
            var credential = await _credentialResolver.LookupAsync(correlationId);

            ValidateConnections(correlationId, connections);

            return ComposeConfig(connections, credential);
        }

        private static string JoinParams(ConfigParams config)
        {
            return string.Join(";", config.Select(x => string.Format("{0}={1}", x.Key, x.Value)));
        }
    }
}
