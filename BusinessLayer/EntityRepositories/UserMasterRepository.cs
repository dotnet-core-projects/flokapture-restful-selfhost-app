using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using MongoDB.Driver;

namespace BusinessLayer.EntityRepositories
{
    public class UserMasterRepository : BaseRepository<UserMaster>
    {
        /*
        public UserMasterRepository()
        {
            var definitionBuilder = Builders<UserMaster>.IndexKeys.Combine(
                Builders<UserMaster>.IndexKeys.Text(d => d.UserName),
                Builders<UserMaster>.IndexKeys.Ascending(d => d.CreatedOn));
            var indexModel = new CreateIndexModel<UserMaster>(definitionBuilder);
            MongoCollection.Indexes.CreateOne(indexModel);
        }
        */
        public override void CreateIndex(IndexKeysDefinitionBuilder<UserMaster> definitionBuilder)
        {
            /*
            var builder = definitionBuilder.Text(d => d.UserName);
            var indexModel = new CreateIndexModel<UserMaster>(builder);
            await MongoCollection.Indexes.CreateOneAsync(indexModel).ConfigureAwait(false);
            */
        }
    }
}
