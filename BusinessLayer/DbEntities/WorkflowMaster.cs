namespace BusinessLayer.DbEntities
{
    public class WorkflowMaster : StatementReferenceMaster
    {
        public string ActionWorkflowId { get; set; }
        public string GraphName { get; set; }
        public string GraphId { get; set; }
        public string ParentId { get; set; }
    }
}
