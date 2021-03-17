using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using CsvHelper.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class UniVerseDataDictionary : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        [Required]
        public string FileId { get; set; }
        //[BsonRepresentation(BsonType.ObjectId)]
        //[BsonRequired]
        //[Required]
        //public string ProjectId { get; set; }
        public string FileName { get; set; }
        public string FieldNo { get; set; }
        public string Description { get; set; }
        public string FieldLabel { get; set; }
        public string RptFieldLength { get; set; }
        public string TypeOfData { get; set; }
        public string SingleArray { get; set; }
        public string DateOfCapture { get; set; }
        public string ReplacementName { get; set; }

        [BsonIgnoreIfNull]
        public virtual FileMaster FileMaster { get; set; }
        //[BsonIgnoreIfNull]
        //public virtual ProjectMaster ProjectMaster { get; set; }

        public override string ToString()
        {
            return $"{FileName} # {FieldNo}";
        }

        [BsonIgnore]
        [JsonIgnore]
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>
        {
            {"FileMaster", new EntityLookup {From = "FileMaster", LocalField = "FileId", As = "FileMaster"}},
            {
                "ProjectMaster", new EntityLookup
                    {As = "FileMaster.ProjectMaster", LocalField = "FileMaster.ProjectId", From = "ProjectMaster"}
            },
            {
                "LanguageMaster", new EntityLookup
                { From = "LanguageMaster", As = "FileMaster.ProjectMaster.LanguageMaster",
                    LocalField = "FileMaster.ProjectMaster.LanguageId"
                }
            }
        };
    }
    public sealed class DataDictMap : ClassMap<UniVerseDataDictionary>
    {
        public DataDictMap()
        {
            Map(m => m.FileName).Name("\"FILE NAME\"", "\"FILE_NAME\"", "\"FILE\"");
            Map(m => m.FieldNo).Name("\"FIELD NO\"", "\"FIELD_NO\"", "\"FIELD\"");
            Map(m => m.Description).Name("\"DESCRIPTION\"");
            Map(m => m.FieldLabel).Name("\"FIELD LABEL\"", "\"FIELD_LABEL\"");
            Map(m => m.RptFieldLength).Name("\"RPT FIELD LENGTH\"", "\"RPT_FIELD_LENGTH\"");
            Map(m => m.TypeOfData).Name("\"TYPE OF DATA\"", "\"TYPE_OF_DATA\"");
            Map(m => m.SingleArray).Name("\"SINGLE/ ARRAY\"", "\"SINGLE_ARRAY\"");
            Map(m => m.DateOfCapture).Name("\"DATE OF CAPTURE\"", "\"DATE_OF_CAPTURE\"");
        }
    }
}
