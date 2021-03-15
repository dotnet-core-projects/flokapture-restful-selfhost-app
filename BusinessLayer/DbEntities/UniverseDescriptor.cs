using System.Collections.Generic;

namespace BusinessLayer.DbEntities
{
    public class UniverseDescriptor : EntityBase
    {
        public int DescriptorId { get; set; }
        public string Entity { get; set; }
        public string StoredProcedureName { get; set; }
        public string Type { get; set; }
        public string DefaultReportDisplayHeading { get; set; }
        public string DefaultFormating { get; set; }
        public string DefaultConversion { get; set; }
        public string ValuedAssociation { get; set; }
        public string LongDescription { get; set; }
        public string StatementString { get; set; }
        public string StatementsListed { get; set; }
        public string ExtractionNotes { get; set; }

        public string CompleteName;
        public int ProjectId { get; set; }
    }

    public class UniverseDescriptorList
    {
        public int FileId { get; set; }
        public string FileName { get; set; }
        public string EntityName { get; set; }
        public string StatementList { get; set; }
    }

    public class UniverseJclList
    {
        public int DescriptorId { get; set; }
        public string StoredProcedureName { get; set; }
        public string CompleteName { get; set; }
        public List<FileMaster> FileMasters { get; set; }
    }
}
