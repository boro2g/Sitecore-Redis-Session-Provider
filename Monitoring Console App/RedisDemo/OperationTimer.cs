using System;
using System.Diagnostics;

namespace RedisDemo
{
    public class OperationTimer : IDisposable
    {
        private readonly string _message;
        private readonly Action<string> _log;

        private readonly Stopwatch _stopwatch;
        
        public long ElapsedMilliseconds => _stopwatch?.ElapsedMilliseconds ?? -1;

        public OperationTimer( string message, Action<string> log)
        {
            _message = message;

            _log = log;

            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();

            _log($"{_message} took: {_stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
