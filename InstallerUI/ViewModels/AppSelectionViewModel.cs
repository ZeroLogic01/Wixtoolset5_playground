using InstallerUI.Bootstrapper;
using InstallerUI.Commands;
using InstallerUI.Constants;
using InstallerUI.Services.Interfaces;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Prism.Mvvm;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace InstallerUI.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class AppSelectionViewModel : BindableBase
    {
        private readonly BootstrapperApplication bootstrapper;
        private readonly BootstrapperBundleData bootstrapperBundleData;
        private readonly Engine engine;

        private bool installApplicationValue;

        private bool installSqlServerValue;

        [Import] private INavigationService navigationService = null;


        [ImportingConstructor]
        public AppSelectionViewModel(BootstrapperApplication bootstrapper, Engine engine,
            BootstrapperBundleData bundleData)
        {
            bootstrapperBundleData = bundleData;
            this.bootstrapper = bootstrapper;
            this.engine = engine;

            BackBtnCommand = new RelayCommand(
                ExecuteBackButton
            );

            NextBtnCommand = new RelayCommand(ExecuteNextCommand, CanExecuteNextCommand);

            InstallApplication = true;
        }

        public ICommand BackBtnCommand { get; set; }
        public ICommand NextBtnCommand { get; set; }

        public bool InstallApplication
        {
            get => installApplicationValue;
            set => SetProperty(ref installApplicationValue, value);
        }

        public bool InstallSqlServer
        {
            get => installSqlServerValue;
            set
            {
                SetProperty(ref installSqlServerValue, value);

                if (engine != null)
                    engine.StringVariables[Common.INSTALL_SQL_SERVER_EXPRESS_2022]
                        = value ? "true" : "false";
            }
        }

        private void ExecuteNextCommand(object obj)
        {
            if (InstallSqlServer)
                navigationService.Navigate(ContractNames._ConfigureLocalSQLServerView);
            else
                navigationService.Navigate(ContractNames._SearchSQLServerInstancesView);
        }

        private bool CanExecuteNextCommand(object obj)
        {
            return InstallApplication || InstallSqlServer;
        }

        private void ExecuteBackButton(object obj)
        {
            navigationService.Navigate(ContractNames._EulaView);
        }
    }
}