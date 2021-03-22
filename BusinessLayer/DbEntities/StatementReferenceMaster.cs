using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Diagnostics;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class StatementReferenceMaster : EntityBase
    {
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }

        public int BaseCommandId { get; set; } = 0;
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }
        public int LineIndex { get; set; }
        public string ResolvedStatement { get; set; }
        public string OriginalStatement { get; set; }
        public string MethodName { get; set; }
        public string VariableNameDeclared { get; set; } 
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string DataOrObjectType { get; set; }
        public string ClassNameDeclared { get; set; }
        public string AlternateName { get; set; }
        public string StatementComment { get; set; }
        public string AnnotateStatement { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster FileMaster { get; set; }

        public override string ToString()
        {
            return $@"{BaseCommandId} # {FileId} # {ResolvedStatement} #";
        }
    }
}
