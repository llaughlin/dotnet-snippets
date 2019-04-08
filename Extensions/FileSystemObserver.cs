using System;
using System.IO;
using System.Reactive.Linq;
using SystemWrapper.IO;

namespace Extensions
{
    public class FileSystemObserver : IFileSystemObserver
    {
        public IObservable<FileSystemEventArgs> AllChanges = Observable.Empty<FileSystemEventArgs>();


        public IObservable<FileSystemEventArgs> ObserveChanges(FileInfo fileInfo)
        {
            return ObserveChangesImpl(fileInfo.DirectoryName, fileInfo.Name);
        }

        private IObservable<FileSystemEventArgs> ObserveChangesImpl(string directoryName, string fileName)
        {
            var watcher = new FileSystemWatcherWrap(directoryName, fileName);
            watcher.EnableRaisingEvents = true;
            var changes = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => watcher.Changed += h,
                    h => watcher.Changed -= h)
                .Select(e => e.EventArgs);

            AllChanges = AllChanges.Merge(changes);

            return changes;
        }
    }
}