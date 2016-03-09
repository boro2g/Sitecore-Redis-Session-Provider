using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.SessionState;
using Microsoft.Web.Redis;
using Sitecore.Diagnostics;
using Sitecore.SessionProvider;
using Sitecore.SessionProvider.Helpers;
using StackExchange.Redis;

namespace TrueClarity.SessionProvider.Redis
{
    public class SitecoreRedisSessionProvider : SitecoreSessionStateStoreProvider
    {
        private readonly RedisSessionStateProvider _redisProvider;
        private ISessionExpirationStore _sessionExpirationStore;
        private SessionStateItemExpireCallback _expireCallback;
        public string SessionType { get; set; }
        public string ProviderName { get; set; }
        public bool IncludeLogging { get; set; }
        public int Timeout { get; set; }

        public SitecoreRedisSessionProvider()
        {
            _redisProvider = new RedisSessionStateProvider();
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            ConfigReader configReader = new ConfigReader(config, name);
            
            ProviderName = name;

            SessionType = configReader.GetString("sessionType", true);

            Timeout = configReader.GetInt32("timeout", 20);

            IncludeLogging = configReader.GetBool("logging", false);

            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configReader.GetString("host", false));

            _sessionExpirationStore = new RedisSessionExpirationStore(connectionMultiplexer.GetDatabase(), Timeout, IncludeLogging);

            _redisProvider.Initialize(name, config);

            base.Initialize(name, config);
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            _expireCallback = expireCallback;

            return base.SetItemExpireCallback(OnItemExpired);
        }

        private void OnItemExpired(string id, SessionStateStoreData item)
        {
            if (IncludeLogging)
            {
                Log.Info($"OnItemExpired - {id}", this);
            }

            _expireCallback.Invoke(id, item);
        }

        public override void EndRequest(HttpContext context)
        {
            _redisProvider.EndRequest(context);

            base.EndRequest(context);
        }

        public override void Dispose()
        {
            _redisProvider.Dispose();

            base.Dispose();
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return _redisProvider.CreateNewStoreData(context, _sessionExpirationStore.TimeoutSkew(timeout));
        }

        protected override SessionStateStoreData GetExpiredItemExclusive(DateTime signalTime, SessionStateLockCookie lockCookie, out string id)
        {
            string itemMarker;

            id = _sessionExpirationStore.FindExpiredItemId(signalTime, lockCookie, out itemMarker);

            if (!String.IsNullOrWhiteSpace(id))
            {
                return _sessionExpirationStore.GetItem(id, itemMarker);
            }

            return null;
        }

        public override void InitializeRequest(HttpContext context)
        {
            _redisProvider.InitializeRequest(context);

            base.InitializeRequest(context);
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId,
            out SessionStateActions actions)
        {
            return _redisProvider.GetItem(context, id, out locked, out lockAge, out lockId, out actions);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge,
            out object lockId, out SessionStateActions actions)
        {
            return _redisProvider.GetItemExclusive(context, id, out locked, out lockAge, out lockId, out actions);
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            _redisProvider.ReleaseItemExclusive(context, id, lockId);
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            if (item.Timeout == Timeout)
            {
                item.Timeout = _sessionExpirationStore.TimeoutSkew(Timeout);
            }

            _sessionExpirationStore.EntryAccessed(id);

            _redisProvider.SetAndReleaseItemExclusive(context, id, item, lockId, newItem);
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            _redisProvider.RemoveItem(context, id, lockId, item);
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            _redisProvider.ResetItemTimeout(context, id);
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            _redisProvider.CreateUninitializedItem(context, id, _sessionExpirationStore.TimeoutSkew(timeout));
        }

        protected override void RemoveItem(string id, string lockCookie)
        {
            _redisProvider.RemoveItem(HttpContext.Current, id, lockCookie, null);
        }
    }
}
