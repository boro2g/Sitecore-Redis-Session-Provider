using System;
using System.Web;
using System.Web.SessionState;
using Sitecore.SessionProvider;

namespace TrueClarity.SessionProvider.Redis.Diagnostics
{
    public interface ISessionDiagnostics
    {
        void OnItemExpired(string id, SessionStateStoreData item, SessionStateItemExpireCallback expireCallback, string sessionType);
        void IdToExpireFound(DateTime signalTime, SessionStateLockCookie lockCookie, string id);
        void EndRequest(HttpContext context);
        void SetItemExpireCallback(bool result, SessionStateItemExpireCallback expireCallback, string sessionType);
        void Error(string message);
    }
}