using Microsoft.OpenApi.Models;
using QmtdltTools.Service;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Swashbuckle;
using Volo.Abp.AspNetCore.Authentication.JwtBearer;

namespace QmtdltTools;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(QmtdltToolsServiceModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(AbpAspNetCoreAuthenticationJwtBearerModule)
    )]
public class QmtdltToolsAPIModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {        
        base.ConfigureServices(context);
    }   
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        
    }
}