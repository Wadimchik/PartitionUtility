using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PartitionUtility
{
    public class AboutWindowVM : INotifyPropertyChanged
    {
        private string versionText;
        public string VersionText
        {
            get { return versionText; }
            set
            {
                versionText = value;
                NotifyPropertyChanged();
            }
        }

        private RelayCommand<RoutedEventArgs> buttonClickCommand;
        public RelayCommand<RoutedEventArgs> ButtonClickCommand
        {
            get
            {
                return buttonClickCommand ?? (buttonClickCommand = new RelayCommand<RoutedEventArgs>(OnButtonClicked, (o) => { return true; }));
            }
        }

        public AboutWindowVM()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Revision == 0) VersionText = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            else VersionText = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        private static void OnButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Окно \"О программе\". Нажата кнопка \"" + (e.Source as Button).Content.ToString() + "\".");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
