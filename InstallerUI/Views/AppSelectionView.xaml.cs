using InstallerUI.Constants;
using InstallerUI.ViewModels;
using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace InstallerUI.Views
{
    /// <summary>
    ///     Interaction logic for AppSelectionView.xaml
    /// </summary>
    [Export(ContractNames._AppSelectionView, typeof(UserControl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class AppSelectionView : UserControl
    {
        [ImportingConstructor]
        public AppSelectionView(AppSelectionViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}