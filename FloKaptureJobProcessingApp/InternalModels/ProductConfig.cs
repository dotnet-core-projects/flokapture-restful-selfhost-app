using System.ComponentModel.DataAnnotations;
using BusinessLayer.DbEntities;
using MongoDB.Bson.Serialization.Attributes;

namespace FloKaptureJobProcessingApp.InternalModels
{
    public class ProductConfig : EntityBase
    {
        [Required]
        [BsonRequired]
        public string PropertyName { get; set; }

        [Required]
        [BsonRequired]
        public string Value { get; set; }

        public override string ToString()
        {
            return $"{PropertyName} # {Value}";
        }
    }
}
