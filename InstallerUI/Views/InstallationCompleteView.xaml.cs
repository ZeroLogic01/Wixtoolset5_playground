using InstallerUI.Constants;
using InstallerUI.ViewModels;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace InstallerUI.Views
{
    /// <summary>
    ///     Interaction logic for InstallationComplete.xaml
    /// </summary>
    [Export(ContractNames._InstallationComplete, typeof(UserControl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class InstallationCompleteView : UserControl
    {
        [ImportingConstructor]
        public InstallationCompleteView(InstallationCompleteViewModel viewModel)
        {
            DataContext = viewModel;

            InitializeComponent();
        }
    }
}