using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using PipServices3.Commons.Config;
using PipServices3.Commons.Data;
using PipServices3.Commons.Errors;
using PipServices3.Commons.Refer;
using PipServices3.Commons.Run;
using PipServices3.Components.Log;
using Npgsql;
using System.Data;
using System.Text;
using PipServices3.Commons.Convert;

namespace PipServices3.Postgres.Persistence
{
    /// <summary>
    /// Abstract persistence component that stores data in PostgreSQL
    /// and is based using Mongoose object relational mapping.
    /// 
    /// This is the most basic persistence component that is only
    /// able to store data items of any type.Specific CRUD operations 
    /// over the data items must be implemented in child classes by 
    /// accessing <c>this._collection</c> or <c>this._model</c> properties.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// - table:                     (optional) PostgreSQL table name
    /// - schema:                    (optional) PostgreSQL schema name
    /// connection(s):
    /// - discovery_key:             (optional) a key to retrieve the connection from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a>
    /// - host:                      host name or IP address
    /// - port:                      port number (default: 27017)
    /// - uri:                       resource URI or connection string with all parameters in it
    /// 
    /// credential(s):
    /// - store_key:                 (optional) a key to retrieve the credentials from <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_auth_1_1_i_credential_store.html">ICredentialStore</a>
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
    /// - *:logger:*:*:1.0           (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_log_1_1_i_logger.html">ILogger</a> components to pass log messages
    /// - *:discovery:*:*:1.0        (optional) <a href="https://pip-services3-dotnet.github.io/pip-services3-components-dotnet/interface_pip_services_1_1_components_1_1_connect_1_1_i_discovery.html">IDiscovery</a> services
    /// - *:credential-store:*:*:1.0 (optional) Credential stores to resolve credentials
    /// </summary>
    /// <typeparam name="T">the class type</typeparam>
    /// <example>
    /// <code>
    /// class MyPostgresPersistence: PostgresPersistence<MyData> 
    /// {
    ///     public MyPostgresPersistence()
    ///     {
    ///         base("mydata");
    ///     }
    ///     public MyData getByName(string correlationId, string name)
    ///     {
    ///         var builder = Builders<BeaconV1>.Filter;
    ///         var filter = builder.Eq(x => x.Name, name);
    ///         var result = await _collection.Find(filter).FirstOrDefaultAsync();
    ///         return result;
    ///     }
    ///     public MyData set(String correlatonId, MyData item)
    ///     {
    ///         var filter = Builders<T>.Filter.Eq(x => x.Id, item.Id);
    ///         var options = new FindOneAndReplaceOptions<T>
    ///         {
    ///             ReturnDocument = ReturnDocument.After,
    ///             IsUpsert = true
    ///         };
    ///         var result = await _collection.FindOneAndReplaceAsync(filter, item, options);
    ///         return result;
    ///     }
    /// }
    /// 
    /// var persistence = new MyPostgresPersistence();
    /// persistence.Configure(ConfigParams.fromTuples(
    /// "host", "localhost",
    /// "port", 27017 ));
    /// 
    /// persitence.Open("123");
    /// var mydata = new MyData("ABC");
    /// persistence.Set("123", mydata);
    /// persistence.GetByName("123", "ABC");
    /// Console.Out.WriteLine(item);                   // Result: { name: "ABC" }
    /// </code>
    /// </example>
    public class PostgresPersistence<T> : IReferenceable, IUnreferenceable, IReconfigurable, IOpenable, ICleanable where T : new()
    {
        private static ConfigParams _defaultConfig = ConfigParams.FromTuples(
            "table", null,
            "schema", null,
            "dependencies.connection", "*:connection:postgres:*:1.0",

            // connections.*
            // credential.*

            "options.max_pool_size", 2,
            "options.keep_alive", 1,
            "options.connect_timeout", 5,
            "options.auto_reconnect", true,
            "options.max_page_size", 100,
            "options.debug", true
        );

        private readonly List<string> _schemaStatements = new List<string>();


        /// <summary>
        /// The Postgres connection.
        /// </summary>
        protected PostgresConnection _connection;

        /// <summary>
        /// The PostgreSQL database name.
        /// </summary>
        protected string _databaseName;

