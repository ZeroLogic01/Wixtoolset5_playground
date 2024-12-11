using InstallerUI.Constants;
using InstallerUI.ViewModel;
using InstallerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InstallerUI.Views
{
    /// <summary>
    /// Interaction logic for SearchSQLServerInstancesView.xaml
    /// </summary>
    [Export(ContractNames._SearchSQLServerInstancesView, typeof(UserControl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class SearchSQLServerInstancesView : UserControl
    {
        [ImportingConstructor]
        public SearchSQLServerInstancesView(SearchSQLServerInstancesViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (this.DataContext != null)
            { ((SearchSQLServerInstancesViewModel)this.DataContext).SecurePassword = ((PasswordBox)sender).SecurePassword; }
        }

        private void LocalInstancesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext != null)
            { ((SearchSQLServerInstancesViewModel)this.DataContext).SqlServerInstanceName = ((ListBox)sender).SelectedValue.ToString(); }
        }

        private void DatabasesComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var viewModel = DataContext as SearchSQLServerInstancesViewModel;
            if (viewModel.DatabaseNames?.Count == 0)
            {
                viewModel?.LoadDatabasesCommand.Execute(null);
            }
        }
    }
}
