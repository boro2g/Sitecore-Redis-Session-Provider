using System;
using System.Web.SessionState;
using Sitecore.SessionProvider;

namespace TrueClarity.SessionProvider.Redis
{
    public interface ISessionExpirationStore
    {
        int TimeoutSkew(int timeout);
        void EntryAccessed(string id);
        string FindExpiredItemId(DateTime signalTime, SessionStateLockCookie lockCookie, out string itemMarker);
        SessionStateStoreData GetItem(string id, string itemMarker);
    }
}