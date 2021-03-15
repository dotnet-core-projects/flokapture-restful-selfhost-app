using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FloKaptureJobProcessingApp.InternalModels
{
    public class MethodReferenceData
    {
        public string MethodName { get; set; }
        public string SourceFileName { get; set; }
        public string SourceFile { get; set; }
        public string ReferencedFile { get; set; }
        public string ReferenceFileName { get; set; }
        public int LineIndex { get; set; }
    }
}
