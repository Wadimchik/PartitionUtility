using DevExpress.Xpf.Core;
using DevExpress.XtraRichEdit;
using MySqlConnector;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace PartitionUtility
{
    public class ImportWindowVM : INotifyPropertyChanged
    {
        public MainWindowVM MainVM { get; set; }

        private DBConfig selectedConfig;
        public DBConfig SelectedConfig
        {
            get { return selectedConfig; }
            set
            {
                selectedConfig = value;
                NotifyPropertyChanged();
            }
        }

        public ObservableCollection<DBConfig> Configs { get; set; } = new ObservableCollection<DBConfig>();

        private RelayCommand<RoutedEventArgs> buttonClickCommand;
        public RelayCommand<RoutedEventArgs> ButtonClickCommand
        {
            get
            {
                return buttonClickCommand ?? (buttonClickCommand = new RelayCommand<RoutedEventArgs>(OnButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<Window> importButtonCommand;
        public RelayCommand<Window> ImportButtonCommand
        {
            get
            {
                return importButtonCommand ?? (importButtonCommand = new RelayCommand<Window>(ImportButtonClicked, (o) => { return SelectedConfig != null; }));
            }
        }

        private RelayCommand<object> deleteButtonCommand;
        public RelayCommand<object> DeleteButtonCommand
        {
            get
            {
                return deleteButtonCommand ?? (deleteButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Удалить конфигурацию из БД", "Вы уверены, что хотите удалить конфигурацию " + SelectedConfig.Name + " из БД?", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.OK)
                    {
                        try
                        {
                            using (MySqlConnection conn = new MySqlConnection("server=" + MainVM.IP + ";" + "port=" + MainVM.Port + ";" + "user=" + MainVM.Username + ";" + "password=" + MainVM.Password + ";" + "database=" + MainVM.Database))
                            {
                                conn.Open();
                                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                                {
                                    using (MySqlCommand command = new MySqlCommand("DELETE FROM `" + MainVM.PartitionConfig + "` WHERE name = '" + SelectedConfig.Name + "'", conn))
                                        command.ExecuteNonQuery();

                                    Configs.Remove(SelectedConfig);
                                    if (Configs.Count > 0) SelectedConfig = Configs.Last();
                                }
                                else
                                {
                                    MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось удалить конфигурацию из БД. Нет соединения с базой данных.", Duration = "0 мс" });
                                    LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить конфигурацию из БД. Нет соединения с базой данных.");
                                }
                                conn.Close();
                            }
                        }
                        catch (MySqlException ex)
                        {
                            MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось удалить конфигурацию из БД." + ex.Message, Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить конфигурацию из БД.", ex);
                        }
                    }
                }, (o) => { return SelectedConfig != null; }));
            }
        }

        public ImportWindowVM()
        {
            MainVM = App.Current.MainWindow.DataContext as MainWindowVM;
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + MainVM.IP + ";" + "port=" + MainVM.Port + ";" + "user=" + MainVM.Username + ";" + "password=" + MainVM.Password + ";" + "database=" + MainVM.Database))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        bool isPartitionConfig = false;
                        using (MySqlCommand command = new MySqlCommand("SHOW COLUMNS FROM `" + MainVM.PartitionConfig + "` LIKE 'name';", conn))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                isPartitionConfig = reader.HasRows;
                            }
                            if (isPartitionConfig)
                            {
                                command.CommandText = "SELECT * FROM `" + MainVM.PartitionConfig + "`";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        Configs.Add(new DBConfig { ID = reader.GetInt32("ID"), Name = reader.GetString("name"), Time = DateTime.ParseExact(reader.GetDateTime("updated").ToString("dd.MM.yyyy HH:mm:ss:fff"), "dd.MM.yyyy HH:mm:ss:fff", null) });
                                    }
                                }
                            }
                            else
                            {
                                MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось прочитать конфигурацию из БД. Таблица " + MainVM.PartitionConfig + " не является таблицей для хранения конфигураций.", Duration = "0 мс" });
                                LogHelper.Log(LogHelper.Status.Error, "Не удалось прочитать конфигурацию из БД. Таблица " + MainVM.PartitionConfig + " не является таблицей для хранения конфигураций.");
                            }
                        }
                    }
                    else
                    {
                        MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось прочитать конфигурацию из БД. Нет соединения с базой данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось прочитать конфигурацию из БД. Нет соединения с базой данных.");
                    }
                    conn.Close();
                } 
            }
            catch (MySqlException ex)
            {
                MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось прочитать конфигурацию из БД." + ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, "Не удалось прочитать конфигурацию из БД.", ex);
            }
            if (Configs.Count > 0) SelectedConfig = Configs[Configs.Count - 1];
        }

        private static void ImportButtonClicked(Window window)
        {
            ImportWindowVM importWindowVM = window.DataContext as ImportWindowVM;
            var result = ThemedMessageBox.Show("Импорт конфигурации из БД", "Импортировать конфигурацию " + importWindowVM.SelectedConfig.Name + "? Текущая конфигурация будет перезаписана.", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (importWindowVM.MainVM.SaveEnabled)
                    {
                        var res = ThemedMessageBox.Show("Импорт конфигурации из БД", "Конфигурация секционирования не сохранена. Сохранить конфигурацию?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (res == MessageBoxResult.Yes)
                        {
                            if (importWindowVM.MainVM.FileName == "Новая конфигурация")
                            {
                                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                                dlg.DefaultExt = ".cfg";
                                dlg.Filter = "Файлы конфигурации (*.cfg)|*.cfg";
                                dlg.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

                                Nullable<bool> resultSave = dlg.ShowDialog();

                                if (resultSave == true)
                                {
                                    importWindowVM.MainVM.FileName = dlg.FileName;
                                    importWindowVM.MainVM.LastFileName = importWindowVM.MainVM.FileName;

                                    Config config = new Config();
                                    config.IP = importWindowVM.MainVM.IP;
                                    config.Port = importWindowVM.MainVM.Port;
                                    config.DataBase = importWindowVM.MainVM.Database;
                                    config.UserName = importWindowVM.MainVM.Username;
                                    config.Password = Security.Protect(importWindowVM.MainVM.Password);
                                    config.PartitionLog = importWindowVM.MainVM.PartitionLog;
                                    config.PartitionConfig = importWindowVM.MainVM.PartitionConfig;
                                    foreach (PartitionTable table in importWindowVM.MainVM.PartitionTables) config.PartitionTables.Add(table);
                                    foreach (SettingsTable table in importWindowVM.MainVM.Tables) config.Tables.Add(table);

                                    XmlSerializer formatter = new XmlSerializer(typeof(Config));

                                    using (FileStream stream = new FileStream(importWindowVM.MainVM.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) formatter.Serialize(stream, config);

                                    string hash = Checker.GetFileHash(importWindowVM.MainVM.FileName);
                                    using (StreamWriter sw = new StreamWriter(importWindowVM.MainVM.FileName, true)) sw.Write("\r\nCRC:" + hash);

                                    ((MainWindow)Application.Current.MainWindow).richEditControl1.LoadDocument(importWindowVM.MainVM.FileName, DocumentFormat.PlainText);

                                    importWindowVM.MainVM.SaveEnabled = false;

                                    importWindowVM.MainVM.LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Файл конфигурации успешно сохранен " + importWindowVM.MainVM.FileName + ".", Duration = "0 мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Файл конфигурации успешно сохранен " + importWindowVM.MainVM.FileName + ".");
                                }
                            }
                            else importWindowVM.MainVM.SaveButtonCommand.Execute(1);
                        }
                    }
                    using (MySqlConnection conn = new MySqlConnection("server=" + importWindowVM.MainVM.IP + ";" + "port=" + importWindowVM.MainVM.Port + ";" + "user=" + importWindowVM.MainVM.Username + ";" + "password=" + importWindowVM.MainVM.Password + ";" + "database=" + importWindowVM.MainVM.Database))
                    {
                        conn.Open();
                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            UInt32 fileSize = 0;
                            string configContent = "";

                            using (MySqlCommand command = new MySqlCommand("SELECT OCTET_LENGTH(config) FROM `" + importWindowVM.MainVM.PartitionConfig + "` WHERE name = '" + importWindowVM.SelectedConfig.Name + "'", conn))
                            {
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        fileSize = reader.GetUInt32(0);
                                    }
                                }
                                command.CommandText = "SELECT * FROM `" + importWindowVM.MainVM.PartitionConfig + "` WHERE name = '" + importWindowVM.SelectedConfig.Name + "'";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        byte[] bytes = new byte[fileSize];
                                        reader.GetBytes(reader.GetOrdinal("config"), 0, bytes, 0, (int)fileSize);
                                        configContent = Encoding.UTF8.GetString(bytes);
                                    }
                                }
                                importWindowVM.MainVM.ExportConfig(configContent, importWindowVM.SelectedConfig.Name);
                            }
                        }
                        else
                        {
                            importWindowVM.MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось прочитать конфигурацию из БД. Нет соединения с базой данных.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось прочитать конфигурацию из БД. Нет соединения с базой данных.");
                        }
                        conn.Close();
                    }
                    window.Close();
                }
                catch (Exception ex)
                {
                    importWindowVM.MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось прочитать конфигурацию из БД." + ex.Message, Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Error, "Не удалось прочитать конфигурацию из БД.", ex);
                }
            }
        }

        private static void OnButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Окно \"Экспорт конфигурации из БД\". Нажата кнопка \"" + (e.Source as Button).Content.ToString() + "\".");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
