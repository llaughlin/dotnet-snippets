using System.Collections.Generic;

namespace Extensions
{
    public interface IFileSystemAccessor
    {
        IEnumerable<string> ReadAllFileLines(string filePath);
        string GetFileVersionNumber(string filePath);
    }
}