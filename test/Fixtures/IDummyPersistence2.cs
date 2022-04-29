using PipServices3.Commons.Data;
using PipServices3.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices3.Postgres.Persistence
{
    public interface IDummyPersistence2 : IGetter<Dummy2, long>, IWriter<Dummy2, long>, IPartialUpdater<Dummy2, long>
    {
        Task<DataPage<Dummy2>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging);
        Task<long> GetCountByFilterAsync(string correlationId, FilterParams filter);
        Task<List<Dummy2>> GetListByIdsAsync(string correlationId, long[] ids);
        Task<Dummy2> SetAsync(string correlationId, Dummy2 item);
        Task DeleteByIdsAsync(string correlationId, long[] ids);
    }
}

