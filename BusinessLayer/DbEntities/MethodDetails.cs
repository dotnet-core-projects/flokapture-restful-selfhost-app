using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    public class MethodDetails : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string ProjectId { get; set; }

        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }

        [BsonRequired]
        public string MethodName { get; set; }
        public string ReturnType { get; set; }
        public string ParameterList { get; set; }
        public string Modifiers { get; set; }
        public List<ParameterDetails> ParameterDetails { get; set; }
        public int ParameterCount { get; set; }
        public string ClassName { get; set; }
        public string MethodMatchRegex { get; set; }
        private string _docComment;
        public string DocumentationComment
        {
            get => _docComment;
            set => _docComment = Regex.Match(value, @"<summary>(?<DocComment>.*)</summary>", RegexOptions.IgnoreCase).Groups["DocComment"].Value;
        }
        public override string ToString()
        {
            return $"{MethodName} # {ReturnType} # {Modifiers}";
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual FileMaster FileMaster { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual ProjectMaster ProjectMaster { get; set; }
    }

    public class ParameterDetails
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsPredefined { get; set; }
        public override string ToString()
        {
            return $@"{Name} # {Type} # {IsPredefined}";
        }
    }
}
