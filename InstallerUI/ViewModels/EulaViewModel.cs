using InstallerUI.Commands;
using InstallerUI.Constants;
using InstallerUI.Services.Interfaces;
using Prism.Mvvm;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace InstallerUI.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class EulaViewModel : BindableBase
    {
        //private string EulaTextValue;

        [Import] private INavigationService navigationService = null;

        [ImportingConstructor]
        public EulaViewModel()
        {
            NextCommand = new RelayCommand(
                ExecuteNextButton
            );
        }

        public ICommand NextCommand { get; set; }

        //public string EulaText
        //{
        //    get => EulaTextValue;
        //    set => SetProperty(ref EulaTextValue, value);
        //}

        private void ExecuteNextButton(object obj)
        {
            navigationService.Navigate(ContractNames._AppSelectionView);
        }
    }
}