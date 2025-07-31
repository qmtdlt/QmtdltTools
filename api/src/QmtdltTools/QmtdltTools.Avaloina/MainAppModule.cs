using Microsoft.Extensions.DependencyInjection;
using QmtdltTools.Avaloina.Views;
using QmtdltTools.Domain;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace QmtdltTools.Avaloina;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(QmtdltToolsDomainModule)
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