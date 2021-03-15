using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;

namespace FloKaptureJobProcessingApp.Controllers
{
    [Route("api/main/language-master")]
    [ApiController]
    public class LanguageMasterController : ControllerBase
    {
        public BaseRepository<LanguageMaster> languageRepository = new GeneralService().BaseRepository<LanguageMaster>();

        [Route("add-language")]
        [HttpPost]
        public ActionResult<LanguageMaster> AddLanguage([FromBody] LanguageMaster languageMaster)
        {
            var addedLanguage = languageRepository.AddDocument(languageMaster).GetAwaiter().GetResult();
            return Ok(addedLanguage);
        }
    }
}