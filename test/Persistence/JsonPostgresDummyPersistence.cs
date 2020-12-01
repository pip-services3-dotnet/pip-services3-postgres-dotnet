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
            EnsureTable("VARCHAR(32)", "JSONB");
            EnsureIndex("dummies_json_key", new Dictionary<string, bool> { { "(data->>'key')", true } }, new IndexOptions { Unique = true });
        }

		public async Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging)
		{
			return await base.GetPageByFilterAsync(correlationId, ComposeFilter(filter), paging, null, null);
		}

		public async Task<long> GetCountByFilterAsync(string correlationId, FilterParams filter)
        {
            return await base.GetCountByFilterAsync(correlationId, ComposeFilter(filter));
        }

        private static string ComposeFilter(FilterParams filter)
        {
            filter ??= new FilterParams();
            var key = filter.GetAsNullableString("key");

            var filterCondition = "";
            if (key != null)
                filterCondition += "data->>'key'='" + key + "'";
            return filterCondition;
        }
    }
}
