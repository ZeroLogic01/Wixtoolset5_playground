using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;
using InstallerUI.Commands;
using InstallerUI.Commands.Interfaces;
using InstallerUI.Constants;
using InstallerUI.Properties;
using InstallerUI.Services.Implementations;
using InstallerUI.Services.Interfaces;
using InstallerUI.Views;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Newtonsoft.Json;
using WixToolset.BootstrapperApplicationApi;

namespace InstallerUI.Bootstrapper
{
    public class InstallerUIBootstrapper : BootstrapperApplication, IInteractionService, INavigationService, ITaskBar
    {
        private BootstrapperBundleData bootstrapperBundleData;
        private CompositionContainer Container;
        private Window installerUIWindow;
        private IntPtr installerUIWindowHandle;

        /// Full application command line
        public IEnumerable<string> CommandLine => Command.GetCommandLineArgs();

        /// Requested display mode from the commandline
        /// (Full, Passive/Silent, Embedded)
        public Display DisplayMode => Command.Display;

        public void CloseUIAndExit()
        {
            installerUIWindow.Dispatcher.BeginInvoke(new Action(() => installerUIWindow.Close()));
        }

        public void RunOnUIThread(Action body)
        {
            installerUIWindow.Dispatcher.BeginInvoke(body, null);
        }

        public IntPtr GetMainWindowHandle()
        {
            return installerUIWindowHandle;
        }


        public async Task<bool> CanGoBack()
        {
            return await Task.Run(() =>
            {
                if (installerUIWindow != null && installerUIWindow is MainView window)
                {
                    bool? result = false;

                    var dispatcher = installerUIWindow.Dispatcher
                        .InvokeAsync(() => { result = window.Frame.CanGoBack; });

                    var resetEvent = new AutoResetEvent(false);

                    try
                    {
                        dispatcher.Completed += (sender, e) =>
                        {
                            if (!resetEvent.SafeWaitHandle.IsClosed)
                                resetEvent?.Reset();
                        };

                        resetEvent?.WaitOne(TimeSpan.FromSeconds(10));
                        return result != false && result != null;
                    }
                    finally
                    {
                        resetEvent?.Dispose();
                    }
                }

                return false;
            });
        }

        public void Navigate(string contractName)
        {
            RunOnUIThread(() =>
            {
                if (installerUIWindow is MainView window)
                    try
                    {
                        var userControl = Container.GetExportedValue<UserControl>(contractName);
                        _ = window.Frame.Navigate(userControl);
                    }
                    catch (Exception ex)
                    {
                        Engine.Log(LogLevel.Verbose, ex.Message);
                    }
            });
        }

        public void GoBack()
        {
            if (installerUIWindow is MainView window)
                _ = installerUIWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (window.Frame.CanGoBack) window.Frame.GoBack();
                }));
        }

        public void UpdateProgressValue(int progressValue)
        {
            if (installerUIWindow is MainView window)
                RunOnUIThread(() => window.TaskbarIconProgress.ProgressValue = (double)progressValue / 100);
        }

        public void UpdateProgressState(TaskbarItemProgressState progressState)
        {
            if (installerUIWindow is MainView window)
                RunOnUIThread(() => window.TaskbarIconProgress.ProgressState = progressState);
        }

        public async Task BlinkWindow()
        {
            if (installerUIWindow is MainView window)
            {
                WindowInteropHelper wih = new WindowInteropHelper(window);
                while (true)
                {
                    if (window.IsActive) break;
                    FlashWindow(wih.Handle, true);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        #region DLL imports

        /// <summary>
        ///     https://stackoverflow.com/a/58217551/10180512
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="bInvert"></param>
        /// <returns></returns>
        [DllImport("user32")]
        public static extern int FlashWindow(IntPtr hwnd, bool bInvert);

        #endregion


        protected override void Run()
        {
#if DEBUG
            if (MessageBox.Show("You are running in the Debug mode, " +
                                "in order to debug this installer you've to attach the debugger. " +
                                "Go to -> Tools > Attach to Process > Find 'Immo Pro Desktop Installer.exe' in the list and attach. \n\n" +
                                "Do you want to attach debugger?",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                // Do not start until debugger attached
                while (!Debugger.IsAttached)
                    Thread.Sleep(1000);
#endif
            try
            {
                var culture = Thread.CurrentThread.CurrentCulture;
                switch (culture.Parent.Name)
                {
                    case "de":
                        Resources.Culture = new CultureInfo("de");
                        break;
                    case "fr":
                        Resources.Culture = new CultureInfo("fr");
                        break;
                    case "it":
                        Resources.Culture = new CultureInfo("it");
                        break;
                }


                Engine.Log(LogLevel.Verbose, "Entry point of WiX - Run method");


                bootstrapperBundleData = new BootstrapperBundleData();
                Engine.Log(LogLevel.Verbose, JsonConvert.SerializeObject(bootstrapperBundleData));

                SetupCompositionContainer();

                // Create main window with associated view model
                installerUIWindow = Container.GetExportedValue<Window>(ContractNames._MainView);
                installerUIWindowHandle = new WindowInteropHelper(installerUIWindow).EnsureHandle();

                Engine.Log(LogLevel.Verbose, $"Display Mode: {DisplayMode}");

                if (Command.Display == Display.Passive || Command.Display == Display.Full)
                {
                    Engine.Detect();
                    installerUIWindow.Show();
                }
                else
                {
                    Navigate(ContractNames._UninstallView);
                }

                Dispatcher.Run();


                Engine.Quit(0);
                Engine.Log(LogLevel.Verbose, "Exiting custom WPF UI.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.InstallerName, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Container?.Dispose();
            }
        }

        private void SetupCompositionContainer()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            Container = new CompositionContainer(catalog);
            Container.ComposeExportedValue<BootstrapperApplication>(this);
            Container.ComposeExportedValue(bootstrapperBundleData);
            Container.ComposeExportedValue(Engine);
            Container.ComposeExportedValue<IInteractionService>(this);
            Container.ComposeExportedValue<ITaskBar>(this);
            Container.ComposeExportedValue<INavigationService>(this);
            Container.ComposeExportedValue<IDatabaseService>(new DatabaseService());
            Container.ComposeExportedValue<IGlobalCommands>(new GlobalCommands());
            Container.ComposeExportedValue<IDialogService>(new DialogService());
        }
    }
}