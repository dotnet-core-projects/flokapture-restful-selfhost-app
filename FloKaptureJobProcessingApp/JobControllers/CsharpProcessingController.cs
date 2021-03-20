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
using MongoDB.Driver;
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
            var extensionReferences = generalService.GetAllListItems(d => d.LanguageId == projectMaster.LanguageId);
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
        public async Task<ActionResult> ProcessMethodReferences(string projectId /* This is comment */)
        {
            MSBuildLocator.RegisterDefaults();
            using (var workspace = MSBuildWorkspace.Create())
            {
                try
                {
                    var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
                    if (projectMaster == null) return BadRequest($@"Project with id {projectId} not found!");
                    /*
                    var previousFile = Path.Combine(projectMaster.PhysicalPath, "reference.json");
                    var rawJson = System.IO.File.ReadAllText(previousFile);
                    var jsonData = JsonConvert.DeserializeObject<Dictionary<string, List<MethodReferenceData>>>(rawJson);
                    */
                    string slnDirPath = projectMaster.PhysicalPath;
                    var allCsFiles = _floKaptureService.FileMasterRepository.GetAllListItems(d => d.ProjectId == projectMaster._id);
                    workspace.WorkspaceFailed += (o, we) => Console.WriteLine(we.Diagnostic.Message);
                    var solutionFiles = Directory.GetFiles(slnDirPath, "*.sln", SearchOption.TopDirectoryOnly).ToList();

                    foreach (var slnFile in solutionFiles)
                    {
                        var referenceList = new Dictionary<string, List<MethodReferenceData>>();
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
                                var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
                                var fileMaster = allCsFiles.Find(d => d.FilePath == document.FilePath && d.FileNameWithoutExt == fileName);
                                if (fileMaster == null) continue;
                                var methodList = (from field in syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>() select field).ToList();
                                var methodReferences = new List<MethodReferenceData>();
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
                                                    int lineIndex = location.Location.GetLineSpan().StartLinePosition.Line;
                                                    var docText = await location.Document.GetTextAsync().ConfigureAwait(false);
                                                    string callingLine = docText.Lines[lineIndex].ToString();
                                                    Console.WriteLine(@"==========================================");
                                                    Console.WriteLine($@"Method Name: {method.Identifier.ValueText} ");
                                                    Console.WriteLine($@"Source File: {Path.GetFileName(document.FilePath)}");
                                                    Console.WriteLine($@"Reference: {Path.GetFileName(location.Location.SourceTree.FilePath)}");
                                                    Console.WriteLine($@"Actual Line: {callingLine}");
                                                    Console.WriteLine($@"Line Index: {lineIndex + 1 }");
                                                    Console.WriteLine(@"===========================================");
                                                    methodReferences.Add(new MethodReferenceData
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
                                if (!methodReferences.Any()) continue;
                                referenceList.Add(fileMaster._id, methodReferences);
                            }
                        }
                        var referenceData = JsonConvert.SerializeObject(referenceList, Formatting.Indented);
                        var slnFileName = Path.GetFileNameWithoutExtension(slnFile);
                        string jsonFile = Path.Combine(slnDirPath, $"{slnFileName}.json");
                        if (System.IO.File.Exists(jsonFile)) System.IO.File.Delete(jsonFile);

                        System.IO.File.WriteAllText(jsonFile, referenceData);
                    }

                    return Ok($"Reference data has been processed successfully for project: {projectMaster.ProjectName}");
                }
                catch (Exception exception)
                { return StatusCode(500, exception); }
            }
        }

        [HttpGet]
        [Route("process-method-details")]
        public async Task<ActionResult> ProcessMethodDetails(string projectId)
        {
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
            if (projectMaster == null) return BadRequest($@"Project with id {projectId} not found!");

            var methodDetailsService = new GeneralService().BaseRepository<MethodDetails>();
            var fieldOrPropertyService = new GeneralService().BaseRepository<FieldAndPropertyDetails>();
            var allCsFiles = _floKaptureService.FileMasterRepository.GetAllListItems(f => f.ProjectId == projectId && f.FileTypeReferenceId == "60507f66591cfa72c53a859e");
            foreach (var fileMaster in allCsFiles)
            {
                if (!System.IO.File.Exists(fileMaster.FilePath)) continue;
                if (Regex.IsMatch(fileMaster.FilePath, "reference.cs|Service References|AssemblyInfo.cs", RegexOptions.IgnoreCase)) continue;
                var allLines = System.IO.File.ReadAllText(fileMaster.FilePath);
                var returnedTuple = CsharpHelper.ExtractMemberDetails(allLines);
                var methodDetails = returnedTuple.Item1;
                var patternList = (from mr in methodDetails where mr.ClassName != mr.MethodName select $@"\b{mr.ClassName}.{mr.MethodName}\b").ToList();
                if (methodDetails.Any() && patternList.Any())
                {
                    string pattern = string.Concat("(?<FoundMatch>", string.Join("|", patternList), ")");
                    methodDetails.Last().MethodMatchRegex = pattern;
                }
                foreach (var methodDetail in methodDetails)
                {
                    methodDetail.ProjectId = fileMaster.ProjectId;
                    methodDetail.FileId = fileMaster._id;
                    await methodDetailsService.AddDocument(methodDetail).ConfigureAwait(false);
                }
                var fieldOrPropertyDetails = returnedTuple.Item2;
                foreach (var fieldOrProperty in fieldOrPropertyDetails)
                {
                    fieldOrProperty.ProjectId = fileMaster.ProjectId;
                    fieldOrProperty.FileId = fileMaster._id;
                    await fieldOrPropertyService.AddDocument(fieldOrProperty).ConfigureAwait(false);
                }
            }
            return Ok(projectMaster);
        }

        [HttpGet]
        [Route("start-main-parsing")]
        public async Task<ActionResult> StartParsingCsFiles(string projectId)
        {
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
            if (projectMaster == null) return BadRequest($@"Project with id {projectId} not found!");
            // var filterDefinition = _floKaptureService.FileMasterRepository.Filter.Or(_floKaptureService.FileMasterRepository.Filter.Eq(d => d.FileTypeReferenceId, "60507f342a60e0e6cbefd106"), _floKaptureService.FileMasterRepository.Filter.Eq(d => d.FileTypeReferenceId, "60507f66591cfa72c53a859e"));
            // var fileCursor = await _floKaptureService.FileMasterRepository.Collection.FindAsync<FileMaster>(filter: filterDefinition).ConfigureAwait(false);
            var allCsFiles = await _floKaptureService.FileMasterRepository.Aggregate()/*.Limit(200)*/.ToListAsync().ConfigureAwait(false);
            foreach (var fileMaster in allCsFiles)
            {
                var programLines = System.IO.File.ReadAllLines(fileMaster.FilePath).ToList();
                var csLineDetails = CsharpHelper.PrepareCsLineDetails(programLines); 
                var assignedTryCatchCommands = BaseCommandExtractor.AssignBaseCommandToTryCatch(csLineDetails);
                var assignedBaseCommands = BaseCommandExtractor.AssignBaseCommandId(assignedTryCatchCommands);
                int methods = assignedBaseCommands.Count(d => d.BaseCommandId == 8);
                int endMethods = assignedBaseCommands.Count(d => d.BaseCommandId == 9);
                int ifs = assignedBaseCommands.Count(d => d.BaseCommandId == 1);
                int elses = assignedBaseCommands.Count(d => d.BaseCommandId == 10);
                int endIfs = assignedBaseCommands.Count(d => d.BaseCommandId == 2);
                int loops = assignedBaseCommands.Count(d => d.BaseCommandId == 3);
                int endLoops = assignedBaseCommands.Count(d => d.BaseCommandId == 4);
                int tries = assignedBaseCommands.Count(d => d.BaseCommandId == 101);
                int endTries = assignedBaseCommands.Count(d => d.BaseCommandId == 102);
                int catches = assignedBaseCommands.Count(d => d.BaseCommandId == 201);
                int endCatches = assignedBaseCommands.Count(d => d.BaseCommandId == 202);
                int finalies = assignedBaseCommands.Count(d => d.BaseCommandId == 301);
                int endFinalies = assignedBaseCommands.Count(d => d.BaseCommandId == 302);
                int switches = assignedBaseCommands.Count(d => d.BaseCommandId == 58);
                int endSwitches = assignedBaseCommands.Count(d => d.BaseCommandId == 59);
                int classes = assignedBaseCommands.Count(d => d.BaseCommandId == 19);
                int endClasses = assignedBaseCommands.Count(d => d.BaseCommandId == 20);

                if (!classes.Equals(endClasses) || !methods.Equals(endMethods) || !ifs.Equals(endIfs) || !loops.Equals(endLoops))
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{fileMaster.FileName} # {fileMaster.FilePath}");
                    Console.BackgroundColor = ConsoleColor.Black;
                }

                Console.WriteLine($"==========================File Statistics=========================");
                Console.WriteLine($"=========================={fileMaster.FileName}===================");
                Console.WriteLine($"\nMethods: {methods}\nEndMethods: {endMethods}\nIfs: {ifs}" +
                                  $"\nElses: {elses}\nEnd Ifs: {endIfs}\nLoops: {loops}\nEnd Loops: {endLoops}" +
                                  $"\nTries: {tries}\nEnd-Tries: {endTries}\nCatches: {catches}" +
                                  $"\nEnd-Catches: {endCatches}\nFinally: {finalies}\nEnd-Finally: {endFinalies}" +
                                  $"\nSwitch: {switches}\nEnd-Switches: {endSwitches}" +
                                  $"\nClass(es): {classes}\nEnd-Clssses: {endClasses}");
                Console.WriteLine("==========================File Statistics=========================");
                Console.WriteLine("\n=======================================================\n");
            }

            return Ok();
        }
    }
}
