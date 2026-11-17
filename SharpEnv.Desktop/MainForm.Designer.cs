#nullable enable

namespace SharpEnv.Desktop;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    private Panel pnlTop = null!;
    private FlowLayoutPanel flpLoadBar = null!;
    private Button btnLoadFolder = null!;
    private Button btnLoadFile = null!;
    private Label lblCurrentPath = null!;
    private ProgressBar progressBar = null!;

    private FlowLayoutPanel flpFilterBar = null!;
    private CheckBox chkJson = null!;
    private CheckBox chkIni = null!;
    private CheckBox chkCSharp = null!;
    private Label lblSearch = null!;
    private TextBox txtSearch = null!;
    private Button btnSelectAll = null!;
    private Button btnUnselectAll = null!;
    private CheckBox chkMaskSensitive = null!;

    private SplitContainer splitMain = null!;
    private DataGridView dgvEntries = null!;
    private Label lblSelectedProject = null!;
    private TabControl tabOutputs = null!;

    private StatusStrip statusStrip = null!;
    private ToolStripStatusLabel lblStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components is not null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        pnlTop = new Panel();
        flpLoadBar = new FlowLayoutPanel();
        btnLoadFolder = new Button();
        btnLoadFile = new Button();
        lblCurrentPath = new Label();
        progressBar = new ProgressBar();

        flpFilterBar = new FlowLayoutPanel();
        chkJson = new CheckBox();
        chkIni = new CheckBox();
        chkCSharp = new CheckBox();
        lblSearch = new Label();
        txtSearch = new TextBox();
        btnSelectAll = new Button();
        btnUnselectAll = new Button();
        chkMaskSensitive = new CheckBox();

        splitMain = new SplitContainer();
        dgvEntries = new DataGridView();
        lblSelectedProject = new Label();
        tabOutputs = new TabControl();

        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel();

        pnlTop.SuspendLayout();
        flpLoadBar.SuspendLayout();
        flpFilterBar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
        splitMain.Panel1.SuspendLayout();
        splitMain.Panel2.SuspendLayout();
        splitMain.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvEntries).BeginInit();
        statusStrip.SuspendLayout();
        SuspendLayout();

        // pnlTop
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Height = 82;
        pnlTop.Padding = new Padding(8, 6, 8, 4);
        pnlTop.Controls.Add(flpFilterBar);
        pnlTop.Controls.Add(flpLoadBar);

        // flpLoadBar
        flpLoadBar.Dock = DockStyle.Top;
        flpLoadBar.Height = 36;
        flpLoadBar.WrapContents = false;
        flpLoadBar.AutoSize = false;
        flpLoadBar.Controls.Add(btnLoadFolder);
        flpLoadBar.Controls.Add(btnLoadFile);
        flpLoadBar.Controls.Add(progressBar);
        flpLoadBar.Controls.Add(lblCurrentPath);

        // btnLoadFolder
        btnLoadFolder.Text = "Load Project Folder";
        btnLoadFolder.AutoSize = true;
        btnLoadFolder.Height = 28;
        btnLoadFolder.Margin = new Padding(0, 2, 6, 2);

        // btnLoadFile
        btnLoadFile.Text = "Load File";
        btnLoadFile.AutoSize = true;
        btnLoadFile.Height = 28;
        btnLoadFile.Margin = new Padding(0, 2, 10, 2);

        // progressBar
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 25;
        progressBar.Size = new Size(140, 20);
        progressBar.Margin = new Padding(0, 6, 10, 2);
        progressBar.Visible = false;

        // lblCurrentPath
        lblCurrentPath.Text = "Drop a project folder or config file (.json, .ini, .cs) here...";
        lblCurrentPath.AutoSize = true;
        lblCurrentPath.Margin = new Padding(0, 7, 0, 2);
        lblCurrentPath.ForeColor = SystemColors.GrayText;

        // flpFilterBar
        flpFilterBar.Dock = DockStyle.Top;
        flpFilterBar.Height = 36;
        flpFilterBar.WrapContents = false;
        flpFilterBar.AutoSize = false;
        flpFilterBar.Controls.Add(chkJson);
        flpFilterBar.Controls.Add(chkIni);
        flpFilterBar.Controls.Add(chkCSharp);
        flpFilterBar.Controls.Add(lblSearch);
        flpFilterBar.Controls.Add(txtSearch);
        flpFilterBar.Controls.Add(btnSelectAll);
        flpFilterBar.Controls.Add(btnUnselectAll);
        flpFilterBar.Controls.Add(chkMaskSensitive);

        // chkJson
        chkJson.Text = "JSON";
        chkJson.Checked = true;
        chkJson.AutoSize = true;
        chkJson.Margin = new Padding(0, 6, 8, 2);

        // chkIni
        chkIni.Text = "INI";
        chkIni.Checked = true;
        chkIni.AutoSize = true;
        chkIni.Margin = new Padding(0, 6, 8, 2);

        // chkCSharp
        chkCSharp.Text = "C#";
        chkCSharp.Checked = true;
        chkCSharp.AutoSize = true;
        chkCSharp.Margin = new Padding(0, 6, 14, 2);

        // lblSearch
        lblSearch.Text = "Search / Filter Key:";
        lblSearch.AutoSize = true;
        lblSearch.Margin = new Padding(0, 7, 6, 2);

        // txtSearch
        txtSearch.Width = 220;
        txtSearch.PlaceholderText = "Filter by key...";
        txtSearch.Margin = new Padding(0, 4, 12, 2);

        // btnSelectAll
        btnSelectAll.Text = "Select All";
        btnSelectAll.AutoSize = true;
        btnSelectAll.Height = 26;
        btnSelectAll.Margin = new Padding(0, 2, 6, 2);

        // btnUnselectAll
        btnUnselectAll.Text = "Unselect All";
        btnUnselectAll.AutoSize = true;
        btnUnselectAll.Height = 26;
        btnUnselectAll.Margin = new Padding(0, 2, 14, 2);

        // chkMaskSensitive
        chkMaskSensitive.Text = "Mask Sensitive Values";
        chkMaskSensitive.Checked = false;
        chkMaskSensitive.AutoSize = true;
        chkMaskSensitive.Margin = new Padding(0, 6, 0, 2);

        // splitMain
        splitMain.Dock = DockStyle.Fill;
        splitMain.Size = new Size(1240, 656);
        splitMain.Panel1MinSize = 320;
        splitMain.Panel2MinSize = 320;
        splitMain.SplitterDistance = 620;
        splitMain.Panel1.Padding = new Padding(8, 0, 4, 8);
        splitMain.Panel2.Padding = new Padding(4, 0, 8, 8);
        splitMain.Panel1.Controls.Add(dgvEntries);
        splitMain.Panel2.Controls.Add(tabOutputs);
        splitMain.Panel2.Controls.Add(lblSelectedProject);

        // lblSelectedProject
        lblSelectedProject.Dock = DockStyle.Top;
        lblSelectedProject.AutoSize = true;
        lblSelectedProject.Padding = new Padding(0, 4, 0, 6);
        lblSelectedProject.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblSelectedProject.Text = "Selected Project: (None)";

        // dgvEntries
        dgvEntries.Dock = DockStyle.Fill;
        dgvEntries.AllowUserToAddRows = false;
        dgvEntries.AllowUserToDeleteRows = false;
        dgvEntries.AutoGenerateColumns = false;
        dgvEntries.RowHeadersVisible = false;
        dgvEntries.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvEntries.BackgroundColor = SystemColors.Window;

        // tabOutputs
        tabOutputs.Dock = DockStyle.Fill;

        // statusStrip
        statusStrip.Items.Add(lblStatus);
        lblStatus.Text = "Ready.";

        // MainForm
        AllowDrop = true;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1240, 760);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "SharpEnv - .NET Configuration to Environment Variable Converter";
        Controls.Add(splitMain);
        Controls.Add(pnlTop);
        Controls.Add(statusStrip);

        pnlTop.ResumeLayout(false);
        flpLoadBar.ResumeLayout(false);
        flpLoadBar.PerformLayout();
        flpFilterBar.ResumeLayout(false);
        flpFilterBar.PerformLayout();
        splitMain.Panel1.ResumeLayout(false);
        splitMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
        splitMain.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvEntries).EndInit();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
