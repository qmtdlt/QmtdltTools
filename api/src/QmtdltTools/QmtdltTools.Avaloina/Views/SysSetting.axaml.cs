using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using Volo.Abp.DependencyInjection;

namespace QmtdltTools.Avaloina.Views;

public partial class SysSetting : Window,ITransientDependency
{
    public SysSetting(SysSettingVm vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}

public class SysSettingVm: ReactiveObject, ITransientDependency
{
    public SysSettingVm()
    {
        TestStr = "±ðÅÂÎÒÉËÐÄ";
    }


    private string _testStr;
    public string TestStr
    {
        get { return _testStr; }
        set
        {
            this.RaiseAndSetIfChanged(ref _testStr, value);
        }
    }
}