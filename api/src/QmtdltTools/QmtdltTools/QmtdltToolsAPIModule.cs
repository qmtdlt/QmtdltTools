using Microsoft.OpenApi.Models;
using QmtdltTools.Service;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Swashbuckle;

namespace QmtdltTools;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(QmtdltToolsServiceModule),
    typeof(AbpAspNetCoreSignalRModule) 
    )]
public class QmtdltToolsAPIModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
       
        // context.Services.AddControllers();
        // context.Services.AddAbpSwaggerGen(optins =>
        // {
        //     optins.SwaggerDoc("v1", new OpenApiInfo { Title = "QmtdltTools API", Version = "v1" });
        //     optins.DocInclusionPredicate((docName, description) => true);
        //     optins.CustomSchemaIds(type => type.FullName);
        // });
        base.ConfigureServices(context);
    }   
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        
    }
}