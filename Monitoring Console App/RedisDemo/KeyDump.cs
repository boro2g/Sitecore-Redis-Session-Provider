using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using StackExchange.Redis;

namespace RedisDemo
{
    internal class KeyDump
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        string timeFormat = "HH:mm:ss";
        private string fullFormat = "yyyy MM dd HH:mm:ss";

        public KeyDump(ConnectionMultiplexer redis, IDatabase database)
        {
            _redis = redis;
            _database = database;
        }

        public void DumpAllKeys()
        {
            while (true)
            {
                Console.Clear();

                var now = DateTime.Now;

                Console.WriteLine($"--- go {now.ToString(timeFormat)} ---");

                bool success = false;
                try
                {
                    DumpKeys(now);
                    success = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    //throw;
                }

                Console.WriteLine($"--- {(success ? "ok" : "error")} {now.ToString(timeFormat)} ---");
                Thread.Sleep(1000);
            }
        }

        private void DumpKeys(DateTime now)
        {
            foreach (var endpoint in _redis.GetEndPoints(true))
            {
                var server = _redis.GetServer(endpoint);

                int count = 0;

                List<Result> results = new List<Result>();

                List<string> notLongToLive = new List<string>();

                int keyCount = server.Keys().Count();

                Console.WriteLine($"Total keys: {keyCount}\n");

                //foreach (var key in server.Keys(pattern: $"Expire {DateTime.Now.ToString("yyyy-MM-dd HH:mm")}*"))
                foreach (var key in server.Keys().Select(a => a.ToString()))
                {
                    Result result = null;

                    count++;

                    if (key == "_log")
                    {
                        result = (new Result
                        {
                            Message = key + ": " + String.Join(",", FormatSortedEntry(_database.SortedSetScan(key))),
                            ResultType = "Log"
                        });
                    }
                    else if (key.EndsWith("_Marker"))
                    {
                        result = (new Result
                        {
                            Message = key + ": " + String.Join(",", _database.SetMembers(key)),
                            ResultType = "Marker"
                        });
                    }
                    else if (key.EndsWith("_Timeout"))
                    {
                        string value = _database.StringGet(key);

                        DateTime valueParsed = DateTime.ParseExact(value, fullFormat, CultureInfo.CurrentCulture);

                        var secondGap = valueParsed.Subtract(now).TotalSeconds;

                        if (secondGap > -10)
                        {
                            result = (new Result
                            {
                                Message =
                                    $"{key} ({(secondGap < 0 ? "---" : secondGap.ToString("000"))} s) value: {value}",
                                ResultType = "Timeout"
                            });
                        }
                    }
                    else
                    {
                        var ttl = _database.KeyTimeToLive(key);

                        result = (new Result
                        {
                            Message = key,
                            ResultType = "General"
                        });

                        if (ttl != null && ttl.Value.TotalMinutes < 2)
                        {
                            notLongToLive.Add(key + " " + ttl);
                        }
                    }

                    if (result != null)
                    {
                        result.Ttl = _database.KeyTimeToLive(key);

                        results.Add(result);
                    }
                }
                
                RenderMessages(results, new List<string> { "Log" });
               
                RenderMessages(
                    results.OrderBy(a => a.Ttl.GetValueOrDefault(new TimeSpan(0, 0, 0, 1)).TotalSeconds).ToList(),
                    new List<string> {"Timeout"});
               
                //RenderMessages(
                //    results.OrderBy(a => a.Ttl.GetValueOrDefault(new TimeSpan(0, 0, 0, 1)).TotalSeconds).ToList(),
                //    new List<string> {"General"});
              
                RenderMessages(results, new List<string> {"Marker"});

                notLongToLive.Sort();

                if (notLongToLive.Any())
                {
                    Console.WriteLine("");
                    Console.WriteLine("Not long to live:");

                    notLongToLive.ForEach(a => Console.WriteLine($" {a}"));
                }
            }
        }

        private static void RenderMessages(List<Result> results, List<string> messagesToShow)
        {
            Console.WriteLine(String.Join(",", messagesToShow) + ":");

            results
                .Where(a => messagesToShow.Contains(a.ResultType))
                .ToList()
                .ForEach(a => Console.WriteLine(a.Ttl == null ? $" - {a.Message}" : $" TTL: {a.Ttl} - {a.Message}"));
        }

        private static IEnumerable<string> FormatSortedEntry(IEnumerable<SortedSetEntry> sortedSetScan)
        {
            return sortedSetScan.Select(set => set.Element + ":" + new DateTime((long)set.Score));
        }
    }
}