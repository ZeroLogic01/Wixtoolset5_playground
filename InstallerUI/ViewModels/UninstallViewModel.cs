using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InstallerUI.Bootstrapper;
using InstallerUI.Commands;
using InstallerUI.Services.Interfaces;
using InstallerUI.Properties;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Prism.Mvvm;
using InstallerUI.Commands.Interfaces;
using InstallerUI.Constants;

namespace InstallerUI.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class UninstallViewModel : BindableBase
    {
        private readonly BootstrapperApplication bootstrapper;
        private readonly BootstrapperBundleData bootstrapperBundleData;
        private readonly Engine engine;

        [Import] private IInteractionService interactionService = null;

        private bool isElevated = true;

        [Import] private ITaskBar taskBarSercice = null;
        private readonly IGlobalCommands _globalCommands;

        [Import] private IDialogService _dialogService;


        [ImportingConstructor]
        public UninstallViewModel(BootstrapperApplication bootstrapper, Engine engine,
            BootstrapperBundleData bundleData, IGlobalCommands globalCommands)
        {
            bootstrapperBundleData = bundleData;
            this.bootstrapper = bootstrapper;
            this.engine = engine;

            UninstallCommand = new RelayCommand(ExecuteUninstallCommand,
                a => !Uninstalling && Status == InstallationStatus.DetectedPresent);

            CancelCommand = new RelayCommand(
                a =>
                {
                    IsCancelled = true;
                    CurrentAction = Resources.CancelUninstallation;
                    if (IsFinished)
                    {
                        //Dispose();
                        interactionService.CloseUIAndExit();
                    }
                },
                a => (Uninstalling && !IsCancelled) || !IsNotFinished);

            FinishCommand = new RelayCommand(a =>
            {
                //Dispose();
                interactionService.CloseUIAndExit();
            },
                a => IsFinished);

            _globalCommands = globalCommands;
            // Register the local cancel action with the global commands
            _globalCommands.RegisterCancelCommand(CancelCommand);

            bootstrapper.DetectBegin += Bootstrapper_DetectBegin;
            bootstrapper.DetectRelatedBundle += HandleExistingBundleDetected;
            bootstrapper.DetectComplete += DetectComplete;

            engine.Detect();
        }

        #region Detect

        private void Bootstrapper_DetectBegin(object sender, DetectBeginEventArgs ea)
        {
            engine.LogEvent("DetectBegin", ea);

            CurrentAction = Properties.Resources.UninstallerViewHeading;
            DisplayMessage = Properties.Resources.DisplayMessageOnUninstallation;

            interactionService.RunOnUIThread(
                () => Status = ea.Installed ? InstallationStatus.DetectedPresent : InstallationStatus.DetectedAbsent);
        }

        /// <summary>
        /// Only gets invoked when related bundle is detected.
        /// </summary>
        private void HandleExistingBundleDetected(object sender, DetectRelatedBundleEventArgs ea)
        {
            engine.LogEvent("DetectRelatedBundle", ea);
            interactionService.RunOnUIThread(() => Downgrade |= ea.Operation == RelatedOperation.Downgrade);
        }

        private void DetectComplete(object sender, DetectCompleteEventArgs e)
        {
            // release detection related events because we don't need them anymore
            bootstrapper.DetectBegin -= Bootstrapper_DetectBegin;
            bootstrapper.DetectRelatedBundle -= HandleExistingBundleDetected;

            if (bootstrapper.Command.Action == LaunchAction.Uninstall)
            {
                engine.Log(LogLevel.Verbose, "Invoking automatic plan for uninstall");
                // Invoke uninstall command
                UninstallCommand.Execute(null);
            }
        }




        #endregion

        private void SetPackagePlannedState(object sender, PlanPackageBeginEventArgs ea)
        {
            engine.LogEvent("SetPackagePlannedState", ea);

            //if (ea.PackageId.Equals(Common.IMMO_PRO_PACKAGE_ID, StringComparison.OrdinalIgnoreCase)
            //    && ea.State == RequestState.Absent)
            //{
            //    engine.Log(LogLevel.Debug, $"Setting State: {ea.State}");
            //    ea.State = RequestState.None;
            //}
        }

        private void Bootstrapper_ExecuteFilesInUse(object sender, ExecuteFilesInUseEventArgs e)
        {
            foreach (var fileInUse in e.Files)
                try
                {
                    // Extract the Process Id from the string (assuming the format is "Process Name (Process Id: ####)")
                    var processIdString = fileInUse.Substring(fileInUse.LastIndexOf(":") + 1).TrimEnd(')');
                    if (int.TryParse(processIdString, out var processId))
                    {
                        // Get the process by its ID and try to close it
                        Process process = Process.GetProcessById(processId);

                        if (process != null && !process.HasExited)
                        {
                            // First try a graceful close
                            process.CloseMainWindow(); // Try to close the process window if it has one
                            process.WaitForExit(5000); // Wait for up to 5 seconds for the process to exit

                            // If still running, force terminate the process
                            if (!process.HasExited) process.Kill();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (bootstrapper.Command.Action != LaunchAction.Uninstall)
                        _dialogService?.ShowMessage($"Unable to close process for file: {fileInUse}\nError: {ex.Message}",
                            Properties.Resources.InstallerName);
                }

            // Allow uninstallation to continue
            e.Result = Result.Ok;
        }

        #region Plan

        private void BootstrapperOnPlanComplete(object sender, PlanCompleteEventArgs ea)
        {
            engine.LogEvent("PlanComplete", ea);

            // release plannning related events 
            bootstrapper.PlanPackageBegin -= SetPackagePlannedState;
            bootstrapper.PlanComplete -= BootstrapperOnPlanComplete;

            if (ea.Status >= 0)
            {
                taskBarSercice.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.Indeterminate);


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

        #region Error Event Handler

        private void Bootstrapper_Error(object sender, ErrorEventArgs ea)
        {
            if (ea.ErrorType == ErrorType.Elevate)
            {
                isElevated = false;
                taskBarSercice.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.None);
            }
        }

        #endregion


        private void ExecuteUninstallCommand(object obj)
        {
            CurrentAction = Resources.CABeforeUninstallationBegin;

            // Before we tell the windows installer to go off and run the uninstall actions,
            // we need to tell it what we want it to do.
            // This is achieved by calling Engine.Plan() with an action enum (install, uninstall, etc)
            // on the BootstrapperApplication base class. Similar to the Detect() sequence,
            // this will initiate an asynchronous process, so before calling, we need to register event handlers:
            bootstrapper.PlanPackageBegin += SetPackagePlannedState;
            bootstrapper.PlanComplete += BootstrapperOnPlanComplete;

            bootstrapper.ExecuteFilesInUse += Bootstrapper_ExecuteFilesInUse;

            engine.Plan(LaunchAction.Uninstall);
        }

        // Call this method when the view model is disposed or no longer in use
        public void Dispose()
        {
            _globalCommands?.UnregisterCancelCommand(CancelCommand);
        }

        #region Properties for data binding

        public ICommand UninstallCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand FinishCommand { get; set; }

        private bool DowngradeValue;
        public bool Downgrade
        {
            get => DowngradeValue;
            set => SetProperty(ref DowngradeValue, value);
        }

        private InstallationStatus StatusValue = InstallationStatus.DetectedPresent;

        public InstallationStatus Status
        {
            get => StatusValue;
            set => SetProperty(ref StatusValue, value);
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

        private bool UninstallingValue;

        public bool Uninstalling
        {
            get => UninstallingValue;
            set => SetProperty(ref UninstallingValue, value);
        }

        private bool IsNotFinishedValue = true;

        public bool IsNotFinished
        {
            get => IsNotFinishedValue;
            set
            {
                SetProperty(ref IsNotFinishedValue, value);
                IsFinished = !IsNotFinished;
            }
        }

        private bool IsFinishedValue;

        public bool IsFinished
        {
            get => IsFinishedValue;
            set => SetProperty(ref IsFinishedValue, value);
        }

        private bool isCancelledValue;

        public bool IsCancelled
        {
            get => isCancelledValue;
            set => SetProperty(ref isCancelledValue, value);
        }

        private string CurrentActionValue = Resources.UninstallerViewHeading;

        public string CurrentAction
        {
            get => CurrentActionValue;
            set => SetProperty(ref CurrentActionValue, value);
        }

        private string DisplayMessageValue = Resources.DisplayMessageOnUninstallation;

        public string DisplayMessage
        {
            get => DisplayMessageValue;
            set => SetProperty(ref DisplayMessageValue, value);
        }

        #endregion

        #region Apply Events

        private void Bootstrapper_ApplyBegin(object sender, ApplyBeginEventArgs ea)
        {
            engine.LogEvent("ApplyBegin", ea);
            taskBarSercice.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.Indeterminate);

            interactionService.RunOnUIThread(() => Uninstalling = true);
        }

        private void Bootstrapper_ApplyComplete(object sender, ApplyCompleteEventArgs ea)
        {
            engine.LogEvent("ApplyComplete", ea);

            if (isElevated && !IsCancelled)
            {
                IsNotFinished = false;

                CurrentAction = Resources.UninstallationCompleteTitle;
                DisplayMessage = Resources.UninstallationCompleteMessage;

                interactionService.RunOnUIThread(() =>
                {
                    CurrentPackage = string.Empty;
                    Progress = string.Empty;
                    LocalProgress = 0;
                    GlobalProgress = 0;
                    CommandManager.InvalidateRequerySuggested();


                    taskBarSercice.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.None);
                    taskBarSercice?.BlinkWindow();

                    // if the display is embeded (this instance is called by upgraded version)
                    if (bootstrapper.Command.Display == Display.Embedded)
                    {
                        engine.Log(LogLevel.Verbose, "Display is embedded, calling CloseUIAndExit()");
                        interactionService.CloseUIAndExit();
                    }
                });
            }
            else
            {
                interactionService.CloseUIAndExit();
            }

            bootstrapper.ExecuteFilesInUse -= Bootstrapper_ExecuteFilesInUse;
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

            taskBarSercice.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.Normal);

            CurrentAction = Status == InstallationStatus.DetectedAbsent
                ? Resources.CAInstalling
                : Resources.CAUninstalling;

            interactionService.RunOnUIThread(() =>
                CurrentPackage =
                    string.Format(Resources.UninstallingCaption, bootstrapperBundleData.Data.Packages
                        .Where(p => p.Id == ea.PackageId).FirstOrDefault()?.DisplayName.ToLower()));


        }

        private void Bootstrapper_ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs ea)
        {
            engine.LogEvent("ExecutePackageComplete", ea);

            interactionService.RunOnUIThread(() => CurrentPackage = string.Empty);
            taskBarSercice.UpdateProgressState(System.Windows.Shell.TaskbarItemProgressState.None);
        }

        private void Bootstrapper_ExecuteProgress(object sender, ExecuteProgressEventArgs ea)
        {
            engine.LogEvent("ExecuteProgress", ea);
            if (IsCancelled) ea.Result = Result.Abort;
            var caption = Resources.ProgressPercentage;
            Progress = string.Format("{0} {1}{2}", caption, ea.OverallPercentage.ToString(), "%");

            interactionService.RunOnUIThread(() =>
            {
                LocalProgress = ea.ProgressPercentage;
                GlobalProgress = ea.OverallPercentage;
            });
            taskBarSercice.UpdateProgressValue(ea.OverallPercentage);
        }

        #endregion
    }
}