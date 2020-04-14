using System;

namespace Shared
{
    public static class GlobalResources
    {
        /// <summary>
        /// Secret key
        /// </summary>
        public const string SecretKey = "CustomProtocol";


        /// <summary>
        /// Timeout
        /// </summary>
        public static int Timeout = (int)TimeSpan.FromSeconds(100).TotalMilliseconds;

        public static class CommonKeys
        {
            public static string Authentication = "authentication";
        }
    }
}
