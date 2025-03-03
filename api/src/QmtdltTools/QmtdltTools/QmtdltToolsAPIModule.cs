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
    const string Cors = "VueApp";
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var allowedCorsOrigins = configuration.GetSection("AllowedCorsOrigins").Get<string[]>();

        context.Services.AddCors(options =>
        {
            options.AddPolicy(Cors, builder =>
            {
                builder.WithOrigins(allowedCorsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

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

        //if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseAbpSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "QmtdltTools API");
            });
        }
        // 增加身份认证和授权
        // 增加跨域配置
        app.UseCors(Cors);



        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseConfiguredEndpoints();
    }
}