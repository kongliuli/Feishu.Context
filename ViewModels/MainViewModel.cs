using CommunityToolkit.Mvvm.ComponentModel;

namespace Feishu.Context.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// 当前选中的 Tab 索引
    /// </summary>
    [ObservableProperty]
    private int _selectedTabIndex;
}
