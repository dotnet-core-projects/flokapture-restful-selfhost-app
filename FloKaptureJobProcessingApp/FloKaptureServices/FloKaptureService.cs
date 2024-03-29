﻿using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;
using BusinessLayer.EntityRepositories;
using BusinessLayer.JobProcessingUtils.UniVerseBasic;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FloKaptureJobProcessingApp.FloKaptureServices
{
    [DebuggerStepThrough]
    public class FloKaptureService : IFloKaptureService
    {
        private IntPtr _nativeResource = Marshal.AllocHGlobal(100);
        private BaseRepository<UserMaster> _userMasterRepository;
        private BaseRepository<UserDetails> _userDetailsRepository;
        private BaseRepository<LanguageMaster> _languageMasterRepository;
        private BaseRepository<ProjectMaster> _projectMasterRepository;
        private BaseRepository<FileMaster> _fileMasterRepository;
        private BaseRepository<ActionWorkflows> _actionWorkflowsRepository;
        private StatementReferenceMasterRepository _statementReferenceMasterRepository;
        private BaseRepository<FileTypeReference> _fileTypeReferenceRepository;
        private BaseRepository<UniVerseDataDictionary> _uniVerseDataDictionaryRepository;
        public IUniVerseBasicUtils UniVerseBasicUtils => new UniVerseBasicUtils();
        public BaseRepository<UserMaster> UserMasterRepository => _userMasterRepository ??= new UserMasterRepository();
        public BaseRepository<UserDetails> UserDetailsRepository => _userDetailsRepository ??= new UserDetailsRepository();
        public BaseRepository<LanguageMaster> LanguageMasterRepository => _languageMasterRepository ??= new LanguageMasterRepository();
        public BaseRepository<ProjectMaster> ProjectMasterRepository => _projectMasterRepository ??= new BaseRepository<ProjectMaster>();
        public BaseRepository<FileMaster> FileMasterRepository => _fileMasterRepository ??= new FileMasterRepository();
        public BaseRepository<ActionWorkflows> ActionWorkflowsRepository => _actionWorkflowsRepository ??= new ActionWorkflowsRepository();
        public StatementReferenceMasterRepository StatementReferenceMasterRepository => _statementReferenceMasterRepository ??= new StatementReferenceMasterRepository();
        public BaseRepository<FileTypeReference> FileTypeReferenceRepository => _fileTypeReferenceRepository ??= new BaseRepository<FileTypeReference>();
        public BaseRepository<UniVerseDataDictionary> UniVerseDataDictionaryRepository => _uniVerseDataDictionaryRepository ??= new BaseRepository<UniVerseDataDictionary>();
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FloKaptureService()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_nativeResource == IntPtr.Zero) return;
            Marshal.FreeHGlobal(_nativeResource);
            _nativeResource = IntPtr.Zero;
        }
    }
}
