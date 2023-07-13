using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace PartitionUtility
{
    public class QueryTextWindowVM : INotifyPropertyChanged
    {
        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
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

        private static void OnButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Окно \"Текст запроса\". Нажата кнопка \"" + (e.Source as Button).Content.ToString() + "\".");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
