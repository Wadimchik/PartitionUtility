using Microsoft.VisualBasic.ApplicationServices;

namespace PartitionUtility
{
    public class SingleInstanceManager : WindowsFormsApplicationBase
    {
        private SingleInstanceApplication app;

        public SingleInstanceManager()
        {
            IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs e)
        {
            // First time app is launched
            app = new SingleInstanceApplication();
            app.InitializeComponent();
            app.Run();
            return false;
        }

        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            // Subsequent launches
            base.OnStartupNextInstance(eventArgs);
            app.Activate();
        }
    }
}
