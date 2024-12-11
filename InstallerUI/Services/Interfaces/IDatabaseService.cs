using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InstallerUI.Services.Interfaces
{
    public interface IDatabaseService
    {
        string PrepareSQLServerExpressNameString(string sqlServerInstanceName);
        Task<ObservableCollection<string>> LoadLocalNetworkSqlServerInstancesAsync();
        ObservableCollection<string> LoadLocalNetworkSqlServerInstances();
        ObservableCollection<string> GetLocalSqlServerInstances();
        Task<bool> IsServerConnected(string connectionString);
        bool UpdateConfigFile(string configFile);
        bool UpdateConfigFile(string sqlServerInstanceName, string configFilePath);
        string FetchDefaultConnectionString(string configFilePath);

        /// <summary>
        /// Keeps a copy of connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        void CopyConnectionString(string connectionString);

    }
}