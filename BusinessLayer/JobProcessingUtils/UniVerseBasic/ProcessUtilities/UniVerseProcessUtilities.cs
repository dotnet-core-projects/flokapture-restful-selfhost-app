using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using BusinessLayer.FloKaptureExtensions;
using BusinessLayer.JobProcessingUtils.UniVerseBasic.Helpers;
using CsvHelper.Configuration;
using LumenWorks.Framework.IO.Csv;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLayer.JobProcessingUtils.UniVerseBasic.ProcessUtilities
{
    public class UniVerseProcessUtilities : StatementMasterHelper
    {
        public readonly UniVerseBasicUtils UniVerseBasicUtils = new UniVerseBasicUtils();
        public bool ChangeExtensions(string rootPath, string extension, string dToSkip, params string[] directoryName)
        {
            dToSkip = dToSkip ?? "";
            var directoriesToSkip = dToSkip.Split(',').ToList();
            var directories = Directory.GetDirectories(rootPath, "*.*", SearchOption.AllDirectories).ToList();
            directoriesToSkip.RemoveAll(string.IsNullOrEmpty);

            if (directoriesToSkip.Any())
                directories.RemoveAll(d => directoriesToSkip.Any(n => Regex.IsMatch(d, n, RegexOptions.IgnoreCase)));

            var dirToProcess = (from d in directories
                                let dir = new DirectoryInfo(d)
                                where null != dir && directoryName.Any(n => Regex.IsMatch(n, dir.Name, RegexOptions.IgnoreCase))
                                select d).ToList();

            foreach (var directory in dirToProcess)
            {
                var currentDirectories = Directory.GetDirectories(directory, "*.*", SearchOption.AllDirectories).ToList();
                if (directoriesToSkip.Any())
                    currentDirectories.RemoveAll(
                        d => directoriesToSkip.Any(n => Regex.IsMatch(d, n, RegexOptions.IgnoreCase)));
                foreach (var cDir in currentDirectories)
                {
                    var allFiles = Directory.GetFiles(cDir, "*.*", SearchOption.AllDirectories).ToList();
                    foreach (var file in allFiles)
                    {
                        var fileExtension = Path.GetExtension(file);
                        if (fileExtension == extension) continue;

                        var newFile = File.ReadAllLines(file);
                        var newFileWithExtension = string.Concat(file, extension);
                        if (File.Exists(newFileWithExtension)) File.Delete(newFileWithExtension);
                        File.WriteAllLines(newFileWithExtension, newFile);
                        File.Delete(file);
                    }
                }
                var rootFiles = Directory.GetFiles(directory, "*.*").ToList();
                foreach (var file in rootFiles)
                {
                    var fileExtension = Path.GetExtension(file);
                    if (fileExtension == extension) continue;

                    var newFile = File.ReadAllLines(file);
                    var newFileWithExtension = string.Concat(file, extension);
                    if (File.Exists(newFileWithExtension)) File.Delete(newFileWithExtension);
                    File.WriteAllLines(newFileWithExtension, newFile);
                    File.Delete(file);
                }
            }
            var allRootFiles = Directory.GetFiles(rootPath, "*.*").ToList();
            foreach (var file in allRootFiles)
            {
                var newFile = File.ReadAllLines(file);
                var newFileWithExtension = string.Concat(file, extension);
                if (File.Exists(newFileWithExtension)) File.Delete(newFileWithExtension);
                File.WriteAllLines(newFileWithExtension, newFile);
                File.Delete(file);
            }

            return true;
        }
        public bool ProcessMenuFile(ProjectMaster projectMaster)
        {
            try
            {
                var universeFileMenuContext = new BaseRepository<UniverseFileMenu>();
                var directoryPath = Path.Combine(projectMaster.PhysicalPath, "Menu");
                var allFiles = Directory.GetFiles(directoryPath, "*.csv", SearchOption.TopDirectoryOnly);
                var listItems = new List<UniverseFileMenu>();
                foreach (var file in allFiles)
                {
                    var methodBlockList = File.ReadAllLines(file, Encoding.UTF7).Skip(1).ToList();
                    methodBlockList.RemoveAll(l => l.Length <= 0);
                    var stream = new StreamReader(file);
                    var csvReader = new CsvReader(stream, true);
                    while (csvReader.ReadNextRecord())
                    {
                        listItems.Add(new UniverseFileMenu
                        {
                            ProjectId = projectMaster._id,
                            ActionExecuted = csvReader[3],
                            MenuId = csvReader[0],
                            MenuDescription = csvReader[2] ?? "",
                            MenuTitle = csvReader[1]
                        });
                    }
                    universeFileMenuContext.InsertMany(listItems);
                }
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                return false;
            }
        }
        public void ProcessUniVerseDataDictionary(ProjectMaster projectMaster)
        {
            var fileMasterRepository = new BaseRepository<FileMaster>();
            var filter = fileMasterRepository.Filter.Eq(f => f.FileTypeReference.FileTypeName, "Entity") &
                         fileMasterRepository.Filter.Eq(f => f.ProjectId, projectMaster._id);
            var entityFiles = fileMasterRepository.FindWithLookup(filter).ToList();
            foreach (var fileMaster in entityFiles)
            {
                var modifiedPath = ReplaceContentsForEscapeChar(fileMaster.FilePath,
                    fileMaster.FileTypeReference.Delimiter ?? ',');
                Console.WriteLine(modifiedPath);
                PrepareDataDictionary(fileMaster, modifiedPath);
                File.Delete(modifiedPath);
            }
        }

        public void ProcessUniVerseJclFiles(ProjectMaster projectMaster)
        {
            var fileMasterRepository = new BaseRepository<FileMaster>();
            var filter = fileMasterRepository.Filter.Eq(f => f.FileTypeReference.FileTypeName, "Jcl") &
                         fileMasterRepository.Filter.Eq(f => f.ProjectId, projectMaster._id);
            var jclFiles = fileMasterRepository.FindWithLookup(filter).ToList();
            foreach (var fileMaster in jclFiles)
            {
                var fileLines = File.ReadAllLines(fileMaster.FilePath);
                var lineDetails = UniVerseBasicUtils.UniVerseUtils.RemoveCommentedAndBlankLines(fileLines);

                lineDetails.PrintToConsole();
            }
        }

        private void PrepareDataDictionary(FileMaster fileMaster, string filePath)
        {
            var configuration = new CsvConfiguration(new CultureInfo("en-us"))
            {
                IgnoreQuotes = true,
                Delimiter = ",",
                HasHeaderRecord = true,
                IncludePrivateMembers = true,
                CultureInfo = new CultureInfo("en-us"),
                HeaderValidated = (a, b, c, r) => { },
                MissingFieldFound = null
            };

            var fileLinesList = File.ReadAllText(filePath);
            byte[] byteArray = Encoding.UTF8.GetBytes(fileLinesList);
            var memoryStream = new MemoryStream(byteArray);
            var streamReader = new StreamReader(memoryStream);
            var csvReader = new CsvHelper.CsvReader(streamReader, configuration, true);
            csvReader.Configuration.PrepareHeaderForMatch = (s, i) => s.ToUpper();
            csvReader.Configuration.MemberTypes = MemberTypes.Fields | MemberTypes.Properties;
            csvReader.Configuration.RegisterClassMap<DataDictMap>();

            var indexPosition = -1;
            while (csvReader.Read())
            {
                var readHeader = csvReader.ReadHeader();
                Console.WriteLine(readHeader);
                var rawRecords = csvReader.GetRecords<UniVerseDataDictionary>().ToList();
                foreach (var dataDict in rawRecords)
                {
                    indexPosition++;
                    var dataDictionary = new UniVerseDataDictionary
                    {
                        FileName = string.IsNullOrEmpty(dataDict.FileName)
                            ? "" : dataDict.FileName.Replace("\"", ""),
                        FieldNo = string.IsNullOrEmpty(dataDict.FieldNo)
                            ? "" : dataDict.FieldNo.Replace("\"", ""),
                        Description = string.IsNullOrEmpty(dataDict.Description)
                            ? "" : dataDict.Description.Replace("\"", ""),
                        FieldLabel = string.IsNullOrEmpty(dataDict.FieldLabel)
                            ? "" : dataDict.FieldLabel.Replace("\"", ""),
                        RptFieldLength = string.IsNullOrEmpty(dataDict.RptFieldLength)
                            ? "" : dataDict.RptFieldLength.Replace("\"", ""),
                        TypeOfData = string.IsNullOrEmpty(dataDict.TypeOfData)
                            ? "" : dataDict.TypeOfData.Replace("\"", ""),
                        SingleArray = string.IsNullOrEmpty(dataDict.SingleArray)
                            ? "" : dataDict.SingleArray.Replace("\"", ""),
                        DateOfCapture = string.IsNullOrEmpty(dataDict.DateOfCapture)
                            ? "" : dataDict.DateOfCapture.Replace("\"", ""),
                        ReplacementName = "",
                        // ProjectId = fileMaster.ProjectId,
                        FileId = fileMaster._id
                    };
                    var isValidNumber = Regex.IsMatch(dataDictionary.FieldNo, @"^[0-9]+(\.[0-9]+)?$");
                    var replacementName = isValidNumber
                        ? "R." + dataDictionary.FileName + "(" + dataDictionary.FieldNo + ")"
                        : "K." + dataDictionary.FileName;
                    if (indexPosition == 0) replacementName = dataDictionary.FileName;
                    dataDictionary.ReplacementName = replacementName;
                    dataDictionary.PrintToConsole();
                    UniVerseBasicUtils.RepositoryOf<UniVerseDataDictionary>().AddDocument(dataDictionary)
                        .GetAwaiter().GetResult();
                }
            }
            memoryStream.Flush();
            memoryStream.Dispose();
            streamReader.Close();
        }

        private static string ReplaceContentsForEscapeChar(string filePath, char separator)
        {
            var fileLines = File.ReadAllLines(filePath);

            /*
            var modifiedLines = (from line in fileLines
                where !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line)
                select Regex
                    .Split(line,
                        $"{separator}(?!(?<=(?:^|,)\\s*\"(?:[^ \"]|\"\"|\")*,)(?:[^ \"]|\"\"|\")* \"\\s*(?:,|$))")
                into splittedLine
                select string.Join(',', splittedLine)).ToList();
            */

            var modifiedLines = new List<string>();
            foreach (var line in fileLines)
            {
                if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line)) continue;
                var splittedLine = Regex
                    .Split(line, $"{separator}(?!(?<=(?:^|,)\\s*\"(?:[^ \"]|\"\"|\")*,)(?:[^ \"]|\"\"|\")* \"\\s*(?:,|$))");
                modifiedLines.Add(string.Join(',', splittedLine));
            }

            var fileName = Path.GetFileName(filePath);
            var modifiedFile = $"modified-{fileName}";
            var dirName = Path.GetDirectoryName(filePath);
            var newFilePath = Path.Combine(dirName, modifiedFile);
            File.WriteAllLines(newFilePath, modifiedLines);
            return newFilePath;
        }
    }
}
