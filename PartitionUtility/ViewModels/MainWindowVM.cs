using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using MySqlConnector;
using DevExpress.Xpf.Core;
using System.Windows;
using System.Xml.Serialization;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using DevExpress.XtraRichEdit;
using System.Xml;
using System.Xml.Schema;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using System.Text;
using System.Globalization;
using System.Windows.Controls;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Grid;
using System.Windows.Input;
using DevExpress.Xpf.RichEdit;
using DevExpress.XtraRichEdit.Services;
using System.Reflection;
using System.Threading.Tasks;

namespace PartitionUtility
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        #region Variables

        RichEditControl richEditControl;

        private object syncLock = new Object();
        public ICollectionView LogCollection { get; set; }

        private System.Timers.Timer timer = new System.Timers.Timer(10000);
        private System.Timers.Timer reconnectTimer = new System.Timers.Timer(1000);
        private System.Timers.Timer ipTimer = new System.Timers.Timer(1000);
        private System.Timers.Timer configCheckTimer = new System.Timers.Timer(1000);

        #endregion

        #region View properties

        public static ObservableCollection<string> IntervalMeasure { get; set; } = new ObservableCollection<string>() { "Сек.", "Мин.", "Час", "День" };
        public static ObservableCollection<string> SectorSizeMeasure = new ObservableCollection<string>() { "День", "Месяц", "Год" };
        public static ObservableCollection<string> DepthMeasure = new ObservableCollection<string>() { "День", "Месяц", "Год" };
        public static ObservableCollection<string> PartitionIntervalMeasure = new ObservableCollection<string>() { "Минута", "Час", "День", "Месяц" };

        public List<SettingsTable> Tables { get; set; } = new List<SettingsTable>();
        public ObservableCollection<Log> LogItems { get; set; } = new ObservableCollection<Log>();
        public ObservableCollection<PartitionLog> PartitionLogItems { get; set; } = new ObservableCollection<PartitionLog>();
        public ObservableCollection<Partition> PartitionsItems { get; set; } = new ObservableCollection<Partition>();
        public ObservableCollection<StoredProcedure> ProceduresItems { get; set; } = new ObservableCollection<StoredProcedure>();
        public ObservableCollection<Event> EventsItems { get; set; } = new ObservableCollection<Event>();
        public BindingList<PartitionTable> PartitionTables { get; set; } = new BindingList<PartitionTable>();
        public BindingList<PartitionTable> InitPartitionTables { get; set; } = new BindingList<PartitionTable>();
        public BindingList<PartitionTableDiagnostics> CurrPartitionTables { get; set; } = new BindingList<PartitionTableDiagnostics>();
        public ObservableCollection<Table> TablesItems { get; set; } = new ObservableCollection<Table>();

        private PartitionTable selectedPartitionTable;
        public PartitionTable SelectedPartitionTable
        {
            get { return selectedPartitionTable; }
            set
            {
                selectedPartitionTable = value;
                NotifyPropertyChanged();
            }
        }

        private string eventSchedulerColor;
        public string EventSchedulerColor
        {
            get { return eventSchedulerColor; }
            set
            {
                eventSchedulerColor = value;
                NotifyPropertyChanged();
            }
        }

        private string eventSchedulerState;
        public string EventSchedulerState
        {
            get { return eventSchedulerState; }
            set
            {
                eventSchedulerState = value;
                NotifyPropertyChanged();
            }
        }

        private string status;
        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                NotifyPropertyChanged();
            }
        }

        private int partitionStatus;
        public int PartitionStatus
        {
            get { return partitionStatus; }
            set
            {
                partitionStatus = value;
                NotifyPropertyChanged();
            }
        }

        private int ipStatus;
        public int IPStatus
        {
            get { return ipStatus; }
            set
            {
                ipStatus = value;
                NotifyPropertyChanged();
            }
        }

        private int databaseStatus;
        public int DatabaseStatus
        {
            get { return databaseStatus; }
            set
            {
                databaseStatus = value;
                NotifyPropertyChanged();
            }
        }

        private string bdAndUser;
        public string BDAndUser
        {
            get { return bdAndUser; }
            set
            {
                bdAndUser = value;
                NotifyPropertyChanged();
            }
        }

        private string ipPort;
        public string IPPort
        {
            get { return ipPort; }
            set
            {
                ipPort = value;
                NotifyPropertyChanged();
            }
        }

        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set
            {
                fileName = value;
                NotifyPropertyChanged();
            }
        }

        private string displayFileName;
        public string DisplayFileName
        {
            get { return displayFileName; }
            set
            {
                displayFileName = value.Length <= 130 ? value : "..." + value.Substring(value.Length - 127);
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Work properties

        private Config LastConfig { get; set; }
        private DateTime LastConfigUpdateTime { get; set; }
        private bool ReconnectShown { get; set; }
        private bool LastConfigNotFoundShown { get; set; }
        public bool ClearAndReadPartitionLog { get; set; }

        private bool isConnected;
        public bool IsConnected
        {
            get { return isConnected; }
            set
            {
                isConnected = value;
                NotifyPropertyChanged();
            }
        }

        private bool isRefreshingPartitioning;
        public bool IsRefreshingPartitioning
        {
            get { return isRefreshingPartitioning; }
            set
            {
                isRefreshingPartitioning = value;
                NotifyPropertyChanged();
            }
        }

        private bool isRemovingPartitioning;
        public bool IsRemovingPartitioning
        {
            get { return isRemovingPartitioning; }
            set
            {
                isRemovingPartitioning = value;
                NotifyPropertyChanged();
            }
        }

        private bool isPartitioning;
        public bool IsPartitioning
        {
            get { return isPartitioning; }
            set
            {
                isPartitioning = value;
                NewOpenButtonsEnabled = !value;
                NotifyPropertyChanged();
            }
        }

        private bool isDroppingPartitioning;
        public bool IsDroppingPartitioning
        {
            get { return isDroppingPartitioning; }
            set
            {
                isDroppingPartitioning = value;
                NewOpenButtonsEnabled = !value;
                NotifyPropertyChanged();
            }
        }

        public static string LogDirectory { get; set; } = "Log";
        public static string OutputDirectory { get; set; } = "Output";

        private bool eventSchedulerEnabled;
        public bool EventSchedulerEnabled
        {
            get { return eventSchedulerEnabled; }
            set
            {
                eventSchedulerEnabled = value;
                if (eventSchedulerEnabled)
                {
                    EventSchedulerState = "включен";
                    EventSchedulerColor = "#008000";
                }
                else
                {
                    EventSchedulerState = "выключен";
                    EventSchedulerColor = "#ff0000";

                }
                NotifyPropertyChanged();
            }
        }

        private bool newOpenButtonsEnabled;
        public bool NewOpenButtonsEnabled
        {
            get { return newOpenButtonsEnabled; }
            set
            {
                newOpenButtonsEnabled = value;
                NotifyPropertyChanged();
            }
        }

        private bool saveButtonEnabled;
        public bool SaveButtonEnabled
        {
            get { return saveButtonEnabled; }
            set
            {
                saveButtonEnabled = value;
                NotifyPropertyChanged();
            }
        }

        private bool saveEnabled;
        public bool SaveEnabled
        {
            get { return saveEnabled; }
            set
            {
                saveEnabled = value;
                SaveButtonEnabled = value;
                if (FileName == "Новая конфигурация") SaveButtonEnabled = false;
                if (value) DisplayFileName = FileName + "*";
                else DisplayFileName = FileName;
            }
        }

        private string ip;
        public string IP
        {
            get { return ip; }
            set
            {
                ip = value;
                IPPort = value + ":" + Port;
                SaveEnabled = true;
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
                IPPort = IP + ":" + value;
                SaveEnabled = true;
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
                SaveEnabled = true;

                BDAndUser = "БД: " + value + ", User: " + Username;

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
                SaveEnabled = true;

                BDAndUser = "БД: " + Database + ", User: " + value;

                NotifyPropertyChanged();
            }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                SaveEnabled = true;
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
                SaveEnabled = true;
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
                SaveEnabled = true;
                NotifyPropertyChanged();
            }
        }

        private string lastFileName;
        public string LastFileName
        {
            get { return lastFileName; }
            set
            {
                lastFileName = value;
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

        public uint LastPartitionLogEntry { get; set; }

        #endregion

        #region AutoFilling properties

        private static bool tableSetTableSize;
        public static bool SetTableSize
        {
            get { return tableSetTableSize; }
            set
            {
                tableSetTableSize = value;
                NotifyStaticPropertyChanged();
            }
        }

        private static bool tableSetEntryCount;
        public static bool SetEntryCount
        {
            get { return tableSetEntryCount; }
            set
            {
                tableSetEntryCount = value;
                NotifyStaticPropertyChanged();
            }
        }

        private static bool tableSetDepth = true;
        public static bool SetDepth
        {
            get { return tableSetDepth; }
            set
            {
                tableSetDepth = value;
                NotifyStaticPropertyChanged();
            }
        }

        private static bool tableSetSectorCount;
        public static bool SetSectorCount
        {
            get { return tableSetSectorCount; }
            set
            {
                tableSetSectorCount = value;
                NotifyStaticPropertyChanged();
            }
        }

        private static bool tableSetSectorSize = true;
        public static bool SetSectorSize
        {
            get { return tableSetSectorSize; }
            set
            {
                tableSetSectorSize = value;
                NotifyStaticPropertyChanged();
            }
        }

        private static bool tableSetInterval = true;
        public static bool SetInterval
        {
            get { return tableSetInterval; }
            set
            {
                tableSetInterval = value;
                NotifyStaticPropertyChanged();
            }
        }

        #endregion

        #region AutoFilling button commands

        /// <summary>
        /// Команды кнопок на столбцах таблиц вкладки "Конфигурация"
        /// </summary>

        private RelayCommand<object> tableIntervalButtonCommand;
        public RelayCommand<object> TableIntervalButtonCommand
        {
            get
            {
                return tableIntervalButtonCommand ?? (tableIntervalButtonCommand = new RelayCommand<object>(obj =>
                {
                    SetTableSize = true;
                    SetEntryCount = true;
                    SetDepth = true;
                    SetInterval = false;

                    if (PartitionTables.Count > 0) foreach (PartitionTable table in PartitionTables.ToList()) table.CalculateIntervalVal();
                }, obj =>
                {
                    return SetInterval;
                }));
            }
        }

        private RelayCommand<object> tableSectorSizeButtonCommand;
        public RelayCommand<object> TableSectorSizeButtonCommand
        {
            get
            {
                return tableSectorSizeButtonCommand ?? (tableSectorSizeButtonCommand = new RelayCommand<object>(obj =>
                {
                    SetSectorCount = !SetSectorCount;
                    SetSectorSize = !SetSectorCount;

                    if (PartitionTables.Count > 0) foreach (PartitionTable table in PartitionTables.ToList()) table.CalculateSectorSizeVal();
                }));
            }
        }

        private RelayCommand<object> tableSectorCountButtonCommand;
        public RelayCommand<object> TableSectorCountButtonCommand
        {
            get
            {
                return tableSectorCountButtonCommand ?? (tableSectorCountButtonCommand = new RelayCommand<object>(obj =>
                {
                    SetSectorCount = !SetSectorCount;
                    SetSectorSize = !SetSectorCount;

                    if (PartitionTables.Count > 0) foreach (PartitionTable table in PartitionTables.ToList()) table.CalculateSectorCount();
                }));
            }
        }

        private RelayCommand<object> tableDepthButtonCommand;
        public RelayCommand<object> TableDepthButtonCommand
        {
            get
            {
                return tableDepthButtonCommand ?? (tableDepthButtonCommand = new RelayCommand<object>(obj =>
                {
                    SetTableSize = true;
                    SetEntryCount = true;
                    SetDepth = false;
                    SetInterval = true;

                    if (PartitionTables.Count > 0) foreach (PartitionTable table in PartitionTables.ToList()) table.CalculateDepthVal();
                }, obj =>
                {
                    return SetDepth;
                }));
            }
        }

        private RelayCommand<object> tableEntryCountButtonCommand;
        public RelayCommand<object> TableEntryCountButtonCommand
        {
            get
            {
                return tableEntryCountButtonCommand ?? (tableEntryCountButtonCommand = new RelayCommand<object>(obj =>
                {
                    SetTableSize = false;
                    SetEntryCount = false;
                    SetDepth = true;
                    SetInterval = true;

                    if (PartitionTables.Count > 0)
                    {
                        foreach (PartitionTable table in PartitionTables.ToList())
                        {
                            table.CalculateEntryCount();
                            table.CalculateTableSize();
                        }
                    }
                }, obj =>
                {
                    return SetEntryCount;
                }));
            }
        }

        private RelayCommand<object> tableSizeButtonCommand;
        public RelayCommand<object> TableSizeButtonCommand
        {
            get
            {
                return tableSizeButtonCommand ?? (tableSizeButtonCommand = new RelayCommand<object>(obj =>
                {
                    SetTableSize = false;
                    SetEntryCount = false;
                    SetDepth = true;
                    SetInterval = true;

                    if (PartitionTables.Count > 0)
                    {
                        foreach (PartitionTable table in PartitionTables.ToList())
                        {
                            table.CalculateEntryCount();
                            table.CalculateTableSize();
                        }
                    }
                }, obj =>
                {
                    return SetTableSize;
                }));
            }
        }

        #endregion

        #region Button commands

        private RelayCommand<RoutedEventArgs> buttonClickCommand;
        public RelayCommand<RoutedEventArgs> ButtonClickCommand
        {
            get
            {
                return buttonClickCommand ?? (buttonClickCommand = new RelayCommand<RoutedEventArgs>(OnButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<RoutedEventArgs> imageButtonClickCommand;
        public RelayCommand<RoutedEventArgs> ImageButtonClickCommand
        {
            get
            {
                return imageButtonClickCommand ?? (imageButtonClickCommand = new RelayCommand<RoutedEventArgs>(OnImageButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<ItemClickEventArgs> menuButtonClickCommand;
        public RelayCommand<ItemClickEventArgs> MenuButtonClickCommand
        {
            get
            {
                return menuButtonClickCommand ?? (menuButtonClickCommand = new RelayCommand<ItemClickEventArgs>(OnMenuButtonClicked, (o) => { return true; }));
            }
        }

        private RelayCommand<RoutedEventArgs> configTableButtonClickCommand;
        public RelayCommand<RoutedEventArgs> ConfigTableButtonClickCommand
        {
            get
            {
                return configTableButtonClickCommand ?? (configTableButtonClickCommand = new RelayCommand<RoutedEventArgs>(OnConfigTableButtonClicked, (o) => { return true; }));
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

        private RelayCommand<GridCellValidationEventArgs> validateCellCommand;
        public RelayCommand<GridCellValidationEventArgs> ValidateCellCommand
        {
            get
            {
                return validateCellCommand ?? (validateCellCommand = new RelayCommand<GridCellValidationEventArgs>(ValidateCell, (o) => { return true; }));
            }
        }

        private RelayCommand<MouseButtonEventArgs> proceduresMouseDownCommand;
        public RelayCommand<MouseButtonEventArgs> ProceduresMouseDownCommand
        {
            get
            {
                return proceduresMouseDownCommand ?? (proceduresMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(ProceduresMouseDown, (o) => { return true; }));
            }
        }

        private RelayCommand<MouseButtonEventArgs> eventsMouseDownCommand;
        public RelayCommand<MouseButtonEventArgs> EventsMouseDownCommand
        {
            get
            {
                return eventsMouseDownCommand ?? (eventsMouseDownCommand = new RelayCommand<MouseButtonEventArgs>(EventsMouseDown, (o) => { return true; }));
            }
        }

        private RelayCommand<RoutedEventArgs> richEditLoadedCommand;
        public RelayCommand<RoutedEventArgs> RichEditLoadedCommand
        {
            get
            {
                return richEditLoadedCommand ?? (richEditLoadedCommand = new RelayCommand<RoutedEventArgs>(RichEditLoaded, (o) => { return true; }));
            }
        }

        private RelayCommand<CellValueChangedEventArgs> configTableCellValueChangingCommand;
        public RelayCommand<CellValueChangedEventArgs> ConfigTableCellValueChangingCommand
        {
            get
            {
                return configTableCellValueChangingCommand ?? (configTableCellValueChangingCommand = new RelayCommand<CellValueChangedEventArgs>(OnConfigTableCellValueChanging, (o) => { return true; }));
            }
        }

        /// <summary>
        /// Команды кнопок в окне
        /// </summary>

        private RelayCommand<object> newButtonCommand;
        public RelayCommand<object> NewButtonCommand
        {
            get
            {
                return newButtonCommand ?? (newButtonCommand = new RelayCommand<object>(obj =>
                {
                    if(SaveEnabled)
                    {
                        var res = ThemedMessageBox.Show("Новая конфигурация", "В текущую конфигурацию были внесены изменения. Сохранить конфигурацию?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        switch (res)
                        {
                            case MessageBoxResult.Yes:
                                if (FileName != "Новая конфигурация") SaveButtonCommand.Execute(this);
                                else SaveAsButtonCommand.Execute(this);
                                break;

                            case MessageBoxResult.Cancel:
                                return;

                            case MessageBoxResult.No: break;
                        }
                    }

                    if (IsConnected)
                    {
                        LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана новая конфигурация. Соединение с БД " + Database + " закрыто.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Ok, "Создана новая конфигурация. Соединение с БД " + Database + " закрыто.");
                        DisconnectDatabase();
                    }
                    else
                    {
                        LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана новая конфигурация.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Ok, "Создана новая конфигурация.");
                    }
                    richEditControl.Document.Delete(richEditControl.Document.Range);
                    Initialize();
                }));
            }
        }

        private RelayCommand<object> openButtonCommand;
        public RelayCommand<object> OpenButtonCommand
        {
            get
            {
                return openButtonCommand ?? (openButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        if (SaveEnabled)
                        {
                            var res = ThemedMessageBox.Show("Загрузка конфигурации", "В текущую конфигурацию были внесены изменения. Сохранить конфигурацию?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                            switch (res)
                            {
                                case MessageBoxResult.Yes:
                                    if (FileName != "Новая конфигурация") SaveButtonCommand.Execute(this);
                                    else SaveAsButtonCommand.Execute(this);
                                    break;

                                case MessageBoxResult.Cancel:
                                    return;

                                case MessageBoxResult.No:
                                    break;
                            }
                        }

                        Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

                        dlg.DefaultExt = ".cfg";
                        dlg.Filter = "Файлы конфигурации (*.cfg)|*.cfg";
                        dlg.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

                        Nullable<bool> result = dlg.ShowDialog();

                        if (result == true) OpenFile(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                    }
                }));
            }
        }

        private RelayCommand<object> openExportButtonCommand;
        public RelayCommand<object> OpenExportButtonCommand
        {
            get
            {
                return openExportButtonCommand ?? (openExportButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        Stopwatch sw = new Stopwatch();
                        using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                        {
                            conn.Open();
                            using (MySqlCommand command = new MySqlCommand("", conn))
                            {
                                command.CommandText = "SELECT * FROM information_schema.tables WHERE table_schema = '" + Database + "' AND table_name = '" + PartitionConfig + "'";

                                bool exists;
                                string query = "";

                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    exists = reader.HasRows;
                                }

                                if (!exists)
                                {
                                    sw.Restart();
                                    query = "CREATE TABLE IF NOT EXISTS `" + Database + "`.`" + PartitionConfig + "`" +
                                       " (" +
                                       " ID INT(10) UNSIGNED NOT NULL AUTO_INCREMENT," +
                                       " name VARCHAR(45) NOT NULL," +
                                       " updated DATETIME(3) NOT NULL," +
                                       " config LONGBLOB NOT NULL, " +
                                       " flag TINYINT(1) NOT NULL DEFAULT 0," +
                                       " PRIMARY KEY (ID, updated)" +
                                       " )" +
                                       " ENGINE = INNODB," +
                                       " CHARACTER SET utf8mb4," +
                                       " COLLATE utf8mb4_general_ci; ";

                                    command.CommandText = query;
                                    command.ExecuteNonQuery();

                                    sw.Stop();

                                    lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана таблица для хранения конфигураций " + PartitionConfig + ".", Duration = sw.ElapsedMilliseconds + " мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Создана таблица для хранения конфигураций " + PartitionConfig + ".");

                                    command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Создана таблица для хранения конфигураций " + PartitionConfig + "', '" + Database + "." + PartitionConfig + "', '00:00:00 sec');";
                                    command.ExecuteNonQuery();
                                }

                                ImportWindow importWindow = new ImportWindow();
                                importWindow.Owner = App.Current.MainWindow;
                                importWindow.ShowDialog();

                                LogHelper.Log(LogHelper.Status.Ok, "Открыто окно \"Импорт конфигурации из БД\".");
                            }
                            conn.Close();
                        }
                    }
                    catch (MySqlException ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Ok, ex.Message);
                    }
                }, obj =>
                {
                    return !IsPartitioning && !IsDroppingPartitioning && IsConnected;
                }));
            }
        }

        private RelayCommand<object> saveButtonCommand;
        public RelayCommand<object> SaveButtonCommand
        {
            get
            {
                return saveButtonCommand ?? (saveButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        Config config = new Config();
                        config.IP = IP;
                        config.Port = Port;
                        config.DataBase = Database;
                        config.UserName = Username;
                        config.Password = Security.Protect(Password);
                        config.PartitionLog = PartitionLog;
                        config.PartitionConfig = PartitionConfig;
                        config.DumpExePath = DumpExePath;
                        foreach (PartitionTable table in PartitionTables) config.PartitionTables.Add(table);
                        foreach (SettingsTable table in Tables) config.Tables.Add(table);

                        XmlSerializer formatter = new XmlSerializer(typeof(Config));

                        using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) formatter.Serialize(stream, config);

                        string hash = Checker.GetFileHash(FileName);
                        using (StreamWriter sw = new StreamWriter(FileName, true)) sw.Write("\r\nCRC:" + hash);

                        ((MainWindow)Application.Current.MainWindow).richEditControl1.LoadDocument(FileName, DocumentFormat.PlainText);

                        SaveEnabled = false;

                        LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Файл конфигурации успешно сохранен " + FileName + ".", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Ok, "Файл конфигурации успешно сохранен " + FileName + ".");
                    }
                    catch(Exception ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось сохранить файл конфигурации " + FileName + ".", ex);
                    }
                }, obj =>
                {
                        return SaveEnabled;
                }));
            }
        }

        private RelayCommand<object> saveAsButtonCommand;
        public RelayCommand<object> SaveAsButtonCommand
        {
            get
            {
                return saveAsButtonCommand ?? (saveAsButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                        dlg.DefaultExt = ".cfg";
                        dlg.Filter = "Файлы конфигурации (*.cfg)|*.cfg";
                        dlg.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

                        Nullable<bool> result = dlg.ShowDialog();

                        if (result == true)
                        {
                            FileName = dlg.FileName;

                            Config config = new Config();
                            config.IP = IP;
                            config.Port = Port;
                            config.DataBase = Database;
                            config.UserName = Username;
                            config.Password = Security.Protect(Password);
                            config.PartitionLog = PartitionLog;
                            config.PartitionConfig = PartitionConfig;
                            config.DumpExePath = DumpExePath;
                            foreach (PartitionTable table in PartitionTables) config.PartitionTables.Add(table);
                            foreach (SettingsTable table in Tables) config.Tables.Add(table);

                            XmlSerializer formatter = new XmlSerializer(typeof(Config));

                            using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) formatter.Serialize(stream, config);

                            string hash = Checker.GetFileHash(FileName);
                            using (StreamWriter sw = new StreamWriter(FileName, true)) sw.Write("\r\nCRC:" + hash);

                            ((MainWindow)Application.Current.MainWindow).richEditControl1.LoadDocument(FileName, DocumentFormat.PlainText);

                            SaveEnabled = false;
                            LastFileName = FileName;

                            LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Файл конфигурации успешно сохранен " + FileName + ".", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Файл конфигурации успешно сохранен " + FileName + ".");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось сохранить файл конфигурации " + FileName + ".", ex);
                    }               
                }));
            }
        }

        private RelayCommand<object> openLogDirectoryButtonCommand;
        public RelayCommand<object> OpenLogDirectoryButtonCommand
        {
            get
            {
                return openLogDirectoryButtonCommand ?? (openLogDirectoryButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        if (Directory.Exists(LogDirectory))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = LogDirectory,
                                FileName = "explorer.exe"
                            };
                            Process.Start(startInfo);
                        }
                        else
                        {
                            LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Папка " + AppDomain.CurrentDomain.BaseDirectory + LogDirectory + " не найдена.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Папка " + AppDomain.CurrentDomain.BaseDirectory + LogDirectory + " не найдена.");
                        }
                    }
                    catch(Exception ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось открыть папку " + AppDomain.CurrentDomain.BaseDirectory + LogDirectory + ".", ex);
                    }
                }));
            }
        }

        private RelayCommand<object> openLogButtonCommand;
        public RelayCommand<object> OpenLogButtonCommand
        {
            get
            {
                return openLogButtonCommand ?? (openLogButtonCommand = new RelayCommand<object>(obj =>
                {
                    string logFilePath = Path.Combine(LogDirectory, "PartitionUtility_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt");
                    try
                    {   
                        if (File.Exists(logFilePath))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = logFilePath,
                                FileName = "notepad.exe"
                            };
                            Process.Start(startInfo);
                        }
                        else
                        {
                            LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Файл " + AppDomain.CurrentDomain.BaseDirectory + logFilePath + " не найден.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Файл " + AppDomain.CurrentDomain.BaseDirectory + logFilePath + " не найден.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось открыть файл " + AppDomain.CurrentDomain.BaseDirectory + logFilePath + ".", ex);
                    }
                }));
            }
        }

        private RelayCommand<object> createReportButtonCommand;
        public RelayCommand<object> CreateReportButtonCommand
        {
            get
            {
                return createReportButtonCommand ?? (createReportButtonCommand = new RelayCommand<object>(async obj =>
                {
                    var result = ThemedMessageBox.Show("Сформировать отчет", "Сформировать отчет о текущем состоянии секционирования БД " + Database + "?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            string reportDirectoryPath = Path.Combine(OutputDirectory, DateTime.Now.ToString("dd-MM-yyyy"));
                            if (!Directory.Exists(reportDirectoryPath)) Directory.CreateDirectory(reportDirectoryPath);
                            else
                            {
                                DirectoryInfo directoryInfo = new DirectoryInfo(reportDirectoryPath);
                                foreach (FileInfo file in directoryInfo.GetFiles()) file.Delete();
                                foreach (DirectoryInfo dir in directoryInfo.GetDirectories()) dir.Delete(true);
                            }

                            if (Directory.Exists(DumpExePath) && Directory.GetFiles(DumpExePath).Contains(Path.Combine(DumpExePath, "mysqldump.exe"))) await CreateReportAsync(reportDirectoryPath);
                            else
                            {
                                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось сформировать отчет. Файл mysqldump.exe не найден в папке " + DumpExePath + ". Задайте корректный путь до папки с файлами СУБД в окне \"Настройки\".", Duration = "0 мс" });
                                LogHelper.Log(LogHelper.Status.Error, "Не удалось сформировать отчет. Файл mysqldump.exe не найден в папке " + DumpExePath + ". Задайте корректный путь до папки с файлами СУБД в окне \"Настройки\".");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось сформировать отчет. " + ex.Message, Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось сформировать отчет.", ex);
                        }
                    }
                }, obj =>
                {
                    return !IsPartitioning && !IsDroppingPartitioning && IsConnected;
                }));
            }
        }

        private RelayCommand<object> openSettingsWindowCommand;
        public RelayCommand<object> OpenSettingsWindowCommand
        {
            get
            {
                return openSettingsWindowCommand ?? (openSettingsWindowCommand = new RelayCommand<object>(obj =>
                {
                    SettingsWindow settingsWindow = new SettingsWindow();
                    settingsWindow.Owner = App.Current.MainWindow;
                    settingsWindow.ShowDialog();

                    LogHelper.Log(LogHelper.Status.Ok, "Открыто окно \"Настройки\".");
                }));
            }
        }

        private RelayCommand<object> openAboutWindowCommand;
        public RelayCommand<object> OpenAboutWindowCommand
        {
            get
            {
                return openAboutWindowCommand ?? (openAboutWindowCommand = new RelayCommand<object>(obj =>
                {
                    AboutWindow aboutWindow = new AboutWindow();
                    aboutWindow.Owner = App.Current.MainWindow;
                    aboutWindow.ShowDialog();

                    LogHelper.Log(LogHelper.Status.Ok, "Открыто окно \"О программе\".");
                }));
            }
        }

        private RelayCommand<object> exitCommand;
        public RelayCommand<object> ExitCommand
        {
            get
            {
                return exitCommand ?? (exitCommand = new RelayCommand<object>(obj =>
                {
                    Application.Current.Shutdown();
                }));
            }
        }

        private RelayCommand<object> clearUILogButtonCommand;
        public RelayCommand<object> ClearUILogButtonCommand
        {
            get
            {
                return clearUILogButtonCommand ?? (clearUILogButtonCommand = new RelayCommand<object>(obj =>
                {
                    LogItems.Clear();
                    LogHelper.Log(LogHelper.Status.Ok, "Журнал очищен.");
                }));
            }
        }

        private RelayCommand<object> clearLogButtonCommand;
        public RelayCommand<object> ClearLogButtonCommand
        {
            get
            {
                return clearLogButtonCommand ?? (clearLogButtonCommand = new RelayCommand<object>(obj =>
                {
                    string logFilePath = Path.Combine(LogDirectory, "PartitionUtility_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt");
                    try
                    {
                        if (File.Exists(logFilePath))
                        {
                            File.WriteAllText(logFilePath, "");
                            LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Журнал работы приложения " + AppDomain.CurrentDomain.BaseDirectory + logFilePath + " очищен.", Duration = "0 мс" });
                        }
                        else
                        {
                            LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Файл " + AppDomain.CurrentDomain.BaseDirectory + logFilePath + " не найден.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Файл " + AppDomain.CurrentDomain.BaseDirectory + logFilePath + " не найден.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось очистить файл " + AppDomain.CurrentDomain.BaseDirectory + logFilePath + ".", ex);
                    }
                }));
            }
        }

        private RelayCommand<object> clearPartitionLogButtonCommand;
        public RelayCommand<object> ClearPartitionLogButtonCommand
        {
            get
            {
                return clearPartitionLogButtonCommand ?? (clearPartitionLogButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Очистка журнала секционирования", "Вы уверены, что хотите очистить журнал секционирования " + PartitionLog + "?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        Thread clearPartitionLogThread = new Thread(ClearPartitionLog);
                        clearPartitionLogThread.Start();
                    }
                }, (o) => { return IsConnected; }));
            }
        }

        private RelayCommand<object> openConfigDirectoryButtonCommand;
        public RelayCommand<object> OpenConfigDirectoryButtonCommand
        {
            get
            {
                return openConfigDirectoryButtonCommand ?? (openConfigDirectoryButtonCommand = new RelayCommand<object>(obj =>
                {
                    try
                    {
                        if (Directory.Exists(Path.GetDirectoryName(FileName)))
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                Arguments = Path.GetDirectoryName(FileName),
                                FileName = "explorer.exe"
                            };
                            Process.Start(startInfo);
                        }
                        else
                        {
                            LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Папка " + Path.GetDirectoryName(FileName) + " не найдена.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Папка " + Path.GetDirectoryName(FileName) + " не найдена.");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось открыть папку " + AppDomain.CurrentDomain.BaseDirectory + LogDirectory + ".", ex);
                    }
                }, obj =>
                {
                    return FileName != "Новая конфигурация";
                }));
            }
        }

        private RelayCommand<object> configureDBButtonCommand;
        public RelayCommand<object> ConfigureDBButtonCommand
        {
            get
            {
                return configureDBButtonCommand ?? (configureDBButtonCommand = new RelayCommand<object>(obj =>
                {
                    if (!IsPartitioning && !IsDroppingPartitioning && !IsRefreshingPartitioning && !IsRemovingPartitioning)
                    {
                        var dialogResult = ThemedMessageBox.Show("Секционирование БД", "Начать секционирование БД " + Database + "?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            bool cancel = false;
                            foreach (PartitionTable table in PartitionTables)
                            {
                                if (table.PartitionIntervalVal < 1 || table.EntrySize < 1 || table.IntervalVal < 1 || table.SectorSizeVal < 1 || table.SectorCount < 1 || table.DepthVal < 1 || table.EntryCount < 1 || table.TableSize < 1)
                                {
                                    LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось секционировать БД. В таблице " + table.Name + " обнаружены некорректные значения (<1).", Duration = "0 мс" });
                                    LogHelper.Log(LogHelper.Status.Error, "Не удалось секционировать БД. В таблице " + table.Name + " обнаружены некорректные значения (<1).");
                                    cancel = true;
                                }
                                if (String.IsNullOrEmpty(table.DateTimeColumn) || String.IsNullOrWhiteSpace(table.DateTimeColumn))
                                {
                                    LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось секционировать БД. Для таблицы " + table.Name + " не задан столбец типа \"DATETIME\".", Duration = "0 мс" });
                                    LogHelper.Log(LogHelper.Status.Error, "Не удалось секционировать БД. Для таблицы " + table.Name + " не задан столбец типа \"DATETIME\".");
                                    cancel = true;
                                }
                            }

                            if (!cancel)
                            {
                                Thread partitioningThread;
                                if (FileName == "Новая конфигурация")
                                {
                                    var res = ThemedMessageBox.Show("Секционирование БД", "Конфигурация секционирования не сохранена. Сохранить конфигурацию?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (res == MessageBoxResult.Yes)
                                    {
                                        Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

                                        dlg.DefaultExt = ".cfg";
                                        dlg.Filter = "Файлы конфигурации (*.cfg)|*.cfg";
                                        dlg.InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");

                                        Nullable<bool> result = dlg.ShowDialog();

                                        if (result == true)
                                        {
                                            FileName = dlg.FileName;
                                            LastFileName = FileName;

                                            Config config = new Config();
                                            config.IP = IP;
                                            config.Port = Port;
                                            config.DataBase = Database;
                                            config.UserName = Username;
                                            config.Password = Security.Protect(Password);
                                            config.PartitionLog = PartitionLog;
                                            config.PartitionConfig = PartitionConfig;
                                            foreach (PartitionTable table in PartitionTables) config.PartitionTables.Add(table);
                                            foreach (SettingsTable table in Tables) config.Tables.Add(table);

                                            XmlSerializer formatter = new XmlSerializer(typeof(Config));

                                            using (FileStream stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) formatter.Serialize(stream, config);

                                            string hash = Checker.GetFileHash(FileName);
                                            using (StreamWriter sw = new StreamWriter(FileName, true)) sw.Write("\r\nCRC:" + hash);

                                            ((MainWindow)Application.Current.MainWindow).richEditControl1.LoadDocument(FileName, DocumentFormat.PlainText);

                                            SaveEnabled = false;

                                            LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Файл конфигурации успешно сохранен " + FileName + ".", Duration = "0 мс" });
                                            LogHelper.Log(LogHelper.Status.Ok, "Файл конфигурации успешно сохранен " + FileName + ".");

                                            partitioningThread = new Thread(ConfigureDB);
                                            partitioningThread.Start();
                                        }
                                    }
                                }
                                else if (FileName != "Новая конфигурация" && SaveEnabled)
                                {
                                    var res = ThemedMessageBox.Show("Секционирование БД", "Конфигурация секционирования не сохранена. Сохранить конфигурацию?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                                    if (res == MessageBoxResult.Yes)
                                    {
                                        SaveButtonCommand.Execute(this);
                                        partitioningThread = new Thread(ConfigureDB);
                                        partitioningThread.Start();
                                    }
                                }
                                else if (FileName != "Новая конфигурация" && !SaveEnabled)
                                {
                                    partitioningThread = new Thread(ConfigureDB);
                                    partitioningThread.Start();
                                }
                            }
                        }
                    }
                    else if (IsPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Warning", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Секционирование уже начато.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Warning, "Невозможно начать секционирование базы данных. Секционирование уже начато.");
                    }
                    else if (IsDroppingPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось секционировать БД. Происходит очистка секционирования в БД.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Невозможно начать секционирование базы данных. Происходит очистка секционирования в БД.");
                    }
                    else if (IsRefreshingPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось секционировать БД. Происходит обновление секционирования таблиц в БД. Пожалуйста, дождитесь окончания.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Невозможно начать секционирование базы данных. Происходит обновление секционирования таблиц в БД. Пожалуйста, дождитесь окончания.");
                    }
                    else if (IsRemovingPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось секционировать БД. Происходит очистка секционирования таблиц в БД. Пожалуйста, дождитесь окончания.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Невозможно начать секционирование базы данных. Происходит очистка секционирования таблиц в БД. Пожалуйста, дождитесь окончания.");
                    }
                }, (o) => { return IsConnected; }));
            }
        }

        private RelayCommand<object> deinitPartitioningButtonCommand;
        public RelayCommand<object> DeinitPartitioningButtonCommand
        {
            get
            {
                return deinitPartitioningButtonCommand ?? (deinitPartitioningButtonCommand = new RelayCommand<object>(obj =>
                {
                    if (!IsRefreshingPartitioning && !IsRemovingPartitioning && !IsPartitioning && !IsDroppingPartitioning)
                    {
                        var result = ThemedMessageBox.Show("Очистка секционирования", "Вы уверены, что хотите очистить секционирование в базе данных " + Database + "?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.Yes)
                        {
                            Thread clearPartitioningThread = new Thread(ClearPartitioning);
                            clearPartitioningThread.Start();
                        }
                    }
                    else if (IsRefreshingPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось очистить секционирование в БД. Происходит обновление секционирования таблиц в БД. Пожалуйста, дождитесь окончания.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось очистить секционирование в БД. Происходит обновление секционирования таблиц в БД. Пожалуйста, дождитесь окончания.");
                    }
                    else if (IsRemovingPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось очистить секционирование в БД. Происходит очистка секционирования таблиц в БД. Пожалуйста, дождитесь окончания.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось очистить секционирование в БД. Происходит очистка секционирования таблиц в БД. Пожалуйста, дождитесь окончания.");
                    }
                    else if (IsPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Невозможно начать очистку секционирования базы данных. Происходит секционирование базы данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Невозможно начать очистку секционирования базы данных. Происходит секционирование базы данных.");
                    }
                    else if (IsDroppingPartitioning)
                    {
                        LogItems.Add(new Log { Status = "Warning", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Очистка секционирования уже начата.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Warning, "Очистка секционирования уже начата.");
                    }
                }, obj =>
                {
                    return IsConnected;
                }));
            }
        }

        private RelayCommand<object> clearTablesButtonCommand;
        public RelayCommand<object> ClearTablesButtonCommand
        {
            get
            {
                return clearTablesButtonCommand ?? (clearTablesButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Очистка таблиц в БД", "Вы уверены, что хотите очистить таблицы в базе данных " + Database + "?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        List<string> TableNames = new List<string>();
                        List<string> ClearedTables = new List<string>();
                        try
                        {
                            Stopwatch sw = new Stopwatch();
                            using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                            {
                                conn.Open();
                                sw.Restart();
                                using (MySqlCommand command = new MySqlCommand("SHOW TABLES FROM " + "`" + Database + "`", conn))
                                {
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read()) TableNames.Add(reader.GetString(0));
                                    }
                                    foreach (string tableName in TableNames)
                                    {
                                        if (tableName == PartitionConfig || tableName == PartitionLog) continue;
                                        command.CommandText = "TRUNCATE TABLE " + tableName;
                                        command.ExecuteNonQuery();
                                        ClearedTables.Add(tableName);
                                    }
                                }
                                sw.Stop();
                                conn.Close();

                                LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Таблицы в БД очищены:\n - " + String.Join("\n - ", ClearedTables), Duration = sw.ElapsedMilliseconds + " мс" });
                                ThemedMessageBox.Show("Очистка таблиц", "Таблицы в БД очищены.", MessageBoxButton.OK, MessageBoxImage.Information);
                                LogHelper.Log(LogHelper.Status.Ok, "Таблицы в БД очищены:\n - " + String.Join("\n - ", ClearedTables));
                            }
                        }
                        catch (MySqlException ex)
                        {
                            LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, ex.Message);
                        }
                    }
                }, obj =>
                {
                    return !IsPartitioning && !IsDroppingPartitioning && IsConnected;
                }));
            }
        }

        private RelayCommand<object> removeDatabaseButtonCommand;
        public RelayCommand<object> RemoveDatabaseButtonCommand
        {
            get
            {
                return removeDatabaseButtonCommand ?? (removeDatabaseButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Удаление базы данных", "Вы уверены, что хотите удалить базу данных " + Database + "?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            Stopwatch sw = new Stopwatch();
                            using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                            {
                                conn.Open();
                                sw.Restart();
                                using (MySqlCommand command = new MySqlCommand("DROP DATABASE " + "`" + Database + "`;", conn))
                                {
                                    command.ExecuteNonQuery();
                                }
                                sw.Stop();
                                conn.Close();
                            }
                            DisconnectDatabase();

                            LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "База данных успешно удалена: " + Database + ". Соединение закрыто.", Duration = sw.ElapsedMilliseconds + " мс" });
                            ThemedMessageBox.Show("Удаление базы данных", "База данных успешно удалена. Соединение закрыто.", MessageBoxButton.OK, MessageBoxImage.Information);

                            LogHelper.Log(LogHelper.Status.Ok, "База данных успешно удалена: " + Database + ". Соединение закрыто.");
                        }
                        catch (MySqlException ex)
                        {
                            LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, ex.Message);
                        }
                    }
                }, obj =>
                {
                    return !IsPartitioning && !IsDroppingPartitioning && IsConnected;
                }));
            }
        }

        private RelayCommand<object> refreshCurrPartitionTablesButtonCommand;
        public RelayCommand<object> RefreshCurrPartitionTablesButtonCommand
        {
            get
            {
                return refreshCurrPartitionTablesButtonCommand ?? (refreshCurrPartitionTablesButtonCommand = new RelayCommand<object>(async obj =>
                {
                    if (IsConnected)
                    {
                        await RefreshCurrPartitionTablesAsync();
                    }
                    else
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось обновить текущее состояние секционируемых таблиц. Нет соединения с базой данных.");
                    }
                }));
            }
        }

        private RelayCommand<object> refreshPartitionsButtonCommand;
        public RelayCommand<object> RefreshPartitionsButtonCommand
        {
            get
            {
                return refreshPartitionsButtonCommand ?? (refreshPartitionsButtonCommand = new RelayCommand<object>(async obj =>
                {
                    if (IsConnected) await RefreshPartitionsAsync();
                    else
                    {
                        LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось обновить секции таблиц. Нет соединения с базой данных.");
                    }
                }, obj =>
                {
                    return !IsRefreshingPartitioning && !IsRemovingPartitioning && !IsPartitioning && !IsDroppingPartitioning;
                }));
            }
        }

        private RelayCommand<object> clearPartitionsButtonCommand;
        public RelayCommand<object> ClearPartitionsButtonCommand
        {
            get
            {
                return clearPartitionsButtonCommand ?? (clearPartitionsButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Удаление секционирования таблиц", "Вы уверены, что хотите удалить секционирование таблиц?", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            if (IsConnected)
                            {
                                Thread clearPartitionsThread = new Thread(ClearPartitions);
                                clearPartitionsThread.Start();
                            }
                            else
                            {
                                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                                LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить секционирование таблиц. Нет соединения с базой данных.");
                            }
                            break;

                        case MessageBoxResult.Cancel:
                            break;
                    }
                }, obj =>
                {
                    return !IsRefreshingPartitioning && !IsRemovingPartitioning && !IsPartitioning && !IsDroppingPartitioning;
                }));
            }
        }

        private RelayCommand<object> clearProceduresButtonCommand;
        public RelayCommand<object> ClearProceduresButtonCommand
        {
            get
            {
                return clearProceduresButtonCommand ?? (clearProceduresButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Удаление хранимых процедур секционирования", "Вы уверены, что хотите удалить хранимые процедуры секционирования?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (IsConnected)
                        {
                            Stopwatch sw = new Stopwatch();
                            try
                            {
                                string query = "";
                                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                                {
                                    conn.Open();
                                    using (MySqlCommand command = new MySqlCommand("", conn))
                                    {
                                        sw.Restart();
                                        foreach (Table table in TablesItems)
                                        {
                                            foreach (StoredProcedure procedure in table.Procedures)
                                            {
                                                query = "DROP PROCEDURE " + procedure.Name + ";";
                                                command.CommandText = query;
                                                command.ExecuteNonQuery();
                                            }
                                            table.Procedures.Clear();
                                        }
                                        sw.Stop();

                                        command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Хранимые процедуры секционирования удалены', '" + Database + "', '00:00:00 sec');";
                                        command.ExecuteNonQuery();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Хранимые процедуры секционирования успешно удалены.", Duration = sw.ElapsedMilliseconds + " мс" });
                                        Application.Current.Dispatcher.Invoke(new Action(() =>
                                        {
                                            ThemedMessageBox.Show("Удаление хранимых процедур", "Хранимые процедуры секционирования успешно удалены.", MessageBoxButton.OK, MessageBoxImage.Information);
                                        }));

                                        LogHelper.Log(LogHelper.Status.Ok, "Хранимые процедуры секционирования успешно удалены.");
                                    }
                                    conn.Close();
                                }
                            }
                            catch (MySqlException ex)
                            {
                                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                                LogHelper.Log(LogHelper.Status.Ok, ex.Message);
                            }
                        }
                        else
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить хранимые процедуры секционирования. Нет соединения с базой данных.");
                        }
                    }
                }));
            }
        }

        private RelayCommand<object> clearEventsButtonCommand;
        public RelayCommand<object> ClearEventsButtonCommand
        {
            get
            {
                return clearEventsButtonCommand ?? (clearEventsButtonCommand = new RelayCommand<object>(obj =>
                {
                    var result = ThemedMessageBox.Show("Удаление событий секционирования", "Вы уверены, что хотите удалить события секционирования?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (IsConnected)
                        {
                            Stopwatch sw = new Stopwatch();
                            try
                            {
                                string query = "";
                                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                                {
                                    conn.Open();
                                    using (MySqlCommand command = new MySqlCommand("", conn))
                                    {
                                        sw.Restart();
                                        foreach (Table table in TablesItems)
                                        {
                                            foreach (Event e in table.Events)
                                            {
                                                query = "DROP EVENT " + e.Name + ";";
                                                command.CommandText = query;
                                                command.ExecuteNonQuery();
                                            }
                                            table.Events.Clear();
                                        }
                                        sw.Stop();

                                        command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'События секционирования удалены', '" + Database + "', '00:00:00 sec');";
                                        command.ExecuteNonQuery();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "События секционирования успешно удалены.", Duration = sw.ElapsedMilliseconds + " мс" });
                                        Application.Current.Dispatcher.Invoke(new Action(() =>
                                        {
                                            ThemedMessageBox.Show("Удаление событий секционирования", "События секционирования успешно удалены.", MessageBoxButton.OK, MessageBoxImage.Information);
                                        }));

                                        LogHelper.Log(LogHelper.Status.Ok, "События секционирования успешно удалены.");
                                    }
                                    conn.Close();
                                }
                            }
                            catch (MySqlException ex)
                            {
                                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                                LogHelper.Log(LogHelper.Status.Ok, ex.Message);
                            }
                        }
                        else
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить события секционирования. Нет соединения с базой данных.");
                        }
                    }
                }));
            }
        }

        #endregion
        
        /// <summary>
        /// Конструктор окна
        /// </summary>

        public MainWindowVM()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandle;

            PartitionTables.ListChanged += new ListChangedEventHandler(ConfigTables_ListChanged);

            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);

            LogCollection = CollectionViewSource.GetDefaultView(LogItems);
            BindingOperations.EnableCollectionSynchronization(LogItems, syncLock);

            Initialize();
            LastFileName = "";

            Status = "БД не доступна";
            IPPort = "IP:";
            BDAndUser = "БД:";
            PartitionStatus = 2;
            IPStatus = 2;
            DatabaseStatus = 2;
            IsConnected = false;

            timer.Elapsed += async (sender, e) => await Update();
            reconnectTimer.Elapsed += async (sender, e) => await Reconnect();
            ipTimer.Elapsed += async (sender, e) => await CheckIP();
            configCheckTimer.Elapsed += async (sender, e) => await CheckLastConfig();

            ipTimer.Start();

            if (Properties.Settings.Default.lastFileName != "") OpenFile(Properties.Settings.Default.lastFileName);
        }

        #region Methods

        /// <summary>
        /// Метод инициализации окна, задания стандартных настроек
        /// </summary>

        public void Initialize()
        {
            if (reconnectTimer != null) reconnectTimer.Stop();
            if (configCheckTimer != null) configCheckTimer.Stop();

            FileName = "Новая конфигурация";

            IP = "127.0.0.1";
            Port = "3306";
            Database = "";
            Username = "root";
            Password = "";
            PartitionLog = "partition_log";
            PartitionConfig = "partition_config";
            DumpExePath = "C:\\Program Files\\MariaDB 10.5\\bin\\";

            LastPartitionLogEntry = 0;
            ClearAndReadPartitionLog = false;

            Tables.Clear();
            PartitionTables.Clear();

            EventSchedulerState = "нет доступа к конфигурации БД";
            EventSchedulerColor = "#ff0000";

            IsRefreshingPartitioning = false;
            IsRemovingPartitioning = false;

            NewOpenButtonsEnabled = true;

            SaveEnabled = false;
        }

        /// <summary>
        /// Метод сравнения файла конфигурации со схемой ConfigSchema.xsd для проверки на совместимость
        /// </summary>

        public bool ValidateSchema(string xmlPath, string xsdPath, bool isString = false)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                if (isString) xml.LoadXml(xmlPath);
                else xml.Load(xmlPath);

                xml.Schemas.Add(null, xsdPath);
            }
            catch(Exception)
            {
                return true;
            }

            try
            {
                xml.Validate(null);
            }
            catch (XmlSchemaValidationException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Метод экспорта конфигурации из БД в программу
        /// </summary>

        public void ExportConfig(string content, string name)
        {
            try
            {
                MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                var serializer = new XmlSerializer(typeof(Config));
                Config config;

                using (TextReader reader = new StringReader(content))
                {
                    config = serializer.Deserialize(reader) as Config;
                }

                Tables.Clear();

                PartitionTables.Clear();

                foreach (SettingsTable table in config.Tables) Tables.Add(table);
                foreach (PartitionTable table in config.PartitionTables) PartitionTables.Add(table);

                if (PartitionTables.Count > 0) SelectedPartitionTable = PartitionTables.First();

                Database = config.DataBase;
                IP = config.IP;
                Port = config.Port;
                Password = Security.Unprotect(config.Password);
                Username = config.UserName;
                PartitionLog = config.PartitionLog;
                PartitionConfig = config.PartitionConfig;
                DumpExePath = config.DumpExePath;

                if (mainWindow.richEditControl1 != null) mainWindow.richEditControl1.Text = content;

                SaveEnabled = true;
                ConnectToDatabase();

                LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Импортирована конфигурация из БД: " + name + ".", Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Info, "Импортирована конфигурация из БД: " + name + ".");
            }
            catch (Exception ex)
            {
                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, "Не удалось импортировать конфигурацию " + fileName + ".", ex);
            }
        }

        /// <summary>
        /// Метод открытия файла конфигурации
        /// </summary>

        public void OpenFile(string fileName)
        {
            if (ValidateSchema(fileName, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigSchema.xsd")))
            {
                try
                {
                    if (timer != null) timer.Stop();
                    if (reconnectTimer != null) reconnectTimer.Stop();
                    if (IsConnected) DisconnectDatabase();
                    if (configCheckTimer != null) configCheckTimer.Stop();

                    MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

                    if (!Checker.IsValid(fileName))
                    {
                        ThemedMessageBox.Show(mainWindow.Title, "Загруженный файл конфигурации " + fileName + " редактировался вручную или был повреждён.", MessageBoxButton.OK, MessageBoxImage.Warning);
                        LogItems.Add(new Log { Status = "Warning", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Загруженный файл конфигурации " + fileName + " редактировался вручную или был повреждён.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Warning, "Загруженный файл конфигурации " + fileName + " редактировался вручную или был повреждён.");
                    }

                    FileStream stream = new FileStream(fileName, FileMode.Open);

                    XmlSerializer formatter = new XmlSerializer(typeof(Config));
                    Config config = formatter.Deserialize(stream) as Config;

                    stream.Close();

                    string hash = Checker.GetFileHash(fileName);
                    using (StreamWriter sw = new StreamWriter(fileName, true)) sw.Write("\r\nCRC:" + hash);

                    Tables.Clear();

                    PartitionTables.Clear();

                    foreach (SettingsTable table in config.Tables) Tables.Add(table);
                    foreach (PartitionTable table in config.PartitionTables) PartitionTables.Add(table);

                    if (PartitionTables.Count > 0) SelectedPartitionTable = PartitionTables.First();

                    Database = config.DataBase;
                    IP = config.IP;
                    Port = config.Port;
                    Password = Security.Unprotect(config.Password);
                    Username = config.UserName;
                    PartitionLog = config.PartitionLog;
                    PartitionConfig = config.PartitionConfig;

                    FileName = fileName;
                    LastFileName = FileName;

                    NewOpenButtonsEnabled = true;

                    SaveEnabled = false;

                    LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Открыт файл конфигурации " + fileName + ".", Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Info, "Открыт файл конфигурации " + fileName + ".");

                    if (mainWindow.richEditControl1 != null) mainWindow.richEditControl1.LoadDocument(FileName, DocumentFormat.PlainText);

                    ConnectToDatabase();
                }
                catch (Exception ex)
                {
                    LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Error, "Не удалось открыть файл конфигурации " + fileName + ".", ex);
                }
            }
            else
            {
                ThemedMessageBox.Show("Загрузка конфигурации", "Не удается прочитать файл по адресу: " + fileName + "\nВозможно версия конфигурации отличается от версии программы, либо файл конфигурации поврежден, либо файл защищён от чтения.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось открыть файл конфигурации " + fileName + ". Файл несовместим.", Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, "Не удалось открыть файл конфигурации " + fileName + ". Файл несовместим.");
            }
        }

        /// <summary>
        /// Метод обновления текущего состояния таблиц на вкладке "Диагностика"
        /// </summary>

        public void RefreshCurrPartitionTables()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        if (LastConfig != null && LastConfig.PartitionTables.Count > 0)
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Начато обновление текущего состояния секционируемых таблиц.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Начато обновление текущего состояния секционируемых таблиц.");

                            List<PartitionTableDiagnostics> tables = new List<PartitionTableDiagnostics>();
                            Stopwatch sw = new Stopwatch();
                            sw.Restart();

                            foreach (PartitionTable table in LastConfig.PartitionTables.ToList()) tables.Add(new PartitionTableDiagnostics
                            {
                                Name = table.Name,
                                DepthVal = table.DepthVal,
                                SectorSizeMeasureSelectedItem = table.SectorSizeMeasureSelectedItem,
                                DepthMeasureSelectedItem = table.DepthMeasureSelectedItem
                            });

                            DateTime todayDate = DateTime.Today;
                            int sectorCount = 0;
                            string date = "";

                            using (MySqlCommand command = new MySqlCommand("", conn))
                            {
                                foreach (PartitionTableDiagnostics table in tables)
                                {
                                    command.CommandText = "SELECT `PARTITION_NAME` FROM information_schema.partitions WHERE TABLE_SCHEMA = '" + Database + "' AND TABLE_NAME = '" + table.Name + "' AND PARTITION_NAME IS NOT NULL";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            date = reader.GetString("PARTITION_NAME").Substring(1);
                                            switch (table.SectorSizeMeasureSelectedItem)
                                            {
                                                case 0:
                                                    if (date != "_future" && DateTime.ParseExact(date, "yyyyMMdd", null) <= todayDate) sectorCount++;
                                                    break;

                                                case 1:
                                                    if (date != "_future" && DateTime.ParseExact(date, "yyyyMM", null) <= todayDate) sectorCount++;
                                                    break;

                                                case 2:
                                                    if (date != "_future" && DateTime.ParseExact(date, "yyyy", null) <= todayDate) sectorCount++;
                                                    break;
                                            }
                                        }
                                    }
                                    table.SectorCount = sectorCount;

                                    command.CommandText = "ANALYZE TABLE `" + table.Name + "`";
                                    command.ExecuteNonQuery();

                                    command.CommandText = "SHOW TABLE STATUS WHERE Name = '" + table.Name + "'";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            table.TableSize = (reader.GetInt64("Data_Length") + reader.GetInt64("Index_Length")) >> 20;
                                            table.AvgEntrySize = reader.GetInt64("Avg_row_length");
                                            table.EntryCount = reader.GetInt64("Rows");
                                        }
                                    }

                                    PartitionTableDiagnostics tableToReplace = CurrPartitionTables.Where(x => x.Name == table.Name).FirstOrDefault();
                                    PartitionTableDiagnostics tableToRemove = CurrPartitionTables.Where(x => !tables.Exists(y => y.Name == x.Name)).FirstOrDefault();
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        if (tableToReplace != null && tableToReplace != table)
                                        {
                                            CurrPartitionTables.Remove(tableToReplace);
                                            CurrPartitionTables.Add(table);
                                        }
                                        else if (tableToReplace == null) CurrPartitionTables.Add(table);

                                        if (tableToRemove != null) CurrPartitionTables.Remove(tableToRemove);
                                    }));
                                    sectorCount = 0;
                                }
                            }
                            sw.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Обновлено текущее состояние секционируемых таблиц.", Duration = sw.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Обновлено текущее состояние секционируемых таблиц.");
                        }
                        else
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось обновить текущее состояние секционируемых таблиц. Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось обновить текущее состояние секционируемых таблиц. Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.");
                        }
                    }
                    conn.Close();
                }
            }
            catch(MySqlException ex)
            {
                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, ex.Message);
            }
        }

        async Task RefreshCurrPartitionTablesAsync()
        {
            await Task.Run(() => RefreshCurrPartitionTables());
        }

        async Task CreateReportAsync(string directoryPath)
        {
            await Task.Run(() => CreateReport(directoryPath));
        }

        /// <summary>
        /// Метод формирования отчета в папку "Output"
        /// </summary>

        public void CreateReport(string directoryPath)
        {
            try
            {
                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Началось формирование отчета.", Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Ok, "Началось формирование отчета.");

                Stopwatch stopwatch = new Stopwatch();
                string path = Path.Combine(directoryPath, "LastConfig.cfg");

                if (LastConfig != null && LastConfig.PartitionTables.Count > 0)
                {
                    stopwatch.Restart();
                    XmlSerializer formatter = new XmlSerializer(typeof(Config));
                    using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) formatter.Serialize(stream, LastConfig);

                    string hash = Checker.GetFileHash(path);
                    using (StreamWriter sw = new StreamWriter(path, true)) sw.Write("\r\nCRC:" + hash);
                    stopwatch.Stop();

                    lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Скопирован последний файл конфигурации \"LastConfig.cfg\".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                    LogHelper.Log(LogHelper.Status.Ok, "Скопирован последний файл конфигурации LastConfig.cfg.");
                }
                else
                {
                    lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось скопировать файл последней конфигурации секционирования с сервера для формирования отчета. Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.", Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Error, "Не удалось скопировать файл последней конфигурации секционирования с сервера для формирования отчета. Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.");
                }

                stopwatch.Restart();
                path = Path.Combine(directoryPath, "Текущее состояние таблиц.txt");
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.WriteLine("Текущее состояние секционированных таблиц:");
                    sw.WriteLine("------------------------------------------------------------------------------------------------------------------");
                    sw.WriteLine("  Таблица в БД\t     Кол-во значащих секторов\tГлубина хранения данных\t      Кол-во записей\tРазмер таблицы, МБ");
                    foreach (PartitionTableDiagnostics table in CurrPartitionTables.ToList()) sw.WriteLine("{0, 14}\t     {1, 24}\t            {2, 5} {3, 5}\t{4, 20}\t{5, 18}", table.Name, table.SectorCount, table.DepthVal, DepthMeasure[table.DepthMeasureSelectedItem], table.EntryCount, table.TableSize);
                }
                stopwatch.Stop();

                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Скопировано текущее состояние секционируемых таблиц в файл \"Текущее состояние таблиц.txt\".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                LogHelper.Log(LogHelper.Status.Ok, "Скопировано текущее состояние секционируемых таблиц в файл \"Текущее состояние таблиц.txt\".");

                stopwatch.Restart();
                foreach (Table table in TablesItems.ToList())
                {
                    path = Path.Combine(directoryPath, table.Name);
                    Directory.CreateDirectory(path);
                    foreach (StoredProcedure procedure in table.Procedures) using (StreamWriter sw = new StreamWriter(Path.Combine(path, procedure.Name + ".sql"), true)) sw.Write(procedure.QueryText);
                    foreach (Event e in table.Events) using (StreamWriter sw = new StreamWriter(Path.Combine(path, e.Name + ".sql"), true)) sw.Write(e.QueryText);
                }
                stopwatch.Stop();

                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Скопированы скрипты создания хранимых процедур и событий секционирования.", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                LogHelper.Log(LogHelper.Status.Ok, "Скопированы скрипты создания хранимых процедур и событий секционирования.");

                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        using (MySqlCommand command = new MySqlCommand("", conn))
                        {
                            stopwatch.Restart();
                            foreach (Table table in TablesItems.ToList())
                            {
                                command.CommandText = "SHOW CREATE TABLE " + table.Name;
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    using (StreamWriter sw = new StreamWriter(Path.Combine(directoryPath, table.Name, "create_table_" + table.Name + ".sql"), true))
                                    {
                                        while (reader.Read()) sw.Write(reader.GetString(1));
                                    }
                                }
                            }
                            stopwatch.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Скопированы запросы CREATE TABLE.", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Скопированы запросы CREATE TABLE.");

                            command.CommandText = "SELECT * FROM " + PartitionConfig;
                            stopwatch.Restart();
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                using (StreamWriter sw = new StreamWriter(Path.Combine(directoryPath, "PartitionConfig.txt"), true))
                                {
                                    while (reader.Read()) sw.WriteLine(reader.GetInt32(0) + "\t" + reader.GetString(1) + "\t" + reader.GetDateTime(2).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                }
                            }
                            stopwatch.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создан список конфигураций секционирования PartitionConfig.txt", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Создан список конфигураций секционирования PartitionConfig.txt");

                            command.CommandText = "SELECT * FROM " + PartitionLog;
                            stopwatch.Restart();
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                using (StreamWriter sw = new StreamWriter(Path.Combine(directoryPath, PartitionLog + ".txt"), true))
                                {
                                    while (reader.Read()) sw.WriteLine(reader.GetInt32(0) + "\t" + reader.GetDateTime(1).ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + reader.GetString(2) + "\t" + reader.GetString(3) + "\t" + reader.GetString(4) + "\t" + reader.GetString(5));
                                }
                            }
                            stopwatch.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Журнал секционирования записан в файл " + PartitionLog + ".txt", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Журнал секционирования записан в файл " + PartitionLog + ".txt");

                            stopwatch.Restart();
                            FileStream fs = new FileStream(directoryPath + "\\" + PartitionLog + ".sql", FileMode.Create, FileAccess.Write);
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo
                                {
                                    Arguments = "-u" + Username + " -p" + Password + " -h" + IP + " " + Database + " " + PartitionLog,
                                    FileName = Path.Combine(DumpExePath, "mysqldump.exe"),
                                    RedirectStandardInput = false,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    WindowStyle = ProcessWindowStyle.Minimized,
                                    CreateNoWindow = true
                                };
                                Process p = Process.Start(startInfo);
                                sw.Write(p.StandardOutput.ReadToEnd());
                                p.WaitForExit();
                                p.Close();
                                sw.Close();
                                fs.Close();
                            }
                            stopwatch.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создан дамп таблицы журнала секционирования " + PartitionLog + ".sql", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Создан дамп таблицы журнала секционирования " + PartitionLog + ".sql");

                            stopwatch.Restart();
                            fs = new FileStream(directoryPath + "\\" + PartitionConfig + ".sql", FileMode.Create, FileAccess.Write);
                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                ProcessStartInfo startInfo = new ProcessStartInfo
                                {
                                    Arguments = "-u" + Username + " -p" + Password + " -h" + IP + " " + Database + " " + PartitionConfig,
                                    FileName = Path.Combine(DumpExePath, "mysqldump.exe"),
                                    RedirectStandardInput = false,
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    WindowStyle = ProcessWindowStyle.Minimized,
                                    CreateNoWindow = true
                                };
                                Process p = Process.Start(startInfo);
                                sw.Write(p.StandardOutput.ReadToEnd());
                                p.WaitForExit();
                                p.Close();
                                sw.Close();
                                fs.Close();
                            }
                            stopwatch.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создан дамп таблицы со списком конфигураций секционирования " + PartitionConfig + ".sql", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Создан дамп таблицы со списком конфигураций секционирования " + PartitionConfig + ".sql");

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Отчет успешно сформирован", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Отчет успешно сформирован");
                        }
                    }
                    conn.Close();
                }
            }
            catch (MySqlException ex)
            {
                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, ex.Message);
            }
        }

        /// <summary>
        /// Метод обновления секций таблиц на вкладке "Диагностика"
        /// </summary>

        public void RefreshPartitions()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Начато обновление секционирования таблиц.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Ok, "Начато обновление секционирования таблиц.");

                        IsRefreshingPartitioning = true;

                        using (MySqlCommand command = new MySqlCommand("", conn))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Restart();
                            foreach (Table table in TablesItems.ToList())
                            {
                                Application.Current.Dispatcher.Invoke(new Action(() => { table.Partitions.Clear(); }));

                                command.CommandText = "SELECT * FROM information_schema.partitions WHERE TABLE_SCHEMA='" + Database + "' AND TABLE_NAME = '" + table.Name + "' AND PARTITION_NAME IS NOT NULL";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        Application.Current.Dispatcher.Invoke(new Action(() =>
                                        {
                                            table.Partitions.Add(new Partition
                                            {
                                                Name = reader.GetString("PARTITION_NAME"),
                                                Position = reader.GetInt32("PARTITION_ORDINAL_POSITION"),
                                                CreateTime = reader.GetDateTime("CREATE_TIME").ToString("dd.MM.yyyy HH:mm:ss:fff"),
                                                DiskSpace = (reader.GetInt64("DATA_LENGTH") + reader.GetInt64("INDEX_LENGTH")) >> 20,
                                                EntryCount = reader.GetInt64("TABLE_ROWS")
                                            });
                                        }));
                                    }
                                }
                                foreach (Partition partition in table.Partitions.ToList())
                                {
                                    command.CommandText = "SELECT MIN(`" + table.DateTimeColumn + "`) FROM `" + table.Name + "` PARTITION (" + partition.Name + ");";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read()) Application.Current.Dispatcher.Invoke(new Action(() => { partition.FirstEdit = reader.IsDBNull(0) ? "" : reader.GetDateTime(0).ToString("dd.MM.yyyy HH:mm:ss:fff"); }));
                                    }
                                    command.CommandText = "SELECT MAX(`" + table.DateTimeColumn + "`) FROM `" + table.Name + "` PARTITION (" + partition.Name + ");";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read()) Application.Current.Dispatcher.Invoke(new Action(() => { partition.LastEdit = reader.IsDBNull(0) ? "" : reader.GetDateTime(0).ToString("dd.MM.yyyy HH:mm:ss:fff"); }));
                                    }
                                }
                            }
                            sw.Stop();

                            IsRefreshingPartitioning = false;

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Секционирование таблиц успешно обновлено.", Duration = sw.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Секционирование таблиц успешно обновлено.");
                        }
                    }
                    else
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось обновить секционирование таблиц. Нет соединения с базой данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось обновить секционирование таблиц. Нет соединения с базой данных.");
                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            DisconnectDatabase();
                        }));
                    }
                    conn.Close();
                } 
            }
            catch (MySqlException ex)
            {
                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, ex.Message);
            }
        }

        async Task RefreshPartitionsAsync()
        {
            await Task.Run(() => RefreshPartitions());
        }

        /// <summary>
        /// Метод удаления секций таблиц на вкладке "Диагностика"
        /// </summary>

        public void ClearPartitions()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        IsRemovingPartitioning = true;
                        bool partitioned = false;

                        using (MySqlCommand command = new MySqlCommand("", conn))
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Restart();
                            foreach (Table table in TablesItems.ToList())
                            {
                                command.CommandText = "SHOW TABLE STATUS WHERE NAME = '" + table.Name + "'";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read()) partitioned = reader.GetString(reader.GetOrdinal("Create_options")) == "partitioned";
                                }
                                if (partitioned)
                                {
                                    command.CommandText = "ALTER TABLE " + table.Name + " REMOVE PARTITIONING";
                                    command.ExecuteNonQuery();
                                    Application.Current.Dispatcher.Invoke(new Action(() => { table.Partitions.Clear(); }));
                                }
                            }
                            sw.Stop();

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Секционирование таблиц успешно удалено.', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();

                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Секционирование таблиц успешно удалено.", Duration = sw.ElapsedMilliseconds + " мс" });
                                ThemedMessageBox.Show("Удаление секций", "Секционирование таблиц успешно удалено.", MessageBoxButton.OK, MessageBoxImage.Information);
                            }));

                            IsRemovingPartitioning = false;
                            LogHelper.Log(LogHelper.Status.Ok, "Секционирование таблиц успешно удалено.");
                        }
                    }
                    else
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить секционирование таблиц. Нет соединения с базой данных.");
                    }
                    conn.Close();
                }
            }
            catch (MySqlException ex)
            {
                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось удалить секционирование таблиц. " + ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Ok, "Не удалось удалить секционирование таблиц. " + ex.Message);
            }
        }

        /// <summary>
        /// Метод для получения последней конфигурации с сервера для использования на вкладке "Диагностика"
        /// </summary>

        public void ExportLastConfig()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    Stopwatch sw = new Stopwatch();
                    bool isPartitionConfig = false;
                    UInt32 fileSize = 0;
                    string configContent = "";

                    using (MySqlCommand command = new MySqlCommand("SELECT * FROM information_schema.tables WHERE table_schema = '" + Database + "' AND table_name = '" + PartitionConfig + "'", conn))
                    {
                        bool exists;
                        string query = "";

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            exists = reader.HasRows;
                        }
                        if (!exists)
                        {
                            sw.Restart();
                            query = "CREATE TABLE IF NOT EXISTS `" + Database + "`.`" + PartitionConfig + "`" +
                               " (" +
                               " ID INT(10) UNSIGNED NOT NULL AUTO_INCREMENT," +
                               " name VARCHAR(45) NOT NULL," +
                               " updated DATETIME(3) NOT NULL," +
                               " config LONGBLOB NOT NULL, " +
                               " flag TINYINT(1) NOT NULL DEFAULT 0," +
                               " PRIMARY KEY (ID, updated)" +
                               " )" +
                               " ENGINE = INNODB," +
                               " CHARACTER SET utf8mb4," +
                               " COLLATE utf8mb4_general_ci; ";

                            command.CommandText = query;
                            command.ExecuteNonQuery();

                            sw.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана таблица для хранения конфигураций " + PartitionConfig + ".", Duration = sw.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Создана таблица для хранения конфигураций " + PartitionConfig + ".");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Создана таблица для хранения конфигураций " + PartitionConfig + "', '" + Database + "." + PartitionConfig + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();
                        }
                        command.CommandText = "SHOW COLUMNS FROM `" + PartitionConfig + "` LIKE 'config';";
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            isPartitionConfig = reader.HasRows;
                        }

                        if (isPartitionConfig)
                        {
                            command.CommandText = "SELECT OCTET_LENGTH(config) FROM `" + PartitionConfig + "` WHERE ID = (SELECT MAX(ID) FROM `" + PartitionConfig + "`)";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    fileSize = reader.GetUInt32(0);
                                }
                            }

                            command.CommandText = "SELECT * FROM `" + PartitionConfig + "` WHERE ID = (SELECT MAX(ID) FROM `" + PartitionConfig + "`)";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    byte[] bytes = new byte[fileSize];
                                    reader.GetBytes(reader.GetOrdinal("config"), 0, bytes, 0, (int)fileSize);
                                    configContent = Encoding.UTF8.GetString(bytes);
                                    LastConfigUpdateTime = reader.GetDateTime(reader.GetOrdinal("updated"));
                                }
                            }

                            var serializer = new XmlSerializer(typeof(Config));
                            using (TextReader reader = new StringReader(configContent))
                            {
                                if (ValidateSchema(configContent, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigSchema.xsd"), true) && !string.IsNullOrEmpty(configContent))
                                {
                                    LastConfigNotFoundShown = false;
                                    LastConfig = serializer.Deserialize(reader) as Config;
                                    if (LastConfig != null && LastConfig.PartitionTables.Count > 0)
                                    {
                                        sw.Restart();
                                        InitPartitionTables.Clear();
                                        foreach (PartitionTable table in LastConfig.PartitionTables.ToList()) InitPartitionTables.Add(new PartitionTable
                                        {
                                            Name = table.Name,
                                            EntrySize = table.EntrySize,
                                            IntervalVal = table.IntervalVal,
                                            IntervalMeasureSelectedItem = table.IntervalMeasureSelectedItem,
                                            PartitionIntervalVal = table.PartitionIntervalVal,
                                            PartitionIntervalMeasureSelectedItem = table.PartitionIntervalMeasureSelectedItem,
                                            SectorSizeVal = table.SectorSizeVal,
                                            SectorSizeMeasureSelectedItem = table.SectorSizeMeasureSelectedItem,
                                            SectorCount = table.SectorCount,
                                            DepthVal = table.DepthVal,
                                            DepthMeasureSelectedItem = table.DepthMeasureSelectedItem,
                                            EntryCount = table.EntryCount,
                                            TableSize = table.TableSize
                                        });
                                        sw.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Обновлено инициализированное состояние секционируемых таблиц.", Duration = sw.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Обновлено инициализированное состояние секционируемых таблиц.");
                                    }
                                    else
                                    {
                                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось обновить инициализированное состояние секционируемых таблиц. Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.", Duration = "0 мс" });
                                        LogHelper.Log(LogHelper.Status.Error, "Не удалось обновить инициализированное состояние секционируемых таблиц. Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.");
                                    }
                                }
                                else
                                {
                                    InitPartitionTables.Clear();
                                    CurrPartitionTables.Clear();
                                    LastConfig = new Config();

                                    if (!LastConfigNotFoundShown)
                                    {
                                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.", Duration = "0 мс" });
                                        LogHelper.Log(LogHelper.Status.Error, "Не найдена последняя совместимая конфигурация секционирования на сервере. Сконфигурируйте новое секционирование сервера.");
                                        LastConfigNotFoundShown = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось загрузить последнюю конфигурацию с сервера для обновления данных на вкладке \"Диагностика\". Таблица " + PartitionConfig + " не является таблицей для хранения конфигураций.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось загрузить последнюю конфигурацию с сервера для обновления данных на вкладке \"Диагностика\". Таблица " + PartitionConfig + " не является таблицей для хранения конфигураций.");
                        }
                    }
                    conn.Close();
                }
            }
            catch (MySqlException ex)
            {
                LogHelper.Log(LogHelper.Status.Error, "Не удалось загрузить последнюю конфигурацию с сервера для обновления данных на вкладке \"Диагностика\".", ex);
            }
        }

        /// <summary>
        /// Метод для создания главного подключения к базе данных, оно обрывается только при исключительных ситуациях или при открытии новой конфигурации
        /// </summary>

        public void ConnectToDatabase()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        IsConnected = true;
                        LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Подключение к серверу успешно. Имя пользователя: " + Username + "; БД: " + Database, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Ok, "Подключение к серверу успешно. Имя пользователя: " + Username + "; БД: " + Database);
                        Status = "Не секционировано";
                        PartitionStatus = 2;
                        DatabaseStatus = 0;

                        string status;
                        using (MySqlCommand command = new MySqlCommand("SHOW VARIABLES WHERE VARIABLE_NAME = 'event_scheduler'", conn))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                status = reader.GetString(1);
                                if (status == "ON") EventSchedulerEnabled = true;
                                else if (status == "OFF") EventSchedulerEnabled = false;
                            }
                        } 

                        timer.Start();
                        reconnectTimer.Stop();

                        ExportLastConfig();
                        configCheckTimer.Start();

                        TablesItems.Clear();
                        foreach (PartitionTable table in PartitionTables) TablesItems.Add(new Table { Name = table.Name, DateTimeColumn = table.DateTimeColumn });

                        RefreshCurrPartitionTablesButtonCommand.Execute(this);
                        RefreshPartitionsButtonCommand.Execute(this);

                        ReconnectShown = false;
                        LastConfigNotFoundShown = false;

                        IsRefreshingPartitioning = false;
                        IsRemovingPartitioning = false;
                        conn.Close();
                    }
                }
            }
            catch (MySqlException exc)
            {
                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = exc.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, "Не удалось подключиться к базе данных " + Database + ".", exc);
                Status = "БД не доступна";
                PartitionStatus = 2;
                DatabaseStatus = 2;

                if (!String.IsNullOrEmpty(Database) && !String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(Password)) reconnectTimer.Start();
            }
        }

        /// <summary>
        /// Метод секционирования БД
        /// </summary>

        public void ConfigureDB()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";Allow User Variables=True" + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        string script = "";
                        DateTime todayDate;

                        int emptyCount = 0;

                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Началось секционирование БД.", Duration = "0 мс" });
                        Status = "Секционирование";
                        PartitionStatus = 1;
                        IsPartitioning = true;
                        LogHelper.Log(LogHelper.Status.Ok, "Началось секционирование БД.");

                        using (MySqlCommand command = new MySqlCommand("", conn))
                        {
                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Началось секционирование БД " + Database + "', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();

                            stopwatch.Restart();
                            foreach (Table table in TablesItems.ToList())
                            {
                                if (table.Procedures.Count == 0) emptyCount++;
                                foreach (StoredProcedure procedure in table.Procedures)
                                {
                                    command.CommandText = "DROP PROCEDURE " + procedure.Name + ";";
                                    command.ExecuteNonQuery();
                                }
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    table.Procedures.Clear();
                                    table.Partitions.Clear();
                                }));
                            }
                            stopwatch.Stop();

                            if (emptyCount != TablesItems.Count)
                            {
                                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Хранимые процедуры секционирования успешно уничтожены.", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                LogHelper.Log(LogHelper.Status.Ok, "Хранимые процедуры секционирования успешно уничтожены.");
                            }
                            emptyCount = 0;

                            stopwatch.Restart();
                            foreach (Table table in TablesItems.ToList())
                            {
                                if (table.Events.Count == 0) emptyCount++;
                                foreach (Event e in table.Events.ToList())
                                {
                                    command.CommandText = "DROP EVENT " + e.Name + ";";
                                    command.ExecuteNonQuery();
                                }
                                Application.Current.Dispatcher.Invoke(new Action(() => { table.Events.Clear(); }));
                            }
                            stopwatch.Stop();

                            if (emptyCount != TablesItems.Count)
                            {
                                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "События секционирования успешно уничтожены.", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                LogHelper.Log(LogHelper.Status.Ok, "События секционирования успешно уничтожены.");
                            }

                            foreach (PartitionTable table in PartitionTables.ToList())
                            {
                                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Началось предварительное секционирование таблицы " + table.Name + ".", Duration = "0 мс" });
                                LogHelper.Log(LogHelper.Status.Ok, "Началось предварительное секционирование таблицы " + table.Name + ".");

                                stopwatch.Restart();
                                switch (table.SectorSizeMeasureSelectedItem)
                                {
                                    case 0:

                                        todayDate = DateTime.Today;
                                        script = "alter table " + Database + "." + table.Name + " PARTITION BY RANGE ( TO_DAYS(" + table.DateTimeColumn + ") ) ( ";

                                        for (long i = -(table.SectorCount - 2); i <= 2; i++)
                                        {
                                            script += "PARTITION p" + todayDate.AddDays((double)(i * table.SectorSizeVal)).ToString("yyyyMMdd") + " VALUES LESS THAN( TO_DAYS('" + todayDate.AddDays((double)(i * table.SectorSizeVal)).ToString("yyyy-MM-dd HH:mm:ss") + "')), ";
                                        }

                                        script += "PARTITION p" + todayDate.AddDays((double)3 * table.SectorSizeVal).ToString("yyyyMMdd") + " VALUES LESS THAN ( TO_DAYS('" + todayDate.AddDays((double)3 * table.SectorSizeVal).ToString("yyyy-MM-dd HH:mm:ss") + "')), PARTITION p_future VALUES LESS THAN (MAXVALUE) );";
                                        break;

                                    case 1:

                                        todayDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                                        script = "alter table " + Database + "." + table.Name + " PARTITION BY RANGE ( TO_DAYS(" + table.DateTimeColumn + ") ) ( ";

                                        for (long i = -(table.SectorCount - 2); i <= 2; i++)
                                        {
                                            script += "PARTITION p" + todayDate.AddMonths((int)(i * table.SectorSizeVal)).ToString("yyyyMM") + " VALUES LESS THAN( TO_DAYS('" + todayDate.AddMonths((int)(i * table.SectorSizeVal)).ToString("yyyy-MM-dd HH:mm:ss") + "')), ";
                                        }

                                        script += "PARTITION p" + todayDate.AddMonths((int)(3 * table.SectorSizeVal)).ToString("yyyyMM") + " VALUES LESS THAN ( TO_DAYS('" + todayDate.AddMonths((int)(3 * table.SectorSizeVal)).ToString("yyyy-MM-dd HH:mm:ss") + "')), PARTITION p_future VALUES LESS THAN (MAXVALUE) );";
                                        break;

                                    case 2:

                                        todayDate = new DateTime(DateTime.Today.Year, 1, 1);
                                        script = "alter table " + Database + "." + table.Name + " PARTITION BY RANGE ( TO_DAYS(" + table.DateTimeColumn + ") ) ( ";

                                        for (long i = -(table.SectorCount - 2); i <= 2; i++)
                                        {
                                            script += "PARTITION p" + todayDate.AddYears((int)(i * table.SectorSizeVal)).ToString("yyyy") + " VALUES LESS THAN( TO_DAYS('" + todayDate.AddYears((int)(i * table.SectorSizeVal)).ToString("yyyy-MM-dd HH:mm:ss") + "')), ";
                                        }

                                        script += "PARTITION p" + todayDate.AddYears((int)(3 * table.SectorSizeVal)).ToString("yyyy") + " VALUES LESS THAN ( TO_DAYS('" + todayDate.AddYears((int)(3 * table.SectorSizeVal)).ToString("yyyy-MM-dd HH:mm:ss") + "')), PARTITION p_future VALUES LESS THAN (MAXVALUE) );";
                                        break;
                                }
                                command.CommandText = script;
                                command.ExecuteNonQuery();
                                stopwatch.Stop();

                                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Предварительное секционирование таблицы " + table.Name + " успешно завершено.", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                LogHelper.Log(LogHelper.Status.Ok, "Предварительное секционирование таблицы " + table.Name + " успешно завершено.");

                            }

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Предварительное секционирование секционируемых таблиц в БД успешно завершено.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Предварительное секционирование секционируемых таблиц в БД успешно завершено.");

                            stopwatch.Restart();

                            command.CommandText = "SET GLOBAL event_scheduler = ON;";
                            command.ExecuteNonQuery();
                            EventSchedulerEnabled = true;

                            stopwatch.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Планировщик событий включен.", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Планировщик событий включен.");

                            command.CommandText = "SELECT * FROM information_schema.tables WHERE table_schema = '" + Database + "' AND table_name = '" + PartitionLog + "'";
                            bool exists = false;

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                exists = reader.HasRows;
                            }

                            if (!exists)
                            {
                                stopwatch.Restart();

                                script = "CREATE TABLE IF NOT EXISTS `" + Database + "`.`" + PartitionLog + "`" +
                                " (" +
                                " id int(10) unsigned NOT NULL AUTO_INCREMENT," +
                                " time datetime(3) NOT NULL," +
                                " status varchar(100) NOT NULL," +
                                " action varchar(150) NOT NULL, " +
                                " message varchar(200) NOT NULL," +
                                " duration varchar(45) DEFAULT NULL," +
                                " PRIMARY KEY (id)" +
                                " )" +
                                " ENGINE = INNODB," +
                                " AUTO_INCREMENT = 6680," +
                                " CHARACTER SET utf8mb4," +
                                " COLLATE utf8mb4_general_ci; ";
                                command.CommandText = script;
                                command.ExecuteNonQuery();

                                stopwatch.Stop();

                                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создан журнал секционирования " + PartitionLog + " .", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                LogHelper.Log(LogHelper.Status.Ok, "Создан журнал секционирования " + PartitionLog + " .");
                            }

                            foreach (PartitionTable table in PartitionTables.ToList())
                            {
                                switch (table.SectorSizeMeasureSelectedItem)
                                {
                                    case 0:

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Procedure_DropOldPartitions_Days.sql")).Replace("schema_name", Database).Replace("size", "" + table.SectorSizeVal).Replace("sectorSize", "" + table.SectorSizeVal).Replace("table_name", table.Name);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана процедура удаления старых секций для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана процедура удаления старых секций для таблицы " + table.Name + ".");

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Procedure_CreateNewPartitions_Days.sql")).Replace("schema_name", Database).Replace("size", "" + table.SectorSizeVal).Replace("sectorSize", "" + table.SectorSizeVal).Replace("table_name", table.Name);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана процедура создания новых секций для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана процедура создания новых секций для таблицы " + table.Name + ".");

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Event_PartitionAttendance.sql")).Replace("schema_name", Database).Replace("partitionSize", "" + table.PartitionIntervalVal).Replace("measure", IntervalMeasureToStringConverter.Convert(table.PartitionIntervalMeasureSelectedItem)).Replace("sectorSize", "" + table.SectorSizeVal).Replace("sectorCount", "" + table.SectorCount).Replace("table_name", table.Name).Replace("tableMaxSize", "" + Math.Round(table.TableSize / 1024m, 2, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("en-US"))).Replace("partition_log", "" + PartitionLog);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создано событие секционирования для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана событие секционирования для таблицы " + table.Name + ".");
                                        break;

                                    case 1:

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Procedure_DropOldPartitions_Months.sql")).Replace("schema_name", Database).Replace("size", "" + table.SectorSizeVal).Replace("sectorSize", "" + table.SectorSizeVal).Replace("table_name", table.Name);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана процедура удаления старых секций для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана процедура удаления старых секций для таблицы " + table.Name + ".");

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Procedure_CreateNewPartitions_Months.sql")).Replace("schema_name", Database).Replace("size", "" + table.SectorSizeVal).Replace("sectorSize", "" + table.SectorSizeVal).Replace("table_name", table.Name);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана процедура создания новых секций для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана процедура создания новых секций для таблицы " + table.Name + ".");

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Event_PartitionAttendance.sql")).Replace("days", "months").Replace("schema_name", Database).Replace("partitionSize", "" + table.PartitionIntervalVal).Replace("measure", IntervalMeasureToStringConverter.Convert(table.PartitionIntervalMeasureSelectedItem)).Replace("sectorSize", "" + table.SectorSizeVal).Replace("sectorCount", "" + table.SectorCount).Replace("table_name", table.Name).Replace("tableMaxSize", "" + Math.Round(table.TableSize / 1024m, 2, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("en-US"))).Replace("partition_log", "" + PartitionLog);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создано событие секционирования для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана событие секционирования для таблицы " + table.Name + ".");
                                        break;

                                    case 2:

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Procedure_DropOldPartitions_Years.sql")).Replace("schema_name", Database).Replace("size", "" + table.SectorSizeVal).Replace("sectorSize", "" + table.SectorSizeVal).Replace("table_name", table.Name);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана процедура удаления старых секций для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана процедура удаления старых секций для таблицы " + table.Name + ".");

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Procedure_CreateNewPartitions_Years.sql")).Replace("schema_name", Database).Replace("size", "" + table.SectorSizeVal).Replace("sectorSize", "" + table.SectorSizeVal).Replace("table_name", table.Name);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана процедура создания новых секций для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана процедура создания новых секций для таблицы " + table.Name + ".");

                                        stopwatch.Restart();

                                        script = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"SQL Scripts\Event_PartitionAttendance.sql")).Replace("days", "years").Replace("schema_name", Database).Replace("partitionSize", "" + table.PartitionIntervalVal).Replace("measure", IntervalMeasureToStringConverter.Convert(table.PartitionIntervalMeasureSelectedItem)).Replace("sectorSize", "" + table.SectorSizeVal).Replace("sectorCount", "" + table.SectorCount).Replace("table_name", table.Name).Replace("tableMaxSize", "" + Math.Round(table.TableSize / 1024m, 2, MidpointRounding.AwayFromZero).ToString(CultureInfo.GetCultureInfo("en-US"))).Replace("partition_log", "" + PartitionLog);
                                        command.CommandText = script;
                                        command.ExecuteNonQuery();

                                        stopwatch.Stop();

                                        lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создано событие секционирования для таблицы " + table.Name + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                        LogHelper.Log(LogHelper.Status.Ok, "Создана событие секционирования для таблицы " + table.Name + ".");
                                        break;
                                }
                            }

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Созданы процедуры удаления старых и создания новых секций, события автоматического запуска процедур.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Созданы процедуры удаления старых и создания новых секций, события автоматического запуска процедур.");

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Секционирование БД " + Database + " успешно завершено.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Секционирование БД " + Database + " успешно завершено.");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Секционирование БД " + Database + " успешно завершено', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();

                            IsPartitioning = false;

                            PartitionStatus = 0;
                            Status = "Секционировано";

                            command.CommandText = "SELECT * FROM information_schema.tables WHERE table_schema = '" + Database + "' AND table_name = '" + PartitionConfig + "'";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                exists = reader.HasRows;
                            }

                            if (!exists)
                            {
                                stopwatch.Restart();
                                script = "CREATE TABLE IF NOT EXISTS `" + Database + "`.`" + PartitionConfig + "`" +
                                   " (" +
                                   " ID INT(10) UNSIGNED NOT NULL AUTO_INCREMENT," +
                                   " name VARCHAR(45) NOT NULL," +
                                   " updated DATETIME(3) NOT NULL," +
                                   " config LONGBLOB NOT NULL, " +
                                   " flag TINYINT(1) NOT NULL DEFAULT 0," +
                                   " PRIMARY KEY (ID, updated)" +
                                   " )" +
                                   " ENGINE = INNODB," +
                                   " CHARACTER SET utf8mb4," +
                                   " COLLATE utf8mb4_general_ci; ";

                                command.CommandText = script;
                                command.ExecuteNonQuery();

                                stopwatch.Stop();

                                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создана таблица для хранения конфигураций " + PartitionConfig + ".", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                LogHelper.Log(LogHelper.Status.Ok, "Создана таблица для хранения конфигураций " + PartitionConfig + ".");

                                command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Создана таблица для хранения конфигураций " + PartitionConfig + "', '" + Database + "." + PartitionConfig + "', '00:00:00 sec');";
                                command.ExecuteNonQuery();
                            }

                            if (FileName != "Новая конфигурация")
                            {
                                bool isPartitionConfig = false;
                                command.CommandText = "SHOW COLUMNS FROM `" + PartitionConfig + "` LIKE 'config';";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    isPartitionConfig = reader.HasRows;
                                }

                                if (isPartitionConfig)
                                {
                                    string text = File.ReadAllText(FileName, Encoding.UTF8);
                                    string file = Path.GetFileNameWithoutExtension(FileName);

                                    int index = text.IndexOf("\r\nCRC:");
                                    string config = text.Substring(0, index);
                                    File.WriteAllText(FileName, config);

                                    byte[] bytes = File.ReadAllBytes(FileName);

                                    string hash = Checker.GetFileHash(FileName);
                                    using (StreamWriter sw = new StreamWriter(FileName, true)) sw.Write("\r\nCRC:" + hash);

                                    stopwatch.Restart();
                                    command.CommandText = "INSERT INTO `" + PartitionConfig + "` (`ID`, `name`, `updated`, `config`) VALUES(NULL, '" + file + "', @Time, @File)";
                                    command.Parameters.AddWithValue("@Time", DateTime.Now);
                                    command.Parameters.AddWithValue("@File", bytes);
                                    command.ExecuteNonQuery();
                                    stopwatch.Stop();

                                    command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Экспорт конфигурации в БД " + file + "', '" + Database + "." + PartitionConfig + "', '00:00:00 sec');";
                                    command.Parameters.Clear();
                                    command.ExecuteNonQuery();

                                    lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Текущая конфигурация успешно загружена в базу данных.", Duration = stopwatch.ElapsedMilliseconds + " мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Текущая конфигурация успешно загружена в базу данных.");

                                    Application.Current.Dispatcher.Invoke(new Action(() => { ExportLastConfig(); }));
                                }
                                else
                                {
                                    lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось загрузить последнюю конфигурацию в базу данных. Таблица " + PartitionConfig + " не является таблицей для хранения конфигураций.", Duration = "0 мс" });
                                    LogHelper.Log(LogHelper.Status.Error, "Не удалось загрузить последнюю конфигурацию в базу данных. Таблица " + PartitionConfig + " не является таблицей для хранения конфигураций.");
                                }
                            }
                        }
                        RefreshPartitionsButtonCommand.Execute(this);
                    }
                    else
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось секционировать БД. Нет соединения с базой данных.");
                        IsPartitioning = false;
                    }
                    conn.Close();
                }
            }
            catch (MySqlException ex)
            {
                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Ошибка во время конфигурации БД. " + ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Error, "Ошибка во время конфигурации БД. " + ex.Message);
                IsPartitioning = false;
            }
        }

        /// <summary>
        /// Метод очистки журнала секционирования
        /// </summary>

        public void ClearPartitionLog()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn != null && conn.State == System.Data.ConnectionState.Open)
                    {
                        Stopwatch sw = new Stopwatch();
                        using (MySqlCommand command = new MySqlCommand("TRUNCATE TABLE `" + PartitionLog + "`", conn))
                        {
                            sw.Restart();
                            command.ExecuteNonQuery();
                            sw.Stop();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Журнал секционирования очищен (" + PartitionLog + ").", Duration = sw.ElapsedMilliseconds + " мс" });
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                PartitionLogItems.Clear();
                                ThemedMessageBox.Show("Журнал секционирования", "Журнал секционирования очищен (" + PartitionLog + ").", MessageBoxButton.OK, MessageBoxImage.Information);
                            }));
                            LastPartitionLogEntry = 0;
                            LogHelper.Log(LogHelper.Status.Ok, "Журнал секционирования очищен (" + PartitionLog + ").");
                        }
                    }
                    else
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось очистить журнал секционирования. Нет соединения с базой данных.");
                    }
                    conn.Close();
                }
            }
            catch (MySqlException ex)
            {
                lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                LogHelper.Log(LogHelper.Status.Ok, ex.Message);
            }
        }

        /// <summary>
        /// Метод очистки секционирования в БД
        /// </summary>

        public void ClearPartitioning()
        { 
            Stopwatch sw = new Stopwatch();
            string query = "";
            int count = 0;
            bool partitioned = false;

            using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
            {
                using (MySqlCommand command = new MySqlCommand("", conn))
                {
                    try
                    {
                        conn.Open();
                        IsDroppingPartitioning = true;
                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Началась очистка секционирования БД', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Началась очистка секционирования БД.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Началась очистка секционирования БД.");

                            foreach (Table table in TablesItems.ToList())
                            {
                                command.CommandText = "SHOW TABLE STATUS WHERE NAME = '" + table.Name + "'";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read()) partitioned = reader.GetString(reader.GetOrdinal("Create_options")) == "partitioned";
                                }

                                if (partitioned)
                                {
                                    lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Началась очистка секционирования таблицы " + table.Name + ".", Duration = "0 мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Началась очистка секционирования таблицы " + table.Name + ".");

                                    sw.Restart();
                                    query = "ALTER TABLE " + table.Name + " REMOVE PARTITIONING";
                                    command.CommandText = query;
                                    command.ExecuteNonQuery();
                                    sw.Stop();

                                    Application.Current.Dispatcher.Invoke(new Action(() => { table.Partitions.Clear(); }));
                                    lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Очистка секционирования таблицы " + table.Name + " успешно завершена.", Duration = sw.ElapsedMilliseconds + " мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Очистка секционирования таблицы " + table.Name + " успешно завершена.");
                                }
                            }

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Секционирование таблиц успешно удалено.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Секционирование таблиц успешно удалено.");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Секционирование таблиц успешно удалено', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();
                            count++;
                        }
                        else
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить секции таблиц. Нет соединения с базой данных.");
                        }
                    }
                    catch (MySqlException ex)
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось удалить секции таблиц. " + ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, "Не удалось удалить секции таблиц. " + ex.Message);
                    }

                    try
                    {
                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            foreach (Table table in TablesItems.ToList())
                            {
                                foreach (StoredProcedure procedure in table.Procedures.ToList())
                                {
                                    sw.Restart();
                                    query = "DROP PROCEDURE " + procedure.Name + ";";
                                    command.CommandText = query;
                                    command.ExecuteNonQuery();
                                    sw.Stop();

                                    lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Хранимая процедура секционирования таблицы " + table.Name + " " + procedure.Name + " успешно уничтожена.", Duration = sw.ElapsedMilliseconds + " мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Хранимая процедура секционирования таблицы " + table.Name + " " + procedure.Name + " успешно уничтожена.");
                                }
                                Application.Current.Dispatcher.Invoke(new Action(() => { table.Procedures.Clear(); }));
                            }

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Хранимые процедуры секционирования успешно уничтожены.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "Хранимые процедуры секционирования успешно уничтожены.");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Хранимые процедуры секционирования успешно удалены', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();
                            count++;
                        }
                        else
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось уничтожить хранимые процедуры секционируемых таблиц. Нет соединения с базой данных.");
                        }
                    }
                    catch (MySqlException ex)
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, ex.Message);
                    }

                    try
                    {
                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            foreach (Table table in TablesItems.ToList())
                            {
                                foreach (Event e in table.Events.ToList())
                                {
                                    sw.Restart();
                                    query = "DROP EVENT " + e.Name + ";";
                                    command.CommandText = query;
                                    command.ExecuteNonQuery();
                                    sw.Stop();

                                    lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Событие секционирования таблицы " + table.Name + " " + e.Name + " успешно уничтожено.", Duration = sw.ElapsedMilliseconds + " мс" });
                                    LogHelper.Log(LogHelper.Status.Ok, "Событие секционирования таблицы " + table.Name + " " + e.Name + " успешно уничтожено.");
                                }
                                Application.Current.Dispatcher.Invoke(new Action(() => { table.Events.Clear(); }));
                            }

                            lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "События секционирования успешно уничтожены.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Ok, "События секционирования успешно уничтожены.");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'События секционирования успешно удалены', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();
                            count++;
                        }
                        else
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Нет соединения с базой данных.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось уничтожить события секционирования. Нет соединения с базой данных.");
                        }
                    }
                    catch (MySqlException ex)
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, ex.Message);
                    }

                    try
                    {
                        if (count == 3)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Очистка секционирования базы данных " + Database + " успешно завершена.", Duration = "0 мс" });
                                ThemedMessageBox.Show("Очистка секционирования БД", "Очистка секционирования базы данных " + Database + " успешно завершена.", MessageBoxButton.OK, MessageBoxImage.Information);
                            }));
                            LogHelper.Log(LogHelper.Status.Ok, "Очистка секционирования базы данных " + Database + " успешно завершена.");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ок', 'Очистка секционирования базы данных " + Database + " успешно завершена', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();
                        }
                        else if (count > 0 && count < 3)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Warning", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Очистка секционирования базы данных " + Database + " завершена частично.", Duration = "0 мс" });
                                ThemedMessageBox.Show("Очистка секционирования БД", "Очистка секционирования базы данных " + Database + " завершена частично.", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }));
                            LogHelper.Log(LogHelper.Status.Warning, "Очистка секционирования базы данных " + Database + " завершена частично.");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Предупреждение', 'Очистка секционирования базы данных " + Database + " завершена частично', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();
                        }
                        else if (count == 0)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось очистить секционирование базы данных " + Database + ".", Duration = "0 мс" });
                                ThemedMessageBox.Show("Очистка секционирования БД", "Не удалось очистить секционирование базы данных " + Database + ".", MessageBoxButton.OK, MessageBoxImage.Error);
                            }));
                            LogHelper.Log(LogHelper.Status.Error, "Не удалось очистить секционирование базы данных " + Database + ".");

                            command.CommandText = "INSERT INTO `" + PartitionLog + "` (`time`, `status`, `action`, `message`, `duration`) VALUES ('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "', 'Ошибка', 'Не удалось очистить секционирование базы данных " + Database + "', '" + Database + "', '00:00:00 sec');";
                            command.ExecuteNonQuery();
                        }
                        conn.Close();
                    }
                    catch (MySqlException ex)
                    {
                        lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = ex.Message, Duration = "0 мс" });
                        LogHelper.Log(LogHelper.Status.Error, ex.Message);
                    }
                }
                IsDroppingPartitioning = false;
            }
        }

        /// <summary>
        /// Метод обрыва главного подключения
        /// </summary>

        public void DisconnectDatabase()
        {
            timer.Stop();
            reconnectTimer.Start();
            configCheckTimer.Stop();

            IsPartitioning = false;
            IsDroppingPartitioning = false;

            TablesItems.Clear();
            InitPartitionTables.Clear();
            CurrPartitionTables.Clear();
            PartitionLogItems.Clear();

            Status = "БД не доступна";
            BDAndUser = "БД:";
            PartitionStatus = 2;
            DatabaseStatus = 2;

            IsRefreshingPartitioning = false;
            IsRemovingPartitioning = false;

            IsConnected = false;
        }

        /// <summary>
        /// Метод разделения таблиц из окна "Настройки" на секционируемые таблицы
        /// </summary>

        public void RebuildPartitionTables()
        {
            List<PartitionTable> tablesToRemove = new List<PartitionTable>();
            foreach (SettingsTable table in Tables)
            {
                PartitionTable tableToEdit = PartitionTables.Where(x => x.Name == table.Name).FirstOrDefault();
                if (tableToEdit == null && !table.DontPartition)
                {
                    PartitionTables.Add(new PartitionTable
                    {
                        Name = table.Name,
                        DateTimeColumn = table.SelectedDateTimeColumn,
                        EntrySize = table.AvgEntrySize > table.EntrySize ? table.AvgEntrySize : table.EntrySize,
                        IntervalVal = 1,
                        IntervalMeasureSelectedItem = 3,
                        SectorSizeVal = 1,
                        SectorSizeMeasureSelectedItem = 1,
                        SectorCount = 120,
                        DepthVal = 10,
                        DepthMeasureSelectedItem = 2,
                        EntryCount = 365,
                        TableSize = 0,
                        PartitionIntervalVal = 2,
                        PartitionIntervalMeasureSelectedItem = 1
                    });
                }
                else if (tableToEdit != null && !table.DontPartition) tableToEdit.DateTimeColumn = table.SelectedDateTimeColumn;
                if (!SetDepth) foreach (PartitionTable entry in PartitionTables.ToList()) entry.CalculateDepthVal();
                if (!SetEntryCount) foreach (PartitionTable entry in PartitionTables.ToList()) entry.CalculateEntryCount();
                if (!SetInterval) foreach (PartitionTable entry in PartitionTables.ToList()) entry.CalculateIntervalVal();
                if (!SetSectorCount) foreach (PartitionTable entry in PartitionTables.ToList()) entry.CalculateSectorCount();
                if (!SetSectorSize) foreach (PartitionTable entry in PartitionTables.ToList()) entry.CalculateSectorSizeVal();
                if (!SetTableSize) foreach (PartitionTable entry in PartitionTables.ToList()) entry.CalculateTableSize();
            }
            foreach(PartitionTable table in PartitionTables) if (!Tables.Any(x => x.Name == table.Name && !x.DontPartition)) tablesToRemove.Add(table);
            foreach (PartitionTable table in tablesToRemove) PartitionTables.Remove(table);

            TablesItems.Clear();
            foreach (PartitionTable table in PartitionTables) TablesItems.Add(new Table { Name = table.Name, DateTimeColumn = table.DateTimeColumn });

            if (PartitionTables.Count > 0) SelectedPartitionTable = PartitionTables.First();
        }

        /// <summary>
        /// Метод проверки подключения к серверу для задания статуса доступности IP и порта (таймер)
        /// </summary>

        private Task CheckIP()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password))
                {
                    conn.Open();
                    if (conn.Ping()) IPStatus = 0;
                    conn.Close();
                }
            }
            catch
            {
                IPStatus = 2;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Задача проверки изменений последней конфигурации на сервере (таймер)
        /// </summary>

        private Task CheckLastConfig()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";Allow User Variables=True" + ";default command timeout=0"))
                {
                    conn.Open();
                    DateTime updateTime = new DateTime();

                    using (MySqlCommand command = new MySqlCommand("SELECT `updated` FROM `" + PartitionConfig + "` WHERE ID = (SELECT MAX(ID) FROM `" + PartitionConfig + "`)", conn))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read()) updateTime = reader.GetDateTime(0);
                        }
                    }

                    if (updateTime != LastConfigUpdateTime && !IsPartitioning)
                    {
                        if (!LastConfigNotFoundShown)
                        {
                            lock (syncLock) LogItems.Add(new Log { Status = "Warning", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Последняя конфигурация секционирования на сервере была изменена вне текущего экземпляра приложения. Последняя конфигурация секционирования будет перезагружена с сервера.", Duration = "0 мс" });
                            LogHelper.Log(LogHelper.Status.Warning, "Последняя конфигурация секционирования на сервере была изменена вне текущего экземпляра приложения. Последняя конфигурация секционирования будет перезагружена с сервера.");
                            LastConfigNotFoundShown = true;
                        }
                        Application.Current.Dispatcher.Invoke(new Action(() => { ExportLastConfig(); }));
                    }
                    conn.Close();
                }
            }
            catch (MySqlException ex)
            {
                if (!LastConfigNotFoundShown)
                {
                    lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось проверить последнюю конфигурацию секционирования на сервере на изменения вне приложения: " + ex.Message + ". Задайте название таблицы для хранения конфигураций в окне \"Настройки\".", Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Error, "Не удалось проверить последнюю конфигурацию секционирования на сервере на изменения вне приложения. Задайте название таблицы для хранения конфигураций в окне \"Настройки\".", ex);
                    LastConfigNotFoundShown = true;
                }    
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Метод пересоздания главного подключения к БД при его обрыве (таймер)
        /// </summary>

        private Task Reconnect()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    if (conn.Ping()) Application.Current.Dispatcher.Invoke(new Action(() => { ConnectToDatabase(); }));
                    conn.Close();
                }
            }
            catch(MySqlException ex)
            {
                if (!ReconnectShown)
                {
                    lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Не удалось переподключиться к базе данных " + Database + ": " + ex.Message, Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Error, "Не удалось переподключиться к базе данных " + Database + ". ", ex);
                    ReconnectShown = true;
                }
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Задача обновления данных для вкладки "Диагностика" и изменения статусов секционирования БД (таймер)
        /// </summary>

        private Task Update()
        {
            try
            {
                int count = 0;
                using (MySqlConnection conn = new MySqlConnection("server=" + IP + ";" + "port=" + Port + ";" + "user=" + Username + ";" + "password=" + Password + ";" + "database=" + Database + ";default command timeout=0"))
                {
                    conn.Open();
                    using (MySqlCommand command = new MySqlCommand("", conn))
                    {
                        if (PartitionLog != "" && conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            string SQL = "CREATE TABLE IF NOT EXISTS `" + PartitionLog + "`" +
                                                " (" +
                                                " id int(10) unsigned NOT NULL AUTO_INCREMENT," +
                                                " time datetime(3) NOT NULL," +
                                                " status varchar(100) NOT NULL," +
                                                " action varchar(150) NOT NULL, " +
                                                " message varchar(200) NOT NULL," +
                                                " duration varchar(45) DEFAULT NULL," +
                                                " PRIMARY KEY (id)" +
                                                " )" +
                                                " ENGINE = INNODB," +
                                                " AUTO_INCREMENT = 6680," +
                                                " CHARACTER SET utf8mb4," +
                                                " COLLATE utf8mb4_general_ci; ";

                            bool exists = false;

                            command.CommandText = "SELECT * FROM information_schema.tables WHERE table_schema = '" + Database + "' AND table_name = '" + PartitionLog + "'";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows) exists = true;
                            }

                            if (!exists)
                            {
                                command.CommandText = SQL;
                                command.ExecuteNonQuery();

                                lock (syncLock) LogItems.Add(new Log { Status = "Ok", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Создан журнал секционирования " + PartitionLog + ".", Duration = "0 мс" });
                                LogHelper.Log(LogHelper.Status.Ok, "Создан журнал секционирования " + PartitionLog + ".");
                            }

                            if (ClearAndReadPartitionLog)
                            {
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    PartitionLogItems.Clear();
                                    LastPartitionLogEntry = 0;
                                    ClearAndReadPartitionLog = false;
                                }));
                            }

                            command.CommandText = "SELECT * FROM `" + PartitionLog + "` WHERE id > " + LastPartitionLogEntry + " ORDER BY id DESC LIMIT 30000";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        PartitionLogItems.Add(new PartitionLog { ID = reader.GetUInt32(0), Time = reader.GetDateTime(1).ToString("dd.MM.yyyy HH:mm:ss:fff"), Status = reader.GetString(2), Event = reader.GetString(3), Message = reader.GetString(4), Duration = reader.GetString(5) });
                                        LastPartitionLogEntry = PartitionLogItems.Max(x => x.ID);
                                    }));
                                }
                            }
                        }
                        else if (conn.State != System.Data.ConnectionState.Open || conn == null)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Ошибка при чтении журнала секционирования: нет соединения с БД " + Database + ".", Duration = "0 мс" });
                                DisconnectDatabase();
                            }));
                            LogHelper.Log(LogHelper.Status.Error, "Ошибка при чтении журнала секционирования: нет соединения с БД " + Database + ".");
                        }

                        List<StoredProcedure> procedures = new List<StoredProcedure>();

                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            command.CommandText = "SHOW PROCEDURE STATUS WHERE Db = '" + Database + "'";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read()) procedures.Add(new StoredProcedure { Name = reader.GetString("Name"), User = reader.GetString("Definer").Substring(0, Username.Length + 1) + "%", Date = reader.GetDateTime("Created").ToString("dd.MM.yyyy HH:mm:ss:fff") });
                            }
                            string query;
                            foreach (StoredProcedure procedure in procedures.ToList())
                            {
                                query = "SHOW CREATE PROCEDURE " + procedure.Name;
                                command.CommandText = query;
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read()) procedure.QueryText = reader.GetString("Create Procedure");
                                }
                            }
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                foreach (StoredProcedure procedure in procedures.ToList())
                                {
                                    var item = TablesItems.Where(x => procedure.Name.Contains(x.Name + "_")).FirstOrDefault();
                                    if (item != null)
                                    {
                                        StoredProcedure procedureToReplace = item.Procedures.Where(x => x.Name == procedure.Name).FirstOrDefault();
                                        if (procedureToReplace != null && procedureToReplace != procedure)
                                        {
                                            item.Procedures.Remove(procedureToReplace);
                                            item.Procedures.Add(procedure);
                                        }
                                        else if (procedureToReplace == null) item.Procedures.Add(procedure);

                                        StoredProcedure procedureToRemove = item.Procedures.Where(x => !procedures.Exists(y => y.Name.Contains(item.Name) && y.Name == x.Name)).FirstOrDefault();
                                        if (procedureToRemove != null) item.Procedures.Remove(procedureToRemove);
                                    }
                                }
                            }));

                            foreach (Table table in TablesItems.ToList()) count += table.Procedures.Count;
                        }
                        else if (conn.State != System.Data.ConnectionState.Open || conn == null)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Ошибка при обновлении списка хранимых процедур: нет соединения с БД " + Database + ".", Duration = "0 мс" });
                                DisconnectDatabase();
                            }));
                            LogHelper.Log(LogHelper.Status.Error, "Ошибка при обновлении списка хранимых процедур: нет соединения с БД " + Database + ".");
                        }

                        List<Event> events = new List<Event>();

                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            command.CommandText = "SHOW EVENTS WHERE Db = '" + Database + "'";
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read()) events.Add(new Event { Name = reader.GetString("Name"), User = reader.GetString("Definer").Substring(0, Username.Length + 1) + "%", Date = reader.GetDateTime("Starts").ToString("dd.MM.yyyy HH:mm:ss:fff"), TimeZ = reader.GetString("Time zone"), Interval = reader.GetString("Interval value") + " " + reader.GetString("Interval field") });
                            }
                            string query;
                            foreach (Event e in events.ToList())
                            {
                                query = "SHOW CREATE EVENT " + e.Name;
                                command.CommandText = query;
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read()) e.QueryText = reader.GetString("Create Event");
                                }
                            }
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                foreach (Event e in events.ToList())
                                {
                                    var item = TablesItems.Where(x => e.Name.Contains(x.Name + "_")).FirstOrDefault();
                                    if (item != null)
                                    {
                                        Event eventToReplace = item.Events.Where(x => x.Name == e.Name).FirstOrDefault();
                                        if (eventToReplace != null && eventToReplace != e)
                                        {
                                            item.Events.Remove(eventToReplace);
                                            item.Events.Add(e);
                                        }
                                        else if (eventToReplace == null) item.Events.Add(e);

                                        Event eventToRemove = item.Events.Where(x => !events.Exists(y => y.Name.Contains(item.Name) && y.Name == x.Name)).FirstOrDefault();
                                        if (eventToRemove != null) item.Events.Remove(eventToRemove);
                                    }
                                }
                            }));

                            foreach (Table table in TablesItems.ToList()) count += table.Events.Count;
                        }
                        else if (conn.State != System.Data.ConnectionState.Open || conn == null)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Ошибка при обновлении списка событий: нет соединения с БД " + Database + ".", Duration = "0 мс" });
                                DisconnectDatabase();
                            }));
                            LogHelper.Log(LogHelper.Status.Error, "Ошибка при обновлении списка событий: нет соединения с БД " + Database + ".");
                        }

                        if (conn != null && conn.State == System.Data.ConnectionState.Open)
                        {
                            command.CommandText = "SHOW VARIABLES WHERE VARIABLE_NAME = 'event_scheduler'";

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                status = reader.GetString(1);
                                if (status == "ON") EventSchedulerEnabled = true;
                                else if (status == "OFF") EventSchedulerEnabled = false;
                            }
                        }
                        else if (conn.State != System.Data.ConnectionState.Open || conn == null)
                        {
                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Ошибка при обновлении статуса планировщика событий: нет соединения с БД " + Database + ".", Duration = "0 мс" });
                                DisconnectDatabase();
                            }));
                            LogHelper.Log(LogHelper.Status.Error, "Ошибка при обновлении статуса планировщика событий: нет соединения с БД " + Database + ".");
                        }
                    }
                    conn.Close();
                }
                if (TablesItems.Count > 0 && count >= TablesItems.Count * 3 && EventSchedulerEnabled && !IsPartitioning)
                {
                    DatabaseStatus = 0;
                    PartitionStatus = 0;
                    Status = "Секционировано";
                    BDAndUser = "БД: " + Database + ", User: " + Username;
                }
                else if ((TablesItems.Count == 0 || count < TablesItems.Count * 3 || !EventSchedulerEnabled) && !IsPartitioning)
                {
                    DatabaseStatus = 0;
                    PartitionStatus = 2;
                    Status = "Не секционировано";
                    BDAndUser = "БД: " + Database + ", User: " + Username;
                }
            }
            catch (MySqlException ex)
            {
                if (!IsPartitioning && !IsDroppingPartitioning)
                {
                    lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Ошибка при обновлении данных: " + ex.Message + ". Соединение закрыто.", Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Error, "Ошибка при обновлении данных. Соединение закрыто.", ex);
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        DisconnectDatabase();
                    }));
                }
                else
                {
                    lock (syncLock) LogItems.Add(new Log { Status = "Error", Time = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss:fff"), Event = "Ошибка при обновлении данных: " + ex.Message + ".", Duration = "0 мс" });
                    LogHelper.Log(LogHelper.Status.Error, "Ошибка при обновлении данных.", ex);
                }
            }
            return Task.CompletedTask;
        }

        #endregion

        #region Events

        /// <summary>
        /// Обработчик события закрытия окна
        /// </summary>

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (SaveEnabled)
            {
                var result = ThemedMessageBox.Show("Новая конфигурация", "В текущую конфигурацию были внесены изменения. Сохранить конфигурацию?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        if (FileName != "Новая конфигурация") SaveButtonCommand.Execute(this);
                        else SaveAsButtonCommand.Execute(this);
                        break;

                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        break;
                }
            }
            if (LastFileName != "Новая конфигурация" && LastFileName != "Новая конфигурация*")
            {
                Properties.Settings.Default.lastFileName = LastFileName;
                Properties.Settings.Default.Save();
            }
            LogHelper.Log(LogHelper.Status.Ok, "Выход из приложения. Последний открытый файл конфигурации: " + LastFileName);
        }

        /// <summary>
        /// Обработчик события изменения коллекций таблиц на вкладке "Конфигурация"
        /// </summary>

        void ConfigTables_ListChanged(object sender, ListChangedEventArgs e)
        {
            SaveEnabled = true;
        }

        #endregion

        #region EventHandlers

        /// <summary>
        /// Обработчики событий взаимодействия с контролами для логгирования действий пользователя в файл
        /// </summary>

        private static void OnButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Главное окно. Нажата кнопка \"" + (e.Source as Button).Content.ToString() + "\".");
        }

        private static void OnImageButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Главное окно. Нажата кнопка \"" + (e.Source as Button).Tag.ToString() + "\".");
        }

        private static void OnMenuButtonClicked(ItemClickEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Главное окно. Нажата кнопка меню \"" + e.Item.Content + "\".");
        }

        private static void OnConfigTableButtonClicked(RoutedEventArgs e)
        {
            LogHelper.Log(LogHelper.Status.User, "Главное окно. Нажат столбец таблицы конфигурации \"" + (e.Source as Button).Tag.ToString() + "\".");
        }

        private static void OnTabChanged(DevExpress.Xpf.Docking.Base.SelectedItemChangedEventArgs e)
        {
            if (e.Item != null) LogHelper.Log(LogHelper.Status.User, "Главное окно. Нажата вкладка \"" + e.Item.Caption + "\".");
        }

        /// <summary>
        /// Обработчик проверки ввода в таблицы конфигураций
        /// </summary>

        private static void ValidateCell(GridCellValidationEventArgs e)
        {
            try
            {
                decimal newValue = Convert.ToDecimal(e.Value);
                if (newValue < 1) e.SetError("Значение должно быть не меньше еденицы.");
            }
            catch
            {
                e.SetError("Значение должно быть числом.");
            }
        }

        /// <summary>
        /// Обработчик события нажатия на ячейку таблицы хранимых процедур
        /// </summary>

        private static void ProceduresMouseDown(MouseButtonEventArgs e)
        {
            GridColumn column = (e.Source as TreeListView).FocusedColumn as GridColumn;
            StoredProcedure procedure = (e.Source as TreeListView).SelectedRows[0] as StoredProcedure;

            if (column.FieldName == "QueryText" && procedure != null)
            {
                QueryTextWindow queryTextWindow = new QueryTextWindow();
                queryTextWindow.Owner = App.Current.MainWindow;
                (queryTextWindow.DataContext as QueryTextWindowVM).Text = procedure.QueryText;
                queryTextWindow.ShowDialog();
                LogHelper.Log(LogHelper.Status.Ok, "Открыто окно \"Текст запроса\".");
            }
        }

        /// <summary>
        /// Обработчик события нажатия на ячейку таблицы хранимых процедур
        /// </summary>

        private static void EventsMouseDown(MouseButtonEventArgs e)
        {
            GridColumn column = (e.Source as TreeListView).FocusedColumn as GridColumn;
            Event ev = (e.Source as TreeListView).SelectedRows[0] as Event;

            if (column.FieldName == "QueryText" && ev != null)
            {
                QueryTextWindow queryTextWindow = new QueryTextWindow();
                queryTextWindow.Owner = App.Current.MainWindow;
                (queryTextWindow.DataContext as QueryTextWindowVM).Text = ev.QueryText;
                queryTextWindow.ShowDialog();
                LogHelper.Log(LogHelper.Status.Ok, "Открыто окно \"Текст запроса\".");
            }
        }

        /// <summary>
        /// Обработчик загрузки компонента RichEditControl
        /// </summary>

        private static void RichEditLoaded(RoutedEventArgs e)
        {
            MainWindowVM mainWindowVM = Application.Current.MainWindow.DataContext as MainWindowVM;
            RichEditControl richEditControl = e.Source as RichEditControl;
            mainWindowVM.richEditControl = richEditControl;
            richEditControl.ReplaceService<ISyntaxHighlightService>(new XMLSyntaxHighlightService(richEditControl.Document));
            if (mainWindowVM.FileName.Contains("\\")) richEditControl.LoadDocument(mainWindowVM.FileName, DocumentFormat.PlainText);
        }

        /// <summary>
        /// Обработчик изменения значений ячеек в таблице конфигурации
        /// </summary>

        private void OnConfigTableCellValueChanging(CellValueChangedEventArgs e)
        {
            MainWindowVM mainVM = Application.Current.MainWindow.DataContext as MainWindowVM;
            mainVM.SaveEnabled = true;
            PartitionTable table = e.Row as PartitionTable;

            switch (e.Cell.Property)
            {
                case "EntrySize":
                    if (!MainWindowVM.SetEntryCount)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (Int64)((1 / Convert.ToDecimal(table.IntervalVal)) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * table.DepthVal * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem));
                        }
                        catch { }
                        table.EntryCount = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetTableSize)
                    {
                        decimal temp = 0;
                        try
                        {
                            temp = (decimal)(table.EntryCount * Convert.ToInt64(e.Cell.Value)) / (1024 * 1024);
                        }
                        catch { }
                        table.TableSize = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetInterval)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)((Convert.ToInt64(e.Cell.Value) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * table.DepthVal * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)) / (table.TableSize * 1024 * 1024));
                        }
                        catch { }
                        table.IntervalVal = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetDepth)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling((table.TableSize * 1024 * 1024) / (Convert.ToInt64(e.Cell.Value) * (1 / Convert.ToDecimal(table.IntervalVal)) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)));
                        }
                        catch { }
                        table.DepthVal = temp <= 0 ? 1 : temp;

                        if (!MainWindowVM.SetSectorCount)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(table.DepthVal / MeasureComparer.ScaleValue(Convert.ToDecimal(table.SectorSizeVal), table.DepthMeasureSelectedItem, table.SectorSizeMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorCount = temp <= 0 ? 1 : temp;
                        }
                        if (!MainWindowVM.SetSectorSize)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(MeasureComparer.ScaleValue((table.DepthVal / Convert.ToDecimal(table.SectorCount)), table.SectorSizeMeasureSelectedItem, table.DepthMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorSizeVal = temp <= 0 ? 1 : temp;
                        }
                    }
                    break;

                case "IntervalVal":
                    if (!MainWindowVM.SetEntryCount)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (Int64)((1 / Convert.ToDecimal(e.Cell.Value)) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * table.DepthVal * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem));
                        }
                        catch { }
                        table.EntryCount = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetTableSize)
                    {
                        decimal temp = 0;
                        try
                        {
                            temp = (decimal)(table.EntryCount * table.EntrySize) / (1024 * 1024);
                        }
                        catch { }
                        table.TableSize = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetDepth)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling((table.TableSize * 1024 * 1024) / (table.EntrySize * (1 / Convert.ToDecimal(e.Cell.Value)) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)));
                        }
                        catch { }
                        table.DepthVal = temp <= 0 ? 1 : temp;

                        if (!MainWindowVM.SetSectorCount)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(table.DepthVal / MeasureComparer.ScaleValue(Convert.ToDecimal(table.SectorSizeVal), table.DepthMeasureSelectedItem, table.SectorSizeMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorCount = temp <= 0 ? 1 : temp;
                        }
                        if (!MainWindowVM.SetSectorSize)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(MeasureComparer.ScaleValue((table.DepthVal / Convert.ToDecimal(table.SectorCount)), table.SectorSizeMeasureSelectedItem, table.DepthMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorSizeVal = temp <= 0 ? 1 : temp;
                        }
                    }
                    break;

                case "SectorSizeVal":
                    if (!MainWindowVM.SetSectorCount)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling(table.DepthVal / MeasureComparer.ScaleValue(Convert.ToDecimal(e.Cell.Value), table.DepthMeasureSelectedItem, table.SectorSizeMeasureSelectedItem));
                        }
                        catch { }
                        table.SectorCount = temp <= 0 ? 1 : temp;
                    }
                    break;

                case "SectorCount":
                    if (!MainWindowVM.SetSectorSize)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling(MeasureComparer.ScaleValue((table.DepthVal / Convert.ToDecimal(e.Cell.Value)), table.SectorSizeMeasureSelectedItem, table.DepthMeasureSelectedItem));
                        }
                        catch { }
                        table.SectorSizeVal = temp <= 0 ? 1 : temp;
                    }
                    break;

                case "DepthVal":
                    if (!MainWindowVM.SetEntryCount)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (Int64)((1 / Convert.ToDecimal(table.IntervalVal)) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * Convert.ToDecimal(e.Cell.Value) * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem));
                        }
                        catch { }
                        table.EntryCount = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetTableSize)
                    {
                        decimal temp = 0;
                        try
                        {
                            temp = (decimal)(table.EntryCount * table.EntrySize) / (1024 * 1024);
                        }
                        catch { }
                        table.TableSize = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetSectorCount)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling(Convert.ToDecimal(e.Cell.Value) / MeasureComparer.ScaleValue(Convert.ToDecimal(table.SectorSizeVal), table.DepthMeasureSelectedItem, table.SectorSizeMeasureSelectedItem));
                        }
                        catch { }
                        table.SectorCount = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetSectorSize)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling(MeasureComparer.ScaleValue(Convert.ToDecimal(e.Cell.Value) / table.SectorCount, table.SectorSizeMeasureSelectedItem, table.DepthMeasureSelectedItem));
                        }
                        catch { }
                        table.SectorSizeVal = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetInterval)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)((table.EntrySize * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * Convert.ToDecimal(e.Cell.Value) * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)) / (table.TableSize * 1024 * 1024));
                        }
                        catch { }
                        table.IntervalVal = temp <= 0 ? 1 : temp;
                    }
                    break;

                case "EntryCount":
                    table.TableSize = ((decimal)(Convert.ToInt64(e.Cell.Value) * table.EntrySize) / (1024 * 1024));
                    if (!MainWindowVM.SetInterval)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling((table.EntrySize * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * table.DepthVal * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)) / (table.TableSize * 1024 * 1024));
                        }
                        catch { }
                        table.IntervalVal = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetDepth)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling((table.TableSize * 1024 * 1024) / (table.EntrySize * (1 / Convert.ToDecimal(table.IntervalVal)) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)));
                        }
                        catch { }
                        table.DepthVal = temp <= 0 ? 1 : temp;

                        if (!MainWindowVM.SetSectorCount)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(table.DepthVal / MeasureComparer.ScaleValue(Convert.ToDecimal(table.SectorSizeVal), table.DepthMeasureSelectedItem, table.SectorSizeMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorCount = temp <= 0 ? 1 : temp;
                        }
                        if (!MainWindowVM.SetSectorSize)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(MeasureComparer.ScaleValue((table.DepthVal / Convert.ToDecimal(table.SectorCount)), table.SectorSizeMeasureSelectedItem, table.DepthMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorSizeVal = temp <= 0 ? 1 : temp;
                        }
                    }
                    break;

                case "TableSize":
                    if (!MainWindowVM.SetInterval)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)((table.EntrySize * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * table.DepthVal * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)) / (Convert.ToDecimal(e.Cell.Value) * 1024 * 1024));
                        }
                        catch { }
                        table.IntervalVal = temp <= 0 ? 1 : temp;
                    }
                    if (!MainWindowVM.SetDepth)
                    {
                        long temp = 0;
                        try
                        {
                            temp = (long)Math.Ceiling((Convert.ToDecimal(e.Cell.Value) * 1024 * 1024) / (table.EntrySize * (1 / Convert.ToDecimal(table.IntervalVal)) * MeasureComparer.Convert(table.IntervalMeasureSelectedItem) * MeasureComparer.ConvertToHours(table.DepthMeasureSelectedItem)));
                        }
                        catch { }
                        table.DepthVal = temp <= 0 ? 1 : temp;

                        if (!MainWindowVM.SetSectorCount)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(table.DepthVal / MeasureComparer.ScaleValue(Convert.ToDecimal(table.SectorSizeVal), table.DepthMeasureSelectedItem, table.SectorSizeMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorCount = temp <= 0 ? 1 : temp;
                        }
                        if (!MainWindowVM.SetSectorSize)
                        {
                            try
                            {
                                temp = (long)Math.Ceiling(MeasureComparer.ScaleValue((table.DepthVal / Convert.ToDecimal(table.SectorCount)), table.SectorSizeMeasureSelectedItem, table.DepthMeasureSelectedItem));
                            }
                            catch { }
                            table.SectorSizeVal = temp <= 0 ? 1 : temp;
                        }
                    }
                    table.EntryCount = (long)Math.Ceiling(((decimal)(Convert.ToInt64(e.Cell.Value) * 1024 * 1024) / table.EntrySize));
                    break;
            }
        }

        /// <summary>
        /// Обработчик события отлова необработанного исключения
        /// </summary>

        static void UnhandledExceptionHandle(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            LogHelper.Log(LogHelper.Status.Error, "", ex);
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;
        private static void NotifyStaticPropertyChanged([CallerMemberName] string staticPropertyName = null)
        {
            StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(staticPropertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        #endregion
    }
}
