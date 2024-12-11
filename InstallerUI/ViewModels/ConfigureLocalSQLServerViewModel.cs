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
    public class ConfigureLocalSQLServerViewModel : BindableBase
    {
        private readonly Engine engine;

        [Import] private INavigationService navigationService = null;


        private string sqlServerInstanceNameValue;


        [ImportingConstructor]
        public ConfigureLocalSQLServerViewModel(Engine engine)
        {
            this.engine = engine;

            BackBtnCommand = new RelayCommand(
                ExecuteBackButton
            );

            NextBtnCommand = new RelayCommand(ExecuteNextCommand, CanExecuteNextCommand);
        }

        public ICommand BackBtnCommand { get; set; }
        public ICommand NextBtnCommand { get; set; }

        public string SqlServerInstanceName
        {
            get => sqlServerInstanceNameValue;
            set
            {
                SetProperty(ref sqlServerInstanceNameValue, value);

                if (engine != null)
                    engine.StringVariables[Common.SQL_INSTANCE_NAME] = value;
            }
        }

        private void ExecuteNextCommand(object obj)
        {
            navigationService.Navigate(ContractNames._InstallView);
        }

        private bool CanExecuteNextCommand(object obj)
        {
            return !string.IsNullOrWhiteSpace(SqlServerInstanceName);
        }

        private void ExecuteBackButton(object obj)
        {
            navigationService.Navigate(ContractNames._AppSelectionView);
        }
    }
}