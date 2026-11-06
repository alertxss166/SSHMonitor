using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
namespace SSHMonitor;
public partial class App : Application
{
    private readonly IHost _host;
    public App()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddJsonFile("appsettings.json", optional: false);
        builder.Services.AddOptions<MonitoringOptions>().BindConfiguration(MonitoringOptions.SectionName).ValidateDataAnnotations().ValidateOnStart();
        builder.Services.AddSingleton<IProfileRepository, JsonProfileRepository>();
        builder.Services.AddSingleton<IServerMonitor, SshServerMonitor>();
        builder.Services.AddSingleton<MainWindow>();
        _host = builder.Build();
    }
    protected override async void OnStartup(StartupEventArgs e) { await _host.StartAsync(); _host.Services.GetRequiredService<MainWindow>().Show(); base.OnStartup(e); }
    protected override async void OnExit(ExitEventArgs e) { await _host.StopAsync(); _host.Dispose(); base.OnExit(e); }
}