        /// <summary>
        /// The PostgreSQL table name.
        /// </summary>
        protected string _tableName;

        /// <summary>
        /// The PostgreSQL schema object.
        /// </summary>
        protected string _schemaName;

        /// <summary>
        /// Maximum page size
        /// </summary>
        protected int _maxPageSize = 100;

        /// <summary>
        /// The dependency resolver.
        /// </summary>
        protected DependencyResolver _dependencyResolver = new DependencyResolver(_defaultConfig);

        /// <summary>
        /// The logger.
        /// </summary>
        protected CompositeLogger _logger = new CompositeLogger();

        private ConfigParams _config;
        private IReferences _references;
        private bool _localConnection;
        private bool _opened;

        /// <summary>
        /// Creates a new instance of the persistence component.
        /// </summary>
        /// <param name="tableName">(optional) a tableName name.</param>
        /// <param name="schemaName">(optional) a schema name.</param>
        public PostgresPersistence(string tableName = null, string schemaName = null)
        {
            _tableName = tableName;
            _schemaName = schemaName;
        }

        /// <summary>
        /// Configures component by passing configuration parameters.
        /// </summary>
        /// <param name="config">configuration parameters to be set.</param>
        public virtual void Configure(ConfigParams config)
        {
            _config = config.SetDefaults(_defaultConfig);
            _dependencyResolver.Configure(_config);

            _tableName = config.GetAsStringWithDefault("collection", _tableName);
            _tableName = config.GetAsStringWithDefault("table", _tableName);
            _schemaName = config.GetAsStringWithDefault("schema", _schemaName);
            _maxPageSize = config.GetAsIntegerWithDefault("options.max_page_size", _maxPageSize);
        }

        /// <summary>
        /// Sets references to dependent components.
        /// </summary>
        /// <param name="references">references to locate the component dependencies.</param>
        public virtual void SetReferences(IReferences references)
        {
            _references = references;

            _logger.SetReferences(references);
            _dependencyResolver.SetReferences(references);

            // Get connection
            _connection = _dependencyResolver.GetOneOptional("connection") as PostgresConnection;
            _localConnection = _connection == null;

            // Or create a local one
            if (_connection == null)
                _connection = CreateLocalConnection();
        }

        /// <summary>
        /// Unsets (clears) previously set references to dependent components.
        /// </summary>
        public virtual void UnsetReferences()
        {
            _connection = null;
        }

        private PostgresConnection CreateLocalConnection()
        {
            var connection = new PostgresConnection();

            if (_config != null)
                connection.Configure(_config);

            if (_references != null)
                connection.SetReferences(_references);

            return connection;
        }

        protected void EnsureIndex(string name, Dictionary<string, bool> keys, IndexOptions options)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("CREATE");

            if (options.Unique)
                builder.Append(" UNIQUE");


            var indexName = QuoteIdentifier(name);
            if (_schemaName != null)
            {
                indexName = QuoteIdentifier(_schemaName) + "." + indexName;
            }

            builder.Append(" INDEX ").Append(indexName).Append(" ON ").Append(QuotedTableName());

            if (!string.IsNullOrWhiteSpace(options.Type))
                builder.Append(" ")
                    .Append(options.Type);

            var fields = string.Join(", ", keys.Select(x => QuoteIdentifier(x.Key) + (x.Value ? "" : " DESC")));

            builder.Append("(").Append(fields).Append(")");

            EnsureSchema(builder.ToString());
        }

        /// <summary>
        /// Adds a statement to schema definition
        /// </summary>
        /// <param name="schemaStatement">a statement to be added to the schema</param>
        protected void EnsureSchema(string schemaStatement)
        {
            _schemaStatements.Add(schemaStatement);
        }

        /// <summary>
        /// Clears all auto-created objects
        /// </summary>
        protected void ClearSchema()
        {
            _schemaStatements.Clear();
        }

        /// <summary>
        /// Defines database schema via auto create objects or convenience methods.
        /// </summary>
        protected virtual void DefineSchema()
        {
            // Todo: override in chile classes
        }

