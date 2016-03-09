using System;
using System.Collections.Generic;
using System.Linq;
using StackExchange.Redis;

namespace RedisDemo
{
    internal class SortedSetPerformance
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        string redisKey = "Sorted";
        int entriesCount = 10000;
        private int _count;

        public SortedSetPerformance(ConnectionMultiplexer redis, IDatabase database)
        {
            _redis = redis;
            _database = database;
        }

        public void Test()
        {
            using (new OperationTimer("SortedSetPerf", Console.WriteLine))
            {
                _count = 0;

                _database.KeyDelete(redisKey);

                var source = Enumerable.Range(0, 10);

                AddToSortedSet(source);

                //UpdateKey(source);

                Console.WriteLine("Count: " + _count);
            }
        }

        private void UpdateKey(IEnumerable<int> source)
        {
            var parallelQuery = from num in source.AsParallel()
                                select num;

            parallelQuery.ForAll(SetEntry);

            Console.WriteLine("First item: " + _database.StringGet(redisKey));
        }

        private void SetEntry(int value)
        {
            for (int i = value * entriesCount; i < (value + 1)*entriesCount; i++)
            {
                _count++;

                _database.StringSet(redisKey, value);
            }
        }

        private void AddToSortedSet(IEnumerable<int> source)
        {
            var parallelQuery = from num in source.AsParallel()
                select num;

            parallelQuery.ForAll(AddEntries);

            var first = _database.SortedSetRangeByScoreWithScores(redisKey).FirstOrDefault();

            Console.WriteLine("Length: " + _database.SortedSetLength(redisKey));

            Console.WriteLine("First item: " + first.Score);
        }

        private void AddEntries(int startIndex)
        {
            for (int i = startIndex * entriesCount; i < (startIndex + 1) * entriesCount; i++)
            {
                _count++;

                _database.SortedSetAdd(redisKey, i.ToString(), i);
            }
        }
    }
}