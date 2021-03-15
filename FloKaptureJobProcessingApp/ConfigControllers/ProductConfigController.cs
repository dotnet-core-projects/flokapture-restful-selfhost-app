using System.Collections.Generic;
using BusinessLayer.BaseRepositories;
using FloKaptureJobProcessingApp.FloKaptureServices;
using FloKaptureJobProcessingApp.InternalModels;
using Microsoft.AspNetCore.Mvc;

namespace FloKaptureJobProcessingApp.ConfigControllers
{
    [Route("config/product-config")]
    [ApiController]
    public class ProductConfigController : ControllerBase
    {
        internal BaseRepository<ProductConfig> ProductConfigRepository { get; set; } = new GeneralService().BaseRepository<ProductConfig>();

        [HttpPost]
        public ActionResult<ProductConfig> Post([FromBody] ProductConfig productConfig)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var addedConfig = ProductConfigRepository.AddDocument(productConfig).GetAwaiter().GetResult();
            return Ok(addedConfig);
        }

        [HttpGet]
        public ActionResult<List<ProductConfig>> Get()
        {
            var list = ProductConfigRepository.ListAllDocuments();
            return Ok(list);
        }
    }
}