using InstallerUI.Commands;
using InstallerUI.Services.Interfaces;
using Prism.Mvvm;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace InstallerUI.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class InstallationCompleteViewModel : BindableBase
    {
        #region Constructor

        [ImportingConstructor]
        public InstallationCompleteViewModel()
        {
            LoadedCommand = new RelayCommand(ExecuteLoadedCommand);

            FinishCommand = new RelayCommand(ExecuteFinishCommand, CanExecuteFinish);
        }

        #endregion

        #region Private Members

        [Import] private IInteractionService interactionService = null;

        [Import] private ITaskBar taskBarService = null;

        #endregion

        #region Properties for data binding

        public ICommand LoadedCommand { set; get; }

        public ICommand FinishCommand { get; set; }

        #endregion

        #region Methods

        private bool CanExecuteFinish(object obj)
        {
            return true;
        }

        private void ExecuteFinishCommand(object obj)
        {
            interactionService.CloseUIAndExit();
        }

        private void ExecuteLoadedCommand(object obj)
        {
            _ = taskBarService?.BlinkWindow();
        }

        #endregion
    }
}