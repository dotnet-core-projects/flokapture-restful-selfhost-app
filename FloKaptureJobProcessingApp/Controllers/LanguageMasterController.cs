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
        public BaseRepository<LanguageMaster> LanguageRepository = new GeneralService().BaseRepository<LanguageMaster>();

        [HttpGet]
        public ActionResult Get()
        {
            var languages = LanguageRepository.GetAllItems();
            return Ok(languages);
        }

        [Route("add-language")]
        [HttpPost]
        public ActionResult<LanguageMaster> AddLanguage([FromBody] LanguageMaster languageMaster)
        {
            var addedLanguage = LanguageRepository.AddDocument(languageMaster).GetAwaiter().GetResult();
            return Ok(addedLanguage);
        }
    }
}