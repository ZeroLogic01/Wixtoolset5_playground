using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstallerUI.Services.Interfaces
{
    public interface IDialogService
    {
        bool ShowConfirmationDialog(string message, string title);
        void ShowMessage(string message, string title);
        void ShowErrorMessage(string message, string title);
    }

}
