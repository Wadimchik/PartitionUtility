using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.Xpf.Core;
using MySqlConnector;
using DevExpress.Xpf.Editors;
using System.Runtime.CompilerServices;
using DevExpress.Xpf.Grid;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using DevExpress.Xpf.Bars;
using System.Collections.ObjectModel;

namespace PartitionUtility
{
    public class SettingsWindowVM : INotifyPropertyChanged
    {
        public MainWindowVM MainVM { get; set; }

        public BindingList<SettingsTable> Tables { get; set; } = new BindingList<SettingsTable>();
        public ObservableCollection<string> TableNames { get; set; } = new ObservableCollection<string>();

        private bool reconnect;
        private bool exportLastConfig;
        private bool readPartitionLog;

        private SettingsTable selectedTable;
        public SettingsTable SelectedTable
        {
            get { return selectedTable; }
            set
            {
                selectedTable = value;
                NotifyPropertyChanged();
            }
        }

        private int selectedTabIndex;
        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                selectedTabIndex = value;
                if (value == 1) ReadTables();
            }
        }

        private bool saveEnabled;
        public bool SaveEnabled
        {
            get { return saveEnabled; }
            set
            {
                saveEnabled = value;
                NotifyPropertyChanged();
            }
        }

        private string ip;
        public string IP
        {
            get { return ip; }
            set
            {
                ip = value;
                reconnect = true;
                NotifyPropertyChanged();
            }
        }

        private string port;
        public string Port
        {
            get { return port; }
            set
            {
                port = value;
                reconnect = true;
                NotifyPropertyChanged();
            }
        }

        private string database;
        public string Database
        {
            get { return database; }
            set
            {
                database = value;
                reconnect = true;
                NotifyPropertyChanged();
            }
        }

        private string username;
        public string Username
        {
            get { return username; }
            set
            {
                username = value;
                reconnect = true;
                NotifyPropertyChanged();
            }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                SaveEnabled = true;
                password = value;
                reconnect = true;
                NotifyPropertyChanged();
            }
        }

        private string partitionLog;
        public string PartitionLog
        {
            get { return partitionLog; }
            set
            {
                partitionLog = value;
                readPartitionLog = true;
                NotifyPropertyChanged();
            }
        }

        private string partitionConfig;
        public string PartitionConfig
        {
            get { return partitionConfig; }
            set
            {
                partitionConfig = value;
                exportLastConfig = true;
                NotifyPropertyChanged();
            }
        }

        private string dumpExePath;
        public string DumpExePath
        {
            get { return dumpExePath; }
            set
            {
                dumpExePath = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Обработчики событий взаимодействия с контролами для логгирования действий пользователя в файл
        /// </summary>

        private RelayCommand<RoutedEventArgs> buttonClickCommand;
        public RelayCommand<RoutedEventArgs> ButtonClickCommand
        {
            get
            {
                return buttonClickCommand ?? (buttonClickCommand = new RelayCommand<RoutedEventArgs>(OnButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<ItemClickEventArgs> contextMenuButtonClickCommand;
        public RelayCommand<ItemClickEventArgs> ContextMenuButtonClickCommand
        {
            get
            {
                return contextMenuButtonClickCommand ?? (contextMenuButtonClickCommand = new RelayCommand<ItemClickEventArgs>(OnContextMenuButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<RoutedEventArgs> dumpExePathButtonClickCommand;
        public RelayCommand<RoutedEventArgs> DumpExePathButtonClickCommand
        {
            get
            {
                return dumpExePathButtonClickCommand ?? (dumpExePathButtonClickCommand = new RelayCommand<RoutedEventArgs>(OnDumpExePathButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<DevExpress.Xpf.Docking.Base.SelectedItemChangedEventArgs> tabChangedCommand;
        public RelayCommand<DevExpress.Xpf.Docking.Base.SelectedItemChangedEventArgs> TabChangedCommand
        {
            get
            {
                return tabChangedCommand ?? (tabChangedCommand = new RelayCommand<DevExpress.Xpf.Docking.Base.SelectedItemChangedEventArgs>(OnTabChanged, (o) => { return true; }));
            }
        }

        private RelayCommand<TextChangedEventArgs> textBoxTextChangedCommand;
        public RelayCommand<TextChangedEventArgs> TextBoxTextChangedCommand
        {
            get
            {
                return textBoxTextChangedCommand ?? (textBoxTextChangedCommand = new RelayCommand<TextChangedEventArgs>(OnTextBoxTextChanged, (o) => { return true; }));
            }
        }

        private RelayCommand<EditValueChangedEventArgs> textEditValueChangedCommand;
        public RelayCommand<EditValueChangedEventArgs> TextEditValueChangedCommand
        {
            get
            {
                return textEditValueChangedCommand ?? (textEditValueChangedCommand = new RelayCommand<EditValueChangedEventArgs>(OnTextEditValueChanged, (o) => { return true; }));
            }
        }

        private RelayCommand<MouseButtonEventArgs> tableMouseDownCommand;
        public RelayCommand<MouseButtonEventArgs> TableMouseDownCommand
        {
            get
            {
                return tableMouseDownCommand ?? (tableMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(OnTableMouseDown, (o) => { return true; }));
            }
        }

        private RelayCommand<System.Windows.Input.KeyEventArgs> tablePreviewKeyDownCommand;
        public RelayCommand<System.Windows.Input.KeyEventArgs> TablePreviewKeyDownCommand
        {
            get
            {
                return tablePreviewKeyDownCommand ?? (tablePreviewKeyDownCommand = new RelayCommand<System.Windows.Input.KeyEventArgs>(OnTablePreviewKeyDown, (o) => { return true; }));
            }
        }

        /// <summary>
        /// Команда кнопки "Сохранить"
        /// </summary>

        private RelayCommand<Window> saveButtonCommand;
        public RelayCommand<Window> SaveButtonCommand
        {
            get
            {
                return saveButtonCommand ?? (saveButtonCommand = new RelayCommand<Window>(OnSaveButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<object> testButtonCommand;
        public RelayCommand<object> TestButtonCommand
        {
            get
            {
                return testButtonCommand ?? (testButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        using (var conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database))
                        {
                            conn.Open();
                            if (conn.Ping())
                            {
                                ThemedMessageBox.Show("Тест", "Подключение к БД с указанными параметрами успешно.", MessageBoxButton.OK, MessageBoxImage.Information);
                                LogHelper.Log(LogHelper.Status.Ok, "Окно \"Настройки\". Подключение к БД с указанными параметрами успешно: IP: " + IP + "; Порт: " + Port + "; База данных: " + Database + "; Имя пользователя: " + Username + ";");
                            }
                            else
                            {
                                ThemedMessageBox.Show("Тест", "Не удалось подключиться к БД с указанными параметрами.", MessageBoxButton.OK, MessageBoxImage.Error);
                                LogHelper.Log(LogHelper.Status.Error, "Окно \"Настройки\". Не удалось подключиться к БД с указанными параметрами: IP: " + IP + "; Порт: " + Port + "; База данных: " + Database + "; Имя пользователя: " + Username + ";");
                            }
                            conn.Close();
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                    }
                }));
            }
        }

        /// <summary>
        /// Команда кнопки "Выбрать папку с файлами СУБД"
        /// </summary>

        private RelayCommand<object> dumpExePathButtonCommand;
        public RelayCommand<object> DumpExePathButtonCommand
        {
            get
            {
                return dumpExePathButtonCommand ?? (dumpExePathButtonCommand = new RelayCommand<object>(obj =>
                {
                    FolderBrowserDialog dlg = new FolderBrowserDialog();

                    DialogResult result = dlg.ShowDialog();
                    if (result == DialogResult.OK) DumpExePath = dlg.SelectedPath + "\\";
                }));
            }
        }

        private RelayCommand<object> clearTableButtonCommand;
        public RelayCommand<object> ClearTableButtonCommand
        {
            get
            {
                return clearTableButtonCommand ?? (clearTableButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Очистка таблицы", "Вы уверены, что хотите очистить таблицу " + SelectedTable.Name + "?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            using (var conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database))
                            {
                                conn.Open();
                                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                                {
                                    using (MySqlCommand command = new MySqlCommand("TRUNCATE TABLE `" + SelectedTable.Name + "`", conn))
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    conn.Close();

                                    MainVM.LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Таблица " + SelectedTable.Name + " успешно очищена.", Duration = "0 мс" });
                                    ThemedMessageBox.Show("Очистка таблицы", "Таблица " + SelectedTable.Name + " успешно очищена.", MessageBoxButton.OK, MessageBoxImage.Information);
                                    LogHelper.Log(LogHelper.Status.Ok, "Окно \"Настройки\". Таблица " + SelectedTable.Name + " успешно очищена.");
                                }
                                else
                                {
                                    MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось очистить таблицу " + SelectedTable.Name + ". Нет соединения с базой данных.", Duration = "0 мс" });
                                    ThemedMessageBox.Show("Очистка таблицы", "Не удалось очистить таблицу " + SelectedTable.Name + ". Нет соединения с базой данных.", MessageBoxButton.OK, MessageBoxImage.Error);
                                    LogHelper.Log(LogHelper.Status.Error, "Не удалось очистить таблицу " + SelectedTable.Name + ". Нет соединения с базой данных.");
                                }
                            }
                        }
                        catch (MySqlException ex)
                        {
                            MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось очистить таблицу " + SelectedTable.Name + ". " + ex.Message, Duration = "0 мс" });
                            ThemedMessageBox.Show("Очистка таблицы", "Не удалось очистить таблицу " + SelectedTable.Name + ". " + ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось очистить таблицу " + SelectedTable.Name + ". " + ex.Message);
                        }
                    }
                }, (o) => { return SelectedTable != null; }));
            }
        }

        private RelayCommand<object> deleteTableButtonCommand;
        public RelayCommand<object> DeleteTableButtonCommand
        {
            get
            {
                return deleteTableButtonCommand ?? (deleteTableButtonCommand = new RelayCommand<object>(obj =>
                {
                    if (SelectedTable == null) return;
                    var result = ThemedMessageBox.Show("Удаление таблицы", "Вы уверены, что хотите удалить таблицу " + SelectedTable.Name + "?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        string name = SelectedTable.Name;
                        try
                        {
                            using (var conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database))
                            {
                                conn.Open();
                                if (conn != null && conn.State == System.Data.ConnectionState.Open)
                                {
                                    using (MySqlCommand command = new MySqlCommand("DROP TABLE `" + SelectedTable.Name + "`", conn))
                                    {
                                        command.ExecuteNonQuery();
                                    }
                                    conn.Close();
                                    Tables.Remove(SelectedTable);

                                    MainVM.LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Таблица " + name + " успешно удалена.", Duration = "0 мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Окно \"Настройки\". Таблица " + name + " успешно удалена.");
                                }
                                else
                                {
                                    MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось удалить таблицу " + name + ". Нет соединения с базой данных.", Duration = "0 мс" });
                                    ThemedMessageBox.Show("Удаление таблицы", "Не удалось удалить таблицу " + name + ". Нет соединения с базой данных.", MessageBoxButton.OK, MessageBoxImage.Error);
                                    LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить таблицу " + name + ". Нет соединения с базой данных.");
                                }
                            }
                        }
                        catch (MySqlException ex)
                        {
                            MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось удалить таблицу " + name + ". " + ex.Message, Duration = "0 мс" });
                            ThemedMessageBox.Show("Очистка таблицы", "Не удалось удалить таблицу " + name + ". " + ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить таблицу " + name + ". " + ex.Message);
                        }
                    }
                }, (o) => { return SelectedTable != null; }));
            }
        }

        /// <summary>
        /// Команда кнопки "Обновить"
        /// </summary>

        private RelayCommand<object> readServerButtonCommand;
        public RelayCommand<object> ReadServerButtonCommand
        {
            get
            {
                return readServerButtonCommand ?? (readServerButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        using (var conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database))
                        {
                            conn.Open();
                            if (conn != null && conn.State == System.Data.ConnectionState.Open)
                            {
                                List<string> serverTables = new List<string>();
                                List<SettingsTable> tablesToRemove = new List<SettingsTable>();
                                List<string> tableColumns = new List<string>();
                                TableNames.Clear();

                                using (MySqlCommand command = new MySqlCommand("SHOW TABLE STATUS", conn))
                                {
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            string name = reader.GetString(0);
                                            int avgEntrySize = reader.GetInt32(reader.GetOrdinal("Avg_row_length"));
                                            SettingsTable table = Tables.Where(x => x.Name == name).FirstOrDefault();

                                            serverTables.Add(name);
                                            if (table != null) table.AvgEntrySize = avgEntrySize;
                                            else Tables.Add(new SettingsTable { Name = name, AvgEntrySize = avgEntrySize, DontPartition = false });
                                        }
                                    }
                                }

                                foreach (SettingsTable table in Tables)
                                {
                                    TableNames.Add(table.Name);
                                    if (!serverTables.Any(x => table.Name == x)) tablesToRemove.Add(table);
                                    else
                                    {
                                        using (MySqlCommand command = new MySqlCommand("SHOW COLUMNS FROM `" + table.Name + "`", conn))
                                        {
                                            using (MySqlDataReader reader = command.ExecuteReader())
                                            {
                                                while (reader.Read())
                                                {
                                                    string name = reader.GetString(reader.GetOrdinal("Field"));
                                                    if (reader.GetString(reader.GetOrdinal("Type")).Contains("datetime"))
                                                    {
                                                        tableColumns.Add(name);
                                                        if (!table.DateTimeColumns.Contains(name)) table.DateTimeColumns.Add(name);
                                                    }
                                                }
                                            }
                                        }
                                        string columnToRemove = table.DateTimeColumns.Where(x => !tableColumns.Contains(x)).FirstOrDefault();
                                        if (columnToRemove != null) table.DateTimeColumns.Remove(columnToRemove);
                                        if (String.IsNullOrEmpty(table.SelectedDateTimeColumn)) table.SelectedDateTimeColumn = table.DateTimeColumns.FirstOrDefault();
                                    }
                                }
                                foreach (SettingsTable table in tablesToRemove) Tables.Remove(table);

                                string query = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Table_CalculateMaxRowSize.sql")).Replace("database", Database);
                                using (MySqlCommand command = new MySqlCommand(query, conn))
                                {
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            string name = reader.GetString(0);
                                            SettingsTable table = Tables.Where(x => x.Name == name).FirstOrDefault();
                                            if (table != null) table.EntrySize = reader.GetInt32(1);
                                        }
                                    }
                                }

                                MainVM.LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Обновление списка таблиц из сервера успешно.", Duration = "0 мс" });
                                ThemedMessageBox.Show("Обновление списка таблиц из БД", "Обновление списка таблиц из сервера успешно.", MessageBoxButton.OK, MessageBoxImage.Information);
                                LogHelper.Log(LogHelper.Status.Ok, "Окно \"Настройки\". Обновление списка таблиц из сервера успешно.");
                            }
                            else
                            {
                                MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось обновить список таблиц из сервера. Нет соединения с базой данных.", Duration = "0 мс" });
                                ThemedMessageBox.Show("Обновление списка таблиц из БД", "Не удалось обновить список таблиц из сервера. Нет соединения с базой данных.", MessageBoxButton.OK, MessageBoxImage.Error);
                                LogHelper.Log(LogHelper.Status.Error, "Не удалось обновить список таблиц из сервера. Нет соединения с базой данных.");
                            }
                            conn.Close();
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MainVM.LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось обновить список таблиц из сервера." + ex.Message, Duration = "0 мс" });
                        ThemedMessageBox.Show("Обновление списка таблиц из БД", "Не удалось обновить список таблиц из сервера.", MessageBoxButton.OK, MessageBoxImage.Error);
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось обновить список таблиц из сервера. " + ex.Message);
                    }
                }));
            }
        }

        public SettingsWindowVM()
        {
            MainVM = App.Current.MainWindow.DataContext as MainWindowVM;
            Username = MainVM.Username;
            Password = MainVM.Password;
            IP = MainVM.IP;
            Port = MainVM.Port;
            Database = MainVM.Database;
            PartitionLog = MainVM.PartitionLog;
            PartitionConfig = MainVM.PartitionConfig;
            DumpExePath = MainVM.DumpExePath;

            Tables.Clear();
            foreach (SettingsTable table in MainVM.Tables) Tables.Add(new SettingsTable
            {
                Name = table.Name,
                DontPartition = table.DontPartition,
                SelectedDateTimeColumn = table.SelectedDateTimeColumn
            });

            Tables.ListChanged += new ListChangedEventHandler(Tables_ListChanged);

            reconnect = false;
            exportLastConfig = false;
            readPartitionLog = false;
            SaveEnabled = false;
        }

        /// <summary>
        /// Метод получения таблиц из БД
        /// </summary>

        void ReadTables()
        {
            try
            {
                using (var conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        List<string> serverTables = new List<string>();
                        List<SettingsTable> tablesToRemove = new List<SettingsTable>();
                        List<string> tableColumns = new List<string>();
                        bool selectDateTimeColumn = false;
                        TableNames.Clear();

                        using (MySqlCommand command = new MySqlCommand("SHOW TABLE STATUS", conn))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string name = reader.GetString(0);
                                    int avgEntrySize = reader.GetInt32(reader.GetOrdinal("Avg_row_length"));
                                    SettingsTable table = Tables.Where(x => x.Name == name).FirstOrDefault();

                                    serverTables.Add(name);
                                    if (table != null) table.AvgEntrySize = avgEntrySize;
                                    else Tables.Add(new SettingsTable { Name = name, AvgEntrySize = avgEntrySize, DontPartition = false });
                                }
                            }
                        }

                        foreach (SettingsTable table in Tables)
                        {
                            TableNames.Add(table.Name);
                            if (!serverTables.Any(x => table.Name == x)) tablesToRemove.Add(table);
                            else
                            {
                                using (MySqlCommand command = new MySqlCommand("SHOW COLUMNS FROM `" + table.Name + "`", conn))
                                {
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        selectDateTimeColumn = table.DateTimeColumns.Count == 0;
                                        while (reader.Read())
                                        {
                                            string name = reader.GetString(reader.GetOrdinal("Field"));
                                            if (reader.GetString(reader.GetOrdinal("Type")).Contains("datetime"))
                                            {
                                                tableColumns.Add(name);
                                                if (!table.DateTimeColumns.Contains(name)) table.DateTimeColumns.Add(name);
                                            }
                                        }
                                    }
                                }
                                string columnToRemove = table.DateTimeColumns.Where(x => !tableColumns.Contains(x)).FirstOrDefault();
                                if (columnToRemove != null) table.DateTimeColumns.Remove(columnToRemove);
                                if (String.IsNullOrEmpty(table.SelectedDateTimeColumn) && selectDateTimeColumn) table.SelectedDateTimeColumn = table.DateTimeColumns.FirstOrDefault();
                                if (table.DateTimeColumns.Count == 0)
                                {
                                    table.PartitionDisabled = true;
                                    table.DontPartition = true;
                                }
                                else table.PartitionDisabled = false;
                            }
                        }
                        foreach (SettingsTable table in tablesToRemove) Tables.Remove(table);

                        string query = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Table_CalculateMaxRowSize.sql")).Replace("database", Database);
                        using (MySqlCommand command = new MySqlCommand(query, conn))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string name = reader.GetString(0);
                                    SettingsTable table = Tables.Where(x => x.Name == name).FirstOrDefault();
                                    if (table != null) table.EntrySize = reader.GetInt32(1);
                                }
                            }
                        }

                        LogHelper.Log(LogHelper.Status.Ok, "Окно \"Настройки\". Обновление списка таблиц из сервера успешно.");
                    }
                    conn.Close();
                }
            }
            catch (MySqlException ex)
            {
                LogHelper.Log(LogHelper.Status.Ok, ex.Message);
            }
        }

        private void OnTableMouseDown(MouseButtonEventArgs e)
        {
            GridColumn column = (e.Source as TableView).FocusedColumn;
            SettingsTable settingsTable = (e.Source as TableView).SelectedRows[0] as SettingsTable;

            if (column.FieldName == "DontPartition" && !settingsTable.PartitionDisabled) settingsTable.DontPartition = !settingsTable.DontPartition;
            (e.Source as TableView).CommitEditing();
        }

        private void OnTablePreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete) DeleteTableButtonCommand.Execute(this);
        }

        private void OnSaveButtonClicked(Window window)
        {
            SettingsWindowVM settingsWindowVM = window.DataContext as SettingsWindowVM;

            settingsWindowVM.MainVM.PartitionLog = settingsWindowVM.PartitionLog;
            settingsWindowVM.MainVM.PartitionConfig = settingsWindowVM.PartitionConfig;
            settingsWindowVM.MainVM.DumpExePath = settingsWindowVM.DumpExePath;

            settingsWindowVM.MainVM.Tables.Clear();
            foreach (SettingsTable table in settingsWindowVM.Tables) settingsWindowVM.MainVM.Tables.Add(table);
            settingsWindowVM.MainVM.RebuildPartitionTables();

            settingsWindowVM.SaveEnabled = false;

            LogHelper.Log(LogHelper.Status.Ok, "Окно \"Настройки\". Сохранены новые настройки: IP - " + settingsWindowVM.IP + "; Port - " + settingsWindowVM.Port + "; Database - " + settingsWindowVM.Database + "; Username - " + settingsWindowVM.Username + ";");

            if (settingsWindowVM.reconnect)
            {
                settingsWindowVM.MainVM.Username = settingsWindowVM.Username;
                settingsWindowVM.MainVM.Password = settingsWindowVM.Password;
                settingsWindowVM.MainVM.IP = settingsWindowVM.IP;
                settingsWindowVM.MainVM.Port = settingsWindowVM.Port;
                settingsWindowVM.MainVM.Database = settingsWindowVM.Database;

                settingsWindowVM.MainVM.ConnectToDatabase();
                settingsWindowVM.reconnect = false;
            }
            if (settingsWindowVM.exportLastConfig && !settingsWindowVM.reconnect)
            {
                settingsWindowVM.MainVM.ExportLastConfig();
                settingsWindowVM.exportLastConfig = false;
            }
            if (settingsWindowVM.readPartitionLog && !settingsWindowVM.reconnect)
            {
                settingsWindowVM.MainVM.ClearAndReadPartitionLog = true;
                settingsWindowVM.readPartitionLog = false;
            }

            window.Close();
        }

        private void OnButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Окно \"Настройки\". Нажата кнопка \"" + (e.Source as System.Windows.Controls.Button).Content.ToString() + "\".");
        }

        private void OnContextMenuButtonClicked(ItemClickEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Окно \"Настройки\". Нажата кнопка контекстного меню \"" + e.Item.Content + "\".");
        }

        private void OnDumpExePathButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Окно \"Настройки\". Нажата кнопка \"" + (e.Source as System.Windows.Controls.Button).Tag.ToString() + "\".");
        }

        private void OnTabChanged(DevExpress.Xpf.Docking.Base.SelectedItemChangedEventArgs e)
        {
            if (e.Item != null) LogHelper.Log(LogHelper.Status.User, "Окно \"Настройки\". Нажата вкладка \"" + e.Item.Caption + "\".");
        }

        private void OnTextBoxTextChanged(TextChangedEventArgs e)
        {
            SaveEnabled = true;
        }

        private void OnTextEditValueChanged(EditValueChangedEventArgs e)
        {
            SaveEnabled = true;
        }

        void Tables_ListChanged(object sender, ListChangedEventArgs e)
        {
            SaveEnabled = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
