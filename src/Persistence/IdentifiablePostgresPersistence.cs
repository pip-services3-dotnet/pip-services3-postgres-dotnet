using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PipServices3.Commons.Data;
using PipServices3.Commons.Reflect;
using PipServices3.Data;

namespace PipServices3.Postgres.Persistence
{
    /// <summary>
    /// Abstract persistence component that stores data in PostgreSQL
    /// and implements a number of CRUD operations over data items with unique ids.
    /// The data items must implement IIdentifiable interface.
    /// 
    /// In basic scenarios child classes shall only override <c>GetPageByFilter()</c>,
    /// <c>GetListByFilter()</c> or <c>DeleteByFilter()</c> operations with specific filter function.
    /// All other operations can be used out of the box.
    /// 
    /// In complex scenarios child classes can implement additional operations by
    /// accessing <c>this._collection</c> and <c>this._model</c> properties.
    /// 
    /// ### Configuration parameters ###
    /// 
    /// - table:                     (optional) PostgreSQL table name
    /// - schema:                    (optional) PostgreSQL schema name
    /// 
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
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    /// <example>
    /// <code>
    /// class MyPostgresPersistence: PostgresPersistence<MyData, string> 
    /// {
    ///     public constructor()
    ///     {
    ///         base("mydata", MyData.class);
    ///     }
    /// 
    ///     private FilterDefinition<MyData> ComposeFilter(FilterParams filter)
    ///     {
    ///         filterParams = filterParams ?? new FilterParams();
    ///         var builder = Builders<BeaconV1>.Filter;
    ///         var filter = builder.Empty;
    ///         String name = filter.getAsNullableString('name');
    ///         if (name != null)
    ///             filter &= builder.Eq(b => b.Name, name);
    ///         return filter;
    ///     }
    ///     
    ///     public GetPageByFilter(String correlationId, FilterParams filter, PagingParams paging)
    ///     {
    ///         base.GetPageByFilter(correlationId, this.ComposeFilter(filter), paging, null, null);
    ///     }
    /// }
    /// 
    /// var persistence = new MyPostgresPersistence();
    /// persistence.Configure(ConfigParams.fromTuples(
    /// "host", "localhost",
    /// "port", 27017 ));
    /// 
    /// persitence.Open("123");
    /// 
    /// persistence.Create("123", new MyData("1", "ABC"));
    /// var mydata = persistence.GetPageByFilter(
    /// "123",
    /// FilterParams.FromTuples("name", "ABC"),
    /// Console.Out.WriteLine(mydata.Data);          // Result: { id: "1", name: "ABC" }
    /// 
    /// persistence.DeleteById("123", "1");
    /// ...
    /// </code>
    /// </example>
    public class IdentifiablePostgresPersistence<T, K> : PostgresPersistence<T>, IWriter<T, K>, IGetter<T, K>, ISetter<T>, IPartialUpdater<T, K>
        where T : IIdentifiable<K>, new()
    {
        /// <summary>
        /// Flag to turn on auto generation of object ids.
        /// </summary>
        protected bool _autoGenerateId = true;

        /// <summary>
        /// Creates a new instance of the persistence component.
        /// </summary>
        /// <param name="tableName">(optional) a table name.</param>
        /// <param name="schemaName">(optional) a schema name.</param>
        public IdentifiablePostgresPersistence(string tableName = null, string schemaName = null)
            : base(tableName, schemaName)
        { }

        /// <summary>
        /// Gets a list of data items retrieved by given unique ids.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="ids">ids of data items to be retrieved</param>
        /// <returns>a data list of results by ids.</returns>
        public virtual async Task<List<T>> GetListByIdsAsync(string correlationId, K[] ids)
        {
            var @params = GenerateParameters(ids);
            var query = "SELECT * FROM " + QuotedTableName() + " WHERE \"id\" IN (" + @params + ")";

            var items = await ExecuteReaderAsync(query, ids);

            _logger.Trace(correlationId, $"Retrieved {items.Count} from {_tableName}");

            return items.Select(item => ConvertToPublic(item)).ToList();
        }

        /// <summary>
        /// Gets a data item by its unique id.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="id">an id of data item to be retrieved.</param>
        /// <returns>a data item by id.</returns>
        public virtual async Task<T> GetOneByIdAsync(string correlationId, K id)
        {
            var @params = new[] { id };
            var query = "SELECT * FROM " + QuotedTableName() + " WHERE \"id\" = @Param1";

            var result = (await ExecuteReaderAsync(query, @params)).FirstOrDefault();

            if (result == null)
            {
                _logger.Trace(correlationId, "Nothing found from {0} with id = {1}", _tableName, id);
                return default;
            }

            _logger.Trace(correlationId, "Retrieved from {0} with id = {1}", _tableName, id);

            var item = ConvertToPublic(result);

            return item;
        }

        /// <summary>
        /// Creates a data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="item">an item to be created.</param>
        /// <returns>created item.</returns>
        public override async Task<T> CreateAsync(string correlationId, T item)
        {
            // Assign unique id
            if (item is IStringIdentifiable stringIdentifiable && stringIdentifiable.Id == null && _autoGenerateId)
                stringIdentifiable.Id = IdGenerator.NextLong();

            return await base.CreateAsync(correlationId, item);
        }

        /// <summary>
        /// Sets a data item. If the data item exists it updates it, otherwise it create a new data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="item">an item to be set.</param>
        /// <returns>updated item.</returns>
        public virtual async Task<T> SetAsync(string correlationId, T item)
        {
            if (item == null || item.Id == null)
                return default;

            // Assign unique id
            if (item is IStringIdentifiable stringIdentifiable && stringIdentifiable.Id == null && _autoGenerateId)
                stringIdentifiable.Id = IdGenerator.NextLong();

            var map = ConvertFromPublic(item);
            var columns = GenerateColumns(map);
            var @params = GenerateParameters(map);
            var setParams = GenerateSetParameters(map);
            var values = GenerateValues(map);

            var query = "INSERT INTO " + QuotedTableName() + " (" + columns + ")"
                + " VALUES (" + @params +")"
                + " ON CONFLICT (\"id\") DO UPDATE SET " + setParams + " RETURNING *";

            var result = (await ExecuteReaderAsync(query, map)).FirstOrDefault();

            _logger.Trace(correlationId, "Set in {0} with id = {1}", _tableName, item.Id);

            var newItem = ConvertToPublic(result);
            return newItem;
        }

        /// <summary>
        /// Updates a data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="item">an item to be updated.</param>
        /// <returns>updated item.</returns>
        public virtual async Task<T> UpdateAsync(string correlationId, T item)
        {
            if (item == null || item.Id == null)
                return default;

            var map = ConvertFromPublic(item);
            var @params = GenerateSetParameters(map);
            var values = GenerateValues(map);
            values.Add(item.Id);

            var query = "UPDATE " + QuotedTableName()
                + " SET " + @params +" WHERE \"id\"=@Param" + values.Count + " RETURNING *";

            var result = (await ExecuteReaderAsync(query, values)).FirstOrDefault();

            _logger.Trace(correlationId, "Update in {0} with id = {1}", _tableName, item.Id);
            
            var newItem = ConvertToPublic(result);
            return newItem;
        }

        /// <summary>
        /// Updates only few selected fields in a data item.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="id">an id of data item to be updated.</param>
        /// <param name="data">a map with fields to be updated.</param>
        /// <returns>updated item</returns>
        public virtual async Task<T> UpdatePartially(string correlationId, K id, AnyValueMap data)
        {
            if (data == null || id == null)
                return default;

            var map = data;
            var @params = GenerateSetParameters(map);
            var values = GenerateValues(map);
            values.Add(id);

            var query = "UPDATE " + QuotedTableName()
                + " SET " + @params + " WHERE \"id\" = @Param" + values.Count + " RETURNING *";

            var result = (await ExecuteReaderAsync(query, values)).FirstOrDefault();

            _logger.Trace(correlationId, "Updated partially in {0} with id = {1}", _tableName, id);

            var newItem = ConvertToPublic(result);
            return newItem;
        }

        /// <summary>
        /// Deleted a data item by it's unique id.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="id">an id of the item to be deleted</param>
        /// <returns>deleted item.</returns>
        public virtual async Task<T> DeleteByIdAsync(string correlationId, K id)
        {
            var values = new[] { id };

            var query = "DELETE FROM " + QuotedTableName() + " WHERE \"id\" = @Param1 RETURNING *";

            var result = (await ExecuteReaderAsync(query, values)).FirstOrDefault();

            _logger.Trace(correlationId, "Deleted from {0} with id = {1}", _tableName, id);

            var newItem = ConvertToPublic(result);
            return newItem;
        }

        /// <summary>
        /// Deletes multiple data items by their unique ids.
        /// </summary>
        /// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        /// <param name="ids">ids of data items to be deleted.</param>
        public virtual async Task DeleteByIdsAsync(string correlationId, K[] ids)
        {
            var @params = GenerateParameters(ids);
            var query = "DELETE FROM " + QuotedTableName() + " WHERE \"id\" IN (" + @params +")";

            var result = await ExecuteNonQuery(query, ids);

            _logger.Trace(correlationId, $"Deleted {result} from {_tableName}");
        }
    }
}