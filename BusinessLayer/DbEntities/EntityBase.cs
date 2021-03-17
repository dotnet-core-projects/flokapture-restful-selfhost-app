using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public abstract class EntityBase
    {
        private ObjectId _objectId;
        [ScaffoldColumn(false)]
        [JsonIgnore]
        [BsonIgnore]
        [BsonIgnoreIfNull]
        public ObjectId Id
        {
            get
            {
                var canParse = ObjectId.TryParse(_id, out _objectId);
                return canParse ? _objectId : ObjectId.Empty;
            }
            set
            {
                var tryParse = ObjectId.TryParse(_id, out value);
                _objectId = tryParse ? value : ObjectId.Empty;
            }
        }

        [ScaffoldColumn(false)]
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId, AllowOverflow = true, AllowTruncation = true)]
        // ReSharper disable once InconsistentNaming
        public string _id { get; set; }

        [JsonIgnore]
        [BsonIgnoreIfNull]
        [BsonIgnoreIfDefault]
        public BsonDocument Document { get; set; }

        public DateTime? CreatedOn { get; set; } = DateTime.Now;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public DateTime? UpdatedOn { get; set; }
    }
}
