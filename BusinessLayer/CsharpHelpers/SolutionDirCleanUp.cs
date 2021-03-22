using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLayer.CsharpHelpers
{
    public class SolutionDirCleanUp
    {
        public bool CleanUpDir(string dirPath)
        {
            var csFiles = Directory.GetFiles(dirPath, "*.cs", SearchOption.AllDirectories).ToList();
            foreach (var csFile in csFiles)
            {
                if (Regex.IsMatch(csFile, @"\b\\bin\\\b|\b\\obj\\\b|\b\.Test\b", RegexOptions.IgnoreCase)) continue;
                string fileName = Path.GetFileName(csFile);
                if (string.IsNullOrEmpty(fileName)) continue;
                if (fileName.Contains(".dll.config")) continue;
                if (new[] { "Reference", "AssemblyInfo" }.Any(fileName.StartsWith)) continue;

                CleanUpSingleFile(csFile);
            }
            return true;
        }

        protected internal void CleanUpSingleFile(string filePath)
        {
            string programText = File.ReadAllText(filePath);
            var rootTree = CSharpSyntaxTree.ParseText(programText);
            var compRoot = rootTree.GetCompilationUnitRoot();
            var syntaxTrivia = new SyntaxTrivia();
            var multiLineCommentTrivias = from t in compRoot.DescendantTrivia()
                                          where t.IsKind(SyntaxKind.MultiLineCommentTrivia)
                                          select t;
            var modifiedText = compRoot.ReplaceTrivia(multiLineCommentTrivias, (t1, t2) => syntaxTrivia).NormalizeWhitespace().GetText().ToString();
            var modifiedProgramText = CsharpHelper.ConcatMultiLineStringLiterals(modifiedText);
            var finalProgramText = CsharpHelper.ConcatMultiLineLinqQueryExpressions(modifiedProgramText);
            var remodifiedText = CsharpHelper.ConcatMultiLineComments(finalProgramText);
            var syntaxTree = CSharpSyntaxTree.ParseText(remodifiedText);
            var compilationRoot = syntaxTree.GetCompilationUnitRoot();
            // if need to remove property declarations
            /*
            var propertyNodes = (from field in compilationRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>() select field).ToList();
            var withoutProperty = compilationRoot.ReplaceNodes(propertyNodes, (syntax, declarationSyntax) => null).NormalizeWhitespace();
            var withoutPropText = withoutProperty.GetText().ToString();
            var tree = CSharpSyntaxTree.ParseText(withoutPropText);
            var root = tree.GetCompilationUnitRoot();
            */
            var commentTrivia = from t in compilationRoot.DescendantTrivia()
                                where // t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia) 
                                   t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                                || t.IsKind(SyntaxKind.RegionDirectiveTrivia)
                                || t.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia)
                                || t.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                                || t.IsKind(SyntaxKind.PragmaKeyword)
                                || t.IsKind(SyntaxKind.EmptyStatement)
                                || t.IsKind(SyntaxKind.AttributeList)
                                select t;
            var newRoot = compilationRoot.ReplaceTrivia(commentTrivia, (t1, t2) => syntaxTrivia).NormalizeWhitespace();
            var text = newRoot.GetText().ToString();
            var attrTree = CSharpSyntaxTree.ParseText(text);
            var attrRoot = attrTree.GetCompilationUnitRoot();
            var attrList = from a in attrRoot.DescendantNodes().OfType<AttributeListSyntax>() select a;
            var withoutAttrList = attrRoot.ReplaceNodes(attrList, (syntax, listSyntax) => null).NormalizeWhitespace().GetText().ToString();
            var withFormattedProperties = CsharpHelper.ReplaceFieldsAndProperties(withoutAttrList);
            
            // attention: do not apply NormalizeWhitespace() after this, as properties are now formatted in single line.
            // var fieldTree = CSharpSyntaxTree.ParseText(withFormattedProperties);
            // var fieldRoot = fieldTree.GetCompilationUnitRoot();

            // if need to remove field declarations
            // var fieldsList = (from field in fieldRoot.DescendantNodes().OfType<FieldDeclarationSyntax>() select field).ToList();
            // var withoutFields = fieldRoot.ReplaceNodes(fieldsList, (syntax, declarationSyntax) => null);
            // var plainTextLines = fieldRoot.GetText().ToString().Split('\n').Select(d => d.TrimEnd('\r')).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
            var plainTextLines = withFormattedProperties.Split('\n').Select(d => d.TrimEnd('\r')).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();

            // if need to cleanup more clutter
            /*
            var nameSpaces = from n in fieldRoot.DescendantNodes().OfType<UsingDirectiveSyntax>() select n;
            var usings = fieldRoot.Usings.AddRange(nameSpaces);
            var plainTextLines = fieldRoot.NormalizeWhitespace().GetText().ToString().Split('\n').Where(d => !usings.Any(u => d.Trim('\r', ' ').Equals(u.ToString())) && !string.IsNullOrEmpty(d.Trim('\r', ' ')) && !Regex.IsMatch(d.Trim('\r', ' '), @"^([;()\[\]]+)$")).Select(s => Regex.Replace(s.TrimEnd('\r', '\n', ' '), "\\s+", " ")).ToList();
            */

            // override original file with better formatting

            File.WriteAllLines(filePath, plainTextLines, Encoding.UTF8);

            Console.WriteLine($@"Formatted document: {filePath}");
            Console.WriteLine($@"==================================================");
        }
    }
}
