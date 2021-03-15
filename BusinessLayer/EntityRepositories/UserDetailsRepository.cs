using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using MongoDB.Driver;

namespace BusinessLayer.EntityRepositories
{
    public class UserDetailsRepository : BaseRepository<UserDetails>
    {
        public override void CreateIndex(IndexKeysDefinitionBuilder<UserDetails> definitionBuilder)
        {
            
        }
    }
}
