using System;
using System.Web;
using System.Web.SessionState;
using Sitecore.Diagnostics;
using Sitecore.SessionProvider;

namespace TrueClarity.SessionProvider.Redis.Diagnostics
{
    public abstract class SessionDiagnosticsBase : ISessionDiagnostics
    {
        protected readonly bool LoggingEnabled;

        protected SessionDiagnosticsBase(bool loggingEnabled)
        {
            LoggingEnabled = loggingEnabled;
        }

        protected void Dump(string message)
        {
            if (LoggingEnabled)
            {
                Log.Info(message, this);
            }
        }

        public virtual void OnItemExpired(string id, SessionStateStoreData item)
        {
        }

        public virtual void IdToExpireFound(DateTime signalTime, SessionStateLockCookie lockCookie, string id)
        {
        }

        public virtual void EndRequest(HttpContext context)
        {
        }
    }
}
