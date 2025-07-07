using Microsoft.Extensions.DependencyInjection;
using Polly;
using QmtdltTools.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.AspNetCore.Authentication.JwtBearer;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace QmtdltTools.WPF
{
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
}
