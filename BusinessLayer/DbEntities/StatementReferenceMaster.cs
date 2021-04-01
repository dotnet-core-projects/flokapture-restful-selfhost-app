using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class StatementReferenceMaster : EntityBase
    {
        // [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }

        public int BaseCommandId { get; set; } = 0;
        // [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }
        public int LineIndex { get; set; }
        public string ResolvedStatement { get; set; }
        public string OriginalStatement { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string MethodName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string VariableNameDeclared { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string DataOrObjectType { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string ClassNameDeclared { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string AlternateName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string StatementComment { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public string AnnotateStatement { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public List<CallExternals> CallExternals { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster FileMaster { get; set; }

        public override string ToString()
        {
            return $@"{LineIndex} # {BaseCommandId} # {ResolvedStatement}";
        }
    }

    public class CallExternals
    {
        public string MethodName { get; set; }
        public int MethodLocation { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string ReferencedFileId { get; set; }

        public override string ToString()
        {
            return $"{MethodName} # {MethodLocation}";
        }
    }
}
