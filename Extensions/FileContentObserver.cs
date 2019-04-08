using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace Extensions
{
    public class FileContentObserver : IFileContentObserver
    {
        private readonly IFileSystemObserver _FileSystemObserver;
        private readonly ILogger _Log;

        public FileContentObserver(ILoggerFactory loggerFactory, IFileSystemObserver fileSystemObserver, FileInfo file)
        {
            _FileSystemObserver = fileSystemObserver;
            _Log = loggerFactory.CreateLogger<FileContentObserver>();

            FileInfo = file;
            Changes = _FileSystemObserver.ObserveChanges(file);

            Reader = file.OpenSharedRead();

            CurrentLines = ReadAllLines().ToObservable();

            var timer = Observable.Interval(TimeSpan.FromSeconds(5)).Select(_ => Unit.Default);
            NewLines = Changes.Select(_ => Unit.Default)
                .Merge(timer)
                .SelectMany(_ => ReadAllLines())
                .Publish()
                .RefCount();
        }

        public FileContentObserver(ILoggerFactory loggerFactory, Stream stream)
        {
            _Log = loggerFactory.CreateLogger<FileContentObserver>();
            Reader = new StreamReader(stream);

            CurrentLines = ReadAllLines().ToObservable().Publish().RefCount();

            var timer = Observable.Interval(TimeSpan.FromSeconds(5)).Select(_ => Unit.Default);
            NewLines = Changes.Select(_ => Unit.Default)
                .Merge(timer)
                .SelectMany(_ => ReadAllLines())
                .Publish()
                .RefCount();
        }

        public IObservable<LogLine> CurrentLines { get; set; }
        public IObservable<LogLine> NewLines { get; set; }

        private FileInfo FileInfo { get; }

        internal StreamReader Reader { get; set; }

        internal int LineNumber { get; set; }

        internal IObservable<FileSystemEventArgs> Changes { get; set; }

        private IEnumerable<LogLine> ReadAllLines()
        {
            string nextLine;

            while ((nextLine = Reader.ReadLine()) != null)
            {
                LineNumber++;

                var logLine = new LogLine(LineNumber, nextLine);
                //_Log.Verbose("Read line {Line}", logLine);
                yield return logLine;
            }
        }
    }
}