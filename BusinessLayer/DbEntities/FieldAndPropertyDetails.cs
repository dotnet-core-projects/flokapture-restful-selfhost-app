using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class FieldAndPropertyDetails : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string ProjectId { get; set; }

        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileId { get; set; }

        public string ReturnType { get; set; }
        public int BaseCommandId { get; set; }
        public string ClassOrInterfaceName { get; set; }
        public string Name { get; set; }
        public FieldOrPropertyType FieldOrProperty { get; set; }
        private string _docComment;
        public string DocumentationComment
        {
            get => _docComment;
            set => _docComment = Regex.Match(value, @"<summary>(?<DocComment>.*)</summary>", RegexOptions.IgnoreCase).Groups["DocComment"].Value;
        }
        public override string ToString()
        {
            return $"{Name} # {ReturnType}";
        }
    }

    public enum FieldOrPropertyType
    {
        Field = 1,
        Property = 2
    }
}
