using System;
using System.Linq;
using System.Web;
using System.Web.SessionState;
using Sitecore.Diagnostics;
using Sitecore.SessionProvider;
using StackExchange.Redis;

namespace TrueClarity.SessionProvider.Redis
{
    public class RedisSessionExpirationStore : ISessionExpirationStore
    {
        private readonly RedisDatabase _redisDatabase;
        private readonly int _timeoutMinutes;
        private readonly bool _includeLogging;
        private const string AllKeysLog = "_log";

        public RedisSessionExpirationStore(IDatabase database, int timeoutMinutes, bool includeLogging)
        {
            _redisDatabase = new RedisDatabase(database, new TimeSpan(0, 0, 5));
            _timeoutMinutes = timeoutMinutes;
            _includeLogging = includeLogging;
        }

        public int TimeoutSkew(int timeout)
        {
            return timeout * 10;
        }

        public void EntryAccessed(string id)
        {
            if (IsValidId(id))
            {
                var now = NowWithTimeout();

                var timeoutKey = Key(id).TimeoutKey;

                ResetExistingEntry(id, timeoutKey);

                AddOrUpdateEntry(id, now, timeoutKey);
            }
        }

        ///We dont need to raise for entries which have a guid key, just entries with a session id
        private bool IsValidId(string id)
        {
            return id.Length < 30;
        }

        private void ResetExistingEntry(string id, string timeoutKey)
        {
            var existingDetails = _redisDatabase.StringGet(timeoutKey);

            if (!String.IsNullOrEmpty(existingDetails))
            {
                var existingNowKey = KeyGenerator.NowKey(existingDetails);

                _redisDatabase.SetRemove(existingNowKey, id);

                if (_redisDatabase.SetLength(existingNowKey) == 0)
                {
                    _redisDatabase.SortedSetRemove(AllKeysLog, existingNowKey);
                }
            }
        }

        private void AddOrUpdateEntry(string id, DateTime now, string timeoutKey)
        {
            string nowString = KeyGenerator.FormatDateTimeKey(now);

            string nowKey = KeyGenerator.NowKey(nowString);

            _redisDatabase.StringSet(timeoutKey, nowString, new TimeSpan(0, TimeoutSkew(_timeoutMinutes), 0));

            _redisDatabase.SetAdd(nowKey, id);

            _redisDatabase.SortedSetAdd(AllKeysLog, nowKey, now.Ticks);
        }

        private DateTime NowWithTimeout()
        {
            return DateTime.UtcNow.AddMinutes(_timeoutMinutes);
        }

        public string FindExpiredItemId(DateTime signalTime, SessionStateLockCookie lockCookie, out string itemMarker)
        {
            var firstSet = _redisDatabase.SortedSetRangeByScoreWithScores(AllKeysLog).FirstOrDefault();

            if (firstSet.Score > 0 && !String.IsNullOrEmpty(firstSet.Element))
            {
                var firstSetDateTime = new DateTime((long) firstSet.Score);

                if (signalTime > firstSetDateTime)
                {
                    var markerKey = KeyGenerator.FormatNowKey(firstSetDateTime);

                    itemMarker = markerKey;

                    string id = _redisDatabase.SetPop(markerKey);

                    if (!String.IsNullOrEmpty(id))
                    {
                        Dump($"Found user id to expire: {id}");

                        return id;
                    }
                }
            }

            itemMarker = null;

            return null;
        }

        public SessionStateStoreData GetItem(string id, string itemMarker)
        {
            if (String.IsNullOrEmpty(id))
            {
                return null;
            }
           
            var item = _redisDatabase.HashGetAll(Key(id).DataKey.Replace(" ","").Replace("{","{/"));
            
            CleanupMarkerEntries(itemMarker);

            if (item.Length > 0)
            {
                var sessionData = SessionStateParser.GetSessionDataStatic(item);

                Dump($"Get session data for user: {id} ({itemMarker})");

                return new SessionStateStoreData(sessionData, new HttpStaticObjectsCollection(), TimeoutSkew(_timeoutMinutes));
            }

            return null;
        }

        private void Dump(string message)
        {
            if (_includeLogging)
            {
                Log.Info(message, this);
            }
        }

        private void CleanupMarkerEntries(string itemMarker)
        {
            if (_redisDatabase.SetLength(itemMarker) == 0)
            {
                _redisDatabase.SortedSetRemove(AllKeysLog, itemMarker);
                _redisDatabase.KeyDelete(itemMarker);
            }
        }

        private static KeyGenerator Key(string id)
        {
            return new KeyGenerator(id, "");
        }
    }
}