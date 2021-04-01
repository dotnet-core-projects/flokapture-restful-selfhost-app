using BusinessLayer.DbEntities;
using BusinessLayer.ExtensionLibrary;
using BusinessLayer.Models;
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
using System.Threading;
using System.Threading.Tasks;

namespace FloKaptureJobProcessingApp.JobControllers
{
    public partial class CsharpProcessingController
    {
        [HttpGet]
        [Route("process-action-workflows")]
        public async Task<ActionResult> ProcessActionWorkflows(string projectId)
        {
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);
            if (projectMaster == null) return BadRequest();

            // var actions = await _floKaptureService.ActionWorkflowsRepository.Aggregate().ToListAsync().ConfigureAwait(false);
            var actionWorkflows = await _floKaptureService.ActionWorkflowsRepository.Collection.FindAsync(d => d.ProjectId == projectId).GetAwaiter().GetResult().ToListAsync().ConfigureAwait(false);

            foreach (var actionWorkflow in actionWorkflows)
            {
                await ProcessWorkflowWorkspace(actionWorkflow).ConfigureAwait(false);
            }
            return Ok(actionWorkflows);
        }

        private async Task<bool> ProcessWorkflowWorkspace(ActionWorkflows actionWorkflow)
        {
            var workflowTree = new List<TreeView>();
            var startStatements = _floKaptureService.StatementReferenceMasterRepository.GetAllListItems(d => d.BaseCommandId == 19 && d.FileId == actionWorkflow.FileId).ToList();
            var startTree = startStatements.ToTreeView();
            workflowTree.AddRange(startTree);
            startTree.ForEach(d => { Console.WriteLine(d.GraphName); });
            var methodBlock = await _floKaptureService.StatementReferenceMasterRepository.GetAnyGenericBlock(actionWorkflow.OriginStatementId, 8, 9).ConfigureAwait(false);
            var blockTree = methodBlock.ToTreeView();
            workflowTree.AddRange(blockTree);

            foreach (var blockMaster in methodBlock)
            {
                if(blockMaster.CallExternals == null) continue;
                if (blockMaster.CallExternals.Count <= 0) continue;
                foreach (var cw in blockMaster.CallExternals)
                {
                    Console.WriteLine(cw.MethodName);
                }
            }

            return true;
        }

