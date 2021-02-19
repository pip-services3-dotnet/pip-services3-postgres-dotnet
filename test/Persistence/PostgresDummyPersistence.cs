using System.Collections.Generic;
using System.Threading.Tasks;
using PipServices3.Commons.Data;

namespace PipServices3.Postgres.Persistence
{
	public class PostgresDummyPersistence : IdentifiablePostgresPersistence<Dummy, string>, IDummyPersistence
    {
        public PostgresDummyPersistence()
            : base("dummies")
        {
        }

		protected override void DefineSchema()
		{
            ClearSchema();
            EnsureSchema($"CREATE TABLE {_tableName} (id TEXT PRIMARY KEY, key TEXT, content TEXT, create_time_utc TIMESTAMP with time zone, sub_dummy JSONB)");
            EnsureIndex($"{_tableName}_key", new Dictionary<string, bool> { { "key", true } }, new IndexOptions { Unique = true });
        }

		//protected override Dummy OnConvertToPublic(AnyValueMap map)
		//{
		//          Dummy dummy = new Dummy();

		//          ObjectWriter.SetProperties(dummy, map);

		//          if (map.TryGetValue(nameof(dummy.SubDummy), out object subDummyJson))
		//          {
		//              var subDummyMap = new AnyValueMap(JsonConverter.ToMap(subDummyJson.ToString()));
		//              dummy.SubDummy = ConvertSubDummyToPublic(subDummyMap);
		//          }

		//          return dummy;
		//}

		//      private SubDummy ConvertSubDummyToPublic(AnyValueMap map)
		//      {
		//          SubDummy subDummy = new SubDummy();
		//          subDummy.Type = map.GetAsNullableString(nameof(subDummy.Type));

		//          var arrayOfDouble = map.GetAsObject(nameof(subDummy.ArrayOfDouble)) as IEnumerable<object>;
		//          subDummy.ArrayOfDouble = arrayOfDouble.Select(x => System.Convert.ToDouble(x)).ToArray();
		//          return subDummy;
		//      }

		public async Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging)
        {
            filter ??= new FilterParams();
            var key = filter.GetAsNullableString("key");
            
            var filterCondition = "";
            if (key != null)
                filterCondition += "key='" + key + "'";

            return await base.GetPageByFilterAsync(correlationId, filterCondition, paging, null, null);
        }

        public async Task<long> GetCountByFilterAsync(string correlationId, FilterParams filter)
        {
            filter ??= new FilterParams();
            var key = filter.GetAsNullableString("key");

            var filterCondition = "";
            if (key != null)
                filterCondition += "key='" + key + "'";

            return await base.GetCountByFilterAsync(correlationId, filterCondition);
        }

    }
}