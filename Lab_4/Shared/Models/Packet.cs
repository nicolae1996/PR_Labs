using System.Collections.Generic;
using Shared.Enums;

namespace Shared.Models
{
    public class Packet
    {
        /// <summary>
        /// Packet type
        /// </summary>
        public PacketType Type { get; set; }

        /// <summary>
        /// Bool result
        /// </summary>
        public bool IsSuccessResult { get; set; }

        /// <summary>
        /// Error
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Token
        /// </summary>
        public string Token { get; set; } = "";

        /// <summary>
        /// Data
        /// </summary>
        public Dictionary<string, string> Data { get; set; }
    }
}
