using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Extensions
{
    public static class DirectoryInfoExtensions
    {
        #region Methods

        public static IEnumerable<FileInfo> SafeEnumerateFiles(this DirectoryInfo target, string pattern = "*")
        {
            var stack = new Stack<DirectoryInfo>();
            stack.Push(target);

            while (stack.Any())
            {
                var current = stack.Pop();
                var files = GetFiles(current, pattern);
                foreach (var fileInfo in files)
                    yield return fileInfo;
                foreach (var subdirectory in GetSubdirectories(current))
                    stack.Push(subdirectory);
            }
        }

        private static IEnumerable<FileInfo> GetFiles(DirectoryInfo directory, string pattern)
        {
            try
            {
                return directory.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        private static IEnumerable<DirectoryInfo> GetSubdirectories(DirectoryInfo directory)
        {
            try
            {
                return directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                return Enumerable.Empty<DirectoryInfo>();
            }
        }

        #endregion
    }
}