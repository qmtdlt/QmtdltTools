using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace QmtdltTools.Service;
[DependsOn(
    typeof(AbpAutofacModule))]
public class QmtdltToolsServiceModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
    }
}