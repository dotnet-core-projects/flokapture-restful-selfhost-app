using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;

namespace FloKaptureJobProcessingApp.FloKaptureServices
{
    public interface IGeneralService
    {
        BaseRepository<T> BaseRepository<T>() where T : EntityBase;
    }

    public class GeneralService : IGeneralService
    {
        public BaseRepository<T> BaseRepository<T>() where T : EntityBase
        {
            return new BaseRepository<T>();
        }
    }
}
