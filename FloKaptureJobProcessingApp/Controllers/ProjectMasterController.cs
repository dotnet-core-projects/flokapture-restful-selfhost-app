using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using FloKaptureJobProcessingApp.InternalModels;
using FloKaptureJobProcessingApp.Utils;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace FloKaptureJobProcessingApp.Controllers
{
    [Route("api/main/project-master")]
    [ApiController]
    public class ProjectMasterController : ControllerBase
    {
        private readonly IFloKaptureService _floKaptureService = new FloKaptureService();

        [HttpGet]
        public ActionResult<List<ProjectMaster>> Get()
        {
            var projects = _floKaptureService.ProjectMasterRepository.Aggregate().ToList();
            return Ok(projects);
        }

        [HttpPost]
        public IActionResult Post([FromBody] ProjectMaster projectMaster)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var isExists = _floKaptureService.ProjectMasterRepository.FindDocument(d => d.ProjectName == projectMaster.ProjectName);
                if (isExists != null) return Conflict($"Project with name: {projectMaster.ProjectName} already exists");

                var extractPath = new GeneralService().BaseRepository<ProductConfig>().FindDocument(f => f.PropertyName == "Project Extract Path").Value;
                var isExistsOrCreated = ProductHelper.CheckAndCreateDir(extractPath);
                if (!isExistsOrCreated) return Unauthorized();

                var zipPath = new GeneralService().BaseRepository<ProductConfig>().FindDocument(f => f.PropertyName == "Project Upload Path").Value;
                string physicalZipPath = Path.Combine(zipPath, projectMaster.PhysicalPath);

                bool isFileReady = ProductHelper.IsFileReady(physicalZipPath);
                if (!isFileReady) return BadRequest("Zip File might be opened by other program and can not access it for process. Try again");
                string completeExtractPath = Path.Combine(extractPath, Path.GetFileNameWithoutExtension(physicalZipPath));
                using (var zipFileToOpen = new FileStream(physicalZipPath, FileMode.Open))
                {
                    using (var archive = new ZipArchive(zipFileToOpen, ZipArchiveMode.Update))
                    {
                        try
                        {
                            archive.ExtractToDirectory(completeExtractPath);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception.Message);
                            Console.WriteLine("Directory already exist, overriding all files...");
                            foreach (var entry in archive.Entries)
                            {
                                if (string.IsNullOrEmpty(entry.Name)) continue;
                                entry.ExtractToFile($"{extractPath}\\{entry.FullName}", true);
                            }
                            Console.WriteLine("==============================================================");
                            Console.WriteLine("Overriding all files at destination directory completed...");
                        }
                    }
                }

                // var fileName = Path.GetFileNameWithoutExtension(physicalZipPath);
                projectMaster.PhysicalPath = completeExtractPath;

                var isValidated = _floKaptureService.UniVerseBasicUtils.UniVerseUtils.ValidateDirStructure(projectMaster);
                if (!isValidated) return BadRequest(projectMaster);

                var allFiles = Directory.GetFiles(projectMaster.PhysicalPath, "*.*", SearchOption.AllDirectories);
                long fileSizeInByte = allFiles.Select(file => new FileInfo(file)).Select(fileInfo => fileInfo.Length).Sum();
                double directorySizeInMb = (fileSizeInByte / 1024f) / 1024f;
                int totalFiles = allFiles.Length;
                projectMaster.TotalFiles = totalFiles;
                projectMaster.UploadedTime = DateTime.Now.ToString("hh:mm:ss tt");
                projectMaster.Size = directorySizeInMb;
                projectMaster.Active = true;

                var addedProject = _floKaptureService.ProjectMasterRepository.AddDocument(projectMaster).GetAwaiter().GetResult();

                return Ok(addedProject);
            }
            catch (Exception exception)
            {
                return BadRequest(exception);
            }
        }
    }
}