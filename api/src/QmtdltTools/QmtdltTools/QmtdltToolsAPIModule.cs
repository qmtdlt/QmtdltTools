using Microsoft.OpenApi.Models;
using QmtdltTools.Service;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp;
using Volo.Abp.Swashbuckle;

namespace QmtdltTools;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(QmtdltToolsServiceModule)
    )]
public class QmtdltToolsAPIModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpSwaggerGen(optins =>
        {
            optins.SwaggerDoc("v1", new OpenApiInfo { Title = "QmtdltTools API", Version = "v1" });
            optins.DocInclusionPredicate((docName, description) => true);
            optins.CustomSchemaIds(type => type.FullName);
        });
        base.ConfigureServices(context);
    }   
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var env = context.GetEnvironment();
        var app = context.GetApplicationBuilder();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseAbpSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "QmtdltTools API");
            });
        }
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseConfiguredEndpoints();
        
    }
}