using PipServices3.Commons.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices3.Postgres.Persistence
{
    public class JsonPostgresDummyPersistence: IdentifiableJsonPostgresPersistence<Dummy, string>, IDummyPersistence
    {
        public JsonPostgresDummyPersistence()
            : base("dummies_json")
        {
            EnsureTable();
            EnsureIndex("dummies_json_key", new Dictionary<string, bool> { { "(data->>'key')", true } }, new IndexOptions { Unique = true });
        }

        public async Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging)
        {
            filter ??= new FilterParams();
            var key = filter.GetAsNullableString("key");

            var filterCondition = "";
            if (key != null)
                filterCondition += "data->key='" + key + "'";

            return await base.GetPageByFilterAsync(correlationId, filterCondition, paging, null, null);
        }

        public async Task<long> GetCountByFilterAsync(string correlationId, FilterParams filter)
        {
            filter ??= new FilterParams();
            var key = filter.GetAsNullableString("key");

            var filterCondition = "";
            if (key != null)
                filterCondition += "data->key='" + key + "'";

            return await base.GetCountByFilterAsync(correlationId, filterCondition);
        }
    }
}
