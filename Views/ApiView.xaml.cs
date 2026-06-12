using System.Windows.Controls;

namespace Feishu.Context.Views;

public partial class ApiView : UserControl
{
    public ApiView(ViewModels.ApiViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
