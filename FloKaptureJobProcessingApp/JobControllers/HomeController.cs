﻿using Microsoft.AspNetCore.Mvc;

namespace FloKaptureJobProcessingApp.JobControllers
{
    [Route("api/job/home")]
    public class HomeController : ControllerBase
    {
        [Route("get-status")]
        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok("Job API is up and running!");
        }
    }
}