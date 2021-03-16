using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;

namespace FloKaptureJobProcessingApp.JobControllers
{
    public partial class UniVerseBasicController
    {
        private bool ChangeFileExtensions(ProjectMaster projectMaster)
        {
            try
            {
                var response = _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers
                    .ChangeExtensions(projectMaster.PhysicalPath, ".jcl", "", "jcl");
                Console.WriteLine(response);

                var icdResponse = _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers
                    .ChangeExtensions(projectMaster.PhysicalPath, ".icd", "", "include,includes");
                Console.WriteLine("Change Extensions Includes: " + icdResponse);

                var pgmResponse = _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers
                    .ChangeExtensions(projectMaster.PhysicalPath, ".pgm",
                        "sbr,sbr.bp,subroutine,subroutines", "program,programs");
                Console.WriteLine("Change Extensions Programs: " + pgmResponse);

                var sbrResponse = _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers
                    .ChangeExtensions(projectMaster.PhysicalPath, ".sbr", "", "sbr,sbr.bp,subroutine,subroutines");
                Console.WriteLine("Change Extensions SubRoutines: " + sbrResponse);

                var menuResponse = _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers
                    .ChangeExtensions(projectMaster.PhysicalPath, ".csv", "", "menu,menus");
                Console.WriteLine("Change Extensions Menu File: " + menuResponse);

                Console.WriteLine("=========================================");
                Console.WriteLine("Extensions changed for all related files.");
                Console.WriteLine("=========================================");

                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return false;
            }
        }

        private async Task<bool> ProcessFileMasterDetails(ProjectMaster projectMaster)
        {
            var entitiesToExcludeService = new GeneralService().BaseRepository<EntitiesToExclude>();
            var fileTypeReferences = _floKaptureService.FileTypeReferenceRepository
                .ListAllDocuments(p => p.LanguageId == projectMaster.LanguageId).ToList();
            var directoryList = new List<string> { projectMaster.PhysicalPath };
            var regExCommented = new Regex(@"^\/\/\*|^\/\*|^\*|^\'", RegexOptions.CultureInvariant);
            foreach (var directory in directoryList)
            {
                try
                {
                    var fileExtensions = fileTypeReferences.Select(extension => extension.FileExtension);
                    var allFiles = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                        .Where(s => fileExtensions.Any(e => s.EndsWith(e) || s.ToUpper().EndsWith(e.ToUpper()))).ToList();
                    foreach (var currentFile in allFiles)
                    {
                        var fileLines = System.IO.File.ReadAllLines(currentFile).ToList();
                        int lineCount = fileLines.Count(line =>
                            !regExCommented.IsMatch(line) || !string.IsNullOrWhiteSpace(line));
                        // TODO: Look for this in old code
                        // if (ignoredFile.Any(f => f.FileName == Path.GetFileName(currentFile))) continue;
                        var fileName = Path.GetFileName(currentFile);
                        if (string.IsNullOrEmpty(fileName)) continue;
                        if (fileName.Contains(".dll.config")) continue;
                        var extension = Path.GetExtension(currentFile);
                        var extensionId = fileTypeReferences.First(e => e.FileExtension == extension || string.Equals(e.FileExtension, extension, StringComparison.CurrentCultureIgnoreCase))._id;
                        var fileMaster = new FileMaster
                        {
                            FileName = fileName,
                            FilePath = currentFile,
                            FileTypeReferenceId = extensionId,
                            ProjectId = projectMaster._id,
                            DoneParsing = false,
                            LinesCount = lineCount,
                            Processed = 0
                        };
                        var addedDocument = await _floKaptureService.FileMasterRepository.AddDocument(fileMaster)
                            .ConfigureAwait(false);

                        if (projectMaster.IsCtCode && extension == ".icd" &&
                            fileMaster.FileName.StartsWith("I_", StringComparison.CurrentCultureIgnoreCase))
                        {
                            await entitiesToExcludeService.AddDocument(new EntitiesToExclude
                            {
                                FileId = addedDocument._id,
                                FileName = Path.GetFileNameWithoutExtension(currentFile),
                                ProjectId = addedDocument.ProjectId
                            }).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
            return true;
        }
    }
}