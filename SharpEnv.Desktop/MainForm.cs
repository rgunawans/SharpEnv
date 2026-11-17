using System.ComponentModel;
using System.Text;
using SharpEnv.Core.Formatters;
using SharpEnv.Core.Models;
using SharpEnv.Core.Services;

namespace SharpEnv.Desktop;

public sealed partial class MainForm : Form
{
    private static readonly string[] SensitiveKeywords =
    [
        "Password",
        "Secret",
        "Key",
        "Token",
        "ConnectionString"
    ];

    private readonly ProjectScannerService _scannerService;
    private readonly IReadOnlyList<IEnvFormatter> _formatters;
    private readonly List<ConfigEntry> _allEntries = [];
    private readonly List<ConfigEntry> _filteredEntries = [];
    private readonly BindingList<GridRowItem> _gridRows = [];
    private readonly Dictionary<string, bool> _expandedProjects = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(IEnvFormatter Formatter, TextBox OutputBox)> _outputTabs = [];

    private readonly IReadOnlyList<string>? _initialPaths;
    private DataGridViewTextBoxColumn _valueColumn = null!;
    private Font _groupHeaderFont = null!;
    private string? _selectedProjectName;
    private bool _suppressSelectionChanged;
    private CancellationTokenSource? _scanCts;

    public MainForm(
        ProjectScannerService scannerService,
        IReadOnlyList<IEnvFormatter> formatters,
        IReadOnlyList<string>? initialPaths = null)
    {
        _scannerService = scannerService;
        _formatters = formatters;
        _initialPaths = initialPaths;

        InitializeComponent();
        _groupHeaderFont = new Font(dgvEntries.Font, FontStyle.Bold);
        ConfigureDataGridView();
        ConfigureOutputTabs();
        WireEvents();
    }

    private void ConfigureDataGridView()
    {
        var includeCol = new DataGridViewCheckBoxColumn
        {
            Name = "Include",
            HeaderText = "Include",
            DataPropertyName = nameof(GridRowItem.IsSelected),
            Width = 58,
            Resizable = DataGridViewTriState.False
        };

        var keyCol = new DataGridViewTextBoxColumn
        {
            Name = "Key",
            HeaderText = "Key",
            DataPropertyName = nameof(GridRowItem.Key),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 34
        };

        _valueColumn = new DataGridViewTextBoxColumn
        {
            Name = "Value",
            HeaderText = "Value",
            DataPropertyName = nameof(GridRowItem.Value),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 34
        };

        var typeCol = new DataGridViewTextBoxColumn
        {
            Name = "Type",
            HeaderText = "Type",
            DataPropertyName = nameof(GridRowItem.SourceTypeText),
            ReadOnly = true,
            Width = 72
        };

        var sourceFileCol = new DataGridViewTextBoxColumn
        {
            Name = "SourceFile",
            HeaderText = "Source File",
            DataPropertyName = nameof(GridRowItem.RelativeFilePath),
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 32
        };

        dgvEntries.Columns.AddRange(includeCol, keyCol, _valueColumn, typeCol, sourceFileCol);
        dgvEntries.DataSource = _gridRows;
    }

