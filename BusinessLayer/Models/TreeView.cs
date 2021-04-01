namespace BusinessLayer.Models
{
    public class TreeView
    {
        public int NodeId { get; set; }
        public string GraphId { get; set; }
        public string GraphName { get; set; }
        public string ParentId { get; set; }
        public int BaseCommandId { get; set; }
        public int PrimaryCommandId { get; set; }
        public string ClassCalled { get; set; }
        public string MethodCalled { get; set; }
        public string ActualStatementId { get; set; }
        public string GroupName { get; set; }
        public int GroupId { get; set; }
        public string FileId { get; set; }
        public int IndentLevel { get; set; }
        public string AlternateName { get; set; }
        public override string ToString()
        {
            return $"{BaseCommandId} # {GraphName}";
        }
    }
}
