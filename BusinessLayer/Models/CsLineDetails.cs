namespace BusinessLayer.Models
{
    public class CsLineDetails
    {
        public int LineIndex { get; set; }
        public string OriginalStatement { get; set; }
        public int BaseCommandId { get; set; }
        public string ResolvedStatement { get; set; }
        public string StatementComment { get; set; }
        public string MethodName { get; set; }
        public string MethodCalled { get; set; }
        public override string ToString()
        {
            return $"{LineIndex} # {ResolvedStatement}";
        }
    }
}
