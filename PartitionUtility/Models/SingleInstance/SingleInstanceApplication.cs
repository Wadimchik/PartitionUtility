using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PartitionUtility
{
    public class SingleInstanceApplication : Application
    {
        private bool _contentLoaded;
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent()
        {
            if (_contentLoaded)
            {
                return;
            }
            _contentLoaded = true;
           
            System.Uri resourceLocater = new System.Uri("/PartitionUtility;component/app.xaml", System.UriKind.Relative);
            System.Windows.Application.LoadComponent(this, resourceLocater);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Revision == 0)
            {
                Application.Current.Resources["MainWindowTitle"] = string.Format("Утилита Секционирования {0}.{1}.{2}", version.Major, version.Minor, version.Build);
                Application.Current.Resources["SettingsWindowTitle"] = string.Format("Утилита Секционирования {0}.{1}.{2} - Настройки", version.Major, version.Minor, version.Build);
                Application.Current.Resources["ImportWindowTitle"] = string.Format("Утилита Секционирования {0}.{1}.{2} - Импорт конфигурации из БД", version.Major, version.Minor, version.Build);
            }
            else
            {
                Application.Current.Resources["MainWindowTitle"] = string.Format("Утилита Секционирования {0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
                Application.Current.Resources["SettingsWindowTitle"] = string.Format("Утилита Секционирования {0}.{1}.{2}.{3} - Настройки", version.Major, version.Minor, version.Build, version.Revision);
                Application.Current.Resources["ImportWindowTitle"] = string.Format("Утилита Секционирования {0}.{1}.{2}.{3} - Импорт конфигурации из БД", version.Major, version.Minor, version.Build, version.Revision);
            }

            SplashScreenManager.Create(() => new SplashWindow(), new DXSplashScreenViewModel { Subtitle = version.Revision == 0 ? string.Format("вер. {0}.{1}.{2}", version.Major, version.Minor, version.Build) : string.Format("вер. {0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision) }).ShowOnStartup();

            ApplicationThemeHelper.ApplicationThemeName = Theme.MetropolisDarkName;

            if (!Directory.Exists("Config")) Directory.CreateDirectory("Config");

            LogHelper.Log(LogHelper.Status.Ok, "Запуск приложения.");

            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotKeyboardFocusEvent, new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.MouseDoubleClickEvent, new RoutedEventHandler(SelectAllText));

            base.OnStartup(e);

            var window = new MainWindow();
            window.Show();
        }

        public void Activate()
        {
            MainWindow.Activate();
        }

        void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox)) parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                if (!textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null) textBox.SelectAll();
        }
    }
}
