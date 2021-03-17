using System.Diagnostics;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class EntityLookup
    {
        public string From { get; set; }
        public string ForeignField { get; set; } = "_id";
        public string LocalField { get; set; }
        public string As { get; set; }
    }
}
