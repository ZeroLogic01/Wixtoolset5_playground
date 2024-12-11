using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InstallerUI.Commands.Interfaces
{
    public interface IGlobalCommands
    {
        void RegisterCancelCommand(ICommand cancelCommand);
        void UnregisterCancelCommand(ICommand cancelCommand);
        void ExecuteGlobalCancel();
    }
}
