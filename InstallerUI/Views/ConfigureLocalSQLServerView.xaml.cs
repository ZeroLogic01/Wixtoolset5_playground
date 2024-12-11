using InstallerUI.Constants;
using InstallerUI.ViewModels;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace InstallerUI.Views
{
    /// <summary>
    ///     Interaction logic for ConfigureLocalSQLServerInstance.xaml
    /// </summary>
    [Export(ContractNames._ConfigureLocalSQLServerView, typeof(UserControl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class ConfigureLocalSQLServerView : UserControl
    {
        [ImportingConstructor]
        public ConfigureLocalSQLServerView(ConfigureLocalSQLServerViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}