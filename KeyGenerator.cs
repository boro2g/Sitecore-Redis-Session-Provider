//
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
//

using System;

namespace TrueClarity.SessionProvider.Redis
{
    internal class KeyGenerator
    {
        private string id;
        public string DataKey { get; private set; }
        public string LockKey { get; private set; }
        public string InternalKey { get; private set; }
        public string TimeoutKey { get; private set; }

        public KeyGenerator(string id, string applicationName)
        {
            this.id = id;
            DataKey = "{" + applicationName + "_" + id + "}_Data";
            LockKey = "{" + applicationName + "_" + id + "}_Write_Lock";
            InternalKey = "{" + applicationName + "_" + id + "}_Internal";
            TimeoutKey = GenerateTimeoutKey(id, applicationName);
        }

        public static string GenerateTimeoutKey(string id, string applicationName)
        {
            return "{" + applicationName + "_" + id + "}_Timeout";
        }

        public void RegenerateKeyStringIfIdModified(string id, string applicationName)
        {
            if (!id.Equals(this.id))
            {
                this.id = id;
                DataKey = "{" + applicationName + "_" + id + "}_Data";
                LockKey = "{" + applicationName + "_" + id + "}_Write_Lock";
                InternalKey = "{" + applicationName + "_" + id + "}_Internal";
                TimeoutKey = GenerateTimeoutKey(id, applicationName);
            }
        }

        internal static string FormatDateTimeKey(DateTime now)
        {
            return now.ToString("yyyy MM dd HH:mm:ss");
        }

        internal static string NowKey(string now)
        {
            return $"{now}_Marker";
        }

        internal static string FormatNowKey(DateTime now)
        {
            return $"{FormatDateTimeKey(now)}_Marker";
        }
    }
}
