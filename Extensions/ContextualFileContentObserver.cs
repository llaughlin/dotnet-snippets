using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Extensions
{
    public class ContextualFileContentObserver
    {
        private readonly ILoggerFactory _LoggerFactory;
        private readonly IFileSystemObserver _FileSystemObserver;
        private readonly ILogger _Log;

        public ContextualFileContentObserver(ILoggerFactory loggerFactory, IFileSystemObserver fileSystemObserver)
        {
            _LoggerFactory = loggerFactory;
            _FileSystemObserver = fileSystemObserver;
            _Log = loggerFactory.CreateLogger<FileContentObserver>();
        }

        public IObservable<LineWithContext> ObserveFile(FileInfo file, int precedingLineCount, int followingLineCount)
        {
            var observer = new FileContentObserver(_LoggerFactory, _FileSystemObserver, file);

            return ObserveLinesWithContext(observer.CurrentLines.Concat(observer.NewLines), precedingLineCount,
                followingLineCount);
        }

        public IObservable<LineWithContext> ObserveFile(FileInfo file, Regex lineRegex, int precedingLineCount,
            int followingLineCount)
        {
            var observer = new FileContentObserver(_LoggerFactory, _FileSystemObserver, file);

            return ObserveLinesWithContext(observer.CurrentLines.Concat(observer.NewLines), lineRegex,
                precedingLineCount, followingLineCount);
        }

        internal DateTimeOffset? TryParseDateTime(LineWithContext line)
        {
            var lineContext = new List<string>();
            lineContext.Add(line.Line.Content);
            lineContext.AddRange(line.PrecedingLines.Select(l => l.Content));
            lineContext.AddRange(line.FollowingLines.Select(l => l.Content));
            foreach (var l in lineContext)
            {
                var split = l.Split(' ');
                if (split.Length >= 2 &&
                    DateTimeOffset.TryParse($"{split[0]} {split[1]}".Replace(',', '.'), out var time)) return time;
            }

            //_Log.Verbose("Couldn't parse datetime from {Line}", lineContent);
            return null;
        }

        private IObservable<LineWithContext> ObserveLinesWithContext(IObservable<LogLine> source, Regex lineRegex,
            int precedingLineCount, int followingLineCount)
        {
            _Log.LogTrace(
                "Creating observable context lines. Regex: {Regex} PreviousLineCount: {PreviousLineCount} FollowingLineCount: {FollowingLineCount}",
                lineRegex, precedingLineCount, followingLineCount);

            return from lines in source.SlidingWindow(precedingLineCount + 1 + followingLineCount)
                where lines.Count >= precedingLineCount
                let errorLine = lines[precedingLineCount].Content
                where errorLine != null
                where lineRegex?.IsMatch(errorLine) ?? true
                select new LineWithContext(lines, precedingLineCount);
        }

        private IObservable<LineWithContext> ObserveLinesWithContext(IObservable<LogLine> source,
            int precedingLineCount, int followingLineCount)
        {
            return ObserveLinesWithContext(source, null, precedingLineCount, followingLineCount);
        }
    }
}