using System.Collections.Concurrent;
using System.Web.SessionState;

namespace TrueClarity.SessionProvider.Redis
{
    internal class CallbackStore
    {
        private static readonly ConcurrentDictionary<string, SessionStateItemExpireCallback> Callbacks = new ConcurrentDictionary<string, SessionStateItemExpireCallback>();
        
        public void SetCallback(SessionStateItemExpireCallback expireCallback, string sessionType)
        {
            if (!Callbacks.ContainsKey(sessionType))
            {
                Callbacks.TryAdd(sessionType, expireCallback);
            }
        }

        public SessionStateItemExpireCallback Callback(string id)
        {
            if (IsSharedId(id))
            {
                return Callbacks["shared"];
            }

            return Callbacks["private"];
        }

        //todo - is this the best way to distinguish between shared/private sessions?
        private bool IsSharedId(string id)
        {
            return id.Length > 30;
        }
    }
}