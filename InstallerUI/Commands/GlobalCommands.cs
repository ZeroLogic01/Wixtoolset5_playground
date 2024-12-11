using InstallerUI.Commands.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Input;

namespace InstallerUI.Commands
{
    [Export(typeof(IGlobalCommands))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class GlobalCommands : IGlobalCommands
    {
        private readonly List<ICommand> _cancelCommands = new List<ICommand>();

        public void RegisterCancelCommand(ICommand cancelCommand)
        {
            if (!_cancelCommands.Contains(cancelCommand))
                _cancelCommands.Add(cancelCommand);
        }

        public void UnregisterCancelCommand(ICommand cancelCommand)
        {
            if (_cancelCommands.Contains(cancelCommand))
                _cancelCommands.Remove(cancelCommand);
        }

        public void ExecuteGlobalCancel()
        {
            foreach (var cancelCommand in _cancelCommands)
            {
                if (cancelCommand.CanExecute(null))
                    cancelCommand.Execute(null);
            }
        }
    }
}
