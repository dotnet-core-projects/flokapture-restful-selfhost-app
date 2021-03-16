using Microsoft.EntityFrameworkCore.Internal;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLayer.DbEntities
{
    public class FileTypeReference : EntityBase
    {
        private readonly List<string> _validExtension = new List<string> { ".xlsx", ".csv", ".xlx" };   
        /*
        public FileTypeReference()
        {
            var ext = this.FileExtension;
            if (!_validExtension.Any(e => e.Equals(ext))) return;
            if (Delimiter == null || string.IsNullOrEmpty(Delimiter.ToString()))
            {
                throw new ArgumentNullException(Delimiter.ToString(),
                    @"If extension is one of '.xlsx or .csv', then you must provide Delimiter");
            }
        }
        */
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string LanguageId { get; set; }
        [BsonRequired]
        public string FileExtension { get; set; }
        [BsonRequired]
        public string FileTypeName { get; set; }
        [BsonRequired]
        public string Color { get; set; }
        public List<string> FileFolders { get; set; } = new List<string>();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public char? Delimiter { get; set; } = null;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [BsonIgnoreIfNull]
        public virtual LanguageMaster LanguageMaster { get; set; }
        public override string ToString()
        {
            return $"{FileTypeName} # {FileExtension}";
        }

        [BsonIgnore]
        [JsonIgnore]
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>{
        {
            "LanguageMaster", new EntityLookup
            {
                From = "LanguageMaster", As = "LanguageMaster", LocalField = "LanguageId"
            }
        }};
    }
}
