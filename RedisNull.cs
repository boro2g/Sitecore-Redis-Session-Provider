using System;
using System.Runtime.Serialization;

namespace TrueClarity.SessionProvider.Redis
{
    [Serializable]
    internal class RedisNull : ISerializable
    {
        public RedisNull() 
        {}
        protected RedisNull(SerializationInfo info, StreamingContext context)
        {}
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {}
    } 
}
