using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.SessionState;
using Sitecore.SessionProvider;
using Sitecore.SessionProvider.Helpers;
using StackExchange.Redis;
using TrueClarity.SessionProvider.Redis.Diagnostics;

namespace TrueClarity.SessionProvider.Redis
{
    public class RedisSessionStateProvider : SitecoreSessionStateStoreProvider
    {
        private readonly Microsoft.Web.Redis.RedisSessionStateProvider _redisProvider;
        private ISessionDiagnostics _sessionDiagnostics;
        private ISessionExpirationStore _sessionExpirationStore;
        private readonly CallbackStore _callbackStore;
        public string SessionType { get; set; }
        public string ProviderName { get; set; }
        public bool IncludeLogging { get; set; }
        public int Timeout { get; set; }

        public RedisSessionStateProvider()
        {
            _redisProvider = new Microsoft.Web.Redis.RedisSessionStateProvider();
            _callbackStore = new CallbackStore();
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            ConfigReader configReader = new ConfigReader(config, name);
            
            ProviderName = name;

            SessionType = configReader.GetString("sessionType", true);

            Timeout = configReader.GetInt32("timeout", 20);

            IncludeLogging = configReader.GetBool("logging", false);

            if (configReader.GetBool("detailedDiagnostics", false))
            {
                _sessionDiagnostics = new DetailedSessionDiagnostics(IncludeLogging);
            }
            else
            {
                _sessionDiagnostics = new BasicSessionDiagnostics(IncludeLogging);
            }

            //config["applicationName"] = SessionType;

            ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(configReader.GetString("host", false));

            _sessionExpirationStore = new RedisSessionExpirationStore(connectionMultiplexer.GetDatabase(), Timeout, IncludeLogging, "");

            _redisProvider.Initialize(name, config);

            base.Initialize(name, config);
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            _callbackStore.SetCallback(expireCallback, SessionType);
            
            bool result = base.SetItemExpireCallback(CustomCallback);

            _sessionDiagnostics.SetItemExpireCallback(result, expireCallback, SessionType);

            return result;
        }

        private void CustomCallback(string id, SessionStateStoreData item)
        {
            SessionStateItemExpireCallback callback = _callbackStore.Callback(id);

            _sessionDiagnostics.OnItemExpired(id, item, callback, SessionType);

            callback.Invoke(id, item);
        }

        public override void EndRequest(HttpContext context)
        {
            _redisProvider.EndRequest(context);

            _sessionDiagnostics.EndRequest(context);

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
                _sessionDiagnostics.IdToExpireFound(signalTime, lockCookie, id);

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
            try
            {
                ExecuteSessionEnd(id, item);
            }
            finally
            {
                _redisProvider.RemoveItem(context, id, lockId, item);
            }
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
