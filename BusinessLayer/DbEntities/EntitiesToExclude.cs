using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessLayer.DbEntities
{
    public class EntitiesToExclude : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }
        public string FileName { get; set; }
        public virtual FileMaster FileMaster { get; set; }
    }
}