using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using Tools.Deployer.Configuration;
using Tools.Deployer.Deployment;

namespace Tools.Deployer;

/// <summary>
/// Hauptfenster: Einstellungen (.env) bearbeiten, Deployment-Skripte aus deploy\ ausführen, Container-Status anzeigen.
/// Die eigentliche Deploy-Logik steckt in den Skripten; dieses Fenster ruft sie nur auf.
/// </summary>
internal sealed class MainForm : Form
{
    private static readonly Color OkColor = Color.SeaGreen;
    private static readonly Color WarnColor = Color.DarkOrange;
    private static readonly Color ErrorColor = Color.Firebrick;
    private static readonly Color IdleColor = Color.Gray;
    private const string PasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

    private readonly AppSettings appSettings = AppSettings.Load();
    private string repositoryPath = "";
    private DeployTarget target = DeployTarget.Development;
    private bool suppressTargetChange;

    // Einstellungen
    private EnvFile? envFile;
    private Dictionary<string, string> savedValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TextBox> editors = new(StringComparer.Ordinal);
    private bool isLoadingSettings;

    // Ausführung
    private CancellationTokenSource? runCancellation;
    private bool statusRefreshRunning;
    private readonly List<string> steps = [];
    private readonly List<Button> actionButtons = [];

    // Steuerelemente
    private readonly Label repositoryLabel = new() { AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
    private readonly Button changeRepositoryButton = new() { Text = "Ändern…", AutoSize = true };
    private readonly ComboBox targetCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, Margin = new Padding(3, 5, 3, 3) };
    private readonly Label dockerLabel = new() { AutoSize = true, Margin = new Padding(20, 8, 3, 3), Text = "● Docker: wird geprüft …", ForeColor = IdleColor };
    private readonly Button startDockerButton = new() { Text = "Docker Desktop starten", AutoSize = true, Visible = false };
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly TabPage settingsPage = new("1 · Einstellungen") { Padding = new Padding(8) };
    private readonly TabPage deploymentPage = new("2 · Deployment") { Padding = new Padding(8) };

    private readonly Panel settingsPanel = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly Label settingsFileLabel = new() { AutoSize = true, Margin = new Padding(3, 3, 3, 6) };
    private readonly Label issuesLabel = new() { AutoSize = true, MaximumSize = new Size(1100, 0), Margin = new Padding(3, 0, 3, 8) };
    private readonly Button saveButton = new() { Text = "Speichern", AutoSize = true, MinimumSize = new Size(110, 32) };
    private readonly Button discardButton = new() { Text = "Verwerfen", AutoSize = true, MinimumSize = new Size(110, 32) };

    private readonly FlowLayoutPanel actionPanel = new() { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly CheckBox noCacheCheck = new() { Text = "Ohne Build-Cache (-NoCache)", AutoSize = true };
    private readonly CheckBox testCheck = new() { Text = "Vorher Tests ausführen (-Test)", AutoSize = true };
    private readonly CheckBox pullCheck = new() { Text = "Neue MySQL/Mailpit-Images holen (-Pull)", AutoSize = true };
    private readonly CheckBox allCheck = new() { Text = "Alle Dienste neu starten (-All)", AutoSize = true };
    private readonly CheckBox skipBackupCheck = new() { Text = "Ohne Datenbank-Backup (-SkipBackup)", AutoSize = true };
    private readonly Button cancelButton = new() { Text = "Abbrechen", Width = 260, Height = 32, Enabled = false, Margin = new Padding(3, 16, 3, 3) };

    private readonly Label resultLabel = new() { AutoSize = true, Text = "Bereit.", ForeColor = IdleColor, Margin = new Padding(3, 3, 3, 6) };
    private readonly ListBox stepList = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly RichTextBox logBox = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        WordWrap = false,
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.Gainsboro,
        BorderStyle = BorderStyle.None,
    };

    private readonly FlowLayoutPanel containerPanel = new() { AutoSize = true, WrapContents = true, Dock = DockStyle.Top };
    private readonly FlowLayoutPanel linkPanel = new() { AutoSize = true, WrapContents = true, Dock = DockStyle.Top };

    private readonly System.Windows.Forms.Timer statusTimer = new() { Interval = 5000 };
    private readonly ToolTip toolTip = new() { AutoPopDelay = 20000 };

    private Font? sectionFont;
    private Font? helpFont;
    private Font? boldFont;

