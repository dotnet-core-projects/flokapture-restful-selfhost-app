using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BusinessLayer.BaseRepositories;
using BusinessLayer.DbEntities;

namespace FloKaptureJobProcessingApp.FloKaptureServices
{ 
    public interface IGeneralService : IDisposable
    {
        BaseRepository<T> BaseRepository<T>() where T : EntityBase;
    }
    
    [DebuggerStepThrough]
    public class GeneralService : IGeneralService
    {
        private IntPtr _nativeResource = Marshal.AllocHGlobal(100);
        public BaseRepository<T> BaseRepository<T>() where T : EntityBase
        {
            return new BaseRepository<T>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GeneralService()
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
