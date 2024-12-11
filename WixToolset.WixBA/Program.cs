// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.WixBA
{
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
