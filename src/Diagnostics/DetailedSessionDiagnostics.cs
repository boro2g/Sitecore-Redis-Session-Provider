using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.SessionState;
using Newtonsoft.Json;
using Sitecore.Analytics.Tracking;
using Sitecore.SessionProvider;

namespace TrueClarity.SessionProvider.Redis.Diagnostics
{
    public class DetailedSessionDiagnostics : SessionDiagnosticsBase
    {
        private readonly ThreadLocal<HttpContext> _stubHttpContext = new ThreadLocal<HttpContext>();

        public override void OnItemExpired(string id, SessionStateStoreData item)
        {
            Dump($"OnItemExpired - {id}");
            Dump($" {SessionContent(item)}");
            Dump($" Contact lock ids: {LockIds()}");
        }

        public override void IdToExpireFound(DateTime signalTime, SessionStateLockCookie lockCookie, string id)
        {
            Dump($"SignalTime: {signalTime}, lockCookie: {JsonConvert.SerializeObject(lockCookie)}, id: {id}");
        }

        public override void EndRequest(HttpContext context)
        {
            
        }

        private string SessionContent(SessionStateStoreData sessionData)
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in sessionData.Items)
            {
                var sessionDataEntry = sessionData.Items[item.ToString()];

                StandardSession standardSession = sessionDataEntry as StandardSession;

                if (standardSession != null)
                {
                    builder.AppendLine($"  - Settings: {JsonConvert.SerializeObject(standardSession.Settings)}");
                    builder.AppendLine($"  - Interaction: Pagecount={standardSession.Interaction.PageCount}");
                }
                 
                builder.AppendLine($" - Session items: {item} - {sessionDataEntry}");
            }

            return builder.ToString();
        }

        private HttpContext HttpContext
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    return HttpContext.Current;
                }
                if (!_stubHttpContext.IsValueCreated)
                {
                    SimpleWorkerRequest wr;
                    try
                    {
                        wr = new SimpleWorkerRequest("/", AppDomain.CurrentDomain.BaseDirectory, "/", string.Empty, TextWriter.Null);
                    }
                    catch (HttpException)
                    {
                        wr = new SimpleWorkerRequest("/", string.Empty, TextWriter.Null);
                    }
                    _stubHttpContext.Value = new HttpContext(wr);
                }
                return _stubHttpContext.Value;
            }
        }

        private string LockIds()
        {
            Dictionary<Guid, object> lockIds =
                HttpContext.Items["ContactLockIds"] as Dictionary<Guid, object>;

            StringBuilder builder = new StringBuilder();

            if (lockIds != null)
            {
                foreach (var key in lockIds.Keys)
                {
                    builder.Append($"{key} - {lockIds[key]}");
                }
            }

            return builder.ToString();
        }

        public DetailedSessionDiagnostics(bool loggingEnabled) : base(loggingEnabled)
        {
        }
    }
}