        /// <summary>
        /// Converts object value from internal to public format.
        /// </summary>
        /// <param name="map">an object in internal format to convert</param>
        /// <returns>converted object in public format</returns>
        protected virtual T ConvertToPublic(AnyValueMap map)
        {
            var json = JsonConverter.ToJson(map);
            T obj = JsonConverter.FromJson<T>(json);
            return obj;
        }

        /// <summary>
        /// Converts object value from public to internal format.
        /// </summary>
        /// <param name="value">an object in public format to convert</param>
        /// <returns>converted object in internal format</returns>
        protected virtual AnyValueMap ConvertFromPublic(T value)
        {
            var json = JsonConverter.ToJson(value);
            var map = new AnyValueMap(MapConverter.ToMap(JsonConverter.FromJson(json)));
            
            return map;
        }

        protected string QuoteIdentifier(string value)
        {
            if (value == null || value == "") return value;

            if (value[0] == '"' || value[0] == '(') return value;

            return '"' + value + '"';
        }

        protected string QuotedTableName()
        {
            if (_tableName == null)
                return null;

            var builder = new StringBuilder();
            
            if (_schemaName != null)
            {
                builder.Append(QuoteIdentifier(_schemaName)).Append('.');
            }

            builder.Append(QuoteIdentifier(_tableName));

            return builder.ToString();
        }

        /// <summary>
        /// Checks if the component is opened.
        /// </summary>
        /// <returns>true if the component has been opened and false otherwise.</returns>
        public virtual bool IsOpen()
        {
            return _opened;
        }

        /// <summary>
        /// Opens the component.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public async virtual Task OpenAsync(string correlationId)
        {
            if (IsOpen()) return;

            if (_connection == null)
            {
                _connection = CreateLocalConnection();
                _localConnection = true;
            }

            if (_localConnection)
                await _connection.OpenAsync(correlationId);

            if (_connection.IsOpen() == false)
                throw new InvalidStateException(correlationId, "CONNECTION_NOT_OPENED", "Database connection is not opened");

            _databaseName = _connection.GetDatabaseName();

            // Define database schema
            DefineSchema();

            // Recreate objects
            await CreateSchemaAsync(correlationId);

            _opened = true;
        }

        /// <summary>
        /// Closes component and frees used resources.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public virtual async Task CloseAsync(string correlationId)
        {
            if (IsOpen())
            {
                if (_connection == null)
                    throw new InvalidStateException(correlationId, "NO_CONNECTION", "Postgres connection is missing");

                _opened = false;

                if (_localConnection)
                    await _connection.CloseAsync(correlationId);
            }
        }

        /// <summary>
        /// Clears component state.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        public virtual async Task ClearAsync(string correlationId)
        {
            // Return error if collection is not set
            if (string.IsNullOrWhiteSpace(_tableName))
                throw new Exception("Table name is not defined");

            try
            {
                await ExecuteNonQuery(correlationId, "DELETE FROM " + QuotedTableName());
            }
            catch (Exception ex)
            {
                throw new ConnectionException(correlationId, "CONNECT_FAILED", "Connection to postgres failed")
                    .WithCause(ex);
            }
        }

