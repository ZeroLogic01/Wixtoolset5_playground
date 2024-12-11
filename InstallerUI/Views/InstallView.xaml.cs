using InstallerUI.Constants;
using InstallerUI.ViewModel;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace InstallerUI.Views
{
    /// <summary>
    ///     Interaction logic for InstallView.xaml
    /// </summary>
    [Export(ContractNames._InstallView, typeof(UserControl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class InstallView : UserControl
    {
        [ImportingConstructor]
        public InstallView(InstallViewModel viewModel)
        {
            DataContext = viewModel;

            InitializeComponent();
        }

        private void UserControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is InstallViewModel viewModel)
            {
                viewModel.Dispose();
            }
        }
    }
}