using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Extensions
{
    public static class ObservableExtensions
    {
        public static IObservable<IList<T>> SlidingWindow<T>(this IObservable<T> source, int windowSize)
        {
            var publishedSource = source.Publish().RefCount();
            return Enumerable.Range(0, windowSize)
                .Select(skip => publishedSource.Skip(skip)).Zip();
        }

        public static IEnumerable<IList<T>> SlidingWindow<T>(this IEnumerable<T> source, int windowSize)
        {
            var window = new List<T>(windowSize);
            foreach (var line in source)
            {
                if (window.Count == windowSize) window.RemoveAt(0);
                window.Add(line);
                if (window.Count == windowSize) yield return window;
            }
        }
    }
}