using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FloKaptureJobProcessingApp.Controllers
{
    [Route("api/main/file-master")]
    [ApiController]
    public class FileMasterController : ControllerBase
    {
        private readonly IFloKaptureService _floKaptureService = new FloKaptureService();

        [Route("{id}")]
        [HttpGet]
        public ActionResult<List<FileMaster>> Get(string id)
        {
            var fileMaster = _floKaptureService.FileMasterRepository.FindDocument(d => d._id == id);
            return Ok(fileMaster);
        }

        [HttpPost]
        [Route("add-file-type-reference")]
        public async Task<ActionResult> AddFileTypeReference([FromBody] FileTypeReference extensionReference)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState.Values);

            using (var generalService = new GeneralService().BaseRepository<FileTypeReference>())
            {
                var extRef = await generalService.AddDocument(extensionReference).ConfigureAwait(false);
                return Ok(extRef);
            }
        }

        [HttpGet]
        [Route("get-file-type-references")]
        public ActionResult GetFileTypeReferences()
        {
            using (var generalService = new GeneralService().BaseRepository<FileTypeReference>())
            {
                var fileMaster = _floKaptureService.FileMasterRepository.Aggregate().Limit(10).ToList();
                Console.WriteLine(fileMaster.Count);
                // var extRef = generalService.Aggregate().ToList();
                // var extRef = generalService.ListAllDocuments();
                var extRef = generalService.GetAllItems().ToList(); // observer all of 2 above and this statement
                return Ok(extRef);
            }
        }

        [HttpGet]
        [Route("method-references")]
        public ActionResult MethodReferences(string projectId)
        {
            using (var generalService = new GeneralService().BaseRepository<MethodReferenceMaster>())
            {
                // var methodReference = generalService.Aggregate(PipelineDefinition<MethodReferenceMaster, MethodReferenceMaster>.Create(new BsonDocument("$match", new BsonDocument("ProjectId", projectId)), new BsonDocument("$limit", 5))).ToList();
                var methodReference = generalService.Aggregate().Limit(5).ToList();
                Console.WriteLine(methodReference.Count);
                // var extRef = generalService.Aggregate().ToList();
                // var extRef = generalService.ListAllDocuments();
                // var extRef = generalService.GetAllItems().ToList(); // observer all of 2 above and this statement
                return Ok(methodReference);
            }
        }
    }
}