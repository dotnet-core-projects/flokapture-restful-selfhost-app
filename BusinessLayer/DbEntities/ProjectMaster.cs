using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    public class ProjectMaster : EntityBase // : IProjectMaster
    {
        [BsonRequired]
        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string LanguageId { get; set; }

        [Required]
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string OrganizationId { get; set; }

        [Required]
        [BsonRequired]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "Project Name must be between 5 to 20 characters")]
        public string ProjectName { get; set; }
        public string PhysicalPath { get; set; }
        public string Description { get; set; }
        public int? TotalFiles { get; set; } = 0;
        public DateTime? UploadedDate { get; set; } = DateTime.Now;
        public string UploadedTime { get; set; } = DateTime.Now.ToString("hh:mm:ss tt");
        public DateTime? ProcessedDate { get; set; }
        public string ProcessedTime { get; set; }
        public int? Classes { get; set; } = 0;
        public int? Screens { get; set; } = 0;
        public int? BusinessRulesCollected { get; set; } = 0;
        public double Size { get; set; } = 0.0;
        public bool Active { get; set; } = true;
        public ProcessStatus Processed { get; set; } = ProcessStatus.Uploaded;
        public bool IsCtCode { get; set; } = true;
        public bool IsTagsFrozen { get; set; } = false;
        public int LinesCount { get; set; } = 0;

        [BsonIgnoreIfNull]
        public virtual LanguageMaster LanguageMaster { get; set; }
        public override string ToString()
        {
            return $"{ProjectName} # {LanguageMaster?.LanguageName}";
        }

        [BsonIgnore]
        [JsonIgnore]
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>
        {
            {
                "LanguageMaster", new EntityLookup
                {
                    From = "LanguageMaster",
                    LocalField = "LanguageId",
                    ForeignField = "_id",
                    As = "LanguageMaster"
                }
            }
        };
    }

    public enum ProcessStatus
    {
        Uploaded = 0,
        Processing,
        UnderException,
        Processed
    }
}