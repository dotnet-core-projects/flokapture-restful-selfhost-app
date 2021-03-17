using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using BusinessLayer.Models;
using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FloKaptureJobProcessingApp.Controllers
{
    [Route("api/main/values")]
    public class ValuesController : ControllerBase
    {
        public IFloKaptureService FloKaptureService = new FloKaptureService();
        public BaseRepository<FileMaster> GeneralService = new GeneralService().BaseRepository<FileMaster>();
        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var fileMaster = FloKaptureService.FileMasterRepository.FindDocument(d => d.FileName.Contains("BP"));
                Console.WriteLine(fileMaster.FilePath);

                var baseCmd = FloKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers
                    .ExtractBaseCommandId(new LineDetails());
                Console.WriteLine(baseCmd);

                var languages = FloKaptureService.LanguageMasterRepository.GetAllItems().ToList();
                Console.WriteLine(languages);
             
                var allFiles = FloKaptureService.FileMasterRepository.Aggregate().Limit(10).ToList();

                return Ok(new { fileMaster, baseCmd, languages, allFiles });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return StatusCode(500);
            }
        }

        [HttpGet("{id}")]
        public ActionResult<UserMaster> Get(string id)
        {
            FloKaptureService.UserDetailsRepository.Add(new UserDetails
            {
                // Address = new Address { City = "Pune", State = "Maharashtra", Pin = 411046, Street = "Katraj" },
                Email = "sonawaneyogesh",
                FirstName = "Yogesh",
                LastName = "Sonawane",
                UserId = "5d03803a951eed37e0f10cf3"
            });
            var userMaster = FloKaptureService.UserMasterRepository.GetById(id);
            return userMaster;
        }

        [HttpPost]
        public async Task<UserDetails> Post([FromBody] UserDetails userDetails)
        {
            if (!ModelState.IsValid)
            {
                return null;
            }
            var ud = await FloKaptureService.UserDetailsRepository.AddDocument(userDetails).ConfigureAwait(false);
            return ud;
        }

        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        [Route("get-union")]
        [HttpGet]
        public IActionResult GetUnion()
        {
            var fileMaster = FloKaptureService.FileMasterRepository.Aggregate()
                .Match(
                    FloKaptureService.FileMasterRepository.Filter.Regex(d => d.FileName,
                        new BsonRegularExpression("BP", "ig")) &
                    GeneralService.Filter.In(d => d.ProjectId, new List<string> {"1", "2"}) &
                    GeneralService.Filter.In(f => f.FileTypeReferenceId, new List<string> {"9", "10", "11"}))
                .Lookup("FileTypeExtensionReference", "FileTypeExtensionId", "FileTypeExtensionId",
                    "FileTypeExtensionReference").As<FileMaster>().Unwind(d => d.FileTypeReference,
                    new AggregateUnwindOptions<FileMaster> {PreserveNullAndEmptyArrays = true})
                .Lookup("ActionWorkflows", "FileId", "FileId", "ActionWorkflows")
                .Unwind("ActionWorkflows").As<FileMaster>()
                .Project(new BsonDocumentProjectionDefinition<FileMaster, BsonDocument>(new BsonDocument
                {
                    {"FileId", 1},
                    {"ProjectId", 1},
                    {"FileName", 1},
                    {"FileTypeExtensionReference", new BsonDocument {{"FileTypeName", 1}, {"FileTypeExtensionId", 1}}},
                    {"ActionWorkflows", new BsonDocument {{"WorkflowBusinessName", 1}}}
                }))
                .As<FileMaster>()
                .ReplaceRoot(new BsonValueAggregateExpressionDefinition<FileMaster, BsonDocument>(new BsonDocument
                    {
                        {
                            "$mergeObjects", new BsonArray
                            {
                                new BsonDocument
                                {
                                    {"FileId", "$FileId"},
                                    {"ProjectId", "$ProjectId"},
                                    {"FileName", "$FileName"},
                                    {"FileTypeName", 0},
                                    {"FileTypeExtensionId", 0},
                                    {"WorkflowBusinessName", 0}
                                },
                                "$FileTypeExtensionReference",
                                "$ActionWorkflows"
                            }
                        }
                    }
                ))/*.As<FileMaster>()*/
                .Limit(20);

            var aggFluent = fileMaster.ToList();
            foreach (var bson in aggFluent)
            {
                Console.WriteLine(bson);
            }
            var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(aggFluent.ToJson());
            return Ok(data);
        }
    }
}
