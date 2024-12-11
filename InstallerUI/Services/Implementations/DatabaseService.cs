using System;
using System.Collections.ObjectModel;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using InstallerUI.Constants;
using InstallerUI.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace InstallerUI.Bootstrapper
{
    public class DatabaseService : IDatabaseService
    {
        public string ConnectionString { get; set; }

        public ObservableCollection<string> GetLocalSqlServerInstances()
        {
            ObservableCollection<string> instances = new ObservableCollection<string>();

            string ServerName = Environment.MachineName;
            Microsoft.Win32.RegistryView registryView = Environment.Is64BitOperatingSystem ? Microsoft.Win32.RegistryView.Registry64 : Microsoft.Win32.RegistryView.Registry32;
            using (Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, registryView))
            {
                Microsoft.Win32.RegistryKey instanceKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", false);
                if (instanceKey != null)
                {
                    foreach (var instanceName in instanceKey.GetValueNames())
                    {
                        if (instanceName == "MSSQLSERVER")
                        {
                            instances.Add(ServerName);

                        }
                        else
                        {
                            instances.Add(ServerName + "\\" + instanceName);
                        }
                    }
                }
            }

            // Debugging output
            foreach (var instance in instances)
            {
                Debug.WriteLine($"Found SQL instance: {instance}");
            }

            return instances;
        }


        public async Task<bool> IsServerConnected(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    return true;
                }
                catch (SqlException ex)
                {
                    throw ex;
                }
            }
        }

        public ObservableCollection<string> LoadLocalNetworkSqlServerInstances()
        {
            ObservableCollection<string> instances = new ObservableCollection<string>();
            var sqlInstances = SqlDataSourceEnumerator.Instance.GetDataSources();

            foreach (System.Data.DataRow row in sqlInstances.Rows)
            {
                string instanceName = string.IsNullOrEmpty(row["InstanceName"].ToString())
                    ? row["ServerName"].ToString()
                    : $"{row["ServerName"]}\\{row["InstanceName"]}";

                instances.Add(instanceName);
                Debug.WriteLine(instanceName);
            }

            return instances;
        }
        public async Task<ObservableCollection<string>> LoadLocalNetworkSqlServerInstancesAsync()
        {
            return await Task.Run(() => { return LoadLocalNetworkSqlServerInstances(); });
        }

        public string PrepareSQLServerExpressNameString(string sqlServerInstanceName)
        {
            return string.Format(@".\{0}", sqlServerInstanceName);
        }

        public bool UpdateConfigFile(string sqlServerInstanceName, string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath)) throw new ArgumentNullException(nameof(configFilePath));

            if (string.IsNullOrEmpty(sqlServerInstanceName)) throw new ArgumentNullException(nameof(sqlServerInstanceName));

            // Read and parse the JSON file
            var json = JObject.Parse(File.ReadAllText(configFilePath));

            // Access the connection string and update the Server name
            var connectionString = json["ConnectionStrings"][Common.ConnectionStringName].ToString();

            // Update the server part of the connection string
            var newConnectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Server\s*=\s*)([^;]+)",
                $"$1{sqlServerInstanceName}"
            );

            // Set the updated connection string back into the JSON object
            json["ConnectionStrings"][Common.ConnectionStringName] = newConnectionString;

            // Save the updated JSON back to the file
            File.WriteAllText(configFilePath, json.ToString());

            return true;
        }

        public bool UpdateConfigFile(string configFilePath)
        {
            if (string.IsNullOrEmpty(ConnectionString)) throw new ArgumentNullException(nameof(ConnectionString));

            if (string.IsNullOrEmpty(configFilePath)) throw new ArgumentNullException(nameof(configFilePath));


            // Read and parse the JSON file
            var json = JObject.Parse(File.ReadAllText(configFilePath));

            // Set the updated connection string back into the JSON object
            json["ConnectionStrings"][Common.ConnectionStringName] = ConnectionString;

            // Save the updated JSON back to the file
            File.WriteAllText(configFilePath, json.ToString());

            return true;
        }

        public string FetchDefaultConnectionString(string configFilePath)
        {
            if (string.IsNullOrEmpty(configFilePath)) throw new ArgumentNullException(nameof(configFilePath));

            // Read and parse the JSON file
            var json = JObject.Parse(File.ReadAllText(configFilePath));

            // Access the connection string and update the Server name
            return json["ConnectionStrings"][Common.ConnectionStringName].ToString();
        }

        public void CopyConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}