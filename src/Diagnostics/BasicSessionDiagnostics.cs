using System.Web.SessionState;

namespace TrueClarity.SessionProvider.Redis.Diagnostics
{
    class BasicSessionDiagnostics : SessionDiagnosticsBase
    {
        public BasicSessionDiagnostics(bool includeLogging) : base(includeLogging)
        {
            
        }

        public override void OnItemExpired(string id, SessionStateStoreData item, SessionStateItemExpireCallback expireCallback, string sessionType)
        {
            Dump($"OnItemExpired - {id}");
        }
    }
}