using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using BusinessLayer.ExtensionLibrary;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLayer.EntityRepositories
{
    public class StatementReferenceMasterRepository : BaseRepository<StatementReferenceMaster>
    {
        public async Task<List<StatementReferenceMaster>> GetAnyGenericBlock(string id, int sbcid, int ebcid)
        {
            var stages = new List<BsonDocument>
            {
                new BsonDocument
                {
                    {"$match", new BsonDocument {{"_id", new BsonDocument {{"$gte", ObjectId.Parse(id)}}}}}
                },
                new BsonDocument
                {
                    {
                        "$match", new BsonDocument {{"BaseCommandId", new BsonDocument {{"$gte", sbcid}, {"$lte", ebcid}}}}
                    }
                },
                new BsonDocument("$limit", 2),
                new BsonDocument {{"$project", new BsonDocument {{"_id", 1}, {"BaseCommandId", 1}}}}
            };
            try
            {
                var result = await Collection.Aggregate().AppendStages(stages).ToListAsync().ConfigureAwait(false);
                if (result.Count != 2) return new List<StatementReferenceMaster>();
                var startAt = result.First();
                var endAt = result.Last();
                var filter = new BsonDocument { { "_id", new BsonDocument { { "$gte", ObjectId.Parse(startAt._id) }, { "$lte", ObjectId.Parse(endAt._id) } } } };
                var genericBlock = Collection.FindAsync(filter).ConfigureAwait(false).GetAwaiter().GetResult().ToList();
                return genericBlock;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return new List<StatementReferenceMaster>();
            }
        }
    }
}