    private void ConfigureOutputTabs()
    {
        foreach (var formatter in _formatters)
        {
            var tabPage = new TabPage(formatter.DisplayName)
            {
                Padding = new Padding(6)
            };

            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                WrapContents = false
            };

            var btnCopy = new Button
            {
                Text = "Copy to Clipboard",
                AutoSize = true,
                Height = 28,
                Margin = new Padding(0, 2, 8, 2)
            };

            var btnExport = new Button
            {
                Text = "Export to File",
                AutoSize = true,
                Height = 28,
                Margin = new Padding(0, 2, 0, 2)
            };

            var outputBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 10f, FontStyle.Regular, GraphicsUnit.Point),
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BackColor = SystemColors.Window
            };

            btnCopy.Click += async (_, _) => await CopyOutputAsync(btnCopy, formatter.Format(GetSelectedProjectEntries()));
            btnExport.Click += async (_, _) => await ExportOutputAsync(formatter, formatter.Format(GetSelectedProjectEntries()));

            actionBar.Controls.Add(btnCopy);
            actionBar.Controls.Add(btnExport);

            tabPage.Controls.Add(outputBox);
            tabPage.Controls.Add(actionBar);
            tabOutputs.TabPages.Add(tabPage);

            _outputTabs.Add((formatter, outputBox));
        }
    }

    private void WireEvents()
    {
        DragEnter += MainForm_DragEnter;
        DragDrop += MainForm_DragDrop;

        btnLoadFolder.Click += async (_, _) => await LoadFolderAsync();
        btnLoadFile.Click += async (_, _) => await LoadFileAsync();

        chkJson.CheckedChanged += (_, _) => ApplyFilters();
        chkIni.CheckedChanged += (_, _) => ApplyFilters();
        chkCSharp.CheckedChanged += (_, _) => ApplyFilters();
        txtSearch.TextChanged += (_, _) => ApplyFilters();

        btnSelectAll.Click += (_, _) => SetAllSelection(true);
        btnUnselectAll.Click += (_, _) => SetAllSelection(false);
        chkMaskSensitive.CheckedChanged += (_, _) =>
        {
            dgvEntries.InvalidateColumn(_valueColumn.Index);
            RefreshOutputs();
        };

        dgvEntries.CurrentCellDirtyStateChanged += DgvEntries_CurrentCellDirtyStateChanged;
        dgvEntries.CellValueChanged += (_, _) => RefreshOutputs();
        dgvEntries.CellBeginEdit += DgvEntries_CellBeginEdit;
        dgvEntries.CellClick += DgvEntries_CellClick;
        dgvEntries.SelectionChanged += DgvEntries_SelectionChanged;
        dgvEntries.CellFormatting += DgvEntries_CellFormatting;
        dgvEntries.CellPainting += DgvEntries_CellPainting;

        Shown += async (_, _) =>
        {
            if (_initialPaths is { Count: > 0 })
            {
                await ScanPathsAsync(_initialPaths);
            }
        };

        FormClosed += (_, _) => _groupHeaderFont.Dispose();
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
        {
            return;
        }

        await ScanPathsAsync(paths);
    }

    private async Task LoadFolderAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select a .NET project or solution folder",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await ScanPathsAsync([dialog.SelectedPath]);
        }
    }

    private async Task LoadFileAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Configuration or C# File",
            Filter = "Supported Files (*.json;*.ini;*.cs)|*.json;*.ini;*.cs|JSON Files (*.json)|*.json|INI Files (*.ini)|*.ini|C# Files (*.cs)|*.cs|All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.FileNames.Length > 0)
        {
            await ScanPathsAsync(dialog.FileNames);
        }
    }

    private async Task ScanPathsAsync(IReadOnlyList<string> paths)
    {
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        SetScanningState(true, paths.Count == 1 ? paths[0] : $"{paths.Count} items selected");

        try
        {
            var options = new ScanOptions
            {
                IncludeJson = chkJson.Checked,
                IncludeIni = chkIni.Checked,
                IncludeCSharp = chkCSharp.Checked
            };

            var scanned = new List<ConfigEntry>();
            foreach (var path in paths)
            {
                var entries = await _scannerService.ScanAsync(path, options, ct);
                scanned.AddRange(entries);
            }

            _allEntries.Clear();
            _allEntries.AddRange(scanned);

            _expandedProjects.Clear();
            foreach (var projectName in _allEntries.Select(e => e.ProjectName).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                _expandedProjects[projectName] = true;
            }

            _selectedProjectName = _allEntries.FirstOrDefault()?.ProjectName;
            ApplyFilters();
        }
        catch (OperationCanceledException)
        {
            lblStatus.Text = "Scan canceled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to scan target:\n{ex.Message}", "Scan Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            lblStatus.Text = "Scan failed.";
        }
        finally
        {
            SetScanningState(false, null);
        }
    }

    private void SetScanningState(bool isScanning, string? pathDisplay)
    {
        progressBar.Visible = isScanning;
        btnLoadFolder.Enabled = !isScanning;
        btnLoadFile.Enabled = !isScanning;

        if (pathDisplay is not null)
        {
            lblCurrentPath.Text = pathDisplay;
            lblCurrentPath.ForeColor = SystemColors.ControlText;
        }

        if (isScanning)
        {
            lblStatus.Text = "Scanning project files...";
        }
    }

    private void ApplyFilters()
    {
        var searchTerm = txtSearch.Text.Trim();
        _filteredEntries.Clear();

        foreach (var entry in _allEntries)
        {
            if (!IsSourceChecked(entry.SourceType))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(searchTerm) &&
                !entry.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _filteredEntries.Add(entry);
        }

        RebuildGridRows(keepSelectionForProject: _selectedProjectName);
        RefreshOutputs();
    }

    private void RebuildGridRows(string? keepSelectionForProject = null)
    {
        _suppressSelectionChanged = true;
        try
        {
            _gridRows.RaiseListChangedEvents = false;
            _gridRows.Clear();

            var groups = _filteredEntries
                .GroupBy(e => e.ProjectName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (groups.Count > 0 &&
                (_selectedProjectName is null ||
                 !groups.Any(g => string.Equals(g.Key, _selectedProjectName, StringComparison.OrdinalIgnoreCase))))
            {
                _selectedProjectName = groups[0].Key;
            }
            else if (groups.Count == 0)
            {
                _selectedProjectName = null;
            }

            var rowIndexToSelect = -1;
            foreach (var group in groups)
            {
                var isExpanded = !_expandedProjects.TryGetValue(group.Key, out var expanded) || expanded;
                var items = group.ToList();

                if (keepSelectionForProject is not null &&
                    string.Equals(group.Key, keepSelectionForProject, StringComparison.OrdinalIgnoreCase) &&
                    rowIndexToSelect == -1)
                {
                    rowIndexToSelect = _gridRows.Count;
                }

                _gridRows.Add(GridRowItem.CreateGroupHeader(group.Key, isExpanded, items.Count));

                if (!isExpanded)
                {
                    continue;
                }

                foreach (var entry in items)
                {
                    _gridRows.Add(GridRowItem.CreateEntryRow(entry));
                }
            }

            _gridRows.RaiseListChangedEvents = true;
            _gridRows.ResetBindings();

            if (rowIndexToSelect >= 0 && rowIndexToSelect < dgvEntries.Rows.Count)
            {
                dgvEntries.ClearSelection();
                dgvEntries.Rows[rowIndexToSelect].Selected = true;
                dgvEntries.CurrentCell = dgvEntries.Rows[rowIndexToSelect].Cells[1];
            }
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
    }

    private bool IsSourceChecked(ConfigSourceType sourceType) =>
        sourceType switch
        {
            ConfigSourceType.Json => chkJson.Checked,
            ConfigSourceType.Ini => chkIni.Checked,
            ConfigSourceType.CSharp => chkCSharp.Checked,
            _ => false
        };

    private void SetAllSelection(bool isSelected)
    {
        foreach (var entry in _filteredEntries)
        {
            entry.IsSelected = isSelected;
        }

        dgvEntries.Refresh();
        RefreshOutputs();
    }

    private void DgvEntries_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dgvEntries.IsCurrentCellDirty && dgvEntries.CurrentCell is DataGridViewCheckBoxCell)
        {
            dgvEntries.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void DgvEntries_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        if (e.RowIndex >= 0 && e.RowIndex < _gridRows.Count && _gridRows[e.RowIndex].IsGroupHeader)
        {
            e.Cancel = true;
        }
    }

    private void DgvEntries_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _gridRows.Count)
        {
            return;
        }

        var row = _gridRows[e.RowIndex];
        _selectedProjectName = row.ProjectName;

        if (row.IsGroupHeader)
        {
            var currentExpanded = !_expandedProjects.TryGetValue(row.ProjectName, out var expanded) || expanded;
            _expandedProjects[row.ProjectName] = !currentExpanded;
            RebuildGridRows(keepSelectionForProject: row.ProjectName);
        }

        RefreshOutputs();
    }

    private void DgvEntries_SelectionChanged(object? sender, EventArgs e)
    {
        if (_suppressSelectionChanged || dgvEntries.CurrentRow is null)
        {
            return;
        }

        var rowIndex = dgvEntries.CurrentRow.Index;
        if (rowIndex < 0 || rowIndex >= _gridRows.Count)
        {
            return;
        }

        var projectName = _gridRows[rowIndex].ProjectName;
        if (!string.Equals(_selectedProjectName, projectName, StringComparison.OrdinalIgnoreCase))
        {
            _selectedProjectName = projectName;
            RefreshOutputs();
        }
    }

    private void DgvEntries_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _gridRows.Count || e.ColumnIndex != 0)
        {
            return;
        }

        if (_gridRows[e.RowIndex].IsGroupHeader)
        {
            e.PaintBackground(e.ClipBounds, true);
            e.Handled = true;
        }
    }

    private void DgvEntries_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _gridRows.Count)
        {
            return;
        }

        var row = _gridRows[e.RowIndex];
        if (row.IsGroupHeader)
        {
            if (e.CellStyle is not null)
            {
                e.CellStyle.BackColor = Color.FromArgb(236, 241, 248);
                e.CellStyle.SelectionBackColor = Color.FromArgb(212, 225, 242);
                e.CellStyle.SelectionForeColor = Color.Black;
                e.CellStyle.Font = _groupHeaderFont;
            }
            return;
        }

        if (!chkMaskSensitive.Checked || e.ColumnIndex != _valueColumn.Index || row.Entry is null)
        {
            return;
        }

        if (IsSensitiveKey(row.Entry.Key) && !string.IsNullOrEmpty(row.Entry.Value))
        {
            e.Value = "••••••••";
            e.FormattingApplied = true;
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        foreach (var keyword in SensitiveKeywords)
        {
            if (key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private List<ConfigEntry> GetSelectedProjectEntries() =>
        _selectedProjectName is null
            ? []
            : _filteredEntries
                .Where(e => string.Equals(e.ProjectName, _selectedProjectName, StringComparison.OrdinalIgnoreCase))
                .ToList();

    private void RefreshOutputs()
    {
        var projectEntries = GetSelectedProjectEntries();

        lblSelectedProject.Text = _selectedProjectName is null
            ? "Selected Project: (None)"
            : $"Selected Project: {_selectedProjectName} ({projectEntries.Count(e => e.IsSelected)}/{projectEntries.Count} selected)";

        var displayEntries = chkMaskSensitive.Checked
            ? projectEntries.Select(e =>
                IsSensitiveKey(e.Key) && !string.IsNullOrEmpty(e.Value)
                    ? new ConfigEntry
                    {
                        Id = e.Id,
                        Key = e.Key,
                        Value = "••••••••",
                        SourceType = e.SourceType,
                        ProjectName = e.ProjectName,
                        RelativeFilePath = e.RelativeFilePath,
                        IsSelected = e.IsSelected
                    }
                    : e).ToList()
            : projectEntries;

        foreach (var (formatter, outputBox) in _outputTabs)
        {
            outputBox.Text = formatter.Format(displayEntries);
        }

        var totalSelected = _filteredEntries.Count(e => e.IsSelected);
        lblStatus.Text = $"Showing {_filteredEntries.Count} of {_allEntries.Count} entries ({totalSelected} selected).";
    }

    private static async Task CopyOutputAsync(Button button, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        Clipboard.SetText(content);

        const string originalText = "Copy to Clipboard";
        button.Text = "Copied!";
        await Task.Delay(1500);
        if (!button.IsDisposed)
        {
            button.Text = originalText;
        }
    }

    private async Task ExportOutputAsync(IEnvFormatter formatter, string content)
    {
        using var dialog = new SaveFileDialog
        {
            Title = $"Export {formatter.DisplayName}",
            FileName = formatter.DefaultFileName,
            Filter = formatter.FileFilter
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await File.WriteAllTextAsync(dialog.FileName, content, new UTF8Encoding(false));
            lblStatus.Text = $"Exported to {dialog.FileName}";
        }
    }

    private sealed class GridRowItem
    {
        private string _groupHeaderText = string.Empty;

        public bool IsGroupHeader { get; private init; }
        public string ProjectName { get; private init; } = string.Empty;
        public ConfigEntry? Entry { get; private init; }

        public bool IsSelected
        {
            get => Entry?.IsSelected ?? false;
            set
            {
                if (Entry is not null)
                {
                    Entry.IsSelected = value;
                }
            }
        }

        public string Key
        {
            get => IsGroupHeader ? _groupHeaderText : Entry?.Key ?? string.Empty;
            set
            {
                if (Entry is not null)
                {
                    Entry.Key = value;
                }
            }
        }

        public string Value
        {
            get => IsGroupHeader ? string.Empty : Entry?.Value ?? string.Empty;
            set
            {
                if (Entry is not null)
                {
                    Entry.Value = value;
                }
            }
        }

        public string SourceTypeText =>
            IsGroupHeader ? "Project" : Entry?.SourceType.ToString() ?? string.Empty;

        public string RelativeFilePath =>
            IsGroupHeader ? string.Empty : Entry?.RelativeFilePath ?? string.Empty;

        public static GridRowItem CreateGroupHeader(string projectName, bool isExpanded, int count)
        {
            var arrow = isExpanded ? "▼" : "▶";
            return new GridRowItem
            {
                IsGroupHeader = true,
                ProjectName = projectName,
                _groupHeaderText = $"{arrow}  {projectName} ({count})"
            };
        }

        public static GridRowItem CreateEntryRow(ConfigEntry entry) =>
            new()
            {
                IsGroupHeader = false,
                ProjectName = entry.ProjectName,
                Entry = entry
            };
    }
}
