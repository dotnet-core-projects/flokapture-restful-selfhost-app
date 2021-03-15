using BusinessLayer.CsharpHelpers;
using FloKaptureJobProcessingApp.InternalModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.MSBuild;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FloKaptureJobProcessingApp.JobControllers
{
    [Route("api/job/csharp")]
    [ApiController]
    public class CsharpProcessingController : ControllerBase
    {
        [HttpGet]
        [Route("cleanup-dir")]
        public ActionResult ProcessSolutionDir(string slnDirPath)
        {
            var solutionDirCleanUp = new SolutionDirCleanUp();
            var status = solutionDirCleanUp.CleanUpDir(slnDirPath);
            return Ok(new { Message = "Solution directory cleaned", Status = "OK", Data = status });
        }

        [HttpGet]
        [Route("process-references")]
        public async Task<ActionResult> ProcessMethodReferences(string slnDirPath)
        {
            MSBuildLocator.RegisterDefaults();
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, we) => Console.WriteLine(we.Diagnostic.Message);
                var solutionFiles = Directory.GetFiles(slnDirPath, "*.sln", SearchOption.TopDirectoryOnly).ToList();
                var referenceList = new List<MethodReferenceData>();
                foreach (var slnFile in solutionFiles)
                {
                    var solutionPath = Path.Combine(slnDirPath, slnFile); // @"E:\core-test-projects\LocationService\Pods.Integration.Services.Location.sln";
                    Console.WriteLine($@"Loading solution '{solutionPath}'");
                    var solution = await workspace.OpenSolutionAsync(solutionPath);
                    Console.WriteLine($@"Finished loading solution '{solutionPath}'");

                    foreach (var project in solution.Projects)
                    {
                        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
                        foreach (var document in project.Documents)
                        {
                            if (Regex.IsMatch(document.FilePath, "reference.cs|Service References|AssemblyInfo.cs", RegexOptions.IgnoreCase)) continue;
                            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);
                            var methodList = (from field in syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                                              select field).ToList();
                            foreach (var method in methodList)
                            {
                                var functionSymbol = compilation.GetSymbolsWithName(method.Identifier.ValueText).ToList();
                                foreach (var funDef in functionSymbol)
                                {
                                    if (funDef.Locations.Any(d => d.SourceTree.FilePath != document.FilePath)) continue;
                                    try
                                    {
                                        var references = await SymbolFinder.FindReferencesAsync(funDef, solution, CancellationToken.None).ConfigureAwait(false);
                                        foreach (var referenced in references)
                                        {
                                            foreach (var location in referenced.Locations)
                                            {
                                                Console.WriteLine(@"==========================================");
                                                Console.WriteLine($@"Method Name: {method.Identifier.ValueText} ");
                                                Console.WriteLine($@"Source File: {Path.GetFileName(document.FilePath)}");
                                                Console.WriteLine($@"Reference: {Path.GetFileName(location.Location.SourceTree.FilePath)}");
                                                Console.WriteLine($@"Line Index: {location.Location.GetLineSpan().StartLinePosition.Line + 1 }");
                                                Console.WriteLine(@"===========================================");
                                                referenceList.Add(new MethodReferenceData
                                                {
                                                    MethodName = method.Identifier.ValueText,
                                                    SourceFileName = Path.GetFileName(document.FilePath),
                                                    SourceFile = document.FilePath,
                                                    ReferencedFile = location.Location.SourceTree.FilePath,
                                                    ReferenceFileName = Path.GetFileName(location.Location.SourceTree.FilePath),
                                                    LineIndex = location.Location.GetLineSpan().StartLinePosition.Line + 1
                                                });
                                                Thread.Sleep(10);
                                            }
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        Console.WriteLine(exception.Message);
                                        Console.WriteLine($@"There is an issue in method definition: {method.Identifier.ValueText}. \nFile Location: {document.FilePath}");
                                    }
                                }
                            }
                        }
                    }
                }
                var referenceData = JsonConvert.SerializeObject(referenceList, Formatting.Indented);

                string jsonFile = Path.Combine(slnDirPath, "reference.json");
                System.IO.File.WriteAllText(jsonFile, referenceData);

                return Ok(referenceData);
            }
        }
    }
}
