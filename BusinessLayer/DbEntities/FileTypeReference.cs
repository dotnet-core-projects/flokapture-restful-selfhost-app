using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    public class FileTypeReference : EntityBase
    {
        public FileTypeReference()
        {
            FileTypeReferenceId = ObjectId.GenerateNewId().ToString();
        }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        [BsonDefaultValue(BsonType.ObjectId)]
        public string FileTypeReferenceId { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string LanguageId { get; set; }
        public string FileExtension { get; set; }
        public string FileTypeName { get; set; }
        public string Color { get; set; }
        public List<string> FileFolders { get; set; } = new List<string>();
        public char? Delimiter { get; set; } = null;
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
