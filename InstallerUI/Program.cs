using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerUI
{
    using InstallerUI.Bootstrapper;
    using System.Diagnostics;
    using System.Threading;
    using System.Windows;
    using WixToolset.BootstrapperApplicationApi;

    internal class Program
    {
        private static int Main()
        {

            //#if DEBUG
            //            if (MessageBox.Show("You are running in the Debug mode, " +
            //                                "in order to debug this installer you've to attach the debugger. " +
            //                                "Go to -> Tools > Attach to Process > Find 'Immo Pro Desktop Installer.exe' in the list and attach. \n\n" +
            //                                "Do you want to attach debugger?",
            //                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            //                // Do not start until debugger attached
            //                while (!Debugger.IsAttached)
            //                    Thread.Sleep(1000);
            //#endif
            var application = new WixBA();

            ManagedBootstrapperApplication.Run(application);

            return 0;
        }
    }
}
