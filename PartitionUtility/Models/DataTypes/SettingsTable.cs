using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PartitionUtility
{
    [Serializable]
    public class SettingsTable : INotifyPropertyChanged
    {
        #region Properties

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

        private int avgEntrySize;
        public int AvgEntrySize
        {
            get { return avgEntrySize; }
            set
            {
                avgEntrySize = value;
                NotifyPropertyChanged();
            }
        }

        private int entrySize;
        public int EntrySize
        {
            get { return entrySize; }
            set
            {
                entrySize = value;
                NotifyPropertyChanged();
            }
        }

        private BindingList<string> dateTimeColumns = new BindingList<string>();
        public BindingList<string> DateTimeColumns
        {
            get { return dateTimeColumns; }
            set
            {
                dateTimeColumns = value;
                NotifyPropertyChanged();
            }
        }

        private string selectedDateTimeColumn;
        public string SelectedDateTimeColumn
        {
            get { return selectedDateTimeColumn; }
            set
            {
                selectedDateTimeColumn = value;
                NotifyPropertyChanged();
            }
        }

        private bool partitionDisabled;
        public bool PartitionDisabled
        {
            get { return partitionDisabled; }
            set
            {
                partitionDisabled = value;
                NotifyPropertyChanged();
            }
        }

        private bool dontPartition;
        public bool DontPartition
        {
            get { return dontPartition; }
            set
            {
                dontPartition = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
