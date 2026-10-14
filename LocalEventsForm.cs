using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Municipal_App.Models;

namespace Municipal_App
{
    /// <summary>
    /// Displays local municipal events and announcements, with search/filter by
    /// category and date, plus a sidebar showing recommended events (driven by
    /// search-pattern analysis), recent searches, and the next events up in the
    /// FIFO upcoming queue.
    /// </summary>
    public class LocalEventsForm : Form
    {
        // ── Controls ────────────────────────────────────────────────────────
        private Panel    _headerPanel;
        private Label    _titleLabel;
        private Label    _subtitleLabel;
        private Panel    _toolbarPanel;
        private TextBox  _txtKeyword;
        private ComboBox _cboCategory;
        private DateTimePicker _dtpDate;
        private CheckBox _chkUseDate;
        private Button   _btnSearch;
        private Button   _btnClear;
        private ListView _listView;
        private Panel    _sidebarPanel;
        private Label    _lblRecommended;
        private ListBox  _lstRecommended;
        private Label    _lblRecent;
        private ListBox  _lstRecent;
        private Label    _lblUpcoming;
        private ListBox  _lstUpcoming;
        private Panel    _bottomPanel;
        private Label    _lblHint;
        private Label    _lblCount;
        private Button   _btnClose;

        // ── Colours (consistent with the rest of the app) ────────────────────
        private static readonly Color ThemeGreen     = Color.FromArgb(0, 128, 96);
        private static readonly Color ThemeDarkGreen = Color.FromArgb(0, 96, 72);
        private static readonly Color ThemeLight     = Color.FromArgb(244, 246, 244);

        public LocalEventsForm()
        {
            LocalEventRepository.EnsureSeeded();
            BuildUI();
            LoadEvents(string.Empty, string.Empty, null);
            RefreshSidebar();
        }

        // ── UI construction ─────────────────────────────────────────────────

        private void BuildUI()
        {
            Text            = "Local Events & Announcements";
            Size            = new Size(940, 620);
            MinimumSize     = new Size(820, 480);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = ThemeLight;
            Font            = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.Sizable;

            BuildHeader();
            BuildBottom();
            BuildToolbar();
            BuildSidebar();
            BuildListView();

            // All panels here are positioned manually (Dock=None) rather than relying on
            // Dock=Top/Bottom/Right/Fill — the same mixed-dock control tree previously
            // caused unreliable bounds computation in ReportIssueForm. Explicit bounds via
            // SyncMainLayout avoid that entirely.
            Resize += (s, e) => SyncMainLayout();
            Shown += (s, e) => SyncMainLayout();
            SyncMainLayout();

            _headerPanel.BringToFront();
            _toolbarPanel.BringToFront();
            _bottomPanel.BringToFront();
        }

        private void SyncMainLayout()
        {
            if (_headerPanel == null || _toolbarPanel == null || _bottomPanel == null ||
                _sidebarPanel == null || _listView == null) return;

            _headerPanel.Location = new Point(0, 0);
            _headerPanel.Size = new Size(ClientSize.Width, _headerPanel.Height);

            _toolbarPanel.Location = new Point(0, _headerPanel.Bottom);
            _toolbarPanel.Size = new Size(ClientSize.Width, _toolbarPanel.Height);

            _bottomPanel.Location = new Point(0, ClientSize.Height - _bottomPanel.Height);
            _bottomPanel.Size = new Size(ClientSize.Width, _bottomPanel.Height);

            int top = _toolbarPanel.Bottom;
            int bottom = _bottomPanel.Top;
            int height = Math.Max(bottom - top, 0);

            _sidebarPanel.Location = new Point(ClientSize.Width - _sidebarPanel.Width, top);
            _sidebarPanel.Size = new Size(_sidebarPanel.Width, height);

            _listView.Location = new Point(0, top);
            _listView.Size = new Size(Math.Max(ClientSize.Width - _sidebarPanel.Width, 0), height);

            _btnClose.Location = new Point(_bottomPanel.ClientSize.Width - 130, 10);
        }

