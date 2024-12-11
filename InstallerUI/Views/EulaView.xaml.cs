using InstallerUI.Constants;
using InstallerUI.ViewModels;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace InstallerUI.Views
{
    /// <summary>
    ///     Interaction logic for EulaView.xaml
    /// </summary>
    [Export(ContractNames._EulaView, typeof(UserControl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class EulaView : UserControl
    {
        [ImportingConstructor]
        public EulaView(EulaViewModel viewModel)
        {
            DataContext = viewModel;

            InitializeComponent();
        }
    }
}