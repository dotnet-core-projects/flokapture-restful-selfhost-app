using System;
using Newtonsoft.Json;

namespace BusinessLayer.FloKaptureExtensions
{
    public static class FloKaptureExtensions
    {
        /*
        public static IMvcBuilder AddCollectionIndexes(this IMvcBuilder builder)
        {
            
            var userMasterRepository = new UserMasterRepository();
            var definitionBuilder = Builders<UserMaster>.IndexKeys.Combine(
                Builders<UserMaster>.IndexKeys.Text(d => d.UserName),
                Builders<UserMaster>.IndexKeys.Ascending(d => d.CreatedOn));
            var indexModel = new CreateIndexModel<UserMaster>(definitionBuilder);
            userMasterRepository.MongoCollection.Indexes.CreateOne(indexModel);
            
            return builder;
        }
        */

        public static void PrintToConsole(this object anyObject)
        {
            Console.WriteLine("=======================================================================");
            Console.WriteLine(JsonConvert.SerializeObject(anyObject));
            Console.WriteLine("=======================================================================");
        }
    }
}
