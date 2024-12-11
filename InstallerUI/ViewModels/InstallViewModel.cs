using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InstallerUI.Bootstrapper;
using InstallerUI.Commands;
using InstallerUI.Constants;
using InstallerUI.Services.Interfaces;
using InstallerUI.Properties;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Prism.Mvvm;
using InstallerUI.Commands.Interfaces;
using System.Management;

namespace InstallerUI.ViewModel
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class InstallViewModel : BindableBase
    {
        private readonly BootstrapperApplication bootstrapper;
        private readonly BootstrapperBundleData bootstrapperBundleData;
        private readonly Engine engine;

        [Import] private IDatabaseService databaseService = null;

        [Import] private IInteractionService interactionService = null;
        [Import] private IDialogService _dialogService;
        private readonly IGlobalCommands _globalCommands;

        private bool isElevated = true;

        [Import] private INavigationService navigationSercice = null;

        private bool packageInstallationFailed;

        /// <summary>
        ///     To determine if the sql server instance is created or not
        /// </summary>
        private bool sqlServerInstanceCreated;

        [Import] private ITaskBar taskBarService = null;

        [ImportingConstructor]
        public InstallViewModel(BootstrapperApplication bootstrapper, Engine engine, BootstrapperBundleData bundleData,
            IGlobalCommands globalCommands)
        {
            bootstrapperBundleData = bundleData;
            this.bootstrapper = bootstrapper;
            this.engine = engine;

            InstallCommand = new RelayCommand(
                ExecuteInstallCommand,
                CanInstall);

            CancelCommand = new RelayCommand(
                a =>
                {
                    IsCancelled = true;
                    CurrentAction = Resources.CancelInstallation;
                },
                a => Installing && !IsCancelled);

            BackBtnCommand = new RelayCommand(
               a =>
               {
                   if (IsSqlServerExpressInstallSelected())
                   {
                       navigationSercice?.Navigate(ContractNames._ConfigureLocalSQLServerView);
                   }
                   else
                   {
                       navigationSercice?.Navigate(ContractNames._AppSelectionView);
                   }
               },
               a => !Installing);

            CurrentAction = Resources.InstallerName;

            bootstrapper.Error += Bootstrapper_Error;

            LoadedCommand = new RelayCommand(ExecuteLoadedCommand);
            _globalCommands = globalCommands;

            // Register the local cancel action with the global commands
            _globalCommands.RegisterCancelCommand(CancelCommand);
        }

        #region Error Event Handler

        private void Bootstrapper_Error(object sender,
            Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ErrorEventArgs ea)
        {
            if (ea.ErrorType == ErrorType.Elevate)
            {
                isElevated = false;
                taskBarService.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.None);
            }
        }

        #endregion


        private void ExecuteInstallCommand(object obj)
        {
            CurrentAction = Resources.CABeforeInstallationBegin;
            packageInstallationFailed = false;

            // Before we tell the windows installer to go off and run the install actions,
            // we need to tell it what we want it to do.
            // This is achieved by calling Engine.Plan() with an action enum (install, uninstall, etc)
            // on the BootstrapperApplication base class. Similar to the Detect() sequence,
            // this will initiate an asynchronous process, so before calling, we need to register event handlers:
            bootstrapper.PlanPackageBegin += SetPackagePlannedState;
            bootstrapper.PlanComplete += BootstrapperOnPlanComplete;

            engine.Plan(LaunchAction.Install);
        }


        private bool CanInstall(object obj)
        {
            return !Installing && Status == InstallationStatus.DetectedAbsent;
        }

        private void ExecuteLoadedCommand(object obj)
        {
            InstallCommand.Execute(obj);

            // refresh commands (install)
            CommandManager.InvalidateRequerySuggested();
        }

        // Call this method when the view model is disposed or no longer in use
        public void Dispose()
        {
            _globalCommands.UnregisterCancelCommand(CancelCommand);
        }

        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid, string parentProcessName)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }

            try
            {
                // Get the parent process
                Process parentProc = Process.GetProcessById(pid);
                string parentProcName = parentProc.ProcessName;

                // Get child processes of the parent process
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
                ManagementObjectCollection moc = searcher.Get();

                foreach (ManagementObject mo in moc.Cast<ManagementObject>())
                {
                    int childPid = Convert.ToInt32(mo["ProcessID"]);
                    Process childProc = Process.GetProcessById(childPid);

                    KillProcessAndChildren(childPid, parentProcessName);
                    // Only kill child process if its name is different from the parent's name
                    if (childProc != null && !childProc.ProcessName.Equals(parentProcName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            if (!childProc.HasExited)
                            {
                                childProc.Kill();
                                childProc.WaitForExit();
                            }
                        }
                        catch (Exception ex)
                        {
                            // Handle exceptions if the process is already terminated or not accessible
                            Console.WriteLine(ex.Message);
                        }
                    }
                }

                // Do not kill the parent process
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
            catch (Exception ex)
            {
                // Handle other exceptions such as access denied
                Console.WriteLine(ex.Message);
            }
        }


        #region Properties for data binding

        public ICommand LoadedCommand { set; get; }
        public ICommand BackBtnCommand { get; set; }

        public ICommand InstallCommand { get; set; }

        public ICommand CancelCommand { get; set; }

        private InstallationStatus StatusValue = InstallationStatus.DetectedAbsent;

        public InstallationStatus Status
        {
            get => StatusValue;
            set => SetProperty(ref StatusValue, value);
        }

        private bool DowngradeValue;

        public bool Downgrade
        {
            get => DowngradeValue;
            set => SetProperty(ref DowngradeValue, value);
        }

        private int LocalProgressValue;

        public int LocalProgress
        {
            get => LocalProgressValue;
            set => SetProperty(ref LocalProgressValue, value);
        }

        private int GlobalProgressValue;

        public int GlobalProgress
        {
            get => GlobalProgressValue;
            set => SetProperty(ref GlobalProgressValue, value);
        }

        private string ProgressValue;

        public string Progress
        {
            get => ProgressValue;
            set => SetProperty(ref ProgressValue, value);
        }

        private string CurrentPackageValue;

        public string CurrentPackage
        {
            get => CurrentPackageValue;
            set => SetProperty(ref CurrentPackageValue, value);
        }

        private bool InstallingValue;

        public bool Installing
        {
            get => InstallingValue;
            set => SetProperty(ref InstallingValue, value);
        }

        private bool isCancelledValue;

        public bool IsCancelled
        {
            get => isCancelledValue;
            set => SetProperty(ref isCancelledValue, value);
        }

        private string CurrentActionValue = Resources.InstallerName;

        public string CurrentAction
        {
            get => CurrentActionValue;
            set => SetProperty(ref CurrentActionValue, value);
        }

        #endregion


        #region Plan

        private void SetPackagePlannedState(object sender, PlanPackageBeginEventArgs ea)
        {
            engine.LogEvent("SetPackagePlannedState", ea);

            if (ea.PackageId.Equals(Common.SQL_SERVER_PACKAGE_ID, StringComparison.OrdinalIgnoreCase))
            {
                if (!IsSqlServerExpressInstallSelected())
                    ea.State = RequestState.Absent;
            }
        }

        private bool IsSqlServerExpressInstallSelected()
        {
            string installSqlServer = engine.StringVariables[Common.INSTALL_SQL_SERVER_EXPRESS_2022];
            return installSqlServer.ToLower().Equals("true");
        }

        private void BootstrapperOnPlanComplete(object sender, PlanCompleteEventArgs ea)
        {
            engine.LogEvent("PlanComplete", ea);

            // release plannning related events 
            bootstrapper.PlanPackageBegin -= SetPackagePlannedState;
            bootstrapper.PlanComplete -= BootstrapperOnPlanComplete;

            if (ea.Status >= 0)
            {
                // Running the action
                //  Once the Engine's planning phase is complete,
                //  we can call Engine.Apply(IntPtr.Zero) to execute the action.
                //  Note: the parameter to Apply() is a window handle. IntPtr.Zero also works.
                //  The Apply action will provide status via events as well:
                bootstrapper.ApplyBegin += Bootstrapper_ApplyBegin;

                bootstrapper.ExecuteMsiMessage += Bootstrapper_ExecuteMsiMessage;
                bootstrapper.ExecuteComplete += Bootstrapper_ExecuteComplete;

                bootstrapper.ExecuteProgress += Bootstrapper_ExecuteProgress;

                bootstrapper.ExecutePackageBegin += Bootstrapper_ExecutePackageBegin;
                bootstrapper.ExecutePackageComplete += Bootstrapper_ExecutePackageComplete;

                bootstrapper.ApplyComplete += Bootstrapper_ApplyComplete;

                // apply
                engine.Apply(interactionService.GetMainWindowHandle());
            }
        }

        #endregion

        #region Apply Events

        private void Bootstrapper_ApplyBegin(object sender, ApplyBeginEventArgs ea)
        {
            engine.LogEvent("ApplyBegin");
            taskBarService.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.Indeterminate);

            interactionService.RunOnUIThread(() => Installing = true);
        }

        private void Bootstrapper_ApplyComplete(object sender, ApplyCompleteEventArgs ea)
        {
            engine.LogEvent("ApplyComplete", ea);

            bootstrapper.ApplyBegin -= Bootstrapper_ApplyBegin;
            bootstrapper.ApplyComplete -= Bootstrapper_ApplyComplete;

            taskBarService.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.None);

            if (isElevated && !IsCancelled && !packageInstallationFailed)
            {
                try
                {
                    UpdateSqlServerInstanceName();
                }
                catch (Exception ex)
                {
                    engine.Log(LogLevel.Error, ex.Message);
                }

                bootstrapper.Error -= Bootstrapper_Error;

                navigationSercice.Navigate(ContractNames._InstallationComplete);
            }
            else
            {
                interactionService.RunOnUIThread(() =>
                {
                    CurrentAction = Resources.InstallerName;
                    IsCancelled = false;
                    Installing = false;
                    CurrentPackage = string.Empty;
                    Progress = string.Empty;
                    LocalProgress = 0;
                    GlobalProgress = 0;
                    isElevated = true;
                    // refresh commands (install)
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }

        private void UpdateSqlServerInstanceName()
        {
            string immoProDesktopAppInstallationDirectory =
                engine.StringVariables[Common.IMMO_PRO_DESKTOP_INSTALL_FOLDER];

            immoProDesktopAppInstallationDirectory =
                immoProDesktopAppInstallationDirectory.ReplacePlaceholdersWithSystemPaths();
            if (!Directory.Exists(immoProDesktopAppInstallationDirectory))
            {
                engine.Log(LogLevel.Error, $"{immoProDesktopAppInstallationDirectory} couldn't be found. " +
                                           $"Failed to update the sql server instance name in the {Common.ConfigFileName} file.");
                return;
            }

            var configFilePath = Path.Combine(immoProDesktopAppInstallationDirectory, Common.ConfigFileName);

            if (sqlServerInstanceCreated)
            {
                string instanceName = engine.StringVariables[Common.SQL_INSTANCE_NAME];
                instanceName = (databaseService.PrepareSQLServerExpressNameString(instanceName));

                engine.Log(LogLevel.Verbose, $"Updating the Server Instance in the {configFilePath}");

                databaseService.UpdateConfigFile(instanceName,
                    configFilePath);
                engine.Log(LogLevel.Verbose, $"{instanceName} is updated as SQL server instance name in the {configFilePath}.");
            }
            else
            {
                databaseService.UpdateConfigFile(configFilePath);
            }
        }

        #endregion

        #region Execute Events

        private void Bootstrapper_ExecuteComplete(object sender, ExecuteCompleteEventArgs ea)
        {
            engine.LogEvent("ExecuteComplete", ea);

            bootstrapper.ExecuteMsiMessage -= Bootstrapper_ExecuteMsiMessage;
            bootstrapper.ExecuteComplete -= Bootstrapper_ExecuteComplete;

            bootstrapper.ExecuteProgress -= Bootstrapper_ExecuteProgress;

            bootstrapper.ExecutePackageBegin -= Bootstrapper_ExecutePackageBegin;
            bootstrapper.ExecutePackageComplete -= Bootstrapper_ExecutePackageComplete;
        }

        private void Bootstrapper_ExecuteMsiMessage(object sender, ExecuteMsiMessageEventArgs ea)
        {
            engine.LogEvent("ExecuteMsiMessage", ea);
        }

        private void Bootstrapper_ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs ea)
        {
            engine.LogEvent("ExecutePackageBegin", ea);
            taskBarService.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.Normal);

            CurrentAction = Status == InstallationStatus.DetectedAbsent
                ? Resources.CAInstalling
                : Resources.CAUninstalling;

            // Imp! When doing major Upgrades, package name will be null
            var currentPackageName = bootstrapperBundleData.Data.Packages.Where(p => p.Id == ea.PackageId)
                .FirstOrDefault()?.DisplayName;
            if (!string.IsNullOrWhiteSpace(currentPackageName))
                interactionService.RunOnUIThread(() =>
                    CurrentPackage = string.Format(Resources.InstallingCaption, currentPackageName.ToLower()));
            else
                CurrentPackage = Resources.RemovingPreviousVersion;


        }

        private void Bootstrapper_ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs ea)
        {
            engine.LogEvent("ExecutePackageComplete", ea);


            if (ea.Status >= 0)
            {
                // On successful install of sql server express, we will write the instance name to config file on
                // installation complete
                if (ea.PackageId.Equals(Common.SQL_SERVER_PACKAGE_ID, StringComparison.OrdinalIgnoreCase))
                    sqlServerInstanceCreated = true;

                interactionService.RunOnUIThread(() => CurrentPackage = string.Empty);
                taskBarService.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.None);
                // Success: SQL Server Express installed correctly
                engine.Log(LogLevel.Verbose, $"{ea.PackageId} installed successfully.");
            }
            else
            {
                if (!IsCancelled)
                {
                    taskBarService.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.Error);

                    var errorMessage = $"{ea.PackageId} installation failed with error code: " + ea.Status.ToString() +
                                       $"{Environment.NewLine} Would you like to try again?";
                    // Failure: SQL Server Express installation failed
                    engine.Log(LogLevel.Error, errorMessage);


                    var confirmed
                        = _dialogService.ShowConfirmationDialog(errorMessage, Properties.Resources.InstallerName);
                    if (!confirmed)
                        engine.Quit(ea.Status);
                    else
                        packageInstallationFailed = true;
                }
            }
        }

        private void Bootstrapper_ExecuteProgress(object sender, ExecuteProgressEventArgs ea)
        {
            engine.LogEvent("ExecuteProgress", ea);
            if (IsCancelled)
            {
                ea.Result = Result.Abort;

                if (ea.PackageId.Equals(Common.SQL_SERVER_PACKAGE_ID, StringComparison.OrdinalIgnoreCase))
                {
                    var mainProcess = Process.GetCurrentProcess();
                    KillProcessAndChildren(Process.GetCurrentProcess().Id, mainProcess.ProcessName);
                }
            }
            var caption = Resources.ProgressPercentage;
            Progress = string.Format("{0} {1}{2}", caption, ea.OverallPercentage.ToString(), "%");


            interactionService.RunOnUIThread(() =>
            {
                LocalProgress = ea.ProgressPercentage;
                GlobalProgress = ea.OverallPercentage;
            });
            taskBarService.UpdateProgressValue(ea.OverallPercentage);
        }

        #endregion
    }
}