using System;
using System.Configuration;
using System.Threading;
using StackExchange.Redis;

namespace RedisDemo
{
    class Program
    {
        private static ConnectionMultiplexer redis;

        private static IDatabase database;

        static void Main(string[] args)
        {
            redis = ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["Redis"].ConnectionString);

            database = redis.GetDatabase();

            //new SortedSetPerformance(redis, database).Test();

            new KeyDump(redis, database).DumpAllKeys();
        }

        private static void Purge()
        {
            foreach (var endpoint in redis.GetEndPoints(true))
            {
                var server = redis.GetServer(endpoint);

                server.FlushAllDatabases();
            }
        }

        private static void FindItem(string id)
        {
            string key = "{/" + "" + "_" + id + "}_Data";

            Console.WriteLine(key);

            var result = database.HashGetAll(key);

            Console.WriteLine(result.Length);
        }
        
        private static void PubSubExample()
        {
            ISubscriber subscriber = redis.GetSubscriber();

            subscriber.Subscribe("messages", (channel, message) => {
                                                                       Console.WriteLine(message);
            });

            while (true)
            {
                subscriber.Publish("messages", "hello");

                Thread.Sleep(2000);
            }
        }
    }
}
