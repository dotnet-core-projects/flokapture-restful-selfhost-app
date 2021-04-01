using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class ActionWorkflows : EntityBase
    {
        private string _originEventMethod = string.Empty;

        [BsonRequired]
        public string WorkflowName { get; set; }
        [BsonRequired]
        public string OriginStatementId { get; set; }
        public string OriginObjectName => FileMaster.FileNameWithoutExt;
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string ProjectId { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string FileId { get; set; }

        public string OriginEventMethod
        {
            get => Regex.Replace(_originEventMethod, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);
            set => _originEventMethod = !string.IsNullOrEmpty(value) ? value : string.Empty;
        }

        [BsonRepresentation(BsonType.Boolean)]
        public bool Processed { get; set; } = false;

        [BsonRepresentation(BsonType.Boolean)]
        public bool IsDeleted { get; set; } = false;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster FileMaster { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual ProjectMaster ProjectMaster { get; set; }

        public override string ToString()
        {
            return $"{WorkflowName} # {OriginObjectName}";
        }

        [BsonIgnore]
        [JsonIgnore]
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>
        {
            {
                "ProjectMaster", new EntityLookup { From = "ProjectMaster", LocalField = "ProjectId", As = "ProjectMaster" }
            }, {
                "FileMaster", new EntityLookup { From = "FileMaster", As = "FileMaster", LocalField = "FileId" }
            }
        };
    }
}
