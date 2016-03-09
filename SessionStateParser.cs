using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.SessionState;
using StackExchange.Redis;

namespace TrueClarity.SessionProvider.Redis
{
    public class SessionStateParser
    {
        internal static ISessionStateItemCollection GetSessionDataStatic(HashEntry[] rowDataFromRedis)
        {
            ISessionStateItemCollection sessionData = null;
            sessionData = new ChangeTrackingSessionStateItemCollection();
            for (int i = 0; i < rowDataFromRedis.Length; i ++)
            {
                string key = rowDataFromRedis[i].Name;

                object val = GetObjectFromBytes(rowDataFromRedis[i].Value);

                if (key != null)
                {
                    sessionData[key] = val;
                }
            }
            
            return sessionData;
        }

        private static object GetObjectFromBytes(byte[] dataAsBytes)
        {
            if (dataAsBytes == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(dataAsBytes, 0, dataAsBytes.Length))
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                object retObject = binaryFormatter.Deserialize(memoryStream);

                if (retObject.GetType() == typeof(RedisNull))
                {
                    return null;
                }
                return retObject;
            }
        }
    }
}