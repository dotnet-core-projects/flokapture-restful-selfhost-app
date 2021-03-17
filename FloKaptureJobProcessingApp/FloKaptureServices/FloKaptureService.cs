using BusinessLayer.BaseRepositories;
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
        private BaseRepository<FileTypeReference> _fileTypeReferenceRepository;
        private BaseRepository<UniVerseDataDictionary> _uniVerseDataDictionaryRepository;
        public IUniVerseBasicUtils UniVerseBasicUtils => new UniVerseBasicUtils();
        public BaseRepository<UserMaster> UserMasterRepository =>
            _userMasterRepository ?? (_userMasterRepository = new UserMasterRepository());
        public BaseRepository<UserDetails> UserDetailsRepository =>
            _userDetailsRepository ?? (_userDetailsRepository = new UserDetailsRepository());
        public BaseRepository<LanguageMaster> LanguageMasterRepository =>
            _languageMasterRepository ?? (_languageMasterRepository = new LanguageMasterRepository());
        public BaseRepository<ProjectMaster> ProjectMasterRepository =>
            _projectMasterRepository ?? (_projectMasterRepository = new BaseRepository<ProjectMaster>());
        public BaseRepository<FileMaster> FileMasterRepository =>
            _fileMasterRepository ?? (_fileMasterRepository = new FileMasterRepository());
        public BaseRepository<FileTypeReference> FileTypeReferenceRepository =>
            _fileTypeReferenceRepository ?? (_fileTypeReferenceRepository = new BaseRepository<FileTypeReference>());
        public BaseRepository<UniVerseDataDictionary> UniVerseDataDictionaryRepository =>
            _uniVerseDataDictionaryRepository ?? (_uniVerseDataDictionaryRepository = new BaseRepository<UniVerseDataDictionary>());
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
