using InstallerUI.Constants;
using InstallerUI.ViewModels;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace InstallerUI.Views
{
    /// <summary>
    ///     Interaction logic for InstallAddon.xaml
    /// </summary>
    [Export(ContractNames._UninstallView, typeof(UserControl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class UninstallView : UserControl
    {
        [ImportingConstructor]
        public UninstallView(UninstallViewModel viewModel)
        {
            DataContext = viewModel;

            InitializeComponent();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UninstallViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}