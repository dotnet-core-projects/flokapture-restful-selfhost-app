using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace FloKaptureJobProcessingApp.Controllers
{
    [Route("api/main/organization-master")]
    [ApiController]
    public class OrganizationMasterController : ControllerBase
    {
        private readonly BaseRepository<OrganizationMaster> _organizationRepository = new GeneralService().BaseRepository<OrganizationMaster>();

        [Route("add-organization")]
        [HttpPost]
        public ActionResult<OrganizationMaster> AddOrganization([FromBody] OrganizationMaster organizationMaster)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var addedOrganization = _organizationRepository.AddDocument(organizationMaster).GetAwaiter().GetResult();
            return Ok(addedOrganization);
        }

        [HttpGet]
        public ActionResult<List<OrganizationMaster>> Get()
        {
            var list = _organizationRepository.AllDocuments().ToList();
            return Ok(list);
        }

        [HttpGet]
        [Route("{id}")]
        public ActionResult<OrganizationMaster> Get(string id)
        {
            var organization = _organizationRepository.FindDocument(d => d._id == id);
            return Ok(organization);
        }
    }
}