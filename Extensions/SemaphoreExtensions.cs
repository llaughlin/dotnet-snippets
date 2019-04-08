using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Extensions
{
    public static class SemaphoreExtensions
    {
        private static bool _flag;

        public static void TryLock(this SemaphoreSlim semaphore, TimeSpan timeout, Action action)
        {
            if (semaphore == null) throw new ArgumentNullException(nameof(semaphore));

            if (action == null) throw new ArgumentNullException(nameof(action));

            var flag = false;
            try
            {
                Trace.WriteLine($"TryLock enter. CurrentCount={semaphore.CurrentCount}");
                flag = semaphore.Wait(timeout);
                Trace.WriteLine($"TryLock WaitAsync flag={flag}, CurrentCount={semaphore.CurrentCount}");
                if (flag) action();
            }
            finally
            {
                if (flag)
                {
                    Trace.WriteLine(
                        $"TryLock Finally flag={flag}, Releasing semaphore. CurrentCount={semaphore.CurrentCount} ");
                    semaphore.Release();
                    Trace.WriteLine(
                        $"TryLock Finally flag={flag}, Released semaphore. CurrentCount={semaphore.CurrentCount}");
                }
            }
        }


        public static async Task TryLockAsync(this SemaphoreSlim semaphore, TimeSpan timeout, Func<Task> action)
        {
            if (semaphore == null) throw new ArgumentNullException(nameof(semaphore));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var flag = false;
            try
            {
                Trace.WriteLine($"TryLock enter. CurrentCount={semaphore.CurrentCount}");
                flag = await semaphore.WaitAsync(timeout);
                Trace.WriteLine($"TryLock WaitAsync flag={flag}, CurrentCount={semaphore.CurrentCount}");
                if (flag) await action();
            }
            finally
            {
                if (flag)
                {
                    Trace.WriteLine(
                        $"TryLock Finally flag={flag}, Releasing semaphore. CurrentCount={semaphore.CurrentCount} ");
                    semaphore.Release();
                    Trace.WriteLine(
                        $"TryLock Finally flag={flag}, Released semaphore. CurrentCount={semaphore.CurrentCount}");
                }
            }
        }


        public static async Task<IDisposable> UseWaitAsync(this SemaphoreSlim semaphore,
            TimeSpan timeout = default(TimeSpan), CancellationToken cancelToken = default(CancellationToken))
        {
            if (timeout == default(TimeSpan)) timeout = TimeSpan.FromSeconds(5);
            Trace.WriteLine($"UseWaitAsync enter, CurrentCount={semaphore.CurrentCount} ");
            _flag = await semaphore.WaitAsync(timeout, cancelToken).ConfigureAwait(false);
            Trace.WriteLine($"UseWaitAsync WaitAsync flag={_flag}, CurrentCount={semaphore.CurrentCount} ");
            return new ReleaseWrapper(semaphore);
        }

        private class ReleaseWrapper : IDisposable
        {
            private readonly SemaphoreSlim _Semaphore;

            private bool _IsDisposed;

            public ReleaseWrapper(SemaphoreSlim semaphore)
            {
                _Semaphore = semaphore;
            }

            public void Dispose()
            {
                if (_IsDisposed) return;

                Trace.WriteLine(
                    $"UseWaitAsync Dispose flag={_flag}, Releasing semaphore. CurrentCount={_Semaphore.CurrentCount} ");
                _Semaphore.Release();
                Trace.WriteLine(
                    $"UseWaitAsync Dispose flag={_flag}, Released semaphore. CurrentCount={_Semaphore.CurrentCount} ");
                _IsDisposed = true;
            }
        }
    }
}