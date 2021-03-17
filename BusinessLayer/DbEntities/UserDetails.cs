using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class UserDetails : EntityBase
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        [DataType(DataType.EmailAddress, ErrorMessage = "Not valid email")]
        [StringLength(15, ErrorMessage = "Length")]
        public string Email { get; set; }

        [BsonIgnoreIfNull]
        [BsonIgnoreIfDefault]
        public UserMaster UserMaster { get; set; }
        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>
        {
            {
                "UserMaster", new EntityLookup
                {
                    As = "UserMaster",
                    ForeignField = "_id",
                    LocalField = "UserId",
                    From = "UserMaster"
                }
            }
        };
    }
}
