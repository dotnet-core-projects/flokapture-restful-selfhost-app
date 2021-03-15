using System.Collections.Generic;
using System.IO;
using System.Linq;
using BusinessLayer.DbEntities;
using BusinessLayer.Models;

namespace BusinessLayer.JobProcessingUtils.UniVerseBasic.Utils
{
    public class UniVerseUtils
    {
        public bool ValidateDirStructure(ProjectMaster projectMaster)
        {
            var allDirectories = Directory.GetDirectories(projectMaster.PhysicalPath).ToList();
            var requiredDirectories = new List<string> { "Menu", "I-Descriptors" };
            var directoryNames = (from d in allDirectories let dir = new DirectoryInfo(d) select dir.Name).ToList();
            bool isPresent = requiredDirectories.All(d => directoryNames.Any(r => r == d));
            return isPresent;
        }
        public List<LineDetails> RemoveCommentedAndBlankLines(string[] inputArray, char commentChar = '*')
        {
            if (inputArray.Length <= 0) return new List<LineDetails>();
            int index = -1;
            var lineDetails = inputArray.Select(line => new LineDetails
            {
                LineIndex = ++index,
                OriginalLine = line,
                ParsedLine = line.Trim()
            }).ToList();

            var inputLines = lineDetails.Where((w, i) =>
                !string.IsNullOrEmpty(w.OriginalLine) &&
                !string.IsNullOrWhiteSpace(w.OriginalLine) &&
                !w.OriginalLine.StartsWith(commentChar)).ToList();

            return inputLines;
        }
    }
}
