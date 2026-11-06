using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Globalization;

namespace SSHMonitor;

public partial class SettingsWindow : Window
{
    private readonly ObservableCollection<ServiceCheck> _checks;
    public ServerProfile Profile { get; }

    public SettingsWindow(ServerProfile source)
    {
        InitializeComponent();
        Profile = new ServerProfile { Name = source.Name, Host = source.Host, Port = source.Port, Username = source.Username, PrivateKeyPath = source.PrivateKeyPath, RefreshSeconds = source.RefreshSeconds, Language = source.Language };
        _checks = new(source.Checks.Select(x => new ServiceCheck { Name = x.Name, Type = x.Type, Target = x.Target }));
        ChecksGrid.ItemsSource = _checks; TypeColumn.ItemsSource = Enum.GetValues<CheckType>();
        NameBox.Text = Profile.Name; RefreshBox.Text = Profile.RefreshSeconds.ToString(CultureInfo.InvariantCulture); KeyText.Text = Profile.PrivateKeyPath ?? "Password authentication";
        KeyButton.Click += (_, _) => ChooseKey(); CancelButton.Click += (_, _) => DialogResult = false; SaveButton.Click += (_, _) => Save();
        AddCheckButton.Click += (_, _) => AddCheck(); RemoveCheckButton.Click += (_, _) => RemoveCheck();
        Localize(); ThemeHelper.EnableDarkTitleBar(this);
    }

    private void AddCheck()
    {
        var check = new ServiceCheck { Name = Texts.Get(Profile.Language, "NewCheck"), Type = CheckType.Service, Target = "nginx" };
        _checks.Add(check); ChecksGrid.SelectedItem = check; ChecksGrid.ScrollIntoView(check); ChecksGrid.BeginEdit();
    }

    private void RemoveCheck() { if (ChecksGrid.SelectedItem is ServiceCheck check) _checks.Remove(check); }

    private void Localize()
    {
        Title = Texts.Get(Profile.Language, "Settings"); ProfileNameLabel.Text = Texts.Get(Profile.Language, "ProfileName"); RefreshLabel.Text = Texts.Get(Profile.Language, "RefreshSeconds");
        AddCheckButton.Content = "＋ " + Texts.Get(Profile.Language, "AddCheck"); RemoveCheckButton.Content = Texts.Get(Profile.Language, "RemoveSelected"); AddHint.Text = Texts.Get(Profile.Language, "AddHint");
        KeyButton.Content = Texts.Get(Profile.Language, "ChooseKey"); CancelButton.Content = Texts.Get(Profile.Language, "Cancel"); SaveButton.Content = Texts.Get(Profile.Language, "Save");
    }

    private void ChooseKey()
    {
        var picker = new OpenFileDialog { Title = "Select an OpenSSH private key", Filter = "Private keys|id_*;*.pem;*.key|All files|*.*" };
        if (picker.ShowDialog(this) == true) { Profile.PrivateKeyPath = picker.FileName; KeyText.Text = picker.FileName; }
    }

    private void Save()
    {
        Profile.Name = NameBox.Text.Trim(); Profile.RefreshSeconds = int.TryParse(RefreshBox.Text, out var n) ? Math.Clamp(n, 3, 3600) : 10;
        Profile.Checks = _checks.Where(x => !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.Target)).ToList();
        DialogResult = true;
    }
}
