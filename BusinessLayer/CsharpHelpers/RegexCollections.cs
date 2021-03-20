using System.Text.RegularExpressions;

namespace BusinessLayer.CsharpHelpers
{ 
    public class RegexCollections
    {
        protected static readonly Regex Namespace = new Regex("^namespace\\s+");
        public static readonly Regex MethodStart = new Regex(@"(?:(?:public|private|protected|static|internal|sub|function)+\s+)+[$_.\w?<,>\[\]\s]*\s+(?<MethodName>[\$_\w<,>]+)\([^\)]*\)?\s*\{?[^\}]*\}?|(?:public|private|protected|static|internal|sub|function)+\s+(?<MethodName>[\$_\w?<,>]+)\([^\)]*\)?", RegexOptions.Singleline);
        public static readonly Regex ExcludeMethodDefRegex = new Regex(" abstract | delegate | extern ", RegexOptions.IgnoreCase);
        public static readonly Regex ClassStart = new Regex(@"(?:(?:public|abstract|protected|static|internal|sealed|partial)+\s+class\s+)|^[\s]*class\s+");
        public static readonly Regex IfStart = new Regex(@"^[\s]*if[\s]*\(.*?\)+", RegexOptions.IgnoreCase);
        protected static readonly Regex ElseStart = new Regex(@"^else$|^else\s+");
        protected static readonly Regex LoopStart = new Regex(@"^([\s]*)?for[\s+]*\(.*\)|^([\s]*)?while[\s+]*\(.*\)|^([\s]*)?foreach[\s+]*\(.*\)|.forEach[\s+]*\(", RegexOptions.IgnoreCase);
        protected static readonly Regex OpeningBracketRegEx = new Regex(@"{(?!.*(['""])(?:.*?)(?<!\\)(?>\\)*?\1*)");
        protected static readonly Regex ClosingBracketRegEx = new Regex(@"}(?!.*(['""])(?:.*?)(?<!\\)(?>\\)*?\1*)");
        public static readonly Regex BlockComment = new Regex(@"(/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/)", RegexOptions.Multiline & RegexOptions.IgnoreCase);
        public static readonly Regex LineComment = new Regex(@"^[\s]*//|^[\s]*\#", RegexOptions.Singleline);
        public static readonly Regex VariableDeclaration = new Regex(@"^[\s]*[\w]+\s+(?<VariableName>[a-zA-Z0-9$_]+)[\s]*=[\s]*", RegexOptions.IgnoreCase & RegexOptions.CultureInvariant);
        public static readonly Regex MethodNameRegex = new Regex(@"(?:(?:public|private|protected|static|internal|sub|function)+\s+)+[$_.\w?<,>\[\]\s]*\s+(?<MethodName>[\$_\w<,>]+)\([^\)]*\)?\s*\{?[^\}]*\}?|(?:public|private|protected|static|internal|sub|function)+\s+(?<MethodName>[\$_\w?<,>]+)\([^\)]*\)?", RegexOptions.IgnoreCase);
        protected static readonly Regex UsingRegex = new Regex(@"^\s*using\s*\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)", RegexOptions.IgnoreCase);
        protected static readonly Regex TryRegex = new Regex(@"^\s*try(\s*)?$", RegexOptions.IgnoreCase);
        protected static readonly Regex CatchRegex = new Regex(@"^\s*catch\s*?", RegexOptions.IgnoreCase);
        protected static readonly Regex FinallyRegex = new Regex(@"^\s*finally\s*?$", RegexOptions.IgnoreCase);
        public static readonly Regex ClassNameRegex = new Regex(@"(?:(?:public|abstract|protected|static|internal|sealed|partial)+\s+class\s+)(?<ClassName>[\S]+)|^[\s]*class\s+(?<ClassName>[\S]+)");
        public static readonly Regex InterfaceNameRegex = new Regex(@"(?:(?:public|internal|partial)+\s+interface\s+)(?<InterfaceName>[\S]+)|^[\s]*interface\s+(?<InterfaceName>[\S]+)");
        // public static readonly Regex EnumNameRegex = new Regex(@"(?:(?:public|internal|partial)+\s+enum\s+)(?<EnumName>[\S]+)|^[\s]*enum\s+(?<EnumName>[\S]+)");
        public static readonly Regex TypeNameRegex = new Regex(@"(?:(?:public|abstract|protected|static|internal|sealed|partial)+\s+class\s+)(?<TypeName>[\S]+)|^[\s]*class\s+(?<TypeName>[\S]+)|(?:(?:public|internal|partial)+\s+interface\s+)(?<TypeName>[\S]+)|^[\s]*interface\s+(?<TypeName>[\S]+)|(?:(?:public|internal|partial)+\s+enum\s+)(?<TypeName>[\S]+)|^[\s]*enum\s+(?<TypeName>[\S]+)");
        public static readonly Regex InterfaceStart = new Regex(@"(?:(?:public|internal|partial)+\s+interface\s+)|^[\s]*interface\s+");
        protected static readonly Regex EnumStart = new Regex(@"(?:(?:public|internal|partial)+\s+enum\s+)|^[\s]*enum\s+");
        protected static readonly Regex StructStart = new Regex(@"(?:(?:public|internal|partial)+\s+struct\s+)|^[\s]*struct\s+");
        protected static readonly Regex SwitchStartRegex = new Regex(@"^\s*switch\s*\(.*?\)");
        public static readonly Regex OpenCloseBraceRegex = new Regex(@"(['""])((?:\\1|(?:(?!\1)).)*)(\1)|(?<OpenMatch>([{]))|(?<CloseMatch>([}]))");
        public static readonly Regex OpenCloseParenRegex = new Regex(@"(['""])((?:\\1|(?:(?!\1)).)*)(\1)|(?<OpenMatch>([(]))|(?<CloseMatch>([)]))");
        // public static readonly Regex MethodOrVariableAttribute = new Regex(@"^\[[a-zA-Z0-9$_]+\]$");

        //=======================test regex's for opening and closing brackets==========================================

        public static readonly Regex TestOpeningBracket = new Regex(@"{(?!.*(['])(?:.*?)(?<!\\)(?>\\)*?\1*)|{(?!.*([""])(?:.*?)(?<!\\)(?>\\)*?\1*)");
        public static readonly Regex TestSkipOpenMatch = new Regex(@"(['""])((?:\\\1)|(?!\1).+)\1");

        public static readonly Regex TestClosingBracket = new Regex(@"}(?!.*(['])(?:.*?)(?<!\\)(?>\\)*?\1*)|}(?!.*([""])(?:.*?)(?<!\\)(?>\\)*?\1*)");
        public static readonly Regex TestSkipCloseMatch = new Regex(@"(['""])((?:\\\1)|(?!\1).+)\1");

        //=======================test regex's for opening and closing brackets==========================================

        public static Regex RegexForMethodName(string methodName)
        {
            return new Regex(@"(?<MethodName>this." + methodName + @"[\s]*)\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)|(?<MethodName>\s+([=(])?" + methodName + @"[\s]*)\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)|^(?<MethodName>" + methodName + @"[\s]*)\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)[\s;]+$");
        }

        public static bool IsConstructorCall(string className, string inputStatement)
        {
            var constructorCall = new Regex(@"new[\s+]" + className + @"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)");
            return constructorCall.IsMatch(inputStatement);
        }

        public static string CallInternalMethodName(string methodName, string inputStatement)
        {
            var callInternalMethodName = new Regex(@"(this.)?(?<MethodName>" + methodName + @"[\s]*)\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)");
            return callInternalMethodName.Match(inputStatement).Groups["MethodName"].Value;
        }
    }
}
