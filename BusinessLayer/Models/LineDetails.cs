using MongoDB.Bson;

namespace BusinessLayer.Models
{
    public class LineDetails
    {
        public int LineIndex { get; set; }
        public string ParsedLine { get; set; }
        public string OriginalLine { get; set; }
        public string StatementComment { get; set; } = null;
        public string BusinessName { get; set; } = null;
        public ObjectId? ReferenceFileId { get; set; } = null;
        public override string ToString()
        {
            return $"{LineIndex} # {ParsedLine}";
        }
    }
}
