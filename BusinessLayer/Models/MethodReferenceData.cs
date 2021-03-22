using BusinessLayer.DbEntities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BusinessLayer.Models
{
    public class MethodReferenceMaster : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string SourceFileId { get; set; }
        public string SourceFileName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster SourceFileMaster { get; set; }
        public virtual List<MethodReferenceData> MethodReferences { get; set; }
        public override string ToString()
        {
            return $@"{SourceFileName}";
        }
    }
    public class MethodReferenceData
    {
        public string MethodName { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string ReferencedFileId { get; set; }
        [BsonRequired]
        public int LineIndex { get; set; } 
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster ReferencedFileMaster { get; set; } 
        public override string ToString()
        {
            return $"{MethodName} # {ReferencedFileMaster.FileName} # {LineIndex}";
        }
    }
}
