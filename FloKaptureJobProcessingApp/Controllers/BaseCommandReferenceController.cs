using System.Collections.Generic;
using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;

namespace FloKaptureJobProcessingApp.Controllers
{
    [Route("api/main/base-command-reference")]
    [ApiController]
    public class BaseCommandReferenceController : ControllerBase
    {
        public BaseRepository<BaseCommandReference> baseCommandReference = new GeneralService().BaseRepository<BaseCommandReference>();

        [HttpGet]
        public ActionResult<List<BaseCommandReference>> Get()
        {
            var list = baseCommandReference.ListAllDocuments();
            return Ok(list);
        }

        [Route("{id}")]
        [HttpGet]
        public ActionResult<BaseCommandReference> Get(string id)
        {
            var baseCommand = baseCommandReference.FindDocument(d => d._id == id);
            return Ok(baseCommand);
        }

        [HttpPost]
        public ActionResult<BaseCommandReference> Post([FromBody] BaseCommandReference baseCommand)
        {
            if (!ModelState.IsValid) return BadRequest(baseCommand);

            var addedReference = baseCommandReference.AddDocument(baseCommand).GetAwaiter().GetResult();
            return Ok(addedReference);
        }
    }
}