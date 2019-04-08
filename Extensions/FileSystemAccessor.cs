using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Extensions
{
    public class FileSystemAccessor : IFileSystemAccessor
    {
        private readonly ILogger _Log;

        public FileSystemAccessor(ILoggerFactory loggerFactory)
        {
            _Log = loggerFactory.CreateLogger<FileSystemAccessor>();
        }


        public string GetFileVersionNumber(string filePath)
        {
            try
            {
                return FileVersionInfo.GetVersionInfo(filePath).FileVersion;
            }
            catch (Exception ex)
            {
                _Log.LogError(0, ex, "Error getting version number of file {File}", filePath);
                return "Unable to get current file version.";
            }
        }

        public IEnumerable<string> ReadAllFileLines(string filePath)
        {
            var reader = new FileInfo(filePath).OpenSharedRead();
            string nextLine;

            while ((nextLine = reader.ReadLine()) != null) yield return nextLine;
        }
    }
}