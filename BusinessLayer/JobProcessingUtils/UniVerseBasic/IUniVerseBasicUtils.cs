using BusinessLayer.JobProcessingUtils.UniVerseBasic.ProcessUtilities;
using BusinessLayer.JobProcessingUtils.UniVerseBasic.Utils;

namespace BusinessLayer.JobProcessingUtils.UniVerseBasic
{
    public interface IUniVerseBasicUtils
    {
        UniVerseProcessUtilities UniVerseProcessHelpers { get; }
        UniVerseUtils UniVerseUtils { get; }
    }
}
