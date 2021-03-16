using BusinessLayer.CsharpHelpers;
using BusinessLayer.DbEntities;
using BusinessLayer.ExtensionLibrary;
using BusinessLayer.Models;
using FloKaptureJobProcessingApp.FloKaptureServices;
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
        private readonly IFloKaptureService _floKaptureService = new FloKaptureService();
        [HttpGet]
        [Route("cleanup-dir")]
        public ActionResult ProcessSolutionDir(string projectId)
        {
            var solutionDirCleanUp = new SolutionDirCleanUp();
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
            if (projectMaster == null) return BadRequest($@"Project with id {projectId} not found!");
            string slnDirPath = projectMaster.PhysicalPath;
            var status = solutionDirCleanUp.CleanUpDir(slnDirPath);
            return Ok(new { Message = "Solution directory cleaned", Status = "OK", Data = status });
        }

        [HttpGet]
        [Route("process-file-master-data")]
        public async Task<ActionResult> ProcessForFileMasterData(string projectId)
        {
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
            if (projectMaster == null) return BadRequest($@"Project with id {projectId} not found!");
            var generalService = new GeneralService().BaseRepository<FileTypeReference>();
            var extensionReferences = generalService.ListAllDocuments(d => d.LanguageId == projectMaster.LanguageId);
            var csFiles = Directory.GetFiles(projectMaster.PhysicalPath, "*.cs", SearchOption.AllDirectories).ToList();
            foreach (var csFile in csFiles)
            {
                if (Regex.IsMatch(csFile, @"\b\\bin\\\b|\b\\obj\\\b|\b\.Test\b", RegexOptions.IgnoreCase)) continue;
                string fileName = Path.GetFileName(csFile);
                if (string.IsNullOrEmpty(fileName)) continue;
                if (fileName.Contains(".dll.config")) continue;
                if (new[] { "Reference", "AssemblyInfo" }.Any(fileName.StartsWith)) continue;
                var extension = csFile.GetFileNameAndExtension(); // .UpTo(".");
                var extRef = extensionReferences.Find(e => Regex.IsMatch(extension.Value, e.FileExtension, RegexOptions.IgnoreCase));
                if (extRef == null) break;
                var fileMaster = new FileMaster
                {
                    FileName = Path.GetFileName(csFile),
                    DoneParsing = false,
                    FilePath = csFile,
                    Processed = 0,
                    ProjectId = projectMaster._id,
                    FileTypeReferenceId = extRef._id,
                    LinesCount = 0,
                    WorkflowStatus = string.Empty
                };
                await _floKaptureService.FileMasterRepository.AddDocument(fileMaster).ConfigureAwait(false);
            }
            return Ok(new { Message = "Project processed successfully", Status = "OK", ProjectId = projectId });
        }

        [HttpGet]
        [Route("process-references")]
        public async Task<ActionResult> ProcessMethodReferences(string projectId)
        {
            MSBuildLocator.RegisterDefaults();
            using (var workspace = MSBuildWorkspace.Create())
            {
                var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
                if (projectMaster == null) return BadRequest($@"Project with id {projectId} not found!");
                string slnDirPath = projectMaster.PhysicalPath;

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
