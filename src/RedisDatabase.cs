using System;
using StackExchange.Redis;

namespace TrueClarity.SessionProvider.Redis
{
    internal class RedisDatabase
    {
        private readonly IDatabase _database;
        private readonly TimeSpan _retryTimeout;

        public RedisDatabase(IDatabase database, TimeSpan retryTimeout)
        {
            _database = database;
            _retryTimeout = retryTimeout;
        }

        public bool SetRemove(RedisKey key, RedisValue value)
        {
            return Retry(() => _database.SetRemove(key, value));
        }

        public string StringGet(RedisKey key)
        {
            return Retry(() => _database.StringGet(key));
        }

        public bool StringSet(RedisKey key, RedisValue value, TimeSpan timeSpan)
        {
            return Retry(() => _database.StringSet(key, value, timeSpan));
        }

        public long SetLength(RedisKey key)
        {
            return Retry(() => _database.SetLength(key));
        }

        public bool SortedSetRemove(RedisKey key, RedisValue value)
        {
            return Retry(() => _database.SortedSetRemove(key, value));
        }

        public bool SetAdd(RedisKey key, RedisValue value)
        {
            return Retry(() => _database.SetAdd(key, value));
        }

        public bool SortedSetAdd(RedisKey key, RedisValue member, double score)
        {
            return Retry(() => _database.SortedSetAdd(key, member, score));
        }

        public SortedSetEntry[] SortedSetRangeByScoreWithScores(RedisKey key)
        {
            return Retry(() => _database.SortedSetRangeByScoreWithScores(key));
        }

        public RedisValue SetPop(RedisKey key)
        {
            return Retry(() => _database.SetPop(key));
        }

        public HashEntry[] HashGetAll(RedisKey key)
        {
            return Retry(() => _database.HashGetAll(key));
        }

        public bool KeyDelete(RedisKey key)
        {
            return Retry(() => _database.KeyDelete(key));
        }

        private T Retry<T>(Func<T> redisAction)
        {
            int timeToSleepBeforeRetryInMiliseconds = 20;
            DateTime startTime = DateTime.Now;
            while (true)
            {
                try
                {
                    return (T)redisAction();
                }
                catch (Exception)
                {
                    TimeSpan passedTime = DateTime.Now - startTime;
                    if (_retryTimeout < passedTime)
                    {
                        throw;
                    }
                    else
                    {
                        int remainingTimeout = (int)(_retryTimeout.TotalMilliseconds - passedTime.TotalMilliseconds);
                        // if remaining time is less than 1 sec than wait only for that much time and than give a last try
                        if (remainingTimeout < timeToSleepBeforeRetryInMiliseconds)
                        {
                            timeToSleepBeforeRetryInMiliseconds = remainingTimeout;
                        }
                    }

                    // First time try after 20 msec after that try after 1 second
                    System.Threading.Thread.Sleep(timeToSleepBeforeRetryInMiliseconds);
                    timeToSleepBeforeRetryInMiliseconds = 1000;
                }
            }
        }
    }
}
