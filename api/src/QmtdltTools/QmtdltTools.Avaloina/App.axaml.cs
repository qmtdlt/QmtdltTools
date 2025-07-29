using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QmtdltTools.Avaloina.ViewModels;
using QmtdltTools.Avaloina.Views;
using QmtdltTools.Domain.Data;
using Serilog;
using Serilog.Events;
using Volo.Abp;

namespace QmtdltTools.Avaloina;

public partial class App : Application
{
    private static IAbpApplicationWithInternalServiceProvider? _abpApplication;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Debug.WriteLine("Starting Avalonia host...");
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Async(c => c.File(ApplicationConst.LogPath))
                .CreateLogger();

            try
            {
                Log.Information("Starting Avalonia host.");

                _abpApplication = AbpApplicationFactory.Create<MainAppModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
                });

                _abpApplication.Initialize();

                var _configuration = _abpApplication.Services.GetRequiredService<IConfiguration>();

                ApplicationConst.SPEECH_KEY = _configuration.GetSection("MySecret:SPEECH_KEY").Value??"";
                ApplicationConst.SPEECH_REGION = _configuration.GetSection("MySecret:SPEECH_REGION").Value ?? "";
                ApplicationConst.GROK_KEY = _configuration.GetSection("MySecret:GROK_KEY").Value ?? "";
                ApplicationConst.GEMINI_KEY = _configuration.GetSection("MySecret:GEMINI_KEY").Value ?? "";
                ApplicationConst.DOU_BAO = _configuration.GetSection("MySecret:DOU_BAO").Value ?? "";
                ApplicationConst.QIAN_WEN = _configuration.GetSection("MySecret:QIAN_WEN").Value ?? "";

                ApplicationConst.AIType = int.Parse(_configuration.GetSection("MySecret:AIType").Value ??"4");

                DisableAvaloniaDataAnnotationValidation();              // avalonia 取消数据验证
                
                desktop.MainWindow = _abpApplication.Services.GetRequiredService<MainWindow>();
                desktop.MainWindow.DataContext = new MainWindowViewModel();
                desktop.MainWindow.Show();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
            }
            
            // DisableAvaloniaDataAnnotationValidation();
            // desktop.MainWindow = new MainWindow
            // {
            //     DataContext = new MainWindowViewModel(),
            // };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    
    // protected override async void OnExit(ExitEventArgs e)
    // {
    //     if (_abpApplication != null)
    //     {
    //         await _abpApplication.ShutdownAsync();
    //     }
    //     Log.CloseAndFlush();
    // }

    public static T Get<T>()
    {
        return _abpApplication.Services.GetRequiredService<T>();
    }
}