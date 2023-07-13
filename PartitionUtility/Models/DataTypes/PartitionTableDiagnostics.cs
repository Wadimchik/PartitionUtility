using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PartitionUtility
{
    public class PartitionTableDiagnostics : INotifyPropertyChanged
    {
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                NotifyPropertyChanged();
            }
        }

        private long avgEntrySize;
        public long AvgEntrySize
        {
            get { return avgEntrySize; }
            set
            {
                avgEntrySize = value;
                NotifyPropertyChanged();
            }
        }

        private int sectorCount;
        public int SectorCount
        {
            get { return sectorCount; }
            set
            {
                sectorCount = value;
                NotifyPropertyChanged();
            }
        }

        private decimal depthVal;
        public decimal DepthVal
        {
            get { return depthVal; }
            set
            {
                depthVal = value;
                NotifyPropertyChanged();
            }
        }

        private long entryCount;
        public long EntryCount
        {
            get { return entryCount; }
            set
            {
                entryCount = value;
                NotifyPropertyChanged();
            }
        }

        private int sectorSizeMeasureSelectedItem;
        public int SectorSizeMeasureSelectedItem
        {
            get { return sectorSizeMeasureSelectedItem; }
            set
            {
                sectorSizeMeasureSelectedItem = value;
                NotifyPropertyChanged();
            }
        }

        private int depthMeasureSelectedItem;
        public int DepthMeasureSelectedItem
        {
            get { return depthMeasureSelectedItem; }
            set
            {
                depthMeasureSelectedItem = value;
                NotifyPropertyChanged();
            }
        }

        private decimal tableSize;
        public decimal TableSize
        {
            get { return tableSize; }
            set
            {
                tableSize = value;
                NotifyPropertyChanged();
            }
        }

        public static bool operator == (PartitionTableDiagnostics trend1, PartitionTableDiagnostics trend2)
        {
            if (ReferenceEquals(trend1, null) || ReferenceEquals(trend2, null)) return ReferenceEquals(trend1, trend2);
            return trend1.Name == trend2.Name && trend1.SectorCount == trend2.SectorCount && trend1.DepthVal == trend2.DepthVal && trend1.EntryCount == trend2.EntryCount && trend1.SectorSizeMeasureSelectedItem == trend2.SectorSizeMeasureSelectedItem && trend1.DepthMeasureSelectedItem == trend2.DepthMeasureSelectedItem && trend1.TableSize == trend2.TableSize;
        }

        public static bool operator != (PartitionTableDiagnostics trend1, PartitionTableDiagnostics trend2)
        {
            return !(trend1 == trend2);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