        public KeyValueData PrepareInheritanceHeirarchy(List<Document> documents)
        {
            var interfacesLines = new KeyValueData { KeyValues = new List<KeyValue>() };
            var classLines = new KeyValueData { KeyValues = new List<KeyValue>() };
            var classAndInterfaceList = new KeyValueData { KeyValues = new List<KeyValue>() };
            foreach (var document in documents)
            {
                if (!System.IO.File.Exists(document.FilePath)) continue;
                if (Regex.IsMatch(document.FilePath, "reference.cs|Service References|AssemblyInfo.cs", RegexOptions.IgnoreCase)) continue;
                string programText = System.IO.File.ReadAllText(document.FilePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(programText);
                var compilationRoot = syntaxTree.GetCompilationUnitRoot();
                var interfaceDeclarations = (from d in compilationRoot.DescendantNodes().OfType<InterfaceDeclarationSyntax>() select d).ToList();
                foreach (var interfaceDeclaration in interfaceDeclarations)
                {
                    string interfaceName = interfaceDeclaration.Identifier.ValueText;
                    string modifiers = interfaceDeclaration.Modifiers.ToString();
                    if (interfaceDeclaration.BaseList?.Types == null)
                    {
                        interfacesLines.KeyValues.Add(new KeyValue
                        {
                            Name = $@"{modifiers} interface {interfaceName}",
                            ObjectName = interfaceName,
                            FileId = document.Id.Id.ToString(),
                            FilePath = document.FilePath,
                            Value = 88
                        });
                        continue;
                    }
                    var list = (interfaceDeclaration.BaseList?.Types).Cast<BaseTypeSyntax>().ToList();
                    var baseElements = string.Join(",", list);
                    interfacesLines.KeyValues.Add(new KeyValue
                    {
                        Name = $@"{modifiers} interface {interfaceName} : {baseElements}",
                        ObjectName = interfaceName,
                        FileId = document.Id.Id.ToString(),
                        FilePath = document.FilePath,
                        Value = 88
                    });
                }
                var classDeclarations = (from d in compilationRoot.DescendantNodes().OfType<ClassDeclarationSyntax>() select d).ToList();
                foreach (var classDeclaration in classDeclarations)
                {
                    string className = classDeclaration.Identifier.ValueText;
                    string modifiers = classDeclaration.Modifiers.ToString();
                    if (classDeclaration.BaseList?.Types == null)
                    {
                        classLines.KeyValues.Add(new KeyValue
                        {
                            Name = $@"{modifiers} class {className}",
                            ObjectName = className,
                            FileId = document.Id.Id.ToString(),
                            FilePath = document.FilePath,
                            Value = 19
                        });
                        continue;
                    }
                    var list = (classDeclaration.BaseList?.Types).Cast<BaseTypeSyntax>().ToList();
                    var baseElements = string.Join(",", list);
                    classLines.KeyValues.Add(new KeyValue
                    {
                        Name = $@"{modifiers} class {className} : {baseElements}",
                        ObjectName = className,
                        FileId = document.Id.Id.ToString(),
                        FilePath = document.FilePath,
                        Value = 19
                    });
                }
            }
            foreach (var keyValue in classLines.KeyValues)
            {
                keyValue.Name = Regex.Replace(keyValue.Name, @"<.*>", "");
                keyValue.ObjectName = Regex.Replace(keyValue.ObjectName, @"<.*>", "");
                classAndInterfaceList.KeyValues.Add(new KeyValue
                {
                    ObjectName = keyValue.ObjectName,
                    ProgramId = keyValue.ProgramId,
                    Value = 19
                });
            }
            foreach (var keyValue in interfacesLines.KeyValues)
            {
                keyValue.Name = Regex.Replace(keyValue.Name, @"<.*>", "");
                keyValue.ObjectName = Regex.Replace(keyValue.ObjectName, @"<.*>", "");
                classAndInterfaceList.KeyValues.Add(new KeyValue
                {
                    ObjectName = keyValue.ObjectName,
                    ProgramId = keyValue.ProgramId,
                    Value = 88
                });
            }
            foreach (var keyValue in classLines.KeyValues)
            {
                if (!keyValue.Name.Contains(": ")) continue;
                var allInherited = keyValue.Name.Split(':').Last().Replace(",", "").Trim(' ', ',').Split(' ');
                foreach (var inherited in allInherited)
                {
                    var isPresent = classLines.KeyValues.Any(d => d.ObjectName == inherited);
                    if (isPresent) continue;
                    isPresent = interfacesLines.KeyValues.Any(d => d.ObjectName == inherited);
                    if (isPresent) continue;

                    keyValue.Name = keyValue.Name.SafeReplace(inherited, true).Trim().TrimEnd(':').Trim().Replace(": ,", ":").Trim().TrimEnd(':');
                }
            }
            var onlyImplementationChains = classLines.KeyValues.Where(d => d.Name.Contains(":")).ToList();
            foreach (var interfacesLine in interfacesLines.KeyValues)
            {
                var implementedChains = onlyImplementationChains.Where(d => d.Name.ContainsWholeWord(interfacesLine.ObjectName, true)).ToList();
                foreach (var implementedChain in implementedChains)
                {
                    // find class who inherits this class
                    var implementations = onlyImplementationChains.Where(d => d.Name.ContainsWholeWord(implementedChain.ObjectName, true) && d.ObjectName != implementedChain.ObjectName).ToList();
                    // find right hand side chain after : of abstract class
                    var baseInheritance = new List<string> { implementedChain.ObjectName };
                    foreach (var nameValue in implementations)
                    {
                        var chains = implementedChain.Name.Split(':').Last().Split(',').Where(d => !string.IsNullOrEmpty(d.Trim()) && d.Trim() != interfacesLine.ObjectName).ToList();
                        baseInheritance.Add(nameValue.ObjectName);
                        baseInheritance.AddRange(chains);
                    }
                    interfacesLine.ObjectType = string.Join(",", baseInheritance.Distinct()).Trim();
                }
            }
            foreach (var nameValue in onlyImplementationChains)
            {
                // this class is not inherited by any other class, but if this class has inherited other class, then find the chain
                int loopCount = 0;
                Console.WriteLine($@"{JsonConvert.SerializeObject(nameValue)}");
                var rootChain = GetNestedInheritanceChain(nameValue, classLines.KeyValues, loopCount, new List<string> { nameValue.ObjectName });
                Console.WriteLine($@"{nameValue.ObjectName} # {nameValue.Name} # {string.Join(", ", rootChain)}");
                nameValue.ObjectType = string.Join(",", rootChain);
            }
            var combinedList = new KeyValueData { KeyValues = new List<KeyValue>() };
            combinedList.KeyValues.AddRange(onlyImplementationChains);
            combinedList.KeyValues.AddRange(interfacesLines.KeyValues);
            foreach (var nameValue in combinedList.KeyValues)
            {
                if (string.IsNullOrEmpty(nameValue.ObjectType)) continue;
                var includedFiles = nameValue.ObjectType.Split(',').Where(s => !string.IsNullOrEmpty(s)).Select(aa => aa.Trim()).ToList();
                var refFileIds = (from includedFile in includedFiles select classAndInterfaceList.KeyValues.Find(d => d.ObjectName == includedFile) into st where st != null select st.ProgramId).ToList();
                nameValue.ObjectIds = string.Join(",", refFileIds).TrimEnd(',', ' ').TrimStart(',', ' ').Trim();
            }
            var hierarchyList = combinedList.KeyValues.Where(d => !string.IsNullOrEmpty(d.ObjectIds)).ToList();
            var heirarchyDetails = new KeyValueData { KeyValues = new List<KeyValue>() };
            foreach (var keyValue in hierarchyList)
            {
                heirarchyDetails.KeyValues.Add(new KeyValue
                {
                    ObjectName = keyValue.ObjectName.TrimEnd(',', ' ').TrimStart(',', ' ').Trim(),
                    ObjectType = keyValue.ObjectType.TrimEnd(',', ' ').TrimStart(',', ' ').Trim(),
                    Name = keyValue.Name.TrimEnd(',', ' ').TrimStart(',', ' ').Trim(),
                    ObjectIds = keyValue.ObjectIds.TrimEnd(',', ' ').TrimStart(',', ' ').Trim(),
                    ProgramId = keyValue.ProgramId,
                    FileId = keyValue.FileId,
                    FilePath = keyValue.FilePath,
                    Value = keyValue.Value
                });
            }
            return heirarchyDetails;
        }

        private static List<string> GetNestedInheritanceChain(KeyValue baseChain, List<KeyValue> inheritanceChains, int loopCount, List<string> chain)
        {
            if (!baseChain.Name.Contains(':')) return chain;

            var checkAnyInherited = baseChain.Name.Split(':').Last().Split(',', ' ').Where(d => !string.IsNullOrEmpty(d)).Select(d => d.Trim()).ToList();
            var distinctList = checkAnyInherited.Where(d => !string.IsNullOrEmpty(d)).Where(d => d != baseChain.ObjectName).ToList();
            if (!distinctList.Any()) return chain;
            // since multiple inheritance is not allowed in C#, we will get exact one if there is any class inherited...
            Console.WriteLine(@"===================================");
            Console.WriteLine($@"{loopCount} # {baseChain.ObjectName}");
            var firstChain = inheritanceChains.Find(d => distinctList.Any(s => d.ObjectName == s));
            if (firstChain == null) return chain;
            chain.Add(firstChain.ObjectName);
            return GetNestedInheritanceChain(firstChain, inheritanceChains, ++loopCount, chain);
        }

        [Obsolete("This method is not in use now, please use ProcessMethodReferences instead.")]
        [HttpGet]
        [Route("process-method-references")]
        public async Task<ActionResult> ProcessReferences(string projectId)
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
                    string slnDirPath = projectMaster.PhysicalPath;
                    var allCsFiles = _floKaptureService.FileMasterRepository.GetAllListItems(d => d.ProjectId == projectMaster._id);
                    workspace.WorkspaceFailed += (o, we) => Console.WriteLine(we.Diagnostic.Message);
                    var solutionFiles = Directory.GetFiles(slnDirPath, "*.sln", SearchOption.TopDirectoryOnly).ToList();
                    var referenceList = new List<MethodReferenceMaster>();

                    foreach (var slnFile in solutionFiles)
                    {
                        var solutionPath = Path.Combine(slnDirPath, slnFile);
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
                                // var classDeclarationsOnly = (from d in syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>() select d).ToList();

                                var fileName = Path.GetFileNameWithoutExtension(document.FilePath);
                                var fileMaster = allCsFiles.Find(d => d.FilePath == document.FilePath && d.FileNameWithoutExt == fileName);
                                if (fileMaster == null) continue;
                                var methodReferences = new List<MethodReferenceMaster>();
                                // foreach (var classDeclaration in classDeclarationsOnly)
                                // {
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
                                                    int lineIndex = location.Location.GetLineSpan().StartLinePosition.Line;
                                                    var docText = await location.Document.GetTextAsync().ConfigureAwait(false);
                                                    string callingLine = docText.Lines[lineIndex].ToString();
                                                    Console.WriteLine(@"==========================================");
                                                    Console.WriteLine($@"Method Name: {method.Identifier.ValueText} ");
                                                    Console.WriteLine($@"Source File: {Path.GetFileName(document.FilePath)}");
                                                    Console.WriteLine($@"Reference: {Path.GetFileName(location.Location.SourceTree.FilePath)}");
                                                    Console.WriteLine($@"Actual Line: {callingLine}");
                                                    Console.WriteLine($@"Line Index: {lineIndex + 1}");
                                                    Console.WriteLine(@"===========================================");
                                                    var refFileMaster = allCsFiles.Find(d => d.FilePath == location.Location.SourceTree.FilePath && d.FileName == Path.GetFileName(location.Location.SourceTree.FilePath));
                                                    if (refFileMaster == null) continue;
                                                    methodReferences.Add(new MethodReferenceMaster
                                                    {
                                                        MethodName = method.Identifier.ValueText,
                                                        InvocationLocation = location.Location.GetLineSpan().StartLinePosition.Line + 1,
                                                        ReferencedFileId = refFileMaster._id
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
                                // }
                                if (!methodReferences.Any()) continue;
                                referenceList.Add(new MethodReferenceMaster
                                {
                                    SourceFileId = fileMaster._id,
                                    SourceFileName = fileMaster.FileName,
                                    // MethodReferences = methodReferences
                                });
                                await methodReferenceMaster.AddDocument(new MethodReferenceMaster
                                {
                                    SourceFileId = fileMaster._id,
                                    SourceFileName = fileMaster.FileName,
                                    // MethodReferences = methodReferences
                                }).ConfigureAwait(false);
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
    }

    public class KeyValueData
    {
        public List<KeyValue> KeyValues { get; set; }
    }

    public class KeyValue
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; }
        public string MethodName { get; set; }
        public string FileId { get; set; }
        public int ProgramId { get; set; }
        public string ObjectIds { get; set; }
        public string FilePath { get; set; }
        public override string ToString()
        {
            return $@"{Key} # {Name} # {ObjectName}";
        }
    }

}