    public MainForm()
    {
        Text = "DartMetrics Deployment";
        Size = new Size(1200, 850);
        MinimumSize = new Size(950, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        logBox.Font = new Font("Consolas", 9.5f);
        stepList.Font = new Font("Segoe UI", 10f);

        BuildLayout();
        statusTimer.Tick += async (_, _) => await RefreshStatusAsync();
    }

    private Font SectionFont => sectionFont ??= new Font(Font.FontFamily, 11f, FontStyle.Bold);

    private Font HelpFont => helpFont ??= new Font(Font.FontFamily, 8.5f);

    private Font BoldFont => boldFont ??= new Font(Font, FontStyle.Bold);

    private bool IsBusy => runCancellation is not null;

    private bool IsDirty => envFile is not null
        && (!envFile.Exists || editors.Any(pair => pair.Value.Text.Trim() != savedValues.GetValueOrDefault(pair.Key, "")));

    // ------------------------------------------------------------------ Aufbau

    private void BuildLayout()
    {
        var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Padding = new Padding(8, 6, 8, 2) };
        changeRepositoryButton.Click += (_, _) => ChangeRepository();
        targetCombo.Items.AddRange(DeployTarget.All.Cast<object>().ToArray());
        targetCombo.SelectedIndexChanged += OnTargetChanged;
        startDockerButton.Click += (_, _) => StartDockerDesktop();
        header.Controls.AddRange(
        [
            Caption("Lösungsordner:"), repositoryLabel, changeRepositoryButton,
            Caption("Umgebung:", 24), targetCombo,
            dockerLabel, startDockerButton,
        ]);

        tabs.TabPages.Add(settingsPage);
        tabs.TabPages.Add(deploymentPage);
        BuildSettingsPage();
        BuildDeploymentPage();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(tabs, 0, 1);
        Controls.Add(root);
    }

    private void BuildSettingsPage()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        saveButton.Click += (_, _) => SaveSettings();
        discardButton.Click += (_, _) => LoadSettings();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.AddRange([saveButton, discardButton]);

        layout.Controls.Add(settingsFileLabel, 0, 0);
        layout.Controls.Add(issuesLabel, 0, 1);
        layout.Controls.Add(settingsPanel, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        settingsPage.Controls.Add(layout);
    }

