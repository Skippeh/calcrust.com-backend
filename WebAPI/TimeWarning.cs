using System;
using System.Diagnostics;

namespace WebAPI
{
    // "Borrowed" from rust source code. :~)

    public class TimeWarning : IDisposable
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private string warningName;
        private double warningMs;
        private bool disposed;

        public static TimeWarning New(string name, float maxSeconds = 0.1f)
        {
            TimeWarning timeWarning = new TimeWarning();
            timeWarning.Start(name, maxSeconds);
            return timeWarning;
        }

        public static TimeWarning New(string name, long maxMilliseconds)
        {
            TimeWarning timeWarning = new TimeWarning();
            timeWarning.Start(name, maxMilliseconds);
            return timeWarning;
        }

        private void Start(string name, float maxSeconds = 0.1f)
        {
            this.warningName = name;
            this.warningMs = (double)maxSeconds * 1000.0;
            this.stopwatch.Reset();
            this.stopwatch.Start();
            this.disposed = false;
        }

        private void Start(string name, long maxMilliseconds)
        {
            this.warningName = name;
            this.warningMs = (double)maxMilliseconds;
            this.stopwatch.Reset();
            this.stopwatch.Start();
            this.disposed = false;
        }

        void IDisposable.Dispose()
        {
            if (this.disposed)
                return;
            this.disposed = true;

            if (this.stopwatch.Elapsed.TotalMilliseconds > this.warningMs)
                Console.WriteLine("TimeWarning: {0} took {1:0.00} seconds ({2:0} ms)", warningName, stopwatch.Elapsed.TotalSeconds, stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}