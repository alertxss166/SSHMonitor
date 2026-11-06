using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using System.Reflection;

namespace SSHMonitor;

public partial class MainWindow : Window
{
    private readonly IProfileRepository _store;
    private readonly IServerMonitor _ssh;
    private ServerProfile _profile;
    private readonly DispatcherTimer _timer = new();
    private bool _busy;

    public MainWindow(IProfileRepository store, IServerMonitor ssh)
    {
        InitializeComponent();
        _store = store; _ssh = ssh;
        _profile = _store.Load();
        LoadProfile();
        ConnectButton.Click += async (_, _) => await ConnectAsync();
        SettingsButton.Click += (_, _) => OpenSettings();
        AddCheckButton.Click += (_, _) => OpenSettings();
        LanguageBox.SelectionChanged += (_, _) => ChangeLanguage();
        _timer.Tick += async (_, _) => await RefreshAsync();
        Closed += (_, _) => _ssh.Dispose();
        ThemeHelper.EnableDarkTitleBar(this);
        VersionText.Text = $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0"}";
        ApplyLanguage();
    }

    private void LoadProfile()
    {
        HostBox.Text = _profile.Host; PortBox.Text = _profile.Port.ToString(CultureInfo.InvariantCulture); UserBox.Text = _profile.Username;
        Subtitle.Text = string.IsNullOrWhiteSpace(_profile.Name) ? "Private, agentless Linux monitoring over SSH" : _profile.Name;
        foreach (ComboBoxItem item in LanguageBox.Items) if ((string?)item.Tag == _profile.Language) { LanguageBox.SelectedItem = item; break; }
    }

    private void SaveFields()
    {
        _profile.Host = HostBox.Text.Trim(); _profile.Username = UserBox.Text.Trim();
        _profile.Port = int.TryParse(PortBox.Text, out var port) ? port : 22;
        _store.Save(_profile);
    }

    private async Task ConnectAsync()
    {
        if (_busy) return;
        SaveFields(); SetBusy(true, "● Connecting…", "#D29922");
        try
        {
            await _ssh.ConnectAsync(_profile, PasswordBox.Password);
            PasswordBox.Clear();
            StatusText.Text = Texts.Get(_profile.Language, "Connected"); StatusText.Foreground = Brush("#3FB950"); ConnectButton.Content = Texts.Get(_profile.Language, "Reconnect");
            _timer.Interval = TimeSpan.FromSeconds(Math.Max(3, _profile.RefreshSeconds)); _timer.Start();
            await RefreshAsync();
        }
        catch (Exception ex) { SetError(ex); }
        finally { _busy = false; ConnectButton.IsEnabled = true; }
    }

    private async Task RefreshAsync()
    {
        if (_busy) return; _busy = true;
        try { Render(await _ssh.CollectAsync(_profile)); }
        catch (Exception ex) { _timer.Stop(); SetError(ex); }
        finally { _busy = false; }
    }

    private void Render(ServerSnapshot s)
    {
        ServerText.Text = $"{s.Hostname}\n{s.Os}"; UptimeText.Text = s.Uptime; LoadText.Text = s.Load;
        MemoryText.Text = $"{Bytes(s.MemoryUsedKb * 1024)}\n/ {Bytes(s.MemoryTotalKb * 1024)}";
        var disk = s.Disk.Split(','); DiskText.Text = disk.Length == 3 ? $"{Bytes(Parse(disk[0]) * 1024)} / {Bytes(Parse(disk[1]) * 1024)}  ({disk[2]})" : "Unavailable";
        DiskBar.Value = disk.Length == 3 && double.TryParse(disk[2].TrimEnd('%'), out var pct) ? pct : 0;
        ChecksList.Items.Clear();
        foreach (var c in s.Checks) ChecksList.Items.Add(new TextBlock { Text = $"{(c.Ok ? "●" : "●")}  {c.Name}     {c.Detail}", Foreground = Brush(c.Ok ? "#3FB950" : "#F85149"), FontSize = 15, Margin = new Thickness(4, 8, 4, 8) });
        ConnectionsText.Text = s.Connections.Count > 0 ? string.Join(Environment.NewLine, s.Connections) : Texts.Get(_profile.Language, "NoConnections");
        SecurityText.Text = s.SecurityEvents.Count > 0 ? string.Join(Environment.NewLine, s.SecurityEvents) : Texts.Get(_profile.Language, "NoSecurity");
        FooterText.Text = $"Updated {s.CollectedAt:HH:mm:ss}  •  refresh every {_profile.RefreshSeconds}s  •  SSH encrypted";
    }

    private void OpenSettings()
    {
        var dialog = new SettingsWindow(_profile) { Owner = this };
        if (dialog.ShowDialog() == true) { _profile = dialog.Profile; _store.Save(_profile); LoadProfile(); ApplyLanguage(); }
    }

    private void ChangeLanguage()
    {
        if (LanguageBox.SelectedItem is not ComboBoxItem item || item.Tag is not string language || language == _profile.Language) return;
        _profile.Language = language; _store.Save(_profile); ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        var l = _profile.Language;
        SettingsButton.Content = Texts.Get(l, "SettingsButton"); AddCheckButton.Content = "＋ " + Texts.Get(l, "AddCheck"); ConnectButton.Content = Texts.Get(l, "Connect");
        HostLabel.Text = Texts.Get(l, "Host"); PortLabel.Text = Texts.Get(l, "Port"); UserLabel.Text = Texts.Get(l, "User"); PasswordLabel.Text = Texts.Get(l, "Password");
        UptimeLabel.Text = Texts.Get(l, "Uptime"); MemoryLabel.Text = Texts.Get(l, "Memory"); LoadLabel.Text = Texts.Get(l, "Load"); ServerLabel.Text = Texts.Get(l, "Server"); RootDiskLabel.Text = Texts.Get(l, "RootDisk");
        ChecksLabel.Text = Texts.Get(l, "Checks"); ChecksHintText.Text = Texts.Get(l, "ChecksHint"); PrivacyText.Text = Texts.Get(l, "Privacy"); FooterText.Text = Texts.Get(l, "Footer");
        OverviewTab.Header = Texts.Get(l, "Overview"); AdditionalTab.Header = Texts.Get(l, "Advanced"); ConnectionsTitle.Text = Texts.Get(l, "Connections"); ConnectionsHint.Text = Texts.Get(l, "ConnectionsHint"); SecurityTitle.Text = Texts.Get(l, "Security"); SecurityHint.Text = Texts.Get(l, "SecurityHint");
        if (StatusText.Text.Contains("Offline", StringComparison.Ordinal) || StatusText.Text.Contains("подключ", StringComparison.OrdinalIgnoreCase) || StatusText.Text.Contains("未连接", StringComparison.Ordinal)) StatusText.Text = Texts.Get(l, "Offline");
    }

    private void SetBusy(bool busy, string status, string color) { _busy = busy; ConnectButton.IsEnabled = !busy; StatusText.Text = busy ? Texts.Get(_profile.Language, "Connecting") : status; StatusText.Foreground = Brush(color); }
    private void SetError(Exception ex) { StatusText.Text = Texts.Get(_profile.Language, "Offline"); StatusText.Foreground = Brush("#F85149"); FooterText.Text = ex.Message; }
    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static long Parse(string s) => long.TryParse(s, out var v) ? v : 0;
    private static string Bytes(long n) { string[] u = ["B", "KB", "MB", "GB", "TB"]; double v = n; var i = 0; while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; } return $"{v:0.#} {u[i]}"; }
}
