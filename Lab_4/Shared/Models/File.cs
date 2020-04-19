using System;

namespace Shared.Models
{
    public class File
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public int Size { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Type { get; set; }
    }
}
