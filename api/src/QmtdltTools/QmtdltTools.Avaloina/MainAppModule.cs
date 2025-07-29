using Microsoft.Extensions.DependencyInjection;
using QmtdltTools.Avaloina.Views;
using QmtdltTools.Service;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace QmtdltTools.Avaloina;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(QmtdltToolsServiceModule)
)]

public class MainAppModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<MainWindow>();
        base.ConfigureServices(context);
    }
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {

    }
}