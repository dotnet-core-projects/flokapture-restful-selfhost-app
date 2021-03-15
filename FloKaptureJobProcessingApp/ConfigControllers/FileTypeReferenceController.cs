using System.Collections.Generic;
using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;

namespace FloKaptureJobProcessingApp.ConfigControllers
{
    [Route("config/file-type-reference")]
    [ApiController]
    public class FileTypeReferenceController : ControllerBase
    {
        internal BaseRepository<FileTypeReference> FileTypeReferenceRepository { get; set; } = new GeneralService().BaseRepository<FileTypeReference>();

        [HttpPost]
        public ActionResult<FileTypeReference> Post([FromBody] FileTypeReference fileTypeReference)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var addedConfig = FileTypeReferenceRepository.AddDocument(fileTypeReference).GetAwaiter().GetResult();
            return Ok(addedConfig);
        }

        [HttpGet]
        public ActionResult<List<FileTypeReference>> Get()
        {
            var list = FileTypeReferenceRepository.ListAllDocuments();
            return Ok(list);
        }
    }
}