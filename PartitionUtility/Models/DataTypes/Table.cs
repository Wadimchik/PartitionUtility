using System.Collections.ObjectModel;

namespace PartitionUtility
{
    public class Table
    {
        public string Name { get; set; }
        public string DateTimeColumn { get; set; }

        public ObservableCollection<Partition> Partitions { get; set; } = new ObservableCollection<Partition>();
        public ObservableCollection<StoredProcedure> Procedures { get; set; } = new ObservableCollection<StoredProcedure> { };
        public ObservableCollection<Event> Events { get; set; } = new ObservableCollection<Event> { };
    }
}
