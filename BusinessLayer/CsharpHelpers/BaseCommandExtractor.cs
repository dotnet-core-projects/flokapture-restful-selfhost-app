using System.Collections.Generic;
using System.Text.RegularExpressions;
using BusinessLayer.Models;

namespace BusinessLayer.CsharpHelpers
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BaseCommandExtractor : RegexCollections
    {
        private static int GetBaseCommandId(string statement)
        {
            if (Namespace.IsMatch(statement)) return 99;
            if (ClassStart.IsMatch(statement)) return 19;
            if (IfStart.IsMatch(statement)) return 1;
            if (ElseStart.IsMatch(statement.Trim())) return 10;
            if (LoopStart.IsMatch(statement)) return 3;
            if (MethodStart.IsMatch(statement) && !ExcludeMethodDefRegex.IsMatch(statement)) return 8;
            if (InterfaceStart.IsMatch(statement)) return 88; // set end interface 89
            if (EnumStart.IsMatch(statement)) return 78; // set end enum 79
            if (SwitchStartRegex.IsMatch(statement)) return 58; // set end switch 59
            if (StructStart.IsMatch(statement)) return 48; // set end struct 49
            return 0;
        }

        private static int TryCatchBaseCommandId(string statement)
        {
            string inputStatement = statement.Trim();
            if (TryRegex.IsMatch(inputStatement)) return 101; // end try set 102
            if (CatchRegex.IsMatch(inputStatement)) return 201; // end catch set 202
            if (FinallyRegex.IsMatch(inputStatement)) return 301; // end finally set 302
            if (UsingRegex.IsMatch(inputStatement)) return 401;// end using set 402
            return 0;
        }

        public static List<CsLineDetails> AssignBaseCommandId(List<CsLineDetails> lstLineDetails)
        {
            for (int len = 0; len < lstLineDetails.Count; len++)
            {
                var lineDetail = lstLineDetails[len];
                var baseCommandId = GetBaseCommandId(lineDetail.ResolvedStatement);
                lineDetail.BaseCommandId = lineDetail.BaseCommandId == 0 ? baseCommandId : lineDetail.BaseCommandId;
                if (baseCommandId == 0) continue;
                int bracketsCount = 0;
                for (int remLength = len + 1; remLength < lstLineDetails.Count; remLength++)
                {
                    var element = lstLineDetails[remLength];
                    // following lines are replaced with previous logic...
                    // this one is more trusted and checked it with multiple files...
                    var matches = OpenCloseBraceRegex.Matches(element.ResolvedStatement);
                    foreach (Match match in matches)
                    {
                        if (match.Groups["OpenMatch"].Success) bracketsCount++;
                        if (match.Groups["CloseMatch"].Success) bracketsCount--;
                    }
                    if (bracketsCount != 0) continue;
                    if (baseCommandId == 1)
                        lstLineDetails[remLength].BaseCommandId = 2;
                    if (baseCommandId == 19)
                        lstLineDetails[remLength].BaseCommandId = 20;
                    if (baseCommandId == 8) // Set method name here...
                    {
                        string methodName = MethodNameRegex.Match(lineDetail.ResolvedStatement).Groups["MethodName"].Value;
                        string actualMethodName = Regex.Replace(methodName, "<.*>", "").Trim();
                        lineDetail.MethodName = actualMethodName;
                        lstLineDetails[remLength].BaseCommandId = 9;
                    }
                    if (baseCommandId == 3)
                        lstLineDetails[remLength].BaseCommandId = 4;
                    if (baseCommandId == 10)
                    {
                        lstLineDetails[remLength].BaseCommandId = 2;
                        for (int ifLen = remLength - 1; ifLen >= 0; ifLen--)
                        {
                            if (lstLineDetails[ifLen].BaseCommandId != 2) continue;
                            lstLineDetails[ifLen].BaseCommandId = 0;
                            break;
                        }
                    }
                    if (baseCommandId == 88)
                        lstLineDetails[remLength].BaseCommandId = 89;
                    if (baseCommandId == 78)
                        lstLineDetails[remLength].BaseCommandId = 79;
                    if (baseCommandId == 58)
                        lstLineDetails[remLength].BaseCommandId = 59;
                    if (baseCommandId == 99)
                        lstLineDetails[remLength].BaseCommandId = 100;
                    if (baseCommandId == 48)
                        lstLineDetails[remLength].BaseCommandId = 49;

                    break;
                }
            }

            return lstLineDetails;
        }

        public static List<CsLineDetails> AssignBaseCommandToTryCatch(List<CsLineDetails> lstLineDetails)
        {
            for (int len = 0; len < lstLineDetails.Count; len++)
            {
                var lineDetail = lstLineDetails[len];
                var baseCommandId = TryCatchBaseCommandId(lineDetail.ResolvedStatement);
                lineDetail.BaseCommandId = lineDetail.BaseCommandId == 0 ? baseCommandId : lineDetail.BaseCommandId;
                if (baseCommandId == 0) continue;
                int bracketsCount = 0;
                for (int remLength = len + 1; remLength < lstLineDetails.Count; remLength++)
                {
                    var element = lstLineDetails[remLength];
                    if (OpeningBracketRegEx.IsMatch(element.ResolvedStatement))
                        bracketsCount++;
                    if (ClosingBracketRegEx.IsMatch(element.ResolvedStatement))
                        bracketsCount--;
                    if (bracketsCount != 0) continue;
                    if (baseCommandId == 101)
                        lstLineDetails[remLength].BaseCommandId = 102;
                    if (baseCommandId == 201)
                        lstLineDetails[remLength].BaseCommandId = 202;
                    if (baseCommandId == 301)
                        lstLineDetails[remLength].BaseCommandId = 302;
                    if (baseCommandId == 401)
                        lstLineDetails[remLength].BaseCommandId = 402;

                    break;
                }
            }
            return lstLineDetails;
        } 
    }
}