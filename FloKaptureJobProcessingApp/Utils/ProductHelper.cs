using System;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace FloKaptureJobProcessingApp.Utils
{
    public static class ProductHelper
    {
        public static bool CheckAndCreateDir(string dirPath)
        {
            if (Directory.Exists(dirPath)) return true;

            var info = Directory.CreateDirectory(dirPath);
            return info.Exists;
        }
        public static bool IsFileReady(string sFilename)
        {
            try
            {
                // If the file can be opened for exclusive access it means that the file
                // is no longer locked by another process.
                using (var inputStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return inputStream.Length > 0;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("File is not ready to open.\n" + exception.Message);
                IsFileReady(sFilename);
                return true;
            }
        }
        public static string NormalizePath(HttpRequest httpRequest)
        {
            return httpRequest.Path.Value;
        }
    }
}