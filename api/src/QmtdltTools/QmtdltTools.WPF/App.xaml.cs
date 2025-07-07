using Serilog.Events;
using Serilog;
using System.Configuration;
using System.Data;
using System.Windows;
using Volo.Abp;
using QmtdltTools.Domain.Data;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
namespace QmtdltTools.WPF;

public partial class App : Application
{
    private static IAbpApplicationWithInternalServiceProvider? _abpApplication;

    protected override async void OnStartup(StartupEventArgs e)
    {
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
            Log.Information("Starting WPF host.");

            _abpApplication = await AbpApplicationFactory.CreateAsync<MainAppModule>(options =>
            {
                options.UseAutofac();
                options.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));
            });

            await _abpApplication.InitializeAsync();

            var _configuration = _abpApplication.Services.GetRequiredService<IConfiguration>();

            ApplicationConst.SPEECH_KEY = _configuration.GetSection("MySecret:SPEECH_KEY").Value??"";
            ApplicationConst.SPEECH_REGION = _configuration.GetSection("MySecret:SPEECH_REGION").Value ?? "";
            ApplicationConst.GROK_KEY = _configuration.GetSection("MySecret:GROK_KEY").Value ?? "";
            ApplicationConst.GEMINI_KEY = _configuration.GetSection("MySecret:GEMINI_KEY").Value ?? "";
            ApplicationConst.DOU_BAO = _configuration.GetSection("MySecret:DOU_BAO").Value ?? "";
            ApplicationConst.QIAN_WEN = _configuration.GetSection("MySecret:QIAN_WEN").Value ?? "";

            _abpApplication.Services.GetRequiredService<MainWindow>()?.Show();

        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_abpApplication != null)
        {
            await _abpApplication.ShutdownAsync();
        }
        Log.CloseAndFlush();
    }

    public static T Get<T>()
    {
        return _abpApplication.Services.GetRequiredService<T>();
    }
}

