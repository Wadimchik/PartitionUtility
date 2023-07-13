using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PartitionUtility
{
    [Serializable]
    public class Config
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public string DataBase { get; set; }

        public string PartitionLog { get; set; }
        public string PartitionConfig { get; set; }

        public string DumpExePath { get; set; }

        public List<SettingsTable> Tables { get; set; } = new List<SettingsTable>();

        public BindingList<PartitionTable> PartitionTables { get; set; } = new BindingList<PartitionTable>();

        public Config()
        {
            UserName = "";
            Password = "";
            IP = "";
            Port = "";
            DataBase = "";
            PartitionLog = "";
            PartitionConfig = "";
        }
    }
}
