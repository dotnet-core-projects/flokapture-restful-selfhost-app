using System;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessLayer.DbEntities
{
    public class OrganizationMaster : EntityBase
    {
        [Required]
        [BsonRequired]
        public string Name { get; set; }
        public int MaxLoC { get; set; }
        public DateTime? IssuedDate { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; } = DateTime.Now.AddYears(2);

        [Required]
        [BsonRequired]
        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Email is required and must be valid")]
        public string Email { get; set; }

        [Required]
        [BsonRequired]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\d{3}-\d{3}-\d{4}$", ErrorMessage = "PhoneNo has an invalid format. Format: 000-000-0000")]
        public string PhoneNumber { get; set; }

        public override string ToString()
        {
            return $"{_id} # {Name}";
        }
    }
}
