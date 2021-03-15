using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    public class LanguageMaster : EntityBase
    {
        [BsonRequired]
        public string LanguageName { get; set; }

        public override string ToString()
        {
            return LanguageName;
        }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string Description { get; set; }
    }
}
