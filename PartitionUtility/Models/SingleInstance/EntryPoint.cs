using System;

namespace PartitionUtility
{
    public class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var manager = new SingleInstanceManager();
            manager.Run(args);
        }
    }
}