        private void BuildHeader()
        {
            _headerPanel = new Panel
            {
                Dock      = DockStyle.None,
                Height    = 70,
                BackColor = ThemeGreen
            };

            _titleLabel = new Label
            {
                Text      = "📅  Local Events & Announcements",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(20, 12)
            };

            _subtitleLabel = new Label
            {
                Text      = "Search events by category or date. Recommendations adapt to what you search for.",
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
                Dock      = DockStyle.None,
                Height    = 50,
                BackColor = Color.White,
                Padding   = new Padding(10, 8, 10, 8)
            };

            _txtKeyword = new TextBox
            {
                Location = new Point(12, 13),
                Size     = new Size(200, 24),
                Font     = new Font("Segoe UI", 9f)
            };
            _txtKeyword.KeyDown += TxtKeyword_KeyDown;

            _cboCategory = new ComboBox
            {
                Location      = new Point(222, 13),
                Size          = new Size(170, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 9f)
            };
            _cboCategory.Items.Add("All Categories");
            foreach (string category in LocalEventRepository.GetCategories())
                _cboCategory.Items.Add(category);
            _cboCategory.SelectedIndex = 0;

            _chkUseDate = new CheckBox
            {
                Text     = "Date:",
                Location = new Point(402, 15),
                Size     = new Size(50, 22),
                Font     = new Font("Segoe UI", 9f)
            };
            _chkUseDate.CheckedChanged += ChkUseDate_CheckedChanged;

            _dtpDate = new DateTimePicker
            {
                Location = new Point(452, 12),
                Size     = new Size(120, 24),
                Format   = DateTimePickerFormat.Short,
                Font     = new Font("Segoe UI", 9f),
                Enabled  = false
            };

            _btnSearch = new Button
            {
                Text                    = "🔍 Search",
                Location                = new Point(582, 10),
                Size                    = new Size(90, 28),
                FlatStyle               = FlatStyle.Flat,
                BackColor               = ThemeGreen,
                ForeColor               = Color.White,
                Font                    = new Font("Segoe UI", 9f),
                Cursor                  = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            _btnSearch.FlatAppearance.BorderSize = 0;
            _btnSearch.FlatAppearance.MouseOverBackColor = ThemeDarkGreen;
            _btnSearch.Click += BtnSearch_Click;

            _btnClear = new Button
            {
                Text                    = "✕ Clear",
                Location                = new Point(680, 10),
                Size                    = new Size(80, 28),
                FlatStyle               = FlatStyle.Flat,
                BackColor               = Color.FromArgb(180, 180, 180),
                ForeColor               = Color.White,
                Font                    = new Font("Segoe UI", 9f),
                Cursor                  = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.Click += BtnClear_Click;

            _lblCount = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(80, 80, 80),
                Font      = new Font("Segoe UI", 8.5f),
                AutoSize  = true,
                Location  = new Point(770, 16),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left
            };

            _toolbarPanel.Controls.Add(_txtKeyword);
            _toolbarPanel.Controls.Add(_cboCategory);
            _toolbarPanel.Controls.Add(_chkUseDate);
            _toolbarPanel.Controls.Add(_dtpDate);
            _toolbarPanel.Controls.Add(_btnSearch);
            _toolbarPanel.Controls.Add(_btnClear);
            _toolbarPanel.Controls.Add(_lblCount);
            Controls.Add(_toolbarPanel);
        }

        private void BuildSidebar()
        {
            // Dock=None: bounds are computed manually by SyncMainLayout (see BuildUI).
            _sidebarPanel = new Panel
            {
                Dock      = DockStyle.None,
                Width     = 260,
                BackColor = Color.White,
                Padding   = new Padding(12, 10, 12, 10)
            };

            _lblRecommended = new Label
            {
                Text     = "⭐ Recommended For You",
                Font     = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ThemeDarkGreen,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            _lstRecommended = new ListBox
            {
                Location    = new Point(12, 34),
                Size        = new Size(230, 130),
                Font        = new Font("Segoe UI", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _lstRecommended.DoubleClick += LstRecommended_DoubleClick;

            _lblRecent = new Label
            {
                Text     = "🕒 Recent Searches",
                Font     = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ThemeDarkGreen,
                AutoSize = true,
                Location = new Point(12, 178)
            };
            _lstRecent = new ListBox
            {
                Location    = new Point(12, 202),
                Size        = new Size(230, 90),
                Font        = new Font("Segoe UI", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblUpcoming = new Label
            {
                Text     = "⏭ Next Up",
                Font     = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ThemeDarkGreen,
                AutoSize = true,
                Location = new Point(12, 306)
            };
            _lstUpcoming = new ListBox
            {
                Location    = new Point(12, 330),
                Size        = new Size(230, 150),
                Font        = new Font("Segoe UI", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor      = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };

            _sidebarPanel.Controls.Add(_lblRecommended);
            _sidebarPanel.Controls.Add(_lstRecommended);
            _sidebarPanel.Controls.Add(_lblRecent);
            _sidebarPanel.Controls.Add(_lstRecent);
            _sidebarPanel.Controls.Add(_lblUpcoming);
            _sidebarPanel.Controls.Add(_lstUpcoming);
            Controls.Add(_sidebarPanel);
        }

        private void BuildListView()
        {
            // Dock=None: bounds are computed manually by SyncMainLayout (see BuildUI).
            _listView = new ListView
            {
                Dock          = DockStyle.None,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                Font          = new Font("Segoe UI", 9.5f),
                BorderStyle   = BorderStyle.None,
                BackColor     = Color.White,
                MultiSelect   = false
            };

            _listView.Columns.Add("Date",     130, HorizontalAlignment.Left);
            _listView.Columns.Add("Title",    240, HorizontalAlignment.Left);
            _listView.Columns.Add("Category", 130, HorizontalAlignment.Left);
            _listView.Columns.Add("Location", 160, HorizontalAlignment.Left);
            _listView.Columns.Add("★",         40, HorizontalAlignment.Center);

            _listView.DoubleClick += ListView_DoubleClick;

            Controls.Add(_listView);
        }

        private void BuildBottom()
        {
            _bottomPanel = new Panel
            {
                Dock      = DockStyle.None,
                Height    = 52,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10)
            };

            _lblHint = new Label
            {
                Text      = "💡 Double-click an event (or a recommendation) to see full details.",
                ForeColor = Color.FromArgb(100, 100, 100),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize  = true,
                Location  = new Point(16, 18)
            };

            _btnClose = new Button
            {
                Text                    = "✕  Close",
                Size                    = new Size(110, 32),
                FlatStyle               = FlatStyle.Flat,
                BackColor               = ThemeGreen,
                ForeColor               = Color.White,
                Font                    = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor                  = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Anchor                  = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = ThemeDarkGreen;
            _btnClose.Click += BtnClose_Click;

            _bottomPanel.Controls.Add(_lblHint);
            _bottomPanel.Controls.Add(_btnClose);
            Controls.Add(_bottomPanel);
        }

        // ── Data loading ─────────────────────────────────────────────────────

        private void LoadEvents(string keyword, string category, DateTime? date)
        {
            _listView.Items.Clear();

            List<EventItem> events = LocalEventRepository.Search(keyword, category, date);

            foreach (EventItem evt in events)
            {
                var item = new ListViewItem(evt.Date.ToString("dd MMM yyyy HH:mm"));
                item.SubItems.Add(evt.Title);
                item.SubItems.Add(evt.Category);
                item.SubItems.Add(evt.Location);
                item.SubItems.Add(evt.IsFeatured ? "★" : "");
                item.Tag = evt;

                item.BackColor = (_listView.Items.Count % 2 == 0)
                    ? Color.White
                    : Color.FromArgb(245, 252, 248);

                _listView.Items.Add(item);
            }

            _lblCount.Text = string.Format("{0} event{1} found", events.Count, events.Count == 1 ? "" : "s");

            if (events.Count == 0)
            {
                var empty = new ListViewItem(string.Empty);
                empty.SubItems.Add("No events match your search.");
                empty.ForeColor = Color.Gray;
                _listView.Items.Add(empty);
            }

            // A fresh search may change frequency-based recommendations, so refresh the sidebar.
            RefreshSidebar();
        }

        private void RefreshSidebar()
        {
            _lstRecommended.Items.Clear();
            foreach (EventItem evt in LocalEventRepository.GetRecommendations(5))
                _lstRecommended.Items.Add(FormatSidebarEntry(evt));
            if (_lstRecommended.Items.Count == 0)
                _lstRecommended.Items.Add("(no recommendations yet)");

            _lstRecent.Items.Clear();
            foreach (string category in LocalEventRepository.GetRecentSearches(6))
                _lstRecent.Items.Add(category);
            if (_lstRecent.Items.Count == 0)
                _lstRecent.Items.Add("(search to build history)");

            _lstUpcoming.Items.Clear();
            foreach (EventItem evt in LocalEventRepository.GetUpcomingQueue(6))
                _lstUpcoming.Items.Add(FormatSidebarEntry(evt));
            if (_lstUpcoming.Items.Count == 0)
                _lstUpcoming.Items.Add("(no upcoming events)");
        }

        private static string FormatSidebarEntry(EventItem evt)
        {
            return string.Format("{0}{1} — {2:dd MMM}", evt.IsFeatured ? "★ " : "", evt.Title, evt.Date);
        }

        // ── Named event handlers ────────────────────────────────────────────

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            RunSearch();
        }

        private void TxtKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                RunSearch();
            }
        }

        private void RunSearch()
        {
            string category = _cboCategory.SelectedIndex > 0 ? _cboCategory.SelectedItem.ToString() : string.Empty;
            DateTime? date   = _chkUseDate.Checked ? (DateTime?)_dtpDate.Value.Date : null;
            LoadEvents(_txtKeyword.Text, category, date);
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _txtKeyword.Text = string.Empty;
            _cboCategory.SelectedIndex = 0;
            _chkUseDate.Checked = false;
            LoadEvents(string.Empty, string.Empty, null);
        }

        private void ChkUseDate_CheckedChanged(object sender, EventArgs e)
        {
            _dtpDate.Enabled = _chkUseDate.Checked;
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;

            var evt = _listView.SelectedItems[0].Tag as EventItem;
            if (evt == null) return;

            ShowEventDetail(evt);
        }

        private void LstRecommended_DoubleClick(object sender, EventArgs e)
        {
            int index = _lstRecommended.SelectedIndex;
            if (index < 0) return;

            List<EventItem> recs = LocalEventRepository.GetRecommendations(5);
            if (index < recs.Count)
                ShowEventDetail(recs[index]);
        }

        private static void ShowEventDetail(EventItem evt)
        {
            MessageBox.Show(
                string.Format(
                    "📅  {0}\n"                            +
                    "─────────────────────────────────\n" +
                    "Date:        {1}\n"                   +
                    "Category:    {2}\n"                    +
                    "Location:    {3}\n\n"                  +
                    "{4}",
                    evt.Title,
                    evt.Date.ToString("dd MMM yyyy HH:mm"),
                    evt.Category,
                    evt.Location,
                    evt.Description),
                evt.IsFeatured ? "★ Featured Event" : "Event Detail",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
