using BusinessLayer.DbEntities;
using BusinessLayer.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BusinessLayer.CsharpHelpers
{
    public static class CsharpHelper
    {
        public static string ConcatMultiLineStringLiterals(string programText)
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
            return ConcatMultiLineStringLiterals(plainTextLines);
        }
        public static string ConcatMultiLineComments(string programText)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(programText);
            var compilationRoot = syntaxTree.GetCompilationUnitRoot();
            var singleLineComments = compilationRoot.DescendantTrivia()
                .Where(d => d.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                            || d.IsKind(SyntaxKind.MultiLineCommentTrivia)).ToList();
            if (!singleLineComments.Any()) return programText;
            var singleLineComment = singleLineComments.First();
            var commentLine = singleLineComment.ToString();
            string completeLine = Regex.Replace(string.Concat(@"// ", Regex.Replace(commentLine, "\r\n", " "), "\r\n"), @"\s+", " ");
            var lineExpression = SyntaxFactory.SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, completeLine);
            var compilationUnitSyntax = compilationRoot.ReplaceTrivia(singleLineComment, lineExpression).NormalizeWhitespace();
            var plainTextLines = compilationUnitSyntax.GetText().ToString();
            return ConcatMultiLineComments(plainTextLines);
        }
        public static string ConcatMultiLineLinqQueryExpressions(string programText)
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
            return ConcatMultiLineLinqQueryExpressions(modifiedProgramText);
        }
        public static string ReplaceFieldsAndProperties(string programText)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(programText);
            var compilationRoot = syntaxTree.GetCompilationUnitRoot();
            var propertyNodes = (from field in compilationRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>() select field).ToList();
            if (!propertyNodes.Any()) return programText;
            var syntaxTriviaList = SyntaxFactory.TriviaList(SyntaxFactory.Tab, SyntaxFactory.ElasticTab);
            var withoutProperty = compilationRoot.ReplaceNodes(propertyNodes, (syntax, declarationSyntax) =>
                declarationSyntax.ReplaceTrivia(declarationSyntax.DescendantTrivia()
                        .Where(t => t.IsKind(SyntaxKind.EndOfLineTrivia)
                                    || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                    || t.IsKind(SyntaxKind.SingleLineCommentTrivia)), (t, _) => SyntaxFactory.Space)
                    .WithoutLeadingTrivia()
                    .WithLeadingTrivia(syntaxTriviaList)
                    // .WithLeadingTrivia(SyntaxTriviaList.Create(SyntaxFactory.ElasticTab))
                    .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)));
            var formattedPropertyText = withoutProperty.GetText().ToString();
            return formattedPropertyText;
        }
        public static List<CsLineDetails> PrepareCsLineDetails(List<string> programLines)
        {
            if (!programLines.Any()) return new List<CsLineDetails>();
            int indPos = -1;
            return programLines.Select(lineDetail => new CsLineDetails
            {
                LineIndex = ++indPos,
                ResolvedStatement = lineDetail.Trim(),
                BaseCommandId = 0,
                MethodName = "",
                OriginalStatement = lineDetail,
                StatementComment = "",
                MethodCalled = string.Empty
            }).ToList();
        }
        public static Tuple<List<MethodDetails>, List<FieldAndPropertyDetails>> ExtractMemberDetails(string programText)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(programText);
            var compilationRoot = syntaxTree.GetCompilationUnitRoot();
            var classDeclarations = (from field in compilationRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
                                     select field).ToList();
            var interfaceDeclarations = (from field in compilationRoot.DescendantNodes().OfType<InterfaceDeclarationSyntax>()
                                         select field).ToList();
            var enumDeclarations = (from field in compilationRoot.DescendantNodes().OfType<EnumDeclarationSyntax>()
                                    select field).ToList();
            var structDeclarations = (from field in compilationRoot.DescendantNodes().OfType<StructDeclarationSyntax>()
                                      select field).ToList();
            var fieldAndProperties = new List<FieldAndPropertyDetails>();
            var listMethodDetails = new List<MethodDetails>();
            foreach (var classDeclaration in classDeclarations)
            {
                var constructorList = (from field in classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>()
                                       select field).ToList();
                var methodList = (from field in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>()
                                  select field).ToList();
                var typeProperties = (from field in classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>()
                                      select field).ToList();
                var fields = (from field in classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
                              select field).ToList();

                foreach (var field in fields)
                {
                    var commentTrivia = (from t in field.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n').Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;

                    var property = new FieldAndPropertyDetails
                    {
                        Name = field.Declaration.Variables.First().Identifier.ToString(),
                        FieldOrProperty = FieldOrPropertyType.Field,
                        ReturnType = field.Declaration.Type.ToString(),
                        BaseCommandId = 19,
                        ClassOrInterfaceName = classDeclaration.Identifier.ValueText,
                        DocumentationComment = docComment
                    };
                    fieldAndProperties.Add(property);
                }
                foreach (var property in typeProperties)
                {
                    var commentTrivia = (from t in property.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n').Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;

                    var field = new FieldAndPropertyDetails
                    {
                        Name = property.Identifier.ToString(),
                        FieldOrProperty = FieldOrPropertyType.Property,
                        ReturnType = property.Type.ToString(),
                        BaseCommandId = 19,
                        ClassOrInterfaceName = classDeclaration.Identifier.ValueText,
                        DocumentationComment = docComment
                    };
                    fieldAndProperties.Add(field);
                }
                foreach (var constructor in constructorList)
                {
                    var commentTrivia = (from t in constructor.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n').Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;

                    var pList = constructor.ParameterList.Parameters.Select(p => new ParameterDetails
                    {
                        Name = p.Identifier.ValueText,
                        Type = p.Type.ToString(),
                        IsPredefined = p.Default != null
                    }).ToList();
                    listMethodDetails.Add(new MethodDetails
                    {
                        MethodName = constructor.Identifier.ToString(),
                        ClassName = classDeclaration.Identifier.ValueText,
                        ParameterDetails = pList,
                        ParameterList = constructor.ParameterList.ToString().TrimStart('(').TrimEnd(')').Trim(),
                        ReturnType = null,
                        ParameterCount = constructor.ParameterList.Parameters.Count,
                        DocumentationComment = docComment,
                        Modifiers = constructor.Modifiers.ToString()
                    });

                    Console.WriteLine($@"{constructor.Identifier} # {constructor.ParameterList} # { constructor.ParameterList.Parameters}");
                }

                foreach (var methodDeclaration in methodList)
                {
                    var commentTrivia = (from t in methodDeclaration.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n').Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;
                    var pList = methodDeclaration.ParameterList.Parameters.Select(p => new ParameterDetails
                    {
                        Name = p.Identifier.ValueText,
                        Type = p.Type.ToString(),
                        IsPredefined = p.Default != null
                    }).ToList();
                    listMethodDetails.Add(new MethodDetails
                    {
                        MethodName = methodDeclaration.Identifier.ToString(),
                        ClassName = classDeclaration.Identifier.ValueText,
                        ParameterDetails = pList,
                        ParameterList = methodDeclaration.ParameterList.ToString().TrimStart('(').TrimEnd(')').Trim(),
                        ReturnType = methodDeclaration.ReturnType.ToString(),
                        ParameterCount = methodDeclaration.ParameterList.Parameters.Count,
                        DocumentationComment = docComment,
                        Modifiers = methodDeclaration.Modifiers.ToString()
                    });
                    Console.WriteLine($@"MethodName: {methodDeclaration.Identifier} # ReturnType: {methodDeclaration.ReturnType} # ParameterList: {methodDeclaration.ParameterList} # ParameterCount: {methodDeclaration.ParameterList.Parameters.Count} # Parameters: {methodDeclaration.ParameterList.Parameters} # ParameterDetails: {JsonConvert.SerializeObject(pList)}");
                }
            }

            var interfaceTuple = ExtractFieldsAndMembers(interfaceDeclarations);
            fieldAndProperties.AddRange(interfaceTuple.Item1);

            var enumTuple = ExtractFieldsAndMembers(enumDeclarations);
            fieldAndProperties.AddRange(enumTuple.Item1);

            var structTuple = ExtractFieldsAndMembers(structDeclarations);
            fieldAndProperties.AddRange(structTuple.Item1);

            return Tuple.Create(listMethodDetails, fieldAndProperties);
        }
        private static Tuple<List<FieldAndPropertyDetails>> ExtractFieldsAndMembers(IEnumerable<InterfaceDeclarationSyntax> interfaceDeclarations)
        {
            var fieldAndProperties = new List<FieldAndPropertyDetails>();
            foreach (var interfaceDeclaration in interfaceDeclarations)
            {
                // string interfaceName = interfaceDeclaration.Identifier.ValueText;
                var typeProperties = (from field in interfaceDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>() select field).ToList();

                foreach (var property in typeProperties)
                {
                    var commentTrivia = (from t in property.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n')
                        .Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;

                    var fieldProperty = new FieldAndPropertyDetails
                    {
                        Name = property.Identifier.ToString(),
                        FieldOrProperty = FieldOrPropertyType.Property,
                        ReturnType = property.Type.ToString(),
                        BaseCommandId = 88,
                        ClassOrInterfaceName = interfaceDeclaration.Identifier.ValueText,
                        DocumentationComment = docComment
                    };
                    fieldAndProperties.Add(fieldProperty);
                }
            }
            return Tuple.Create(fieldAndProperties);
        }
        private static Tuple<List<FieldAndPropertyDetails>> ExtractFieldsAndMembers(IEnumerable<EnumDeclarationSyntax> enumDeclarations)
        {
            var fieldAndProperties = new List<FieldAndPropertyDetails>();
            foreach (var enumDeclaration in enumDeclarations)
            {
                // string interfaceName = interfaceDeclaration.Identifier.ValueText;
                var typeProperties = (from field in enumDeclaration.DescendantNodes()
                        .OfType<EnumMemberDeclarationSyntax>()
                                      select field).ToList();

                foreach (var property in typeProperties)
                {
                    var commentTrivia = (from t in property.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n')
                        .Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;

                    var memberProperty = new FieldAndPropertyDetails
                    {
                        Name = property.Identifier.ToString(),
                        FieldOrProperty = FieldOrPropertyType.Property,
                        ReturnType = property.Identifier.ValueText,
                        BaseCommandId = 78,
                        ClassOrInterfaceName = enumDeclaration.Identifier.ValueText,
                        DocumentationComment = docComment
                    };
                    fieldAndProperties.Add(memberProperty);
                }
            }
            return Tuple.Create(fieldAndProperties);
        }
        private static Tuple<List<FieldAndPropertyDetails>> ExtractFieldsAndMembers(IEnumerable<StructDeclarationSyntax> structDeclarations)
        {
            var fieldAndProperties = new List<FieldAndPropertyDetails>();
            foreach (var structDeclaration in structDeclarations)
            {
                // string interfaceName = interfaceDeclaration.Identifier.ValueText;
                var typeProperties = (from field in structDeclaration.DescendantNodes()
                        .OfType<PropertyDeclarationSyntax>()
                                      select field).ToList();

                var fields = (from field in structDeclaration.DescendantNodes()
                        .OfType<FieldDeclarationSyntax>()
                              select field).ToList();
                foreach (var property in fields)
                {
                    var commentTrivia = (from t in property.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n')
                        .Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;

                    var field = new FieldAndPropertyDetails
                    {
                        Name = property.Declaration.Variables.First()?.Identifier.ValueText,
                        FieldOrProperty = FieldOrPropertyType.Field,
                        ReturnType = property.Declaration.Type.ToString(),
                        BaseCommandId = 48,
                        ClassOrInterfaceName = structDeclaration.Identifier.ValueText,
                        DocumentationComment = docComment
                    };
                    fieldAndProperties.Add(field);
                }
                foreach (var property in typeProperties)
                {
                    var commentTrivia = (from t in property.DescendantTrivia()
                                         where t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                                         select t).ToList();
                    string docComment = commentTrivia.Any() ? string.Join("", commentTrivia.First().ToString().Split('\n')
                        .Select(d => d.Replace('\r', ' ').Replace("///", "").Trim()).Where(d => !string.IsNullOrEmpty(d))) : string.Empty;

                    var field = new FieldAndPropertyDetails
                    {
                        Name = property.Identifier.ToString(),
                        FieldOrProperty = FieldOrPropertyType.Property,
                        ReturnType = property.Type.ToString(),
                        BaseCommandId = 48,
                        ClassOrInterfaceName = structDeclaration.Identifier.ValueText,
                        DocumentationComment = docComment
                    };
                    fieldAndProperties.Add(field);
                }
            }
            return Tuple.Create(fieldAndProperties);
        }
    }
}
