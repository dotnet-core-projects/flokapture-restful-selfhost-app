using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using MongoDB.Driver;

namespace BusinessLayer.EntityRepositories
{
    public class LanguageMasterRepository : BaseRepository<LanguageMaster>
    {
        public override void CreateIndex(IndexKeysDefinitionBuilder<LanguageMaster> definitionBuilder)
        {
            
        }
    }
}
