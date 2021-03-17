using System.Diagnostics;
using MongoDB.Bson.Serialization.Attributes;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class BaseCommandReference : EntityBase
    {
        [BsonRequired]
        public int BaseCommandId { get; set; }

        [BsonRequired]
        public string BaseCommand { get; set; }

        public override string ToString()
        {
            return $"{BaseCommandId} # {BaseCommand}";
        }
    }
}
