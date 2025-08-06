# avalonia 知识点

1. 使用 ReactiveUI
```xml
  <PackageReference Include="Avalonia.ReactiveUI" Version="11.3.2" />
```
2. 程序入口 UseReactiveUI
```CSharp
public static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace()
        .UseReactiveUI();
```
3. ViewModel
```CSharp

public partial class SysSetting : Window
{
    public SysSetting()
    {
        InitializeComponent();
        DataContext = new TestViewModel();
    }
}

public class TestViewModel: ReactiveObject
{
    public SysSettingVm()
    {
        TestStr = "别怕我伤心";
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
```
4. axaml xmlns and Datatype,Design.DataContext
```xml
<Window>
    ...
    xmlns:vm="using:QmtdltTools.Avaloina.Views"
    x:DataType="vm:SysSettingVm"
    ...
/>

</Window>
<Design.DataContext>
  <vm:SysSettingVm />
</Design.DataContext>

<Grid>
</Grid>
```
5. 