        protected async Task CreateSchemaAsync(string correlationId)
        {
            if (_schemaStatements == null || _schemaStatements.Count == 0)
                return;

            // If table already exists then exit
            if (await TableExistAsync(correlationId, _tableName))
                return;

            _logger.Debug(correlationId, "Table {0} does not exist. Creating database objects...", _tableName);

            // Run all DML commands
            try
            {
                foreach (var dml in _schemaStatements)
                {
                    await ExecuteNonQuery(correlationId, dml);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(correlationId, ex, "Failed to autocreate database object");
                throw;
            }
        }

        /// <summary>
        /// Generates a list of column names to use in SQL statements like: "column1,column2,column3"
        /// </summary>
        /// <param name="map">key-value map</param>
        /// <returns>a generated list of column names</returns>
        protected string GenerateColumns(AnyValueMap map)
        {
            return GenerateColumns(map.Keys.ToList());
        }

        /// <summary>
        /// Generates a list of column names to use in SQL statements like: "column1,column2,column3"
        /// </summary>
        /// <param name="values">an array with column values</param>
        /// <returns>a generated list of column names</returns>
        protected string GenerateColumns(IEnumerable<string> values)
        {
            return string.Join(",", values.Select(x => QuoteIdentifier(x)));
        }

        /// <summary>
        /// Generates a list of value parameters to use in SQL statements like: "@Param1,@Param2,@Param3"
        /// </summary>
        /// <param name="map">key-value map</param>
        /// <returns>a generated list of value parameters</returns>
        protected string GenerateParameters(AnyValueMap map)
        {
            return GenerateParameters(map.Keys.ToList());
        }

        /// <summary>
        /// Generates a list of value parameters to use in SQL statements like: "@Param1,@Param2,@Param3"
        /// </summary>
        /// <param name="values">an array with column values</param>
        /// <returns>a generated list of value parameters</returns>
        protected string GenerateParameters<K>(IEnumerable<K> values)
        {
            List<string> result = new List<string>();

            var index = 1;
            foreach (var value in values)
            {
                result.Add("@Param" + index);
                index++;
            }

            return string.Join(",", result);
        }

        /// <summary>
        /// Generates a list of column sets to use in UPDATE statements like: column1=@Param1,column2=@Param2
        /// </summary>
        /// <param name="map">a key-value map with columns and values</param>
        /// <returns></returns>
        protected string GenerateSetParameters(AnyValueMap map)
        {
            return GenerateSetParameters(map.Keys.ToList());
        }

        /// <summary>
        /// Generates a list of column sets to use in UPDATE statements like: column1=@Param1,column2=@Param2
        /// </summary>
        /// <param name="values">an array with column names</param>
        /// <returns></returns>
        protected string GenerateSetParameters(IEnumerable<string> values)
        {
            var result = "";
            var index = 1;
            foreach (var column in values)
            {
                if (result != "") result += ",";
                result += QuoteIdentifier(column) + "=@Param" + index;
                index++;
            }

            return result;
        }

        /// <summary>
        /// Generates a list of column parameters
        /// </summary>
        /// <param name="map">a key-value map with columns and values</param>
        /// <returns>generated list of column values</returns>
        protected List<object> GenerateValues(AnyValueMap map)
        {
            return map.Values.ToList();
        }

        /// <summary>
        /// Gets a page of data items retrieved by a given filter and sorted according to sort parameters.
        /// 
        /// This method shall be called by a public getPageByFilter method from child
        /// class that receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object</param>
        /// <param name="paging">(optional) paging parameters</param>
        /// <param name="sortDefinition">(optional) sorting JSON object</param>
        /// <returns>data page of results by filter.</returns>
        public virtual async Task<DataPage<T>> GetPageByFilterAsync(string correlationId, string filter,
                PagingParams paging = null, string sort = null, string select = null)
        {
            select = string.IsNullOrWhiteSpace(select) ? "*" : select;
            var query = string.Format("SELECT {0} FROM {1}", select, QuotedTableName());

            // Adjust max item count based on configuration
            paging = paging ?? new PagingParams();
            var skip = paging.GetSkip(-1);
            var take = paging.GetTake(_maxPageSize);
            var pagingEnabled = paging.Total;

            if (!string.IsNullOrWhiteSpace(filter))
                query += " WHERE " + filter;

            if (!string.IsNullOrWhiteSpace(sort))
                query += " ORDER BY " + sort;

            if (skip >= 0) query += " OFFSET " + skip;
            query += " LIMIT " + take;

            var result = await ExecuteReaderAsync(correlationId, query);

            var items = result.Select(map => ConvertToPublic(map)).ToList();

            long? total = pagingEnabled ? (long?)await GetCountByFilterAsync(correlationId, filter) : null;

            return new DataPage<T>
            {
                Data = items,
                Total = total
            };
        }

        /// <summary>
        /// Gets a number of data items retrieved by a given filter.
        /// 
        /// This method shall be called by a public getCountByFilter method from child class that
        /// receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filter">(optional) a filter JSON object</param>
        /// <returns></returns>
        protected virtual async Task<long> GetCountByFilterAsync(string correlationId, string filter)
        {
            var query = "SELECT COUNT(*) AS count FROM " + QuotedTableName();

            if (!string.IsNullOrWhiteSpace(filter))
                query += " WHERE " + filter;

            var count = await ExecuteScalarAsync<long>(correlationId, query);

            _logger.Trace(correlationId, "Counted {0} items in {1}", count, _tableName);

            return count;
        }

        /// <summary>
        /// Gets a list of data items retrieved by a given filter and sorted according to sort parameters.
        /// 
        /// This method shall be called by a public getListByFilter method from child class that
        /// receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filter">(optional) a filter JSON object</param>
        /// <param name="sort">(optional) sorting JSON object</param>
        /// <param name="select">(optional) projection JSON object</param>
        /// <returns>data list</returns>
        protected async Task<List<T>> GetListByFilterAsync(string correlationId, string filter,
            string sort = null, string select = null)
        {
            select = string.IsNullOrWhiteSpace(select) ? "*" : select;
            var query = string.Format("SELECT {0} FROM {1}", select, QuotedTableName());

            if (!string.IsNullOrWhiteSpace(filter))
                query += " WHERE " + filter;

            if (!string.IsNullOrWhiteSpace(sort))
                query += " ORDER BY " + sort;

            var result = await ExecuteReaderAsync(correlationId, query);

            var items = result.Select(map => ConvertToPublic(map)).ToList();

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_tableName}");

            return items;
        }

