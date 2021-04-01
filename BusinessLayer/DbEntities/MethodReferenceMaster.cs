using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class MethodReferenceMaster : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }
        public string MethodName { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string SourceFileId { get; set; }
        public string SourceFileName { get; set; }
        public string SourceFilePath { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string ReferencedFileId { get; set; }
        public int MethodLocation { get; set; }
        public string InvocationLine { get; set; }
        public string ReferencedFilePath { get; set; }
        public string ReferencedFileName { get; set; }
        public int InvocationLocation { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual ProjectMaster ProjectMaster { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster SourceFileMaster { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster ReferenceFileMaster { get; set; }
        public override string ToString()
        {
            return $"{MethodName} # {SourceFileName} # {ReferencedFileName}";
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnore]
        [JsonIgnore]
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>
        {
            {
                "ProjectMaster", new EntityLookup {From = "ProjectMaster", LocalField = "ProjectId", As = "ProjectMaster"}
            }, {
                "SourceFileMaster", new EntityLookup {As = "SourceFileMaster", LocalField = "SourceFileId", From = "FileMaster"}
            }, {
                "ReferenceFileMaster", new EntityLookup {As = "ReferenceFileMaster", LocalField = "ReferencedFileId", From = "FileMaster"}
            }
        };
    }
}
