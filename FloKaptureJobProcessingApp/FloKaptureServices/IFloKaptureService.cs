﻿using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using BusinessLayer.JobProcessingUtils.UniVerseBasic;
using System;

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
        BaseRepository<StatementReferenceMaster> StatementReferenceMasterRepository { get; }
        BaseRepository<FileTypeReference> FileTypeReferenceRepository { get; }
        BaseRepository<UniVerseDataDictionary> UniVerseDataDictionaryRepository { get; }
    }
}
