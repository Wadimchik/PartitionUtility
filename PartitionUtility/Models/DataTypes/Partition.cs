namespace PartitionUtility
{
    public class Partition
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public long EntryCount { get; set; }
        public string FirstEdit { get; set; }
        public string LastEdit { get; set; }
        public string CreateTime { get; set; }
        public long DiskSpace { get; set; }
    }
}
