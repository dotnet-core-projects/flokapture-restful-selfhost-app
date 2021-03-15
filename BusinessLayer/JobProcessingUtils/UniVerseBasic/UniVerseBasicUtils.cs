using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using BusinessLayer.JobProcessingUtils.UniVerseBasic.ProcessUtilities;
using BusinessLayer.JobProcessingUtils.UniVerseBasic.Utils;

namespace BusinessLayer.JobProcessingUtils.UniVerseBasic
{
    public class UniVerseBasicUtils : IUniVerseBasicUtils
    {
        private UniVerseProcessUtilities _uniVerseProcessHelpers;
        private UniVerseUtils _uniVerseUtils;
        public UniVerseProcessUtilities UniVerseProcessHelpers => _uniVerseProcessHelpers ?? (_uniVerseProcessHelpers = new UniVerseProcessUtilities());
        public UniVerseUtils UniVerseUtils => _uniVerseUtils ?? (_uniVerseUtils = new UniVerseUtils());
        public BaseRepository<T> RepositoryOf<T>() where T : EntityBase
        {
            return new BaseRepository<T>();
        }
    }
}
