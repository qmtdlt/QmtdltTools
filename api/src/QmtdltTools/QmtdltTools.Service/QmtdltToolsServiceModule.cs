using QmtdltTools.EFCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace QmtdltTools.Service;
[DependsOn(
    typeof(AbpAutofacModule),
    typeof(QmtdltToolsEFCoreModule)
    )]
public class QmtdltToolsServiceModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
    }
}