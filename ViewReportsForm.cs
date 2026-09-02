using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Municipal_App.Models;

namespace Municipal_App
{
    /// <summary>
    /// Displays all submitted issue reports stored in the List&lt;IssueReport&gt; repository.
    /// Demonstrates data-structure retrieval, sorting, and filtering through named event handlers.
    /// </summary>
    public class ViewReportsForm : Form
    {
        // ── Controls ────────────────────────────────────────────────────────
        private Panel     _headerPanel;
        private Label     _titleLabel;
        private Label     _subtitleLabel;
        private Panel     _toolbarPanel;
        private Label     _lblFilter;
        private ComboBox  _cboFilter;
        private Button    _btnRefresh;
        private Label     _lblCount;
        private ListView  _listView;
        private Panel     _bottomPanel;
        private Button    _btnClose;
        private Label     _lblHint;

        // ── Colours (consistent with the rest of the app) ────────────────────
        private static readonly Color ThemeGreen      = Color.FromArgb(0, 128, 96);
        private static readonly Color ThemeDarkGreen  = Color.FromArgb(0, 96, 72);
        private static readonly Color ThemeLight      = Color.FromArgb(244, 246, 244);

        public ViewReportsForm()
        {
            BuildUI();
            LoadReports(string.Empty);
        }

        // ── UI construction ─────────────────────────────────────────────────

        private void BuildUI()
        {
            Text            = "My Submitted Reports";
            Size            = new Size(780, 560);
            MinimumSize     = new Size(640, 420);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = ThemeLight;
            Font            = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.Sizable;

            BuildHeader();
            BuildBottom();
            BuildToolbar();
            BuildListView();
        }

        private void BuildHeader()
        {
            _headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = ThemeGreen
            };

            _titleLabel = new Label
            {
                Text      = "📄  My Submitted Reports",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(20, 12)
            };

