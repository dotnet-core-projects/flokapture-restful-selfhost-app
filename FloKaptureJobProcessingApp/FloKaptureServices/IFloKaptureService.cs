using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using BusinessLayer.JobProcessingUtils.UniVerseBasic;
using System;
using BusinessLayer.EntityRepositories;

namespace FloKaptureJobProcessingApp.FloKaptureServices
{
    public interface IFloKaptureService : IDisposable
    {
        IUniVerseBasicUtils UniVerseBasicUtils { get; }
        BaseRepository<UserMaster> UserMasterRepository { get; }
        BaseRepository<UserDetails> UserDetailsRepository { get; }
        BaseRepository<LanguageMaster> LanguageMasterRepository { get; }
        BaseRepository<ProjectMaster> ProjectMasterRepository { get; }
        BaseRepository<FileMaster> FileMasterRepository { get; }
        BaseRepository<ActionWorkflows> ActionWorkflowsRepository { get; }
        StatementReferenceMasterRepository StatementReferenceMasterRepository { get; }
        BaseRepository<FileTypeReference> FileTypeReferenceRepository { get; }
        BaseRepository<UniVerseDataDictionary> UniVerseDataDictionaryRepository { get; }
    }
}
