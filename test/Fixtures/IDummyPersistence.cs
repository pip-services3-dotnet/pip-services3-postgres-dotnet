using PipServices3.Commons.Data;
using PipServices3.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PipServices3.Postgres.Persistence
{
    public interface IDummyPersistence : IGetter<Dummy, string>, IWriter<Dummy, string>, IPartialUpdater<Dummy, string>
    {
        Task<DataPage<Dummy>> GetPageByFilterAsync(string correlationId, FilterParams filter, PagingParams paging);
        Task<long> GetCountByFilterAsync(string correlationId, FilterParams filter);
        Task<List<Dummy>> GetListByIdsAsync(string correlationId, string[] ids);
        Task<Dummy> SetAsync(string correlationId, Dummy item);
        Task DeleteByIdsAsync(string correlationId, string[] ids);
    }
}

