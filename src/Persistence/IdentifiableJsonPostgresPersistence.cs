using Npgsql;
using PipServices3.Commons.Convert;
using PipServices3.Commons.Data;
using PipServices3.Commons.Data.Mapper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PipServices3.Postgres.Persistence
{
    public class IdentifiableJsonPostgresPersistence<T, K>: IdentifiablePostgresPersistence<T, K>
        where T : IIdentifiable<K>, new()
        where K : class
    {
        public IdentifiableJsonPostgresPersistence(string tableName)
            : base(tableName)
        { }

        /// <summary>
        /// Adds DML statement to automatically create JSON(B) table
        /// </summary>
        /// <param name="idType">type of the id column (default: TEXT)</param>
        /// <param name="dataType">type of the data column (default: JSONB)</param>
        protected void EnsureTable(string idType = "TEXT", string dataType = "JSONB")
        { 
            var query = "CREATE TABLE IF NOT EXISTS " + QuoteIdentifier(_tableName)
            + " (\"id\" " + idType + " PRIMARY KEY, \"data\" " + dataType + ")";

            AutoCreateObject(query);
        }

        /// <summary>
        /// Converts object value from internal to public format.
        /// </summary>
        /// <param name="value">an object in internal format to convert.</param>
        /// <returns>converted object in public format.</returns>
        protected override T ConvertToPublic(AnyValueMap map)
        {
            if (map != null && map.TryGetValue("data", out object value) && value != null)
            {
                return base.ConvertToPublic(value as AnyValueMap);
            }

            return default;
        }

        /// <summary>
        /// Convert object value from public to internal format.
        /// </summary>
        /// <param name="value">an object in public format to convert.</param>
        /// <returns>converted object in internal format.</returns>
        protected override AnyValueMap ConvertFromPublic(T value)
        {
            if (value == null) return null;
            return AnyValueMap.FromTuples("id", value.Id, "data", base.ConvertFromPublic(value));
        }

        ///// <summary>
        ///// Updates only few selected fields in a data item.
        ///// </summary>
        ///// <param name="correlationId">(optional) transaction id to trace execution through call chain.</param>
        ///// <param name="id">an id of data item to be updated.</param>
        ///// <param name="data">a map with fields to be updated.</param>
        ///// <returns>updated item</returns>
        public override async Task<T> UpdatePartially(string correlationId, K id, AnyValueMap data)
        {
            if (data == null || id == null)
                return default;

            var values = new object[] { id, data.GetAsObject() };

            var query = "UPDATE " + _tableName + " SET \"data\"=\"data\"||@Param2 WHERE \"id\"=@Param1 RETURNING *";

            var result = (await ExecuteReaderAsync(query, values)).FirstOrDefault();

            _logger.Trace(correlationId, "Updated partially in {0} with id = {1}", _tableName, id);

            var newItem = ConvertToPublic(result);
            return newItem;
        }
    }
}
