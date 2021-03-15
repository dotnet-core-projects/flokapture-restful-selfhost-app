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
            var modifiedProgramText = ConcateMultilineStringLiterals(programText);
            var finalProgramText = ConcateMultilineLinqQueryExpressions(modifiedProgramText);
            var syntaxTree = CSharpSyntaxTree.ParseText(finalProgramText);
            var compilationRoot = syntaxTree.GetCompilationUnitRoot();
            var st = new SyntaxTrivia();
            // if need to remove property declarations
            /*
            var propertyNodes = (from field in compilationRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>() select field).ToList();
            var withoutProperty = compilationRoot.ReplaceNodes(propertyNodes, (syntax, declarationSyntax) => null).NormalizeWhitespace();
            var withoutPropText = withoutProperty.GetText().ToString();
            var tree = CSharpSyntaxTree.ParseText(withoutPropText);
            var root = tree.GetCompilationUnitRoot();
            */
            var commentTrivia = from t in compilationRoot.DescendantTrivia()
                                where // t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                                t.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                                || t.IsKind(SyntaxKind.RegionDirectiveTrivia)
                                || t.IsKind(SyntaxKind.PragmaChecksumDirectiveTrivia)
                                || t.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                                || t.IsKind(SyntaxKind.PragmaKeyword)
                                || t.IsKind(SyntaxKind.EmptyStatement)
                                || t.IsKind(SyntaxKind.AttributeList) // || t.IsKind(SyntaxKind.XmlComment) || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                select t;
            var newRoot = compilationRoot.ReplaceTrivia(commentTrivia, (t1, t2) => st).NormalizeWhitespace();
            var text = newRoot.GetText().ToString();
            var attrTree = CSharpSyntaxTree.ParseText(text);
            var attrRoot = attrTree.GetCompilationUnitRoot();
            var attrList = from a in attrRoot.DescendantNodes().OfType<AttributeListSyntax>() select a;
            var withoutAttrList = attrRoot.ReplaceNodes(attrList, (syntax, listSyntax) => null).NormalizeWhitespace().GetText().ToString();
            var fieldTree = CSharpSyntaxTree.ParseText(withoutAttrList);
            var fieldRoot = fieldTree.GetCompilationUnitRoot();

            // if need to remove field declarations
            // var fieldsList = (from field in fieldRoot.DescendantNodes().OfType<FieldDeclarationSyntax>() select field).ToList();
            // var withoutFields = fieldRoot.ReplaceNodes(fieldsList, (syntax, declarationSyntax) => null);
            var plainTextLines = fieldRoot.NormalizeWhitespace().GetText().ToString().Split('\n').Select(d => d.TrimEnd('\r')).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();

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

        private string ConcateMultilineStringLiterals(string programText)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(programText);
            var compilationRoot = syntaxTree.GetCompilationUnitRoot();
            var stringLiteralExp = compilationRoot.DescendantNodes().Where(d => d.IsKind(SyntaxKind.StringLiteralExpression) && d.GetText().Lines.Count >= 2).ToList();
            if (!stringLiteralExp.Any(d => d.GetText().Lines.Count >= 2)) return programText;
            var strLiteral = stringLiteralExp.First();
            if (strLiteral == null) return programText;
            var textLitLines = strLiteral.GetText();
            if (textLitLines.Lines.Count <= 1) return programText;
            var textLit = textLitLines.ToString();
            var textLines = textLit.Split(new[] { "\r\n" }, StringSplitOptions.None);
            string completeLine = Regex.Replace(string.Join(" ", textLines), "\\s+", " ");
            var lineExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(completeLine));
            var compilationUnitSyntax = compilationRoot.ReplaceNode(strLiteral, lineExpression);
            var plainTextLines = compilationUnitSyntax.GetText().ToString();
            return ConcateMultilineStringLiterals(plainTextLines);
        }

        private string ConcateMultilineLinqQueryExpressions(string programText)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(programText);
            var compilationRoot = syntaxTree.GetCompilationUnitRoot();
            var linqQueryExpressions = compilationRoot.DescendantNodes().Where(d => d.IsKind(SyntaxKind.QueryExpression) && d.GetText().Lines.Count >= 2).ToList();
            if (!linqQueryExpressions.Any(d => d.GetText().Lines.Count >= 2)) return programText;
            var firstToModify = linqQueryExpressions.First(d => d.GetText().Lines.Count >= 2);
            if (firstToModify == null) return programText;
            var textLitLines = firstToModify.GetText();
            if (textLitLines.Lines.Count <= 1) return programText;
            var textLit = textLitLines.ToString();
            var textLines = textLit.Split(new[] { "\r\n" }, StringSplitOptions.None);
            string completeLine = Regex.Replace(string.Join(' ', textLines), "\\s+", " ");
            var lineExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(completeLine));
            var compilationUnitSyntax = compilationRoot.ReplaceNode(firstToModify, lineExpression);
            var modifiedProgramText = compilationUnitSyntax.GetText().ToString();
            return ConcateMultilineLinqQueryExpressions(modifiedProgramText);
        }
    }
}
