using FloKaptureJobProcessingApp.FloKaptureServices;
using Microsoft.AspNetCore.Mvc;

namespace FloKaptureJobProcessingApp.JobControllers
{
    [Route("api/job/universe-basic")]
    [ApiController]
    public partial class UniVerseBasicController : ControllerBase
    {
        private readonly IFloKaptureService _floKaptureService = new FloKaptureService();

        [Route("start-project-processing")]
        [HttpGet]
        // public async Task<IActionResult> ExecuteProcessActionsOneByOne(string projectId)
        public IActionResult ExecuteProcessActionsOneByOne(string projectId)
        {
            var projectMaster = _floKaptureService.ProjectMasterRepository.GetById(projectId);

            // Working step...
            // Step 1: Validate directory structure
            // var isValidated = _floKaptureService.UniVerseBasicUtils.UniVerseUtils.ValidateDirStructure(projectMaster);
            // if (!isValidated) return BadRequest(projectMaster);

            // Working step...
            // Step 2: Update project status
            // Update project status to Processing
            // projectMaster.Processed = ProcessStatus.Processing;
            // await _floKaptureService.ProjectMasterRepository.UpdateDocument(projectMaster).ConfigureAwait(false);

            // Working step...
            // Step 3: Change file extensions w.r.t directories and their types
            // bool changedExtensions = ChangeFileExtensions(projectMaster);
            // Console.WriteLine(changedExtensions);

            // Working step...
            // Step 4: Dump file details to file master table
            // bool isDone = await ProcessFileMasterDetails(projectMaster);
            // Console.WriteLine(isDone);

            // Working step...
            // Step 4: Process UniVerse File Menu CSV file
            // bool isProcessed = _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers.ProcessMenuFile(projectMaster);
            // Console.WriteLine(isProcessed);

            // Working step...
            // Step 5: Process UniVerse Data Dictionary files
            // _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers.ProcessUniVerseDataDictionary(projectMaster);

            // Important... do not delete or uncomment
            /*
            var filter = _floKaptureService.UniVerseDataDictionaryRepository
                .Filter.Eq(f => f.FileMaster.ProjectId, projectMaster._id);
            var uniDict = _floKaptureService.UniVerseDataDictionaryRepository.FindWithLookup(filter).ToList();
            return Ok(uniDict);
            */
            _floKaptureService.UniVerseBasicUtils.UniVerseProcessHelpers.ProcessUniVerseJclFiles(projectMaster);

            return Ok();
        }
    }
}