using InstallerUI.Constants;
using InstallerUI.ViewModel;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace InstallerUI.Views
{
    /// <summary>
    ///     Interaction logic for InstallerUIWindow.xaml
    /// </summary>
    [Export(ContractNames._MainView, typeof(Window))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class MainView : Window
    {
        [ImportingConstructor]
        public MainView(MainViewModel viewModel, Engine engine)
        {
            DataContext = viewModel;

            Loaded += (sender, e) => { engine.CloseSplashScreen(); };
            Closed += (sender, e) =>
            {
              //  Process mainProcess = Process.GetCurrentProcess();
                Dispatcher.InvokeShutdown(); // shutdown dispatcher when the window is closed.
              //  KillProcessAndChildren(mainProcess.Id);
            };
            Closing += (sender, e) =>
            {
                viewModel.CancelAllOperations();
            };
            MouseLeftButtonDown += (sender, e) => DragMove();

            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}