        /// <summary>
        /// Gets a random item from items that match to a given filter.
        /// 
        /// This method shall be called by a public getOneRandom method from child class
        /// that receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object</param>
        /// <returns>a random item by filter.</returns>
        protected virtual async Task<T> GetOneRandomAsync(string correlationId, string filter)
        {
            var count = await GetCountByFilterAsync(correlationId, filter);

            if (count <= 0)
            {
                _logger.Trace(correlationId, "Nothing found for filter {0}", filter);
                return default;
            }

            var pos = new Random().Next(0, Convert.ToInt32(count) - 1);

            var query = "SELECT * FROM " + QuotedTableName();

            if (!string.IsNullOrWhiteSpace(filter))
                query += " WHERE " + filter;

            query += string.Format(" OFFSET {0} LIMIT 1", pos);

            var items = await ExecuteReaderAsync(correlationId, query);

            var item = items.FirstOrDefault();

            if (item == null)
                _logger.Trace(correlationId, "Random item wasn't found from {0}", _tableName);
            else
                _logger.Trace(correlationId, "Retrieved random item from {0}", _tableName);

            return ConvertToPublic(item);
        }

        /// <summary>
        /// Creates a data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="item">an item to be created.</param>
        /// <returns>created item.</returns>
        public virtual async Task<T> CreateAsync(string correlationId, T item)
        {
            if (item == null)
            {
                return default;
            }

            var map = ConvertFromPublic(item);
            var columns = GenerateColumns(map);
            var @params = GenerateParameters(map);

            var query = "INSERT INTO " + QuotedTableName() + " (" + columns + ") VALUES (" + @params + ") RETURNING *";

            var result = await ExecuteReaderAsync(correlationId, query, map);

            var newItem = result != null && result.Count == 1
                ? ConvertToPublic(result[0]) : default;

            _logger.Trace(correlationId, "Created in {0} item {1}", _tableName, newItem);

            return newItem;
        }

        /// <summary>
        /// Deletes data items that match to a given filter.
        /// 
        /// This method shall be called by a public deleteByFilter method from child
        /// class that receives FilterParams and converts them into a filter function.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="filterDefinition">(optional) a filter JSON object.</param>
        public virtual async Task DeleteByFilterAsync(string correlationId, string filter)
        {
            var query = "DELETE FROM " + QuotedTableName();

            if (!string.IsNullOrWhiteSpace(filter))
                query += " WHERE " + filter;

            var deletedCount = await ExecuteNonQuery(correlationId, query);

            _logger.Trace(correlationId, $"Deleted {deletedCount} from {_tableName}");
        }

        private async Task<bool> TableExistAsync(string correlationId, string tableName)
        {
            var result = await ExecuteScalarAsync<bool>(correlationId, "SELECT to_regclass('" + tableName + "') IS NOT NULL");
            return result;
        }

