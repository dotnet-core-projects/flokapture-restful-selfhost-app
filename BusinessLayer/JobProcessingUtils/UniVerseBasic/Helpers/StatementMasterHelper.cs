using BusinessLayer.DbEntities;
using BusinessLayer.Models;

namespace BusinessLayer.JobProcessingUtils.UniVerseBasic.Helpers
{
    public class StatementMasterHelper
    {
        public virtual BaseCommandReference ExtractBaseCommandId(LineDetails lineDetails)
        {
            return new BaseCommandReference { BaseCommandId = 5 };
        }

        public UniVerseDataDictionary PrepareDataDictionary(string row, string projectId)
        {
            return new UniVerseDataDictionary();
        }
    }
}
