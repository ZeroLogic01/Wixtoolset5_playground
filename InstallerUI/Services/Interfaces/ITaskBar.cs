using System.Threading.Tasks;
using System.Windows.Shell;

namespace InstallerUI.Services.Interfaces
{
    internal interface ITaskBar
    {
        /// <summary>
        ///     Updates TaskbarItemInfo Progress value
        /// </summary>
        /// <param name="progressValue">Progress value</param>
        void UpdateProgressValue(int progressValue);

        /// <summary>
        ///     Update TaskbarItemInfo Progress state
        /// </summary>
        /// <param name="progressState">Progress State eunm.</param>
        void UpdateProgressState(TaskbarItemProgressState progressState);

        /// <summary>
        ///     Makes WPF Window to blink on the taskbar if the window is not active
        /// </summary>
        Task BlinkWindow();
    }
}