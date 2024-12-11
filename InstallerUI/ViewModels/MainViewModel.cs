using InstallerUI.Bootstrapper;
using InstallerUI.Commands.Interfaces;
using InstallerUI.Constants;
using InstallerUI.Services.Interfaces;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Prism.Mvvm;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WixToolset.BootstrapperApplicationApi;

namespace InstallerUI.ViewModel
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MainViewModel : BindableBase
    {
        private readonly BootstrapperApplication bootstrapper;
        private readonly Engine engine;

        [Import] private IDialogService _dialogService;
        [Import] private INavigationService navigationService = null;

        private readonly IGlobalCommands _globalCommands;

        private bool _isMajorUpgradeDetected = false;

        [Import] private IDatabaseService _databaseService = null;

        /// <summary>
        ///     Name of the next view to load on detect complete event.
        /// </summary>
        private string nextView = string.Empty;

        [ImportingConstructor]
        public MainViewModel(BootstrapperApplication bootstrapper, Engine engine, IGlobalCommands globalCommands)
        {
            this.bootstrapper = bootstrapper;
            this.engine = engine;
            this._globalCommands = globalCommands;

            bootstrapper.DetectBegin += Bootstrapper_DetectBegin;
            bootstrapper.DetectComplete += Bootstrapper_DetectComplete;

            bootstrapper.DetectRelatedBundle += Bootstrapper_DetectRelatedBundle;
        }

        private void Bootstrapper_DetectRelatedBundle(object sender, DetectRelatedBundleEventArgs ea)
        {
            // Handle related bundle detection
            engine.LogEvent($"Related Bundle Detected: ", ea);
            if (ea.Operation == RelatedOperation.MajorUpgrade)
            {
                nextView = ContractNames._InstallView;
                _isMajorUpgradeDetected = true;
            }
        }



        public void CancelAllOperations()
        {
            //var cancel = _dialogService.ShowConfirmationDialog("Are you sure you want to close the installer? " +
            //    "Exiting now will interrupt the installation/uninstallation process, " +
            //    "and any changes made so far may be lost. You might need to restart the " +
            //    "process from the beginning if you proceed.",
            //            Properties.Resources.InstallerName);
            //if (cancel)
            //{
            // Execute all registered cancel commands
            _globalCommands.ExecuteGlobalCancel();
            //}
            //return cancel;
            //return true;
        }

        #region Detect Events
        private void Bootstrapper_DetectBegin(object sender, DetectBeginEventArgs e)
        {
            engine.LogEvent("DetectBegin", e);
              //      this.uninstallCommand = new RelayCommand(param => WixBA.Plan(LaunchAction.Uninstall)
              //      , param =>
              //      this.root.DetectState == DetectionState.Present && this.root.InstallState == InstallationState.Waiting);
            //
            if (e.RegistrationType == RegistrationType.Full)
            {

            }
            //this.root.DetectState = RegistrationType.Full == e.RegistrationType ? DetectionState.Present : DetectionState.Absent;
            //

            WixBA.Model.PlannedAction = LaunchAction.Unknown;
            if (ea.Installed || LaunchAction.Uninstall == bootstrapper..Action)
                
            else
                nextView = ContractNames._EulaView;
        }

        private void Bootstrapper_DetectComplete(object sender, DetectCompleteEventArgs ea)
        {
            engine.LogEvent("DetectComplete", ea);
            engine.Log(LogLevel.Debug, "Unsubscribing DetectBegin & DetectComplete");

            bootstrapper.DetectBegin -= Bootstrapper_DetectBegin;
            bootstrapper.DetectComplete -= Bootstrapper_DetectComplete;
            bootstrapper.DetectRelatedBundle -= Bootstrapper_DetectRelatedBundle;

            if (_isMajorUpgradeDetected)
            {
                string immoProDesktopAppInstallationDirectory =
               engine.GetVariableString(Common.IMMO_PRO_DESKTOP_INSTALL_FOLDER);

                immoProDesktopAppInstallationDirectory =
                    immoProDesktopAppInstallationDirectory.ReplacePlaceholdersWithSystemPaths();
                if (!Directory.Exists(immoProDesktopAppInstallationDirectory))
                {
                    engine.Log(LogLevel.Error, $"{immoProDesktopAppInstallationDirectory} couldn't be found. " +
                                               $"Failed to update the sql server instance name in the {Common.ConfigFileName} file.");
                    return;
                }

                var configFilePath = Path.Combine(immoProDesktopAppInstallationDirectory, Common.ConfigFileName);


                string connectionString = _databaseService.FetchDefaultConnectionString(configFilePath);
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    _databaseService.CopyConnectionString(connectionString);
                }
            }

            // Parse the command line string before any planning.
            this.ParseCommandLine();
          //  this.root.InstallState = InstallationState.Waiting;

            if (LaunchAction.Uninstall == WixBA.Model.Command.Action &&
                ResumeType.Arp != WixBA.Model.Command.Resume) // MSI and WixStdBA require some kind of confirmation before proceeding so WixBA should, too.
            {
                WixBA.Model.Engine.Log(LogLevel.Verbose, "Invoking automatic plan for uninstall");
                WixBA.Plan(LaunchAction.Uninstall);
            }
            else if (Hresult.Succeeded(e.Status))
            {
                if (this.root.UpgradeDetectState == UpgradeDetectionState.Newer)
                {
                    this.Downgrade = true;
                    this.DowngradeMessage = "There is already a newer version of WiX installed on this machine.";
                }

                if (LaunchAction.Layout == WixBA.Model.Command.Action)
                {
                    WixBA.PlanLayout();
                }
                else if (WixBA.Model.Command.Display != Display.Full)
                {
                    // If we're not waiting for the user to click install, dispatch plan with the default action.
                    WixBA.Model.Engine.Log(LogLevel.Verbose, "Invoking automatic plan for non-interactive mode.");
                    WixBA.Plan(WixBA.Model.Command.Action);
                }
            }
            else
            {
                this.root.InstallState = InstallationState.Failed;
            }

            // Force all commands to reevaluate CanExecute.
            // InvalidateRequerySuggested must be run on the UI thread.
            this.root.Dispatcher.Invoke(new Action(CommandManager.InvalidateRequerySuggested));

            navigationService.Navigate(nextView);
        }
        #endregion
    }
}