    private void BuildDeploymentPage()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));

        // Links: Aktionen und Optionen
        var options = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        options.Controls.AddRange([noCacheCheck, testCheck, pullCheck, allCheck, skipBackupCheck]);
        toolTip.SetToolTip(skipBackupCheck, "Sonst wird die Datenbank vor dem Update nach backups\\ gesichert (die API wendet beim Start Migrationen an).");
        cancelButton.Click += (_, _) => runCancellation?.Cancel();

        var left = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        left.Controls.AddRange([SectionLabel("Aktionen"), actionPanel, SectionLabel("Optionen"), options, cancelButton]);

        // Rechts: Ergebnis, Schritte, Log
        var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        resultLabel.Font = BoldFont;
        right.Controls.Add(resultLabel, 0, 0);
        right.Controls.Add(stepList, 0, 1);
        right.Controls.Add(logBox, 0, 2);

        // Unten: Container-Status und Links
        var statusBox = new GroupBox { Text = "Status", Dock = DockStyle.Fill };
        var statusLayout = new Panel { Dock = DockStyle.Fill };
        statusLayout.Controls.Add(linkPanel);
        statusLayout.Controls.Add(containerPanel);   // zuletzt hinzugefügt = oben angedockt
        statusBox.Controls.Add(statusLayout);

        layout.Controls.Add(left, 0, 0);
        layout.Controls.Add(right, 1, 0);
        layout.Controls.Add(statusBox, 0, 1);
        layout.SetColumnSpan(statusBox, 2);
        deploymentPage.Controls.Add(layout);
    }

    private static Label Caption(string text, int leftMargin = 3)
        => new() { Text = text, AutoSize = true, Margin = new Padding(leftMargin, 8, 3, 3) };

    private Label SectionLabel(string text)
        => new() { Text = text, AutoSize = true, Font = SectionFont, Margin = new Padding(3, 8, 3, 6) };

    // ------------------------------------------------------------------ Start, Ordner, Umgebung

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        var path = RepositoryLocator.Find(appSettings.RepositoryPath) ?? AskForRepository();
        if (path is null)
        {
            MessageBox.Show(this, "Ohne den Lösungsordner (sources) kann nichts deployt werden.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            return;
        }

        SetRepository(path);
        target = DeployTarget.All.FirstOrDefault(t => t.Name == appSettings.TargetName) ?? DeployTarget.Development;
        suppressTargetChange = true;
        targetCombo.SelectedItem = target;
        suppressTargetChange = false;

        ReloadForTarget();
        statusTimer.Start();
        await RefreshStatusAsync();
    }

    private string? AskForRepository()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Ordner „sources“ der DartMetrics-Lösung wählen (enthält docker-compose.yml)",
            UseDescriptionForTitle = true,
            InitialDirectory = repositoryPath,
        };

        while (dialog.ShowDialog(this) == DialogResult.OK)
        {
            var path = RepositoryLocator.Normalize(dialog.SelectedPath);
            if (path is not null)
            {
                return path;
            }

            MessageBox.Show(this, "In diesem Ordner fehlen docker-compose.yml, .env.example oder deploy\\deploy.ps1.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        return null;
    }

    private void ChangeRepository()
    {
        if (!ConfirmSaveOrDiscard())
        {
            return;
        }

        var path = AskForRepository();
        if (path is null)
        {
            return;
        }

        SetRepository(path);
        ReloadForTarget();
        _ = RefreshStatusAsync();
    }

    private void SetRepository(string path)
    {
        repositoryPath = path;
        repositoryLabel.Text = path;
        appSettings.RepositoryPath = path;
        appSettings.Save();
    }

    private void OnTargetChanged(object? sender, EventArgs e)
    {
        if (suppressTargetChange || targetCombo.SelectedItem is not DeployTarget selected || selected == target)
        {
            return;
        }

        if (!ConfirmSaveOrDiscard())
        {
            suppressTargetChange = true;
            targetCombo.SelectedItem = target;
            suppressTargetChange = false;
            return;
        }

        target = selected;
        appSettings.TargetName = target.Name;
        appSettings.Save();
        ReloadForTarget();
        _ = RefreshStatusAsync();
    }

    private void ReloadForTarget()
    {
        LoadSettings();
        RebuildActions();
        RebuildLinks();
        foreach (var control in containerPanel.Controls.Cast<Control>().ToList())
        {
            control.Dispose();
        }
    }

    // ------------------------------------------------------------------ Einstellungen (.env)

    private void LoadSettings()
    {
        isLoadingSettings = true;
        settingsPanel.SuspendLayout();
        var oldControls = settingsPanel.Controls.Cast<Control>().ToList();
        settingsPanel.Controls.Clear();
        oldControls.ForEach(control => control.Dispose());
        editors.Clear();

        var templatePath = Path.Combine(repositoryPath, ".env.example");
        var envPath = Path.Combine(repositoryPath, target.EnvFileName);
        var mainEnvPath = Path.Combine(repositoryPath, ".env");

        // Neue .env.beta: wie deploy-beta.ps1 aus der .env übernehmen (sonst aus der Vorlage).
        var fallbackPath = target.IsBeta && File.Exists(mainEnvPath) ? mainEnvPath : templatePath;
        envFile = EnvFile.Load(envPath, fallbackPath);
        savedValues = envFile.Exists ? envFile.ReadValues() : new Dictionary<string, string>(StringComparer.Ordinal);
        var values = envFile.ReadValues();

        var fields = EnvTemplate.Parse(File.ReadAllLines(templatePath));
        if (target.IsBeta)
        {
            fields.AddRange(EnvTemplate.BetaFields);
            if (!envFile.Exists)
            {
                values["BETA_HTTPS_PORT"] = "7354";
                values["BETA_HTTP_PORT"] = "5223";
                values["WEB_APP_PORT"] = "7354";
            }
        }

        var knownKeys = fields.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var key in values.Keys.Where(key => !knownKeys.Contains(key) && key != "COMPOSE_FILE").Order())
        {
            fields.Add(new EnvField(key, EnvTemplate.OtherSection, "Steht in der Datei, aber nicht in der .env.example.", "", true));
        }

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            Padding = new Padding(0, 0, 24, 0),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 270));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        string? section = null;
        foreach (var field in fields)
        {
            if (field.Section != section)
            {
                section = field.Section;
                AddSectionRow(table, section);
            }

            AddFieldRows(table, field, values.GetValueOrDefault(field.Key, ""));
        }

        settingsPanel.Controls.Add(table);
        settingsPanel.ResumeLayout();

        settingsFileLabel.Text = envFile.Exists
            ? $"Datei: {envPath}"
            : $"{target.EnvFileName} gibt es noch nicht. Sie wird beim Speichern angelegt (Vorlage: {Path.GetFileName(fallbackPath)}).";
        isLoadingSettings = false;
        UpdateSettingsState();
    }

    private static int AddRow(TableLayoutPanel table)
    {
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return table.RowCount++;
    }

    private void AddSectionRow(TableLayoutPanel table, string title)
    {
        var row = AddRow(table);
        var label = new Label
        {
            Text = title,
            AutoSize = true,
            Font = SectionFont,
            ForeColor = Color.FromArgb(40, 70, 120),
            Margin = new Padding(0, row == 0 ? 4 : 18, 0, 6),
        };
        table.Controls.Add(label, 0, row);
        table.SetColumnSpan(label, 3);
    }

    private void AddFieldRows(TableLayoutPanel table, EnvField field, string value)
    {
        var row = AddRow(table);
        var isRequired = EnvValidator.RequiredKeys.Contains(field.Key);
        var caption = new Label
        {
            Text = FieldTexts.DisplayName(field.Key) + (isRequired ? " *" : ""),
            AutoSize = true,
            Margin = new Padding(0, 7, 8, 0),
        };

        var editor = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = value,
            UseSystemPasswordChar = field.IsSecret,
            PlaceholderText = field.IsOptional && field.DefaultValue.Length > 0 ? $"Standard: {field.DefaultValue}" : "",
            Margin = new Padding(0, 3, 6, 3),
        };
        editor.TextChanged += (_, _) =>
        {
            if (!isLoadingSettings)
            {
                UpdateSettingsState();
            }
        };
        editors[field.Key] = editor;
        toolTip.SetToolTip(editor, field.Key);

        var extras = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        if (field.IsSecret)
        {
            var show = new CheckBox { Text = "anzeigen", AutoSize = true, Margin = new Padding(3, 6, 3, 0) };
            show.CheckedChanged += (_, _) => editor.UseSystemPasswordChar = !show.Checked;
            var generate = new Button { Text = "Erzeugen", AutoSize = true };
            generate.Click += (_, _) => GenerateSecret(field.Key, editor);
            extras.Controls.AddRange([show, generate]);
        }

        table.Controls.Add(caption, 0, row);
        table.Controls.Add(editor, 1, row);
        table.Controls.Add(extras, 2, row);

        var helpRow = AddRow(table);
        var help = new Label
        {
            Text = FieldTexts.Help(field),
            AutoSize = true,
            MaximumSize = new Size(820, 0),
            ForeColor = Color.DimGray,
            Font = HelpFont,
            Margin = new Padding(0, 0, 0, 8),
        };
        table.Controls.Add(help, 1, helpRow);
        table.SetColumnSpan(help, 2);
    }

    private void GenerateSecret(string key, TextBox editor)
    {
        var current = editor.Text.Trim();
        var isRealValue = current.Length > 0 && !current.StartsWith("change-me", StringComparison.OrdinalIgnoreCase);
        if (isRealValue && key.StartsWith("MYSQL_", StringComparison.Ordinal)
            && MessageBox.Show(this,
                "Das MySQL-Passwort wirkt nur bei der Erstinstallation. Wird es danach hier geändert, kann die API sich nicht mehr anmelden.\n\nTrotzdem ein neues erzeugen?",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        editor.Text = RandomNumberGenerator.GetString(PasswordChars, key == "JWT_KEY" ? 64 : 24);
    }

    private Dictionary<string, string> CurrentValues()
        => editors.ToDictionary(pair => pair.Key, pair => pair.Value.Text.Trim(), StringComparer.Ordinal);

    private void UpdateSettingsState()
    {
        var issues = EnvValidator.Validate(CurrentValues(), target, repositoryPath);
        foreach (var (key, editor) in editors)
        {
            var hasError = issues.Any(issue => issue.Key == key && issue.Severity != IssueSeverity.Warning);
            editor.BackColor = hasError ? Color.MistyRose : SystemColors.Window;
        }

        var blocking = issues.Any(issue => issue.Severity != IssueSeverity.Warning);
        issuesLabel.Text = issues.Count == 0
            ? "✔ Alle Pflichtangaben sind ausgefüllt."
            : string.Join(Environment.NewLine, issues.Select(issue => (issue.Severity == IssueSeverity.Warning ? "⚠ " : "✖ ") + issue.Message));
        issuesLabel.ForeColor = blocking ? ErrorColor : issues.Count > 0 ? WarnColor : OkColor;

        var dirty = IsDirty;
        saveButton.Enabled = dirty && !IsBusy;
        discardButton.Enabled = dirty && envFile is { Exists: true };
        settingsPage.Text = dirty ? "1 · Einstellungen *" : "1 · Einstellungen";
    }

    private bool SaveSettings()
    {
        if (envFile is null)
        {
            return false;
        }

        var values = CurrentValues();
        var invalid = EnvValidator.Validate(values, target, repositoryPath)
            .Where(issue => issue.Severity == IssueSeverity.Invalid)
            .ToList();
        if (invalid.Count > 0)
        {
            MessageBox.Show(this, "Nicht gespeichert:\n\n" + string.Join("\n", invalid.Select(issue => "• " + issue.Message)), Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // Nur geänderte Werte schreiben: unveränderte Zeilen bleiben exakt wie sie sind.
        var isNew = !envFile.Exists;
        foreach (var (key, value) in values)
        {
            if (isNew || value != savedValues.GetValueOrDefault(key, ""))
            {
                envFile.Set(key, value);
            }
        }

        try
        {
            envFile.Save();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"{envFile.FilePath} konnte nicht gespeichert werden:\n{exception.Message}", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        savedValues = envFile.ReadValues();
        settingsFileLabel.Text = $"Datei: {envFile.FilePath}  –  gespeichert um {DateTime.Now:HH:mm:ss}";
        UpdateSettingsState();
        RebuildLinks();
        return true;
    }

    /// <summary>true = weitermachen (gespeichert oder verworfen), false = abbrechen.</summary>
    private bool ConfirmSaveOrDiscard()
    {
        if (!IsDirty)
        {
            return true;
        }

        var answer = MessageBox.Show(this, "Die Einstellungen wurden geändert. Speichern?", Text,
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        return answer switch
        {
            DialogResult.Yes => SaveSettings(),
            DialogResult.No => true,
            _ => false,
        };
    }

    // ------------------------------------------------------------------ Aktionen

    private sealed record DeployAction(string Text, string Hint, Func<Task> Run, bool IsPrimary = false);

    private IEnumerable<DeployAction> GetActions()
    {
        if (target.IsBeta)
        {
            yield return new("Beta bauen und starten",
                "deploy-beta.ps1: prüft .env.beta, baut die Images (:beta) und startet die Beta-Instanz parallel zur normalen.",
                () => RunScriptAsync("deploy-beta.ps1", NoCacheArgs()), true);
            yield return new("Einstellungen übernehmen",
                "Startet die Container mit der gespeicherten .env.beta neu (ohne Build). Geänderte Container werden ersetzt.",
                () => RunComposeAsync(true, "up", "-d", "--remove-orphans"));
            foreach (var action in CommonComposeActions())
            {
                yield return action;
            }

            yield return new("Beta zurücksetzen …",
                "Löscht Container UND Datenbank der Beta und setzt sie neu auf (deploy-beta.ps1 -Reset).",
                () => ConfirmAndRunScriptAsync("Beta-Instanz einschließlich ihrer Datenbank löschen und neu aufsetzen?", "deploy-beta.ps1", ["-Reset"], true));
            yield return new("Beta entfernen …",
                "Entfernt Container, Volumes (Datenbank) und Images der Beta (deploy-beta.ps1 -Remove).",
                () => ConfirmAndRunScriptAsync("Beta-Instanz vollständig entfernen, einschließlich ihrer Datenbank?", "deploy-beta.ps1", ["-Remove"], false));
            yield break;
        }

        yield return new("Erstinstallation / Neuaufbau",
            "deploy.ps1: prüft .env, Zertifikat und Schriften, baut die Images und startet alles. Für den ersten Start oder einen kompletten Neuaufbau.",
            () => RunScriptAsync("deploy.ps1", DeployArgs()), true);
        yield return new("Aktualisieren (neuer Code)",
            "update-containers.ps1: sichert die Datenbank, baut API und Web-App neu und ersetzt die Container. Daten bleiben erhalten.",
            () => RunScriptAsync("update-containers.ps1", UpdateArgs()), true);
        yield return new("Einstellungen übernehmen",
            "Startet die Container mit der gespeicherten .env neu (ohne Build). Nur Container mit geänderter Konfiguration werden ersetzt.",
            () => RunComposeAsync(true, "up", "-d", "--remove-orphans"));
        foreach (var action in CommonComposeActions())
        {
            yield return action;
        }
    }

    private IEnumerable<DeployAction> CommonComposeActions()
    {
        yield return new("Starten", "Startet die vorhandenen Container (docker compose start).", () => RunComposeAsync(false, "start"));
        yield return new("Stoppen", "Stoppt die Container. Daten bleiben erhalten (docker compose stop).", () => RunComposeAsync(false, "stop"));
        yield return new("Logs anzeigen", "Letzte 150 Zeilen von API und Web-App.", () => RunComposeAsync(false, "logs", "--tail", "150", "--no-color", "api", "web"));
    }

    private void RebuildActions()
    {
        foreach (var button in actionButtons)
        {
            button.Dispose();
        }

        actionButtons.Clear();
        actionPanel.Controls.Clear();
        foreach (var action in GetActions())
        {
            var button = new Button
            {
                Text = action.Text,
                Width = 260,
                Height = 34,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(3, 3, 3, 5),
            };
            if (action.IsPrimary)
            {
                button.Font = BoldFont;
            }

            toolTip.SetToolTip(button, action.Hint);
            button.Click += async (_, _) => await action.Run();
            actionPanel.Controls.Add(button);
            actionButtons.Add(button);
        }

        var updateOptions = !target.IsBeta;
        testCheck.Visible = updateOptions;
        pullCheck.Visible = updateOptions;
        allCheck.Visible = updateOptions;
        skipBackupCheck.Visible = updateOptions;
    }

    private List<string> NoCacheArgs() => noCacheCheck.Checked ? ["-NoCache"] : [];

    private List<string> DeployArgs()
    {
        var arguments = NoCacheArgs();
        if (target.IsProduction)
        {
            arguments.Insert(0, "-Production");
        }

        return arguments;
    }

    private List<string> UpdateArgs()
    {
        var arguments = NoCacheArgs();
        if (allCheck.Checked) arguments.Add("-All");
        if (pullCheck.Checked) arguments.Add("-Pull");
        if (testCheck.Checked) arguments.Add("-Test");
        if (skipBackupCheck.Checked) arguments.Add("-SkipBackup");
        return arguments;
    }

    private async Task ConfirmAndRunScriptAsync(string question, string scriptName, List<string> arguments, bool requireValidSettings)
    {
        if (MessageBox.Show(this, question, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
        {
            await RunScriptAsync(scriptName, arguments, requireValidSettings);
        }
    }

    private async Task RunScriptAsync(string scriptName, List<string> arguments, bool requireValidSettings = true)
    {
        if (!PrepareRun(requireValidSettings))
        {
            return;
        }

        var scriptPath = Path.Combine(repositoryPath, "deploy", scriptName);
        await RunProcessAsync(
            ProcessRunner.PowerShellExecutable,
            ProcessRunner.PowerShellArguments(scriptPath, arguments),
            $"{scriptName} {string.Join(' ', arguments)}".Trim());
    }

    private async Task RunComposeAsync(bool requireValidSettings, params string[] arguments)
    {
        if (!PrepareRun(requireValidSettings))
        {
            return;
        }

        await RunProcessAsync("docker", [.. target.ComposePrefix, .. arguments], "docker compose " + string.Join(' ', arguments));
    }

    /// <summary>Ungespeicherte Änderungen klären und – für Build/Start – die Einstellungen prüfen.</summary>
    private bool PrepareRun(bool requireValidSettings)
    {
        if (IsBusy)
        {
            return false;
        }

        if (IsDirty)
        {
            var answer = MessageBox.Show(this, "Die Einstellungen wurden geändert. Vorher speichern?", Text,
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel || (answer == DialogResult.Yes && !SaveSettings()))
            {
                return false;
            }
        }

        if (!requireValidSettings)
        {
            return true;
        }

        if (envFile is not { Exists: true })
        {
            MessageBox.Show(this, $"{target.EnvFileName} gibt es noch nicht. Bitte zuerst die Einstellungen ausfüllen und speichern.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            tabs.SelectedTab = settingsPage;
            return false;
        }

        var errors = EnvValidator.Validate(savedValues, target, repositoryPath)
            .Where(issue => issue.Severity != IssueSeverity.Warning)
            .ToList();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, "Die Einstellungen sind noch nicht vollständig:\n\n" + string.Join("\n", errors.Select(issue => "• " + issue.Message)),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            tabs.SelectedTab = settingsPage;
            return false;
        }

        return true;
    }

    // ------------------------------------------------------------------ Ausführung und Log

    private async Task RunProcessAsync(string fileName, IReadOnlyList<string> arguments, string title)
    {
        tabs.SelectedTab = deploymentPage;
        steps.Clear();
        stepList.Items.Clear();
        logBox.Clear();
        ShowResult($"▶ {title} läuft …", WarnColor);
        AppendLog($"> {title}", Color.Gray);

        using var cancellation = new CancellationTokenSource();
        runCancellation = cancellation;
        SetBusy(true);
        var stopwatch = Stopwatch.StartNew();

        // Lesbare Build-Ausgabe statt Fortschrittsbalken.
        var environment = new Dictionary<string, string?>(target.ProcessEnvironment) { ["BUILDKIT_PROGRESS"] = "plain" };

        try
        {
            var exitCode = await ProcessRunner.RunAsync(
                fileName, arguments, repositoryPath, environment, line => PostToUi(() => OnOutputLine(line)), cancellation.Token);
            var success = exitCode == 0;
            CompleteCurrentStep(success);
            ShowResult(
                success
                    ? $"✔ {title}: erfolgreich ({stopwatch.Elapsed:mm\\:ss})"
                    : $"✖ {title}: fehlgeschlagen (Exit-Code {exitCode}). Details im Log.",
                success ? OkColor : ErrorColor);
        }
        catch (OperationCanceledException)
        {
            CompleteCurrentStep(false);
            AppendLog("Abgebrochen.", Color.Orange);
            ShowResult($"■ {title}: abgebrochen", WarnColor);
        }
        catch (Win32Exception exception)
        {
            ShowResult($"✖ {fileName} konnte nicht gestartet werden: {exception.Message}", ErrorColor);
        }
        finally
        {
            runCancellation = null;
            if (!IsDisposed)
            {
                SetBusy(false);
                UpdateSettingsState();
                _ = RefreshStatusAsync();
            }
        }
    }

    private void PostToUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            BeginInvoke(action);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ObjectDisposedException)
        {
            // Fenster wird gerade geschlossen.
        }
    }

    private void OnOutputLine(string line)
    {
        // Die Skripte melden jeden Schritt mit "==> Titel".
        if (line.StartsWith("==> ", StringComparison.Ordinal))
        {
            CompleteCurrentStep(true);
            steps.Add(line[4..].Trim());
            stepList.Items.Add("▶  " + steps[^1]);
            stepList.TopIndex = stepList.Items.Count - 1;
        }

        AppendLog(line, ColorFor(line));
    }

    private void CompleteCurrentStep(bool success)
    {
        if (steps.Count == 0)
        {
            return;
        }

        var index = steps.Count - 1;
        if (stepList.Items[index] is string text && text.StartsWith('▶'))
        {
            stepList.Items[index] = (success ? "✔  " : "✖  ") + steps[index];
        }
    }

    private static Color ColorFor(string line)
    {
        if (line.StartsWith("==>", StringComparison.Ordinal))
        {
            return Color.DeepSkyBlue;
        }

        var lower = line.ToLowerInvariant();
        if (lower.Contains("fehlgeschlagen") || lower.Contains("fehler") || lower.Contains("fehlt") || lower.Contains("error"))
        {
            return Color.IndianRed;
        }

        if (lower.Contains("hinweis") || lower.Contains("warn"))
        {
            return Color.Orange;
        }

        return line.TrimStart().StartsWith("Fertig", StringComparison.Ordinal) ? Color.LightGreen : Color.Gainsboro;
    }

    private void AppendLog(string text, Color color)
    {
        logBox.SelectionStart = logBox.TextLength;
        logBox.SelectionLength = 0;
        logBox.SelectionColor = color;
        logBox.AppendText(text + Environment.NewLine);
        logBox.ScrollToCaret();
    }

    private void ShowResult(string text, Color color)
    {
        resultLabel.Text = text;
        resultLabel.ForeColor = color;
    }

    private void SetBusy(bool busy)
    {
        foreach (var button in actionButtons)
        {
            button.Enabled = !busy;
        }

        foreach (var check in new[] { noCacheCheck, testCheck, pullCheck, allCheck, skipBackupCheck })
        {
            check.Enabled = !busy;
        }

        targetCombo.Enabled = !busy;
        changeRepositoryButton.Enabled = !busy;
        cancelButton.Enabled = busy;
        saveButton.Enabled = !busy && IsDirty;
    }

    // ------------------------------------------------------------------ Status

    private async Task RefreshStatusAsync()
    {
        if (statusRefreshRunning || repositoryPath.Length == 0 || IsDisposed)
        {
            return;
        }

        statusRefreshRunning = true;
        try
        {
            var dockerRunning = await DockerStatus.IsRunningAsync();
            if (IsDisposed)
            {
                return;
            }

            dockerLabel.Text = dockerRunning ? "● Docker läuft" : "● Docker läuft nicht";
            dockerLabel.ForeColor = dockerRunning ? OkColor : ErrorColor;
            startDockerButton.Visible = !dockerRunning && File.Exists(DockerStatus.DesktopPath);

            var states = dockerRunning
                ? await DockerStatus.GetContainersAsync(target.Containers)
                : target.Containers.Select(name => new ContainerState(name, DockerStatus.Unknown, "")).ToList();
            if (!IsDisposed)
            {
                UpdateContainerPanel(states);
            }
        }
        finally
        {
            statusRefreshRunning = false;
        }
    }

    private void UpdateContainerPanel(List<ContainerState> states)
    {
        if (containerPanel.Controls.Count != states.Count)
        {
            foreach (var control in containerPanel.Controls.Cast<Control>().ToList())
            {
                control.Dispose();
            }

            foreach (var _ in states)
            {
                containerPanel.Controls.Add(new Label { AutoSize = true, Font = BoldFont, Margin = new Padding(3, 4, 22, 4) });
            }
        }

        for (var i = 0; i < states.Count; i++)
        {
            var (text, color) = Describe(states[i]);
            var label = containerPanel.Controls[i];
            label.Text = $"● {ShortName(states[i].Name)}: {text}";
            label.ForeColor = color;
        }
    }

    private static (string Text, Color Color) Describe(ContainerState state) => (state.Status, state.Health) switch
    {
        ("running", "healthy" or "") => ("läuft", OkColor),
        ("running", "starting") => ("startet …", WarnColor),
        ("running", "unhealthy") => ("läuft, aber nicht gesund", ErrorColor),
        ("restarting", _) => ("startet ständig neu – Logs prüfen", ErrorColor),
        ("exited", _) => ("gestoppt", IdleColor),
        ("created", _) => ("angelegt, nicht gestartet", IdleColor),
        (DockerStatus.Missing, _) => ("nicht installiert", IdleColor),
        _ => (state.Status, IdleColor),
    };

    private static string ShortName(string containerName)
        => containerName.Replace("dartmetrics-beta-", "", StringComparison.Ordinal).Replace("dartmetrics-", "", StringComparison.Ordinal);

    private void RebuildLinks()
    {
        foreach (var control in linkPanel.Controls.Cast<Control>().ToList())
        {
            control.Dispose();
        }

        linkPanel.Controls.Add(new Label { Text = "Öffnen:", AutoSize = true, Margin = new Padding(3, 6, 3, 3) });
        foreach (var (title, url) in target.GetLinks(savedValues))
        {
            AddLink($"{title} ({url})", url);
        }

        AddLink("Lösungsordner", repositoryPath);
        var backups = Path.Combine(repositoryPath, "backups");
        if (Directory.Exists(backups))
        {
            AddLink("Backups", backups);
        }
    }

    private void AddLink(string text, string location)
    {
        var link = new LinkLabel { Text = text, AutoSize = true, Margin = new Padding(6, 6, 14, 3) };
        link.LinkClicked += (_, _) => Open(location);
        linkPanel.Controls.Add(link);
    }

    private void Open(string location)
    {
        try
        {
            Process.Start(new ProcessStartInfo(location) { UseShellExecute = true });
        }
        catch (Win32Exception exception)
        {
            MessageBox.Show(this, $"{location} konnte nicht geöffnet werden:\n{exception.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void StartDockerDesktop()
    {
        Open(DockerStatus.DesktopPath);
        dockerLabel.Text = "● Docker Desktop startet (ca. 1 Minute) …";
        dockerLabel.ForeColor = WarnColor;
        startDockerButton.Visible = false;
    }

    // ------------------------------------------------------------------ Beenden

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (IsBusy && MessageBox.Show(this, "Es läuft noch ein Vorgang. Abbrechen und beenden?", Text,
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        if (!IsBusy && !ConfirmSaveOrDiscard())
        {
            e.Cancel = true;
            return;
        }

        runCancellation?.Cancel();
        statusTimer.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            statusTimer.Dispose();
            toolTip.Dispose();
            sectionFont?.Dispose();
            helpFont?.Dispose();
            boldFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}
