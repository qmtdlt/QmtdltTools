using Microsoft.Extensions.DependencyInjection;
using QmtdltTools.Domain;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;

namespace QmtdltTools.EFCore;

[DependsOn(
    typeof(AbpEntityFrameworkCoreModule),
    typeof(QmtdltToolsDomainModule)
    )]
public class QmtdltToolsEFCoreModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddDbContext<DC>(option => { }, ServiceLifetime.Scoped);
    }
}