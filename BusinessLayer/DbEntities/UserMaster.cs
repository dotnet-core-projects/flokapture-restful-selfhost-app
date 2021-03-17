using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace BusinessLayer.DbEntities
{
    [DebuggerStepThrough]
    public class UserMaster : EntityBase
    {
        public string UserName { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
        public static Dictionary<string, EntityLookup> Lookup => new Dictionary<string, EntityLookup>();
    }
}
