using System;
using System.Threading.Tasks;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using FloKaptureJobProcessingApp.Utils;
using Microsoft.AspNetCore.Mvc;

namespace FloKaptureJobProcessingApp.Controllers
{
    [Route("api/main/home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        [Route("get-status")]
        [HttpGet]
        public IActionResult GetStatus()
        {
            Console.WriteLine(Request);
            string serverPath = ProductHelper.NormalizePath(Request);
            return Ok("API is up and running! " + serverPath);
        }

        [Route("add-doc")]
        [HttpGet]
        public async Task<IActionResult> AddDoc()
        {
            var floKaptureService = new FloKaptureService();
            var fileMaster = await floKaptureService.FileMasterRepository.AddDocument(new FileMaster
            {
                FileName = "AD.FLE.txt",
                FilePath = @"E:\Auctor\CCSES-20180829\DataDictionary\AD.FLE.txt"
            }).ConfigureAwait(false);
            return Ok(fileMaster);
        }
    }
}