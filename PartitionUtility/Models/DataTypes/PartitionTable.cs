using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PartitionUtility
{
    [Serializable]
    public class PartitionTable : INotifyPropertyChanged
    {
        #region Properties

        private int intervalMeasureSelectedItem;
        public int IntervalMeasureSelectedItem
        {
            get { return intervalMeasureSelectedItem; }
            set
            {
                intervalMeasureSelectedItem = value;
                NotifyPropertyChanged();

                if (!MainWindowVM.SetInterval) CalculateIntervalVal();
                if (!MainWindowVM.SetEntryCount) CalculateEntryCount();
                if (!MainWindowVM.SetTableSize) CalculateTableSize();
                if (!MainWindowVM.SetDepth)
                {
                    CalculateDepthVal();
                    if (!MainWindowVM.SetSectorCount) CalculateSectorCount();
                    if (!MainWindowVM.SetSectorSize) CalculateSectorSizeVal();
                }
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

                if (!MainWindowVM.SetSectorSize) CalculateSectorSizeVal();
                if (!MainWindowVM.SetSectorCount) CalculateSectorCount();
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

                if (!MainWindowVM.SetDepth) CalculateDepthVal();
                if (!MainWindowVM.SetEntryCount) CalculateEntryCount();
                if (!MainWindowVM.SetTableSize) CalculateTableSize();
                if (!MainWindowVM.SetInterval) CalculateIntervalVal();
                if (!MainWindowVM.SetSectorCount) CalculateSectorCount();
                if (!MainWindowVM.SetSectorSize) CalculateSectorSizeVal();
            }
        }

        private int partitionIntervalVal;
        public int PartitionIntervalVal
        {
            get { return partitionIntervalVal; }
            set
            {
                partitionIntervalVal = value;
                NotifyPropertyChanged();
            }
        }

        private int partitionIntervalMeasureSelectedItem;
        public int PartitionIntervalMeasureSelectedItem
        {
            get { return partitionIntervalMeasureSelectedItem; }
            set
            {
                partitionIntervalMeasureSelectedItem = value;
                NotifyPropertyChanged();
            }
        }

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

        private string dateTimeColumn;
        public string DateTimeColumn
        {
            get { return dateTimeColumn; }
            set
            {
                dateTimeColumn = value;
                NotifyPropertyChanged();
            }
        }

        private long entrySize;
        public long EntrySize
        {
            get { return entrySize; }
            set
            {
                entrySize = value;
                NotifyPropertyChanged();
            }
        }

        private long intervalVal;
        public long IntervalVal
        {
            get { return intervalVal; }
            set
            {
                intervalVal = value;
                NotifyPropertyChanged();
            }
        }

        private long sectorSizeVal;
        public long SectorSizeVal
        {
            get { return sectorSizeVal; }
            set
            {
                sectorSizeVal = value;
                NotifyPropertyChanged();
            }
        }

        private long sectorCount;
        public long SectorCount
        {
            get { return sectorCount; }
            set
            {
                sectorCount = value;
                NotifyPropertyChanged();
            }
        }

        private long depthVal;
        public long DepthVal
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

        private decimal tableSize;
        public decimal TableSize
        {
            get { return this.tableSize; }
            set
            {
                this.tableSize = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public void CalculateTableSize()
        {
            decimal temp = 0;
            try
            {
                temp = (decimal)(EntryCount * EntrySize) / (1024 * 1024);
            }
            catch { }
            TableSize = temp <= 0 ? 1 : temp;
        }

        public void CalculateEntryCount()
        {
            long temp = 0;
            try
            {
                temp = (Int64)((1 / Convert.ToDecimal(IntervalVal)) * MeasureComparer.Convert(IntervalMeasureSelectedItem) * DepthVal * MeasureComparer.ConvertToHours(DepthMeasureSelectedItem));
            }
            catch { }
            EntryCount = temp <= 0 ? 1 : temp;
        }

        public void CalculateSectorSizeVal()
        {
            long temp = 0;
            try
            {
                temp = (long)Math.Ceiling(MeasureComparer.ScaleValue(DepthVal / Convert.ToDecimal(SectorCount), SectorSizeMeasureSelectedItem, DepthMeasureSelectedItem));
            }
            catch { }
            SectorSizeVal = temp <= 0 ? 1 : temp;
        }

        public void CalculateSectorCount()
        {
            long temp = 0;
            try
            {
                temp = (long)Math.Ceiling(DepthVal / MeasureComparer.ScaleValue(Convert.ToDecimal(SectorSizeVal), DepthMeasureSelectedItem, SectorSizeMeasureSelectedItem));
            }
            catch { }
            SectorCount = temp <= 0 ? 1 : temp;
        }

        public void CalculateDepthVal()
        {
            long temp = 0;
            try
            {
                temp = (long)Math.Ceiling((TableSize * 1024 * 1024) / (EntrySize * (1 / Convert.ToDecimal(IntervalVal)) * MeasureComparer.Convert(IntervalMeasureSelectedItem) * MeasureComparer.ConvertToHours(DepthMeasureSelectedItem)));
            }
            catch { }
            DepthVal = temp <= 0 ? 1 : temp;
        }

        public void CalculateIntervalVal()
        {
            long temp = 0;
            try
            {
                temp = (long)((EntrySize * MeasureComparer.Convert(IntervalMeasureSelectedItem) * Convert.ToDecimal(DepthVal) * MeasureComparer.ConvertToHours(DepthMeasureSelectedItem)) / (TableSize * 1024 * 1024));
            }
            catch { }
            IntervalVal = temp <= 0 ? 1 : temp;
        }

        #endregion

        #region Event handlers

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        #endregion
    }
}
