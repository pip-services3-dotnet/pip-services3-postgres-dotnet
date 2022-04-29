using PipServices3.Commons.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices3.Postgres.Persistence
{
    public class JsonPostgresDummyPersistence2: IdentifiableJsonPostgresPersistence<Dummy2, long>, IDummyPersistence2
    {
        public JsonPostgresDummyPersistence2()
            : base("dummies_json2")
        {
            _autoGenerateId = false;
        }

		protected override void DefineSchema()
		{
            ClearSchema();
            EnsureTable("NUMERIC", "JSONB");
            EnsureIndex($"{_tableName}_key", new Dictionary<string, bool> { { "(data->>'key')", true } }, new IndexOptions { Unique = true });
        }

        public async Task<DataPage<Dummy2>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging)
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
