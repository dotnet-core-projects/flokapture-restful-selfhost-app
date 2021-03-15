using System;
using System.Collections.Generic;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    public class FileMaster : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProjectId { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string FileTypeReferenceId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int Processed { get; set; }
        public string WorkflowStatus { get; set; }
        public int LinesCount { get; set; }
        public bool DoneParsing { get; set; } = false;

        [BsonIgnore] private string _fileNameWithoutExt;
        public string FileNameWithoutExt
        {
            get
            {
                try
                {
                    _fileNameWithoutExt = Path.GetFileNameWithoutExtension(FilePath);
                    return _fileNameWithoutExt;
                }
                catch (Exception)
                {
                    return _fileNameWithoutExt = "";
                }
            }
        }

        [BsonIgnoreIfNull]
        public virtual ProjectMaster ProjectMaster { get; set; }
        [BsonIgnoreIfNull]
        public virtual FileTypeReference FileTypeReference { get; set; }
        public override string ToString()
        {
            return $"{FileName}";
        }

        [BsonIgnore]
        [JsonIgnore]
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>
        {
            {
                "ProjectMaster", new EntityLookup
                {
                    From = "ProjectMaster", LocalField = "ProjectId", As = "ProjectMaster"
                }
            }, {
                "LanguageMaster", new EntityLookup
                { From = "LanguageMaster", As = "ProjectMaster.LanguageMaster",
                    LocalField = "ProjectMaster.LanguageId"
                }
            }, {
                "FileTypeReference", new EntityLookup
                {
                    From = "FileTypeReference", LocalField = "FileTypeReferenceId", ForeignField = "FileTypeReferenceId",
                    As = "FileTypeReference"
                }
            }
        };
    }
}
