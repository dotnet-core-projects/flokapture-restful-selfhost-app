using BusinessLayer.CsharpHelpers;
using BusinessLayer.DbEntities;
using BusinessLayer.ExtensionLibrary;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
using System.Threading.Tasks;

namespace FloKaptureJobProcessingApp.JobControllers
{
    [Route("api/job/csharp")]
    [ApiController]
    public partial class CsharpProcessingController : ControllerBase
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
        public async Task<ActionResult> ProcessMethodReferences(string projectId)
        {
            MSBuildLocator.RegisterDefaults();
            using (var workspace = MSBuildWorkspace.Create())
            {
                try
                {
                    var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
                    if (projectMaster == null) return BadRequest($@"Project with id {projectId} not found!");
                    var methodReferenceMaster = new GeneralService().BaseRepository<MethodReferenceMaster>();
                    /*
                    var previousFile = Path.Combine(projectMaster.PhysicalPath, "reference.json");
                    var rawJson = System.IO.File.ReadAllText(previousFile);
                    var jsonData = JsonConvert.DeserializeObject<Dictionary<string, List<MethodReferenceData>>>(rawJson);
                    */
                    string slnDirPath = projectMaster.PhysicalPath; // @"D:\FloKapture-DotNet-Projects\flokapture-dotnet-job-processing-api"; 
                    var allCsFiles = _floKaptureService.FileMasterRepository.GetAllListItems(d => d.ProjectId == projectMaster._id);
                    workspace.WorkspaceFailed += (o, we) => Console.WriteLine(we.Diagnostic.Message);
                    var solutionFiles = Directory.GetFiles(slnDirPath, "*.sln", SearchOption.TopDirectoryOnly).ToList();
                    workspace.LoadMetadataForReferencedProjects = true;

                    foreach (var slnFile in solutionFiles)
                    {
                        var solutionPath = Path.Combine(slnDirPath, slnFile);
                        Console.WriteLine($@"Loading solution '{solutionPath}'");
                        var solution = await workspace.OpenSolutionAsync(solutionPath);
                        Console.WriteLine($@"Finished loading solution '{solutionPath}'");
                        var projectDocuments = new List<Document>();
                        var syntaxTrees = new List<SyntaxTree>();
                        var diagnostics = workspace.Diagnostics;
                        foreach (var diagnostic in diagnostics)
                        {
                            Console.WriteLine(diagnostic.Message);
                        }

                        foreach (var project in solution.Projects)
                        {
                            // var currentProject = await workspace.OpenProjectAsync(project.FilePath).ConfigureAwait(false); // .Result;
                            foreach (var doc in project.Documents)
                            {
                                var syntaxTree = await doc.GetSyntaxTreeAsync().ConfigureAwait(false);
                                syntaxTrees.Add(syntaxTree);
                                projectDocuments.Add(doc);
                            }
                        }

                        var msCorLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
                        var compilation = CSharpCompilation.Create("Compilation", syntaxTrees: syntaxTrees, references: new[] { msCorLib });
                        var methodReferences = new List<MethodReferenceMaster>();

                        var heirarchyDetails = PrepareInheritanceHeirarchy(documents: projectDocuments);
                        Console.WriteLine(heirarchyDetails.KeyValues.Count);
                        /*
                        var model = compilation.GetSemanticModel(tree);
                        var forStatement = tree.GetRoot().DescendantNodes().OfType<ForStatementSyntax>().Single();
                        DataFlowAnalysis dataFlowAnalysis = model.AnalyzeDataFlow(forStatement);
                        */
                        foreach (var document in projectDocuments)
                        {
                            // if (document.Name != "CustomerCommunicationRepository.cs" && document.Name != "CustomerCommunicationRepositoryBase.cs" && document.Name != "CommunicationDataManager.cs") continue;
                            if (Regex.IsMatch(document.FilePath, "reference.cs|Service References|AssemblyInfo.cs", RegexOptions.IgnoreCase)) continue;
                            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);
                            var hasDiagnostics = syntaxTree.GetDiagnostics();
                            if (hasDiagnostics.Any()) continue; // do some work here to set flags
                            // this is referenced document in which other/same/from-chain documents method is called.
                            var refFileMaster = allCsFiles.Find(d => d.FilePath == document.FilePath && d.FileName == Path.GetFileName(document.FilePath));
                            if (refFileMaster == null) continue;

                            var sm = await document.GetSyntaxTreeAsync().ConfigureAwait(false);
                            var semantic = compilation.GetSemanticModel(sm);
                            var invocations = (from d in syntaxTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>() select d).ToList();
                            Console.WriteLine($@"=================== {document.FilePath} ===============================");
                            foreach (var invocation in invocations)
                            {
                                var methodSymbol = semantic.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                                if (methodSymbol == null) continue;
                                var foundMethodDefs = (from ds in methodSymbol.DeclaringSyntaxReferences let methodDecl = ds.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>() let methods = methodDecl.Where(md => invocation.Expression.ToString().Split('.').Last() == md.Identifier.ValueText) select methods).ToList();
                                var foundInFirst = false;
                                foreach (var foundMethod in foundMethodDefs)
                                {
                                    foreach (var fm in foundMethod)
                                    {
                                        if (fm.ParameterList.Parameters.Count != invocation.ArgumentList.Arguments.Count) continue;
                                        // int lineIndex = fm.GetLocation().GetLineSpan().StartLinePosition.Line;
                                        // var docText = await fm.GetLocation().SourceTree.GetTextAsync().ConfigureAwait(false);
                                        // string definitionLine = docText.Lines[lineIndex].ToString();
                                        var doc = projectDocuments.Find(d => d.FilePath == fm.GetLocation().SourceTree.FilePath);
                                        var heirarchyDoc = heirarchyDetails.KeyValues.Find(d => d.FileId == doc.Id.Id.ToString());
                                        if (heirarchyDoc != null && heirarchyDoc.Value.Equals(88)) continue;
                                        var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
                                        var fileMaster = allCsFiles.Find(d => d.FilePath == document.FilePath && d.FileNameWithoutExt == fileName);
                                        if (fileMaster == null) continue;
                                        var mrm = new MethodReferenceMaster
                                        {
                                            ProjectId = projectMaster._id,
                                            MethodName = fm.Identifier.ValueText,
                                            MethodLocation = fm.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                            InvocationLocation = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                            InvocationLine = invocation.ToString().Trim().Trim('\r', '\n').Trim(),
                                            SourceFileName = Path.GetFileName(doc.FilePath),
                                            ReferencedFileName = Path.GetFileName(document.FilePath),
                                            SourceFileId = fileMaster._id,
                                            ReferencedFileId = refFileMaster._id,
                                            SourceFilePath = doc.FilePath,
                                            ReferencedFilePath = document.FilePath
                                        };
                                        methodReferences.Add(mrm);
                                        await methodReferenceMaster.AddDocument(mrm).ConfigureAwait(false);

                                        Console.WriteLine($@"{invocation.Expression}");
                                        foundInFirst = true;
                                        break;
                                    }
                                    if (foundInFirst) break;
                                }
                                if (foundInFirst) continue;
                                var referencesTo = await SymbolFinder.FindReferencesAsync(methodSymbol, document.Project.Solution).ConfigureAwait(false);
                                var de = SymbolFinder.FindDeclarationsAsync(document.Project, referencesTo.First().Definition.Name, false, SymbolFilter.Member).ConfigureAwait(false).GetAwaiter().GetResult().ToList();
                                if (!de.Any()) continue;
                                // means there might be interface method which is defined in implemented class and inherited chain as well.
                                var foundRef = false;
                                de.Reverse(); // TODO: this is not good idea here, but it's OK for now...
                                foreach (var dSymbol in de)
                                {
                                    var objectSymbol = (from ds in dSymbol.DeclaringSyntaxReferences let interfc = ds.SyntaxTree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>() where interfc.ToList().Count > 0 select interfc.First()).ToList();
                                    if (objectSymbol.Any()) continue;
                                    var foundMethods = (from ds in dSymbol.DeclaringSyntaxReferences let methodDecl = ds.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>() let methods = methodDecl.Where(md => invocation.Expression.ToString().Split('.').Last() == md.Identifier.ValueText) select methods).ToList();
                                    foreach (var foundMethod in foundMethods)
                                    {
                                        foreach (var fm in foundMethod)
                                        {
                                            if (fm.ParameterList.Parameters.Count != invocation.ArgumentList.Arguments.Count) continue;
                                            // int lineIndex = fm.GetLocation().GetLineSpan().StartLinePosition.Line;
                                            // var docText = await fm.GetLocation().SourceTree.GetTextAsync().ConfigureAwait(false);
                                            // string definitionLine = docText.Lines[lineIndex].ToString();
                                            var doc = projectDocuments.Find(d => d.FilePath == fm.GetLocation().SourceTree.FilePath);
                                            var heirarchyDoc = heirarchyDetails.KeyValues.Find(d => d.FileId == doc.Id.Id.ToString());
                                            if (heirarchyDoc != null && heirarchyDoc.Value.Equals(88)) continue;

                                            var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
                                            var fileMaster = allCsFiles.Find(d => d.FilePath == document.FilePath && d.FileNameWithoutExt == fileName);
                                            if (fileMaster == null) continue;

                                            var mrm = new MethodReferenceMaster
                                            {
                                                ProjectId = projectMaster._id,
                                                MethodName = fm.Identifier.ValueText,
                                                MethodLocation = fm.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                                InvocationLocation = invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                                                InvocationLine = invocation.ToString().Trim().Trim('\r', '\n').Trim(),
                                                SourceFileName = Path.GetFileName(doc.FilePath),
                                                ReferencedFileName = Path.GetFileName(document.FilePath),
                                                SourceFileId = fileMaster._id,
                                                ReferencedFileId = refFileMaster._id,
                                                SourceFilePath = doc.FilePath,
                                                ReferencedFilePath = document.FilePath
                                            };
                                            methodReferences.Add(mrm);
                                            await methodReferenceMaster.AddDocument(mrm).ConfigureAwait(false);

                                            Console.WriteLine($@"{invocation.Expression}");
                                            foundRef = true;
                                            break;
                                        }
                                        if (foundRef) break; // actually not needed, but added for now
                                    }
                                    if (foundRef) break;
                                }

                                Console.WriteLine(@"------------------------------------------------------------------------------");
                            }
                        }
                        Console.WriteLine($@"Total References inserted: {methodReferences.Count}");
                        var referenceData = JsonConvert.SerializeObject(methodReferences, Formatting.Indented);
                        var slnFileName = Path.GetFileNameWithoutExtension(solutionFiles.First());
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

                string lineComment = string.Empty;
                foreach (var csLineDetail in assignedBaseCommands)
                {
                    if (string.IsNullOrEmpty(csLineDetail.ResolvedStatement)) continue;
                    if (Regex.IsMatch(csLineDetail.ResolvedStatement, "^using\\s")) continue;
                    if (RegexCollections.LineComment.IsMatch(csLineDetail.ResolvedStatement))
                    {
                        lineComment += csLineDetail.ResolvedStatement;
                        continue;
                    }
                    string ceiName = RegexCollections.TypeNameRegex.Match(csLineDetail.ResolvedStatement).Groups["TypeName"].Value.Trim();
                    string variableName = RegexCollections.VariableDeclaration.Match(csLineDetail.ResolvedStatement).Groups["VariableName"].Value;
                    int baseCommandId = Regex.IsMatch(csLineDetail.ResolvedStatement.Trim(), @"^{$") ? 41 : csLineDetail.BaseCommandId;
                    var statementReference = new StatementReferenceMaster
                    {
                        BaseCommandId = baseCommandId,
                        LineIndex = csLineDetail.LineIndex,
                        FileId = fileMaster._id,
                        OriginalStatement = csLineDetail.OriginalStatement,
                        ResolvedStatement = csLineDetail.ResolvedStatement,
                        ProjectId = fileMaster.ProjectId
                    };
                    if (!string.IsNullOrEmpty(variableName))
                        statementReference.VariableNameDeclared = variableName;
                    if (!string.IsNullOrEmpty(ceiName))
                        statementReference.ClassNameDeclared = ceiName;
                    if (!string.IsNullOrEmpty(lineComment))
                        statementReference.StatementComment = lineComment;
                    if (!string.IsNullOrEmpty(csLineDetail.MethodName))
                        statementReference.MethodName = csLineDetail.MethodName;

                    await _floKaptureService.StatementReferenceMasterRepository.AddDocument(statementReference).ConfigureAwait(false);
                    lineComment = string.Empty;
                }
            }
            return Ok();
        }

        [HttpGet]
        [Route("update-call-externals")]
        public async Task<ActionResult> UpdateCallExternals(string projectId)
        {
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
            if (projectMaster == null) return BadRequest();

            var methodReferenceMaster = new GeneralService().BaseRepository<MethodReferenceMaster>().Aggregate().ToListAsync().GetAwaiter().GetResult(); //.ConfigureAwait(false); // JsonConvert.DeserializeObject<List<MethodReferenceMaster>>(jsonText);
            try
            {
                foreach (var methodReference in methodReferenceMaster)
                {
                    var srm = _floKaptureService.StatementReferenceMasterRepository.GetDocument(d => d.FileId == methodReference.ReferencedFileId && d.LineIndex == methodReference.InvocationLocation - 1);
                    if (srm == null) continue;
                    if (srm.CallExternals != null) continue;
                    srm.CallExternals = srm.CallExternals ?? new List<CallExternals>();
                    srm.CallExternals.Add(new CallExternals
                    {
                        MethodName = methodReference.MethodName,
                        ReferencedFileId = methodReference.ReferencedFileId,
                        MethodLocation = methodReference.MethodLocation
                    });
                    srm.BaseCommandId = methodReference.ReferencedFileId == srm.FileId ? 5 : 6;
                    await _floKaptureService.StatementReferenceMasterRepository.UpdateDocument(srm).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return StatusCode(500, e);
            }
            var allCsFiles = _floKaptureService.FileMasterRepository.GetAllListItems(d => d.ProjectId == projectMaster._id).ToList();
            foreach (var fileMaster in allCsFiles)
            {
                var fileReferenceMaster = methodReferenceMaster.Where(d => d.ReferencedFileId == fileMaster._id).ToList();
                var statementReferenceData = _floKaptureService.StatementReferenceMasterRepository.GetAllListItems(d => d.ProjectId == projectMaster._id && d.FileId == fileMaster._id).ToList();
                foreach (var statementReference in statementReferenceData)
                {
                    int lineIndex = statementReference.LineIndex + 1;
                    if (fileReferenceMaster.All(d => d.InvocationLocation != lineIndex)) continue;
                    var references = fileReferenceMaster.Where(d => d.InvocationLocation == lineIndex).ToList();
                    statementReference.CallExternals = statementReference.CallExternals ?? new List<CallExternals>();
                    foreach (var reference in references)
                    {
                        statementReference.CallExternals.Add(new CallExternals
                        {
                            MethodName = reference.MethodName,
                            ReferencedFileId = reference.ReferencedFileId,
                            MethodLocation = reference.MethodLocation
                        });
                        Console.WriteLine(@"==================================================================");
                        Console.WriteLine(statementReference.OriginalStatement);
                        Console.WriteLine(reference.InvocationLine);
                        Console.WriteLine(@"==================================================================");
                    }
                    statementReference.BaseCommandId = references.All(d => d.ReferencedFileId == statementReference.FileId) ? 5 : 6;
                    await _floKaptureService.StatementReferenceMasterRepository.UpdateDocument(statementReference).ConfigureAwait(false);
                }
            }

            return Ok("Call externals updated successfully.");
        }

        [HttpGet]
        [Route("collect-action-workflows")]
        public ActionResult CollectActionWorkflows(string projectId)
        {
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
            if (projectMaster == null) return BadRequest();

            var awRepository = new GeneralService().BaseRepository<ActionWorkflows>();
            var allCsFiles = _floKaptureService.FileMasterRepository.GetAllListItems(d => d.FileTypeReferenceId == "60507f66591cfa72c53a859e" && d.ProjectId == projectMaster._id).ToList();
            foreach (var fileMaster in allCsFiles)
            {
                if (!fileMaster.FileName.EndsWith("svc.cs")) continue;
                var allMethods = _floKaptureService.StatementReferenceMasterRepository.GetAllListItems(d => d.BaseCommandId == 8 && d.FileId == fileMaster._id).ToList();
                foreach (var method in allMethods)
                {
                    if (!Regex.IsMatch(method.ResolvedStatement, @"^public\s*", RegexOptions.IgnoreCase)) continue;
                    var aw = new ActionWorkflows
                    {
                        WorkflowName = method.MethodName,
                        FileId = fileMaster._id,
                        OriginEventMethod = method.OriginalStatement,
                        ProjectId = fileMaster.ProjectId,
                        OriginStatementId = method._id
                    };

                    var actionWorkflow = awRepository.AddDocument(aw).GetAwaiter().GetResult();
                    Console.WriteLine($"Added action workflow: {actionWorkflow.WorkflowName}");
                }
            }

            return Ok($"Collected all action workflows for project: {projectMaster.ProjectName}");
        }
    }
}