using System.Diagnostics;
using Microsoft.Win32;
using Ora.Core;

namespace Ora;

public sealed class MainForm : Form
{
    private readonly Store store = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Ora"));
    private readonly NotifyIcon tray;
    private readonly TextBox name = new() { Dock = DockStyle.Fill, PlaceholderText = "Például: Munka: Tervezés" };
    private readonly Label status = new() { AutoSize = true, ForeColor = Color.FromArgb(72, 90, 112) };
    private readonly Label taskName = new() { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 8, 0, 8), ForeColor = Color.FromArgb(20, 48, 70) };
    private readonly Label elapsed = new() { AutoSize = true, ForeColor = Color.FromArgb(28, 112, 115) };
    private readonly FlowLayoutPanel frequent = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly MediaButton stop = new(MediaAction.Stop);
    private readonly MediaButton playPause = new(MediaAction.Play);
    private readonly ToolTip tips = new();
    private readonly ToolStripMenuItem trayStop = new("Leállítás"), trayPause = new("Szünet"), trayResume = new("Folytatás");
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 1000 };
    private Entry? active;
    private string? pausedName;
    private bool exiting, busy;
    private DateTime lastCheckpoint;

    public MainForm()
    {
        Text = "Óra · Időmérés";
        Size = new Size(680, 520);
        MinimumSize = new Size(480, 380);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 11);
        using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("Ora.AppIcon.ico")!)
        using (var icon = new Icon(stream)) Icon = (Icon)icon.Clone();
        BackColor = Color.FromArgb(246, 248, 252);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(24), ColumnCount = 1, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var current = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(16), BackColor = Color.White, Margin = new Padding(0, 0, 0, 16) };
        current.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        taskName.Font = new Font(Font.FontFamily, 19, FontStyle.Bold);
        elapsed.Font = new Font(Font.FontFamily, 14);
        current.Controls.Add(status); current.Controls.Add(taskName); current.Controls.Add(elapsed);
        layout.Controls.Add(current);
        layout.Controls.Add(new Label { Text = "Mivel foglalkozol most?", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        var input = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };
        input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        playPause.AccessibleName = "Indítás";
        stop.AccessibleName = "Leállítás";
        tips.SetToolTip(playPause, "Indítás (Enter)");
        tips.SetToolTip(stop, "Leállítás");
        input.Controls.Add(name);
        layout.Controls.Add(input);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 10, 0, 12) };
        var report = new Button { Text = "Kimutatás", AutoSize = true, Font = new Font(Font.FontFamily, 9), FlatStyle = FlatStyle.Flat, ForeColor = SystemColors.GrayText };
        var folder = new Button { Text = "Adatmappa", AutoSize = true, Font = report.Font, FlatStyle = FlatStyle.Flat, ForeColor = SystemColors.GrayText };
        report.FlatAppearance.BorderSize = folder.FlatAppearance.BorderSize = 0;
        actions.Controls.AddRange([playPause, stop]);
        layout.Controls.Add(actions);
        layout.Controls.Add(new Label { Text = "A 10 leggyakoribb tevékenység", AutoSize = true, Padding = new Padding(0, 8, 0, 8) });
        layout.Controls.Add(frequent);
        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 8, 0, 0) };
        footer.Controls.AddRange([report, folder]);
        layout.Controls.Add(footer);
        Controls.Add(layout);
        AcceptButton = playPause;
        var menu = new ContextMenuStrip();
        menu.Items.Add("Ablak / új tevékenység", null, (_, _) => Reveal());
        menu.Items.Add(trayStop); menu.Items.Add(trayPause); menu.Items.Add(trayResume);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Kilépés", null, (_, _) => Exit());
        tray = new NotifyIcon { Icon = Icon, Text = "Óra · nincs aktív mérés", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => Reveal();
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) menu.Show(Cursor.Position); };
        playPause.Click += (_, _) => { if (active is not null) StopTask(true); else StartTask(name.Text); };
        stop.Click += (_, _) => StopTask(false);
        trayStop.Click += (_, _) => StopTask(false);
        trayPause.Click += (_, _) => StopTask(true);
        trayResume.Click += (_, _) => { if (pausedName is not null) StartTask(pausedName); };
        report.Click += (_, _) => Open(store.ReportPath);
        folder.Click += (_, _) => Open(store.DirectoryPath);
        Shown += (_, _) => InitializeData();
        FormClosing += OnClosing;
        SystemEvents.SessionEnding += SessionEnding;
        timer.Tick += (_, _) => Tick();
        timer.Start();
    }

    private static DateTime Now => Entry.Seconds(DateTime.Now);

    private void InitializeData() => RunIO(() =>
    {
        var rows = store.Read();
        if (rows.Any(e => e.End is null))
        {
            var checkpoint = store.ReadCheckpoint();
            var answer = MessageBox.Show(this,
                "Befejezetlen mérést találtam egy korábbi futásból.\n\nIgen: lezárás az utolsó mentett életjelnél (ennek hiányában a kezdésnél).\nNem: lezárás a jelenlegi idővel.\n\nA CSV-ben később is módosíthatod az időpontokat.",
                "Korábbi mérés helyreállítása", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            rows = rows.SelectMany(e => e.End is not null ? new[] { e } : CloseRecovered(e, answer == DialogResult.No ? Now : checkpoint?.Start == e.Start && checkpoint.Name == e.Name ? checkpoint.LastSeen : e.Start)).ToList();
        }
        store.Write(rows); RefreshTags(rows); UpdateState();
    });

    private static IEnumerable<Entry> CloseRecovered(Entry entry, DateTime end) =>
        end <= entry.Start ? [entry with { End = entry.Start }] : Entry.Split(entry.Start, end, entry.Name);

    private void StartTask(string value)
    {
        string normalized;
        try { normalized = Entry.NormalizeName(value); }
        catch (FormatException ex) { MessageBox.Show(this, ex.Message, "Megnevezés"); return; }
        RunIO(() =>
        {
            var at = Now;
            var rows = store.Read();
            Finish(rows, at);
            var next = new Entry(at, null, normalized);
            rows.Add(next);
            // Retry only the same prepared snapshot if either output is temporarily unavailable.
            Persist(rows, next, at);
            active = next; pausedName = null; name.Text = normalized;
            RefreshTags(rows); UpdateState(); Hide();
        });
    }

    private void StopTask(bool pausing)
    {
        if (busy) return;
        if (active is null)
        {
            if (pausing) return;
            pausedName = null;
            name.Clear();
            UpdateState();
            return;
        }
        RunIO(() =>
        {
            var at = Now;
            var rows = store.Read();
            string previous = active!.Name;
            Finish(rows, at);
            Persist(rows, null, at);
            active = null; pausedName = pausing ? previous : null;
            if (!pausing) name.Clear();
            RefreshTags(rows); UpdateState(); Reveal();
        });
    }

    private void Finish(List<Entry> rows, DateTime at)
    {
        if (rows.Any(e => e.End is null && e.Start != active?.Start))
            throw new FormatException("A CSV-ben az alkalmazás aktív mérésén kívüli nyitott sor található. Töltsd ki ennek a végidejét, majd próbáld újra.");
        if (active is null) return;
        // Identify the open row by start time, allowing its description to be edited manually.
        var matches = rows.Where(e => e.Start == active.Start && e.End is null).ToList();
        if (matches.Count > 1) throw new FormatException("Több nyitott sor kezdődik az aktív időpontban. Javítsd a CSV-t.");
        if (matches.Count == 0) return; // An explicit manual deletion or completion takes precedence.
        var row = matches[0];
        rows.Remove(row);
        rows.AddRange(at == row.Start ? [row with { End = at }] : Entry.Split(row.Start, at, row.Name));
    }

    private void Persist(List<Entry> rows, Entry? next, DateTime at)
    {
        // Keep this transaction in memory while retrying to avoid duplicate starts after a partial save.
        Retry(() => store.Write(rows));
        Retry(() => { if (next is null) File.Delete(store.CheckpointPath); else store.Checkpoint(next, at); });
        lastCheckpoint = at;
    }

    private void RefreshTags(List<Entry> rows)
    {
        frequent.Controls.Clear();
        foreach (var tag in rows.GroupBy(e => e.Name).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Max(e => e.Start)).Take(10))
        {
            var button = new Button { Text = tag.Key, AutoSize = true, MaximumSize = new Size(540, 0), Padding = new Padding(8), Margin = new Padding(4), BackColor = Color.White };
            button.Click += (_, _) => StartTask(tag.Key);
            frequent.Controls.Add(button);
        }
    }

    private void UpdateState()
    {
        bool running = active is not null;
        trayStop.Enabled = true;
        trayPause.Enabled = running;
        playPause.Action = running ? MediaAction.Pause : MediaAction.Play;
        playPause.AccessibleName = running ? "Szünet" : pausedName is not null ? "Folytatás" : "Indítás";
        tips.SetToolTip(playPause, playPause.AccessibleName + " (Enter)");
        trayResume.Enabled = !running && pausedName is not null;
        Tick();
    }

    private void Tick()
    {
        string text = active is null ? pausedName is null ? "Válassz tevékenységet az indításhoz." : $"Szünet: {pausedName}" : $"{active.Name} · {Now - active.Start:dd\\.hh\\:mm\\:ss}";
        status.Text = active is not null ? "FUTÓ TEVÉKENYSÉG" : pausedName is not null ? "SZÜNETEL" : "NINCS AKTÍV MÉRÉS";
        taskName.Text = active?.Name ?? pausedName ?? "Válassz tevékenységet az indításhoz.";
        elapsed.Text = active is not null ? $"{Now - active.Start:dd\\.hh\\:mm\\:ss}" : pausedName is not null ? "A folytatáshoz nyomd meg a ▶ gombot." : "";
        tray.Text = ("Óra · " + text)[..Math.Min(63, text.Length + 6)];
        if (!busy && active is not null && Now - lastCheckpoint >= TimeSpan.FromSeconds(15))
            RunIO(() => { store.Checkpoint(active, Now); lastCheckpoint = Now; });
    }

    private void RunIO(Action action)
    {
        if (busy) return;
        busy = true;
        try { Retry(action); }
        finally { busy = false; }
    }

    private void Retry(Action action)
    {
        while (true)
        {
            try { action(); return; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
            {
                MessageBox.Show(this, $"Nem sikerült elérni vagy feldolgozni az adatokat.\n\n{ex.Message}\n\nAdatmappa: {store.DirectoryPath}\n\nZárd be a fájlt zároló programot, vagy javítsd a hibás sort, majd kattints az OK-ra az újrapróbáláshoz.", "Óra · Beavatkozás szükséges", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void Reveal() { Show(); WindowState = FormWindowState.Normal; Activate(); name.Focus(); }
    private void Open(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException) { MessageBox.Show(this, ex.Message, "Megnyitás"); }
    }
    private void Exit()
    {
        if (busy) return;
        StopTask(false);
        exiting = true; Close();
    }
    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.WindowsShutDown || exiting)
        {
            StopTask(false);
            timer.Stop(); tray.Visible = false; tray.Dispose();
            SystemEvents.SessionEnding -= SessionEnding;
        }
        else { e.Cancel = true; Hide(); }
    }
    private void SessionEnding(object? sender, SessionEndingEventArgs e)
    {
        if (IsHandleCreated && !IsDisposed) Invoke(() => StopTask(false));
    }
}
