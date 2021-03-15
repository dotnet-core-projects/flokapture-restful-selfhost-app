using System.Collections.Generic;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

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
    }
}