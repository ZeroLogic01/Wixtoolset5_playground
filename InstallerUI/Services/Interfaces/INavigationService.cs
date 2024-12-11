using System.Threading.Tasks;

namespace InstallerUI.Services.Interfaces
{
    public interface INavigationService
    {
        Task<bool> CanGoBack();
        void GoBack();
        void Navigate(string contractName);
    }
}