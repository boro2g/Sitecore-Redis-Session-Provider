using System;

namespace RedisDemo
{
    public class Result
    {
        public string ResultType { get; set; }
        public string Message { get; set; }
        public TimeSpan? Ttl { get; set; }
    }
}