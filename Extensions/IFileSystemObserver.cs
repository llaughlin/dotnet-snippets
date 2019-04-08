using System;
using System.IO;

namespace Extensions
{
    public interface IFileSystemObserver
    {
        IObservable<FileSystemEventArgs> ObserveChanges(FileInfo fileInfo);
    }
}