using System;
using System.Threading.Tasks;
using PipServices3.Commons.Config;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Components.Log;
using PipServices3.Postgres.Connect;
using Npgsql;
using System.Linq;

namespace PipServices3.Postgres.Persistence
{
    /// <summary>
    /// PostgreSQL connection using plain driver.
    /// 
    /// By defining a connection and sharing it through multiple persistence components
    /// you can reduce number of used database connections.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// connection(s):
    /// - discovery_key:             (optional) a key to retrieve the connection from <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
    /// - host:                      host name or IP address
    /// - port:                      port number (default: 27017)
    /// - uri:                       resource URI or connection string with all parameters in it
    /// 
    /// credential(s):
    /// - store_key:                 (optional) a key to retrieve the credentials from <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_auth_1_1_i_credential_store.html">ICredentialStore</a>
    /// - username:                  (optional) user name
    /// - password:                  (optional) user password
    /// 
    /// options:
    /// - max_pool_size:             (optional) maximum connection pool size (default: 2)
    /// - keep_alive:                (optional) enable connection keep alive (default: true)
    /// - connect_timeout:           (optional) connection timeout in milliseconds (default: 5 sec)
    /// - auto_reconnect:            (optional) enable auto reconnection (default: true)
    /// - max_page_size:             (optional) maximum page size (default: 100)
    /// - debug:                     (optional) enable debug output (default: false).
    /// 
    /// ### References ###
    /// 
    /// - *:logger:*:*:1.0           (optional) <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_log_1_1_i_logger.html">ILogger</a> components to pass log messages
    /// - *:discovery:*:*:1.0        (optional) <a href="https://rawgit.com/pip-services3-dotnet/pip-services3-components-dotnet/master/doc/api/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services
    /// - *:credential-store:*:*:1.0 (optional) Credential stores to resolve credentials
    /// </summary>
    public class PostgresConnection : IReferenceable, IReconfigurable, IOpenable
    {
        private ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "options.connect_timeout", 0,
            "options.idle_timeout", 10000,
            "options.max_pool_size", 3
        );

        /// <summary>
        /// The connection resolver.
        /// </summary>
        protected PostgresConnectionResolver _connectionResolver = new PostgresConnectionResolver();

        /// <summary>
        /// The configuration options.
        /// </summary>
        protected ConfigParams _options = new ConfigParams();

        /// <summary>
        /// The PostgreSQL connection object.
        /// </summary>
        protected NpgsqlConnection _connection;

        /// <summary>
        /// The database name.
        /// </summary>
        protected string _databaseName;

        /// <summary>
        /// The logger.
        /// </summary>
        protected CompositeLogger _logger = new CompositeLogger();

        /// <summary>
        /// Creates a new instance of the connection component.
        /// </summary>
        public PostgresConnection()
        { }

        /// <summary>
        /// Gets PostgreSQL connection object.
        /// </summary>
        /// <returns>The PostgreSQL connection object.</returns>
        public NpgsqlConnection GetConnection()
        {
            return _connection;
        }

        /// <summary>
        /// Gets the name of the connected database.
        /// </summary>
        /// <returns>The name of the connected database.</returns>
        public string GetDatabaseName()
        {
            return _databaseName;
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public void SetReferences(IReferences references)
        {
            _logger.SetReferences(references);
            _connectionResolver.SetReferences(references);
        }

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public virtual void Configure(ConfigParams config)
        {
            config = config.SetDefaults(_defaultConfig);

            _connectionResolver.Configure(config);

            _options = _options.Override(config.GetSection("options"));
        }

        /// <summary>
        /// Checks if the component is opened.
        /// </summary>
        /// <returns>true if the component has been opened and false otherwise.</returns>
        public virtual bool IsOpen()
        {
            return _connection != null;
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public async virtual Task OpenAsync(string correlationId)
        {
            var connectionString = await _connectionResolver.ResolveAsync(correlationId);

            _logger.Trace(correlationId, "Connecting to postgres...");

            try
            {
                var settings = ComposeSettings();
                var connString = connectionString.TrimEnd(';') + ";" + JoinParams(settings);

                _connection = new NpgsqlConnection(connString);
                _databaseName = _connection.Database;

                // Try to connect
                await _connection.OpenAsync();

                _logger.Debug(correlationId, "Connected to postgres database {0}", _databaseName);
            }
            catch (Exception ex)
            {
                throw new ConnectionException(correlationId, "CONNECT_FAILED", "Connection to postgres failed", ex);
            }
        }

        private ConfigParams ComposeSettings()
        {
            var maxPoolSize = _options.GetAsNullableInteger("max_pool_size");
            var connectTimeout = _options.GetAsNullableInteger("connect_timeout");
            var idleTimeout = _options.GetAsNullableInteger("idle_timeout");

            ConfigParams settings = new ConfigParams();

            if (maxPoolSize.HasValue) settings["Maximum Pool Size"] = maxPoolSize.Value.ToString();
            if (connectTimeout.HasValue) settings["Timeout"] = connectTimeout.Value.ToString();
            if (idleTimeout.HasValue) settings["Keepalive"] = idleTimeout.Value.ToString();

            return settings;
        }

        private static string JoinParams(ConfigParams config)
        { 
            return string.Join(";", config.Select(x => string.Format("{0}={1}", x.Key, x.Value))); 
        }
        
        /// <summary>
        /// Closes component and frees used resources.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public async virtual Task CloseAsync(string correlationId)
        {
            // Todo: Properly close the connection
            await _connection.CloseAsync();

            _connection = null;
            _databaseName = null;
        }
    }
}
