using System;

namespace InstallerUI.Services.Interfaces
{
    public interface IInteractionService
    {
        void CloseUIAndExit();
        void RunOnUIThread(Action body);
        IntPtr GetMainWindowHandle();
    }
}