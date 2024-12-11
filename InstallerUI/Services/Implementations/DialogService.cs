using InstallerUI.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InstallerUI.Services.Implementations
{
    [Export(typeof(IDialogService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DialogService : IDialogService
    {
        public bool ShowConfirmationDialog(string message, string title)
        {
            // Use MessageBox as an example. Returns true if the user clicks "Yes".
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowErrorMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

}
