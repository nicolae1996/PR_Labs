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
        /// Token
        /// </summary>
        public string Token { get; set; } = "";

        /// <summary>
        /// Data
        /// </summary>
        public Dictionary<string, string> Data { get; set; }
    }
}