            _subtitleLabel = new Label
            {
                Text      = "All reports are stored in-memory using a List<IssueReport> data structure.",
                ForeColor = Color.FromArgb(210, 240, 225),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize  = true,
                Location  = new Point(22, 46)
            };

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_subtitleLabel);
            Controls.Add(_headerPanel);
        }

        private void BuildToolbar()
        {
            _toolbarPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.White,
                Padding   = new Padding(10, 8, 10, 8)
            };

            _lblFilter = new Label
            {
                Text     = "Filter by category:",
                Font     = new Font("Segoe UI", 9f),
                AutoSize = true,
                Location = new Point(12, 14)
            };

            _cboFilter = new ComboBox
            {
                Location         = new Point(138, 10),
                Size             = new Size(220, 24),
                DropDownStyle    = ComboBoxStyle.DropDownList,
                Font             = new Font("Segoe UI", 9f)
            };
            _cboFilter.Items.Add("All Categories");
            _cboFilter.Items.AddRange(new object[]
            {
                "Roads & Pavements",
                "Water & Sanitation",
                "Electricity & Street Lights",
                "Refuse & Waste Collection",
                "Parks & Recreation",
                "Public Transport",
                "Noise & Environmental",
                "Building & Safety",
                "Other"
            });
            _cboFilter.SelectedIndex = 0;
            _cboFilter.SelectedIndexChanged += CboFilter_SelectedIndexChanged;

            _btnRefresh = new Button
            {
                Text               = "↻ Refresh",
                Location           = new Point(372, 8),
                Size               = new Size(90, 28),
                FlatStyle          = FlatStyle.Flat,
                BackColor          = ThemeGreen,
                ForeColor          = Color.White,
                Font               = new Font("Segoe UI", 9f),
                Cursor             = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            _btnRefresh.FlatAppearance.BorderSize = 0;
            _btnRefresh.FlatAppearance.MouseOverBackColor = ThemeDarkGreen;
            _btnRefresh.Click += BtnRefresh_Click;

            _lblCount = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(80, 80, 80),
                Font      = new Font("Segoe UI", 8.5f),
                AutoSize  = true,
                Location  = new Point(478, 14),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left
            };

            _toolbarPanel.Controls.Add(_lblFilter);
            _toolbarPanel.Controls.Add(_cboFilter);
            _toolbarPanel.Controls.Add(_btnRefresh);
            _toolbarPanel.Controls.Add(_lblCount);
            Controls.Add(_toolbarPanel);
        }

        private void BuildListView()
        {
            _listView = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                Font          = new Font("Segoe UI", 9.5f),
                BorderStyle   = BorderStyle.None,
                BackColor     = Color.White,
                MultiSelect   = false
            };

            // Define columns
            _listView.Columns.Add("#",           45,  HorizontalAlignment.Center);
            _listView.Columns.Add("Date",        140, HorizontalAlignment.Left);
            _listView.Columns.Add("Category",    180, HorizontalAlignment.Left);
            _listView.Columns.Add("Location",    220, HorizontalAlignment.Left);
            _listView.Columns.Add("Files",        45, HorizontalAlignment.Center);
            _listView.Columns.Add("Status",       80, HorizontalAlignment.Center);

            _listView.DoubleClick        += ListView_DoubleClick;
            _listView.ColumnWidthChanged += ListView_ColumnWidthChanged;

            Controls.Add(_listView);
        }

        private void BuildBottom()
        {
            _bottomPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 52,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10)
            };

            _lblHint = new Label
            {
                Text      = "💡 Double-click a row to see the full report details.",
                ForeColor = Color.FromArgb(100, 100, 100),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize  = true,
                Location  = new Point(16, 18)
            };

            _btnClose = new Button
            {
                Text               = "✕  Close",
                Size               = new Size(110, 32),
                FlatStyle          = FlatStyle.Flat,
                BackColor          = ThemeGreen,
                ForeColor          = Color.White,
                Font               = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor             = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Anchor             = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = ThemeDarkGreen;
            _btnClose.Location = new Point(644, 10);
            _btnClose.Click   += BtnClose_Click;

            _bottomPanel.Controls.Add(_lblHint);
            _bottomPanel.Controls.Add(_btnClose);
            Controls.Add(_bottomPanel);
        }

        // ── Data loading (uses List<IssueReport> helpers) ───────────────────

        private void LoadReports(string categoryFilter)
        {
            _listView.Items.Clear();

            // Retrieve sorted list from the repository (newest first)
            List<IssueReport> reports = IssueReportRepository.GetSortedByDate();

            // Apply category filter if one is chosen
            if (!string.IsNullOrEmpty(categoryFilter) && categoryFilter != "All Categories")
                reports = IssueReportRepository.GetByCategory(categoryFilter);

            foreach (IssueReport report in reports)
            {
                var item = new ListViewItem(report.Id.ToString());
                item.SubItems.Add(report.DateReported.ToString("dd MMM yyyy  HH:mm"));
                item.SubItems.Add(report.Category);
                item.SubItems.Add(report.Location);
                item.SubItems.Add(report.AttachedFilePaths.Count.ToString());
                item.SubItems.Add(report.Status);
                item.Tag = report;

                // Alternate row shading for readability
                item.BackColor = (_listView.Items.Count % 2 == 0)
                    ? Color.White
                    : Color.FromArgb(245, 252, 248);

                _listView.Items.Add(item);
            }

            int total     = IssueReportRepository.GetCount();
            int displayed = _listView.Items.Count;

            _lblCount.Text = string.Format(
                "Showing {0} of {1} report{2}",
                displayed, total, total == 1 ? "" : "s");

            if (total == 0)
            {
                var empty = new ListViewItem(string.Empty);
                empty.SubItems.Add("No reports submitted yet. Go to Report Issues to get started.");
                empty.ForeColor = Color.Gray;
                _listView.Items.Add(empty);
            }
        }

        // ── Named event handlers ────────────────────────────────────────────

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            string filter = _cboFilter.SelectedIndex > 0
                ? _cboFilter.SelectedItem.ToString()
                : string.Empty;
            LoadReports(filter);
        }

        private void CboFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            string filter = _cboFilter.SelectedIndex > 0
                ? _cboFilter.SelectedItem.ToString()
                : string.Empty;
            LoadReports(filter);
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var report = _listView.SelectedItems[0].Tag as IssueReport;
            if (report == null) return;

            string attachments = report.AttachedFilePaths.Count > 0
                ? string.Join("\n   • ", report.AttachedFilePaths)
                : "(none)";

            MessageBox.Show(
                string.Format(
                    "📋  Issue Report Detail\n"              +
                    "─────────────────────────────────\n"   +
                    "Reference #:   {0}\n"                  +
                    "Date:          {1}\n"                  +
                    "Status:        {2}\n\n"                +
                    "Category:      {3}\n"                  +
                    "Location:      {4}\n\n"                +
                    "Description:\n{5}\n\n"                 +
                    "Attachments:\n   • {6}",
                    report.Id,
                    report.DateReported.ToString("dd MMM yyyy HH:mm"),
                    report.Status,
                    report.Category,
                    report.Location,
                    report.Description,
                    attachments),
                "Report #" + report.Id,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void ListView_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            // Prevent the ID column from being resized below 40 px
            if (e.ColumnIndex == 0 && _listView.Columns[0].Width < 40)
                _listView.Columns[0].Width = 40;
        }
    }
}
