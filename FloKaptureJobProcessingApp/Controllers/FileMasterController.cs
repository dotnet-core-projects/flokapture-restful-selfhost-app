using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
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
            var fileMaster = _floKaptureService.FileMasterRepository.Aggregate().Limit(10).ToList();
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
                // var extRef = generalService.Aggregate().ToList();
                // var extRef = generalService.ListAllDocuments();
                var extRef = generalService.AllDocuments().ToList(); // observer all of 2 above and this statement
                return Ok(extRef);
            }
        }
    }
}