        protected virtual void SetParameters<T_VALUE>(NpgsqlCommand cmd, IEnumerable<T_VALUE> values)
        {
            if (values != null && values.Count() > 0)
            {
                int index = 1;
                foreach (var value in values)
                {
                    AddParameter(cmd, "Param" + index, value);
                    index++;
                }
            }
        }

        protected virtual void AddParameter(NpgsqlCommand cmd, string name, object value)
        {
            if (value is T || value is Dictionary<string, object>)
            {
                cmd.Parameters.AddWithValue(name, NpgsqlTypes.NpgsqlDbType.Jsonb, value);
                return;
            }

            cmd.Parameters.AddWithValue(name, value);
        }

        protected async Task<int> ExecuteNonQuery(string correlationId, string cmdText)
        {
            using (var conn = await _connection.GetConnection(correlationId))
            using (var cmd = new NpgsqlCommand(cmdText, conn))
            {
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        protected async Task<int> ExecuteNonQuery(string correlationId, string cmdText, AnyValueMap map)
        {
            return await ExecuteNonQuery(correlationId, cmdText, map.Values);
        }

        protected async Task<int> ExecuteNonQuery<T_VALUE>(string correlationId, string cmdText, IEnumerable<T_VALUE> values = null)
        {
            using (var conn = await _connection.GetConnection(correlationId))
            using (var cmd = new NpgsqlCommand(cmdText, conn))
            {
                SetParameters(cmd, values);
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        protected async Task<List<AnyValueMap>> ExecuteReaderAsync(string correlationId, string cmdText)
        {
            using (var conn = await _connection.GetConnection(correlationId))
            using (var cmd = new NpgsqlCommand(cmdText, conn))
            {
                await cmd.PrepareAsync();
                return await ExecuteReaderAsync(cmd);
            }
        }

        protected async Task<List<AnyValueMap>> ExecuteReaderAsync(string correlationId, string cmdText, AnyValueMap map)
        {
            return await ExecuteReaderAsync(correlationId, cmdText, map.Values);
        }

        protected async Task<List<AnyValueMap>> ExecuteReaderAsync<T_VALUE>(string correlationId, string cmdText, IEnumerable<T_VALUE> values = null)
        {
            using (var conn = await _connection.GetConnection(correlationId))
            using (var cmd = new NpgsqlCommand(cmdText, conn))
            {
                SetParameters(cmd, values);
                await cmd.PrepareAsync();

                return await ExecuteReaderAsync(cmd);
            }
        }

        protected static async Task<List<AnyValueMap>> ExecuteReaderAsync(NpgsqlCommand cmd)
        {
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var schemaTable = reader.GetSchemaTable();
                var columnTypes = new Dictionary<string, string>();

				foreach (DataRow row in schemaTable.Rows)
				{
                    columnTypes[row["ColumnName"].ToString()] = row["DataTypeName"].ToString();
				}

                DataTable table = new DataTable();
                table.Load(reader);

                List<AnyValueMap> result = new List<AnyValueMap>();
                foreach (DataRow row in table.Rows)
                {
                    AnyValueMap map = new AnyValueMap();
                    foreach (DataColumn column in table.Columns)
                    {
                        var columnType = columnTypes.TryGetValue(column.ColumnName, out string type) ? type : null;

                        var value = row[column];
                        if (value != DBNull.Value)
                        {
                            if (columnType == "jsonb")
                            {
                                map[column.ColumnName] = new AnyValueMap(MapConverter.ToMap(JsonConverter.FromJson(value.ToString())));
                            }
                            else if (value is DateTime time)
                            {
                                map[column.ColumnName] = time.ToUniversalTime();
                            }
                            else
                            {
                                map[column.ColumnName] = value;
                            }
                        }
                    }

                    result.Add(map);
                }

                return result;
            }
        }

        private async Task<R> ExecuteScalarAsync<R>(string correlationId, string cmdText)
        {
            using (var conn = await _connection.GetConnection(correlationId))
            using (var cmd = new NpgsqlCommand(cmdText, conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                return (R)Convert.ChangeType(result, typeof(R));
            }
        }
    }
}
