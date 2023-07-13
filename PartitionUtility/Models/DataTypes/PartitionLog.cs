using System;

namespace PartitionUtility
{
    public class PartitionLog
    {
        public UInt32 ID { get; set; }
        public string Time { get; set; }
        public string Status { get; set; }
        public string Event { get; set; }
        public string Message { get; set; }
        public string Duration { get; set; }
    }
}
