using InstallerUI.Commands;
using InstallerUI.Constants;
using InstallerUI.Enums;
using InstallerUI.Helpers;
using InstallerUI.Services.Interfaces;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace InstallerUI.ViewModels
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SearchSQLServerInstancesViewModel : BindableBase
    {
        private readonly Engine engine;

        [Import] private INavigationService navigationService = null;
        [Import] private IDatabaseService databaseService = null;
        [Import] private IDialogService dialogService = null;


        #region Properties

        private AuthenticationTypeEnum _authenticationType;
        public AuthenticationTypeEnum SelectedAuthenticationType
        {
            get { return _authenticationType; }
            set
            {
                SetProperty(ref _authenticationType, value);

                IsUserNamePasswordEnabled = value != AuthenticationTypeEnum.WindowsAuthentication;
                if (value == AuthenticationTypeEnum.WindowsAuthentication)
                {
                    UserName = $"{Environment.MachineName}\\{Environment.UserName}";
                    //if (!IsSecureStringEmpty(SecurePassword))
                    //    SecurePassword.Clear();
                }
                else
                {
                    UserName = string.Empty;
                }


            }
        }

        private BooleanEnum _encrypt;
        public BooleanEnum Encrypt
        {
            get { return _encrypt; }
            set { SetProperty(ref _encrypt, value); }
        }

        private BooleanEnum _trustServerCertificate;
        public BooleanEnum TrustServerCertificate
        {
            get { return _trustServerCertificate; }
            set { SetProperty(ref _trustServerCertificate, value); }
        }

        private ObservableCollection<string> _installedInstances;
        public ObservableCollection<string> LocallyInstalledInstances
        {
            get { return _installedInstances; }
            set { SetProperty(ref _installedInstances, value); }
        }
        private ObservableCollection<string> _installedInstancesOnLocalNetwork;
        public ObservableCollection<string> InstalledInstancesOnLocalNetwork
        {
            get { return _installedInstancesOnLocalNetwork; }
            set { SetProperty(ref _installedInstancesOnLocalNetwork, value); }
        }

        private string _userName;

        public string UserName
        {
            get { return _userName; }
            set { SetProperty(ref _userName, value); }
        }

        private SecureString _securePassword;
        public SecureString SecurePassword
        {
            get { return _securePassword; }
            set { SetProperty(ref _securePassword, value); }
        }
        private bool _isUserNamePasswordEnabled;

        public bool IsUserNamePasswordEnabled
        {
            get { return _isUserNamePasswordEnabled; }
            set { SetProperty(ref _isUserNamePasswordEnabled, value); }
        }

        private string _sqlServerInstanceNameValue;
        public string SqlServerInstanceName
        {
            get => _sqlServerInstanceNameValue;
            set
            {
                if (!value.Equals(_sqlServerInstanceNameValue))
                {
                    DatabaseNames?.Clear();
                }
                SetProperty(ref _sqlServerInstanceNameValue, value);
            }
        }
        private bool _isBusy;

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                SetProperty(ref _isBusy, value);
            }
        }

        private ObservableCollection<string> _databaseNames;
        public ObservableCollection<string> DatabaseNames
        {
            get { return _databaseNames; }
            set { SetProperty(ref _databaseNames, value); }
        }

        private string _selectedDatabaseName;
        public string SelectedDatabaseName
        {
            get { return _selectedDatabaseName; }
            set { SetProperty(ref _selectedDatabaseName, value); }
        }
        #endregion


        #region Constructor
        [ImportingConstructor]
        public SearchSQLServerInstancesViewModel(Engine engine)
        {
            this.engine = engine;

            BackBtnCommand = new RelayCommand(
                ExecuteBackButton
            );

            //NextBtnCommand = new AsyncRelayCommand(ExecuteNextCommandAsync, CanExecuteNextCommand);

            LoadedCommand = new RelayCommand(ExecuteLoadedCommand);
            DatabaseNames = new ObservableCollection<string>();
            LoadDatabasesCommand = new RelayCommand(LoadDatabases, CanLoadDatabases);
            IsBusy = false;
        }

        private bool CanLoadDatabases(object obj)
        {
            return !IsBusy;
        }

        private void LoadDatabases(object data)
        {
            string connectionString = ConstructConnectionString();
            var databases = GetDatabaseNames(connectionString);
            DatabaseNames.Clear();
            foreach (var dbName in databases)
            {
                DatabaseNames.Add(dbName);
            }
            SelectedDatabaseName = DatabaseNames.First();
        }
        #endregion

        #region Commands
        public ICommand BackBtnCommand { get; set; }
        private AsyncRelayCommand _nextBtnCommand;
        public AsyncRelayCommand NextBtnCommand
        {
            get
            {
                if (_nextBtnCommand == null)
                {
                    _nextBtnCommand = new AsyncRelayCommand(ExecuteNextCommandAsync, CanExecuteNextCommand);
                }
                return _nextBtnCommand;
            }
        }
        public ICommand LoadedCommand { set; get; }

        public ICommand LoadDatabasesCommand { get; }

        private async void ExecuteLoadedCommand(object obj)
        {
            LocallyInstalledInstances = databaseService.GetLocalSqlServerInstances();

            SelectedAuthenticationType = AuthenticationTypeEnum.WindowsAuthentication;
            Encrypt = BooleanEnum.True;
            TrustServerCertificate = BooleanEnum.True;

            try
            {
                InstalledInstancesOnLocalNetwork = await Task.Run(() =>
                { return databaseService.LoadLocalNetworkSqlServerInstancesAsync(); });
            }
            catch (Exception ex)
            {
                engine.Log(LogLevel.Error, ex.Message);
                dialogService.ShowErrorMessage(ex.Message, Properties.Resources.InstallerName);
            }
        }
        private async Task ExecuteNextCommandAsync(object obj)
        {
            try
            {
                IsBusy = true;
                string connectionString = ConstructConnectionString();
                if (await databaseService.IsServerConnected(connectionString))
                {// if connection successfull include db name in the connection string.
                    databaseService.CopyConnectionString(ConstructConnectionString(true));
                    navigationService.Navigate(ContractNames._InstallView);
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowErrorMessage(ex.Message, Properties.Resources.InstallerName);
            }
            finally
            {
                IsBusy = false;
            }
        }


        private bool CanExecuteNextCommand(object obj)
        {
            if (IsBusy)
            {
                return false;
            }

            if (IsUserNamePasswordEnabled)
            {
                if (string.IsNullOrWhiteSpace(UserName) || IsSecureStringEmpty(SecurePassword))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(UserName))
                {
                    return false;
                }
            }

            bool isServerNameEmpty = string.IsNullOrWhiteSpace(SqlServerInstanceName);

            return !isServerNameEmpty;
        }

        private void ExecuteBackButton(object obj)
        {
            navigationService.Navigate(ContractNames._AppSelectionView);
        }
        #endregion

        public static bool IsSecureStringEmpty(SecureString secureString) { return secureString == null || secureString.Length == 0; }

        public string ConstructConnectionString(bool includeInitialCatalog = false)
        {

            // Construct the connection string
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = SqlServerInstanceName,
                //      InitialCatalog = database,
                Encrypt = this.Encrypt == BooleanEnum.True,
                TrustServerCertificate = this.TrustServerCertificate == BooleanEnum.True,

            };
            if (includeInitialCatalog)
            {
                // if database name is empty or equals to <default> 
                if (string.IsNullOrWhiteSpace(SelectedDatabaseName) ||
                    SelectedDatabaseName.Equals(Common.Default_Database))
                {
                    SelectedDatabaseName = Common.Default_Database_Name;
                }
                builder.InitialCatalog = SelectedDatabaseName;
                builder.MultipleActiveResultSets = true;
            }

            if (SelectedAuthenticationType == AuthenticationTypeEnum.WindowsAuthentication)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = UserName;
                builder.Password = PasswordBoxHelper.ConvertToUnsecureString(SecurePassword);
            }

            return builder.ConnectionString;
        }

        public static List<string> GetDatabaseNames(string connectionString)
        {
            List<string> databases = new List<string>
            {
                Common.Default_Database,
            };
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT name FROM sys.databases", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            databases.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return databases;
        }

    }
}