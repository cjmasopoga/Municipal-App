using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Municipal_App.Models;

namespace Municipal_App
{
    /// <summary>
    /// Displays submitted service requests and lets citizens track progress by unique
    /// reference number. Showcases several tree/graph structures over the same data:
    /// a BinarySearchTree (track by reference), an AVL tree (priority order), a
    /// Red-Black tree (chronological order), a binary StatusWorkflowTree (valid status
    /// transitions), a heap-based "most urgent" ranking, and a relationship Graph with
    /// BFS traversal ("related requests") and a Prim's-algorithm Minimum Spanning Tree
    /// (the "service network" view).
    /// </summary>
    public class ServiceRequestStatusForm : Form
    {
        // ── Controls ────────────────────────────────────────────────────────
        private Panel    _headerPanel;
        private Label    _titleLabel;
        private Label    _subtitleLabel;
        private Panel    _toolbarPanel;
        private TextBox  _txtReference;
        private Button   _btnTrack;
        private ComboBox _cboSort;
        private Button   _btnNetwork;
        private Panel    _treePanel;
        private TreeView _categoryTree;
        private ListView _listView;
        private Panel    _sidebarPanel;
        private Label    _lblUrgent;
        private ListBox  _lstUrgent;
        private Label    _lblRelated;
        private ListBox  _lstRelated;
        private Label    _lblWorkflow;
        private ListBox  _lstWorkflow;
        private Panel    _bottomPanel;
        private Label    _lblHint;
        private Label    _lblCount;
        private Button   _btnClose;

        // ── Colours (orange/amber accent distinguishes this feature from the others) ──
        private static readonly Color ThemeAccent     = Color.FromArgb(214, 118, 21);
        private static readonly Color ThemeAccentDark = Color.FromArgb(168, 92, 14);
        private static readonly Color ThemeLight      = Color.FromArgb(244, 246, 244);

        public ServiceRequestStatusForm()
        {
            ServiceRequestRepository.EnsureSeeded();
            BuildUI();
            PopulateCategoryTree();
            LoadRequests(SortMode.Default);
            RefreshUrgentList();
            RefreshSelectionDependentPanels(null);
        }

        private enum SortMode { Default, Priority, Date }

        // ── UI construction ─────────────────────────────────────────────────

        private void BuildUI()
        {
            Text            = "Service Request Status";
            Size            = new Size(1040, 660);
            MinimumSize     = new Size(900, 520);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = ThemeLight;
            Font            = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.Sizable;

            BuildHeader();
            BuildBottom();
            BuildToolbar();
            BuildCategoryTreePanel();
            BuildSidebar();
            BuildListView();

            // All panels are positioned manually (Dock=None) rather than relying on
            // Dock=Top/Bottom/Left/Right/Fill — a mixed-dock control tree like this one
            // previously computed Fill bounds unreliably in other forms in this app.
            // Explicit bounds via SyncMainLayout avoid that entirely.
            Resize += (s, e) => SyncMainLayout();
            Shown  += (s, e) => SyncMainLayout();
            SyncMainLayout();

            _headerPanel.BringToFront();
            _toolbarPanel.BringToFront();
            _bottomPanel.BringToFront();
        }

        private void SyncMainLayout()
        {
            if (_headerPanel == null || _toolbarPanel == null || _bottomPanel == null ||
                _treePanel == null || _sidebarPanel == null || _listView == null) return;

            _headerPanel.Location = new Point(0, 0);
            _headerPanel.Size = new Size(ClientSize.Width, _headerPanel.Height);

            _toolbarPanel.Location = new Point(0, _headerPanel.Bottom);
            _toolbarPanel.Size = new Size(ClientSize.Width, _toolbarPanel.Height);

            _bottomPanel.Location = new Point(0, ClientSize.Height - _bottomPanel.Height);
            _bottomPanel.Size = new Size(ClientSize.Width, _bottomPanel.Height);

            int top = _toolbarPanel.Bottom;
            int bottom = _bottomPanel.Top;
            int height = Math.Max(bottom - top, 0);

            _treePanel.Location = new Point(0, top);
            _treePanel.Size = new Size(_treePanel.Width, height);

            _sidebarPanel.Location = new Point(ClientSize.Width - _sidebarPanel.Width, top);
            _sidebarPanel.Size = new Size(_sidebarPanel.Width, height);

            int listLeft = _treePanel.Right;
            int listWidth = Math.Max(_sidebarPanel.Left - listLeft, 0);
            _listView.Location = new Point(listLeft, top);
            _listView.Size = new Size(listWidth, height);

            _btnClose.Location = new Point(_bottomPanel.ClientSize.Width - 130, 10);
        }

        private void BuildHeader()
        {
            _headerPanel = new Panel { Dock = DockStyle.None, Height = 70, BackColor = ThemeAccent };

            _titleLabel = new Label
            {
                Text      = "🔧  Service Request Status",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                AutoSize  = true,
                Location  = new Point(20, 12)
            };

            _subtitleLabel = new Label
            {
                Text      = "Track requests by reference number. Backed by BST, AVL, Red-Black tree, heap, and graph structures.",
                ForeColor = Color.FromArgb(255, 235, 210),
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
            _toolbarPanel = new Panel { Dock = DockStyle.None, Height = 50, BackColor = Color.White, Padding = new Padding(10, 8, 10, 8) };

            var lblRef = new Label { Text = "Track by reference #:", Font = new Font("Segoe UI", 9f), AutoSize = true, Location = new Point(12, 16) };

            _txtReference = new TextBox { Location = new Point(158, 13), Size = new Size(120, 24), Font = new Font("Segoe UI", 9f) };
            _txtReference.KeyDown += TxtReference_KeyDown;

            _btnTrack = MakeFlatButton("🔍 Track", new Point(284, 10), new Size(80, 28), ThemeAccent, ThemeAccentDark);
            _btnTrack.Click += BtnTrack_Click;

            var lblSort = new Label { Text = "Sort:", Font = new Font("Segoe UI", 9f), AutoSize = true, Location = new Point(388, 16) };
            _cboSort = new ComboBox { Location = new Point(424, 13), Size = new Size(210, 24), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9f) };
            _cboSort.Items.Add("Default order");
            _cboSort.Items.Add("By Priority (AVL Tree)");
            _cboSort.Items.Add("By Date (Red-Black Tree)");
            _cboSort.SelectedIndex = 0;
            _cboSort.SelectedIndexChanged += CboSort_SelectedIndexChanged;

            _btnNetwork = MakeFlatButton("🕸 Service Network", new Point(646, 10), new Size(150, 28), Color.FromArgb(90, 90, 90), Color.FromArgb(60, 60, 60));
            _btnNetwork.Click += BtnNetwork_Click;

            _lblCount = new Label { Text = "", ForeColor = Color.FromArgb(80, 80, 80), Font = new Font("Segoe UI", 8.5f), AutoSize = true, Location = new Point(810, 18) };

            _toolbarPanel.Controls.Add(lblRef);
            _toolbarPanel.Controls.Add(_txtReference);
            _toolbarPanel.Controls.Add(_btnTrack);
            _toolbarPanel.Controls.Add(lblSort);
            _toolbarPanel.Controls.Add(_cboSort);
            _toolbarPanel.Controls.Add(_btnNetwork);
            _toolbarPanel.Controls.Add(_lblCount);
            Controls.Add(_toolbarPanel);
        }

        private Button MakeFlatButton(string text, Point location, Size size, Color backColor, Color hoverColor)
        {
            var btn = new Button
            {
                Text = text, Location = location, Size = size,
                FlatStyle = FlatStyle.Flat, BackColor = backColor, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f), Cursor = Cursors.Hand, UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = hoverColor;
            return btn;
        }

        private void BuildCategoryTreePanel()
        {
            _treePanel = new Panel { Dock = DockStyle.None, Width = 220, BackColor = Color.White, Padding = new Padding(8) };

            var lbl = new Label
            {
                Text = "📂 Categories", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = ThemeAccentDark, AutoSize = true, Location = new Point(10, 8)
            };

            _categoryTree = new TreeView
            {
                Location = new Point(8, 34),
                Size = new Size(200, 540),
                Font = new Font("Segoe UI", 8.75f),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            _categoryTree.AfterSelect += CategoryTree_AfterSelect;

            _treePanel.Controls.Add(lbl);
            _treePanel.Controls.Add(_categoryTree);
            Controls.Add(_treePanel);
        }

        private void BuildSidebar()
        {
            _sidebarPanel = new Panel { Dock = DockStyle.None, Width = 280, BackColor = Color.White, Padding = new Padding(12, 10, 12, 10) };

            _lblUrgent = new Label { Text = "🔥 Most Urgent (Heap)", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = ThemeAccentDark, AutoSize = true, Location = new Point(12, 8) };
            _lstUrgent = new ListBox { Location = new Point(12, 32), Size = new Size(250, 120), Font = new Font("Segoe UI", 8.5f), BorderStyle = BorderStyle.FixedSingle };
            _lstUrgent.DoubleClick += LstUrgent_DoubleClick;

            _lblRelated = new Label { Text = "🔗 Related Requests (Graph BFS)", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = ThemeAccentDark, AutoSize = true, Location = new Point(12, 162) };
            _lstRelated = new ListBox { Location = new Point(12, 186), Size = new Size(250, 120), Font = new Font("Segoe UI", 8.5f), BorderStyle = BorderStyle.FixedSingle };
            _lstRelated.DoubleClick += LstRelated_DoubleClick;

            _lblWorkflow = new Label { Text = "🔀 Status Workflow (Binary Tree)", Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = ThemeAccentDark, AutoSize = true, Location = new Point(12, 316) };
            _lstWorkflow = new ListBox
            {
                Location = new Point(12, 340), Size = new Size(250, 150), Font = new Font("Segoe UI", 8.5f),
                BorderStyle = BorderStyle.FixedSingle, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };

            _sidebarPanel.Controls.Add(_lblUrgent);
            _sidebarPanel.Controls.Add(_lstUrgent);
            _sidebarPanel.Controls.Add(_lblRelated);
            _sidebarPanel.Controls.Add(_lstRelated);
            _sidebarPanel.Controls.Add(_lblWorkflow);
            _sidebarPanel.Controls.Add(_lstWorkflow);
            Controls.Add(_sidebarPanel);
        }

        private void BuildListView()
        {
            _listView = new ListView
            {
                Dock = DockStyle.None, View = View.Details, FullRowSelect = true, GridLines = true,
                Font = new Font("Segoe UI", 9.5f), BorderStyle = BorderStyle.None, BackColor = Color.White, MultiSelect = false
            };

            _listView.Columns.Add("Ref #",     75,  HorizontalAlignment.Left);
            _listView.Columns.Add("Title",     220, HorizontalAlignment.Left);
            _listView.Columns.Add("Category",  140, HorizontalAlignment.Left);
            _listView.Columns.Add("Priority",  75,  HorizontalAlignment.Center);
            _listView.Columns.Add("Status",    95,  HorizontalAlignment.Center);
            _listView.Columns.Add("Submitted", 90,  HorizontalAlignment.Left);
            _listView.Columns.Add("Team",      120, HorizontalAlignment.Left);

            _listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
            _listView.DoubleClick += ListView_DoubleClick;

            Controls.Add(_listView);
        }

        private void BuildBottom()
        {
            _bottomPanel = new Panel { Dock = DockStyle.None, Height = 52, BackColor = Color.White, Padding = new Padding(16, 10, 16, 10) };

            _lblHint = new Label
            {
                Text = "💡 Select a request to see related requests & its status workflow. Double-click for full details.",
                ForeColor = Color.FromArgb(100, 100, 100), Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize = true, Location = new Point(16, 18)
            };

            _btnClose = new Button
            {
                Text = "✕  Close", Size = new Size(110, 32), FlatStyle = FlatStyle.Flat,
                BackColor = ThemeAccent, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = ThemeAccentDark;
            _btnClose.Click += (s, e) => Close();

            _bottomPanel.Controls.Add(_lblHint);
            _bottomPanel.Controls.Add(_btnClose);
            Controls.Add(_bottomPanel);
        }

        // ── Data loading ─────────────────────────────────────────────────────

        private void PopulateCategoryTree()
        {
            // A TreeView is itself backed by a tree data structure — used here as the
            // "basic tree" organising requests hierarchically by category.
            _categoryTree.Nodes.Clear();
            var root = new TreeNode("All Requests") { Tag = null };

            foreach (string category in ServiceRequestRepository.GetCategories())
            {
                var catNode = new TreeNode(category) { Tag = null };
                foreach (ServiceRequest request in ServiceRequestRepository.GetAll().Where(r => string.Equals(r.Category, category, StringComparison.OrdinalIgnoreCase)))
                    catNode.Nodes.Add(new TreeNode($"{request.ReferenceNumber} — {request.Title}") { Tag = request });
                root.Nodes.Add(catNode);
            }

            _categoryTree.Nodes.Add(root);
            root.Expand();
        }

        private void LoadRequests(SortMode mode)
        {
            _listView.Items.Clear();

            List<ServiceRequest> requests;
            switch (mode)
            {
                case SortMode.Priority: requests = ServiceRequestRepository.GetByPriorityDescending(); break;
                case SortMode.Date:     requests = ServiceRequestRepository.GetChronological(); break;
                default:                requests = ServiceRequestRepository.GetAll(); break;
            }

            foreach (ServiceRequest request in requests)
            {
                var item = new ListViewItem(request.ReferenceNumber);
                item.SubItems.Add(request.Title);
                item.SubItems.Add(request.Category);
                item.SubItems.Add(request.Priority.ToString());
                item.SubItems.Add(request.Status.ToString());
                item.SubItems.Add(request.DateSubmitted.ToString("dd MMM yyyy"));
                item.SubItems.Add(request.AssignedTeam);
                item.Tag = request;

                item.BackColor = (_listView.Items.Count % 2 == 0) ? Color.White : Color.FromArgb(255, 247, 237);
                if (request.Priority == RequestPriority.Critical) item.ForeColor = Color.FromArgb(180, 40, 20);

                _listView.Items.Add(item);
            }

            _lblCount.Text = string.Format("{0} of {1} requests", _listView.Items.Count, ServiceRequestRepository.GetCount());
        }

        private void RefreshUrgentList()
        {
            _lstUrgent.Items.Clear();
            foreach (ServiceRequest request in ServiceRequestRepository.GetMostUrgent(6))
                _lstUrgent.Items.Add($"{request.ReferenceNumber} [{request.Priority}] — {request.Title} ({request.DaysOpen}d open)");
            if (_lstUrgent.Items.Count == 0) _lstUrgent.Items.Add("(no open requests)");
        }

        private void RefreshSelectionDependentPanels(ServiceRequest selected)
        {
            _lstRelated.Items.Clear();
            _lstWorkflow.Items.Clear();

            if (selected == null)
            {
                _lstRelated.Items.Add("(select a request)");
                _lstWorkflow.Items.Add("(select a request)");
                return;
            }

            List<ServiceRequest> related = ServiceRequestRepository.GetRelated(selected.Id);
            if (related.Count == 0) _lstRelated.Items.Add("(no closely related requests)");
            foreach (ServiceRequest r in related)
                _lstRelated.Items.Add($"{r.ReferenceNumber} — {r.Title}");

            _lstWorkflow.Items.Add($"Current: {selected.Status}");
            List<ServiceRequestStatus> nextSteps = ServiceRequestRepository.GetNextSteps(selected.Status);
            if (nextSteps.Count == 0)
            {
                _lstWorkflow.Items.Add("(final status — no further steps)");
            }
            else
            {
                _lstWorkflow.Items.Add("Possible next step(s):");
                foreach (ServiceRequestStatus step in nextSteps)
                    _lstWorkflow.Items.Add("  → " + step);
            }
        }

        // ── Named event handlers ────────────────────────────────────────────

        private void BtnTrack_Click(object sender, EventArgs e) => TrackReference();

        private void TxtReference_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; TrackReference(); }
        }

        private void TrackReference()
        {
            ServiceRequest found = ServiceRequestRepository.TrackByReference(_txtReference.Text);
            if (found == null)
            {
                MessageBox.Show("No service request found with that reference number.", "Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (ListViewItem item in _listView.Items)
            {
                if (!(item.Tag is ServiceRequest r) || r.Id != found.Id) continue;
                item.Selected = true;
                item.EnsureVisible();
                _listView.Select();
                break;
            }

            ShowRequestDetail(found);
        }

        private void CboSort_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadRequests((SortMode)_cboSort.SelectedIndex);
        }

        private void CategoryTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!(e.Node.Tag is ServiceRequest request)) return;

            foreach (ListViewItem item in _listView.Items)
            {
                if (!(item.Tag is ServiceRequest r) || r.Id != request.Id) continue;
                item.Selected = true;
                item.EnsureVisible();
                break;
            }
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            ServiceRequest selected = _listView.SelectedItems.Count > 0 ? _listView.SelectedItems[0].Tag as ServiceRequest : null;
            RefreshSelectionDependentPanels(selected);
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            if (_listView.SelectedItems.Count == 0) return;
            if (_listView.SelectedItems[0].Tag is ServiceRequest request) ShowRequestDetail(request);
        }

        private void LstUrgent_DoubleClick(object sender, EventArgs e)
        {
            int index = _lstUrgent.SelectedIndex;
            List<ServiceRequest> urgent = ServiceRequestRepository.GetMostUrgent(6);
            if (index >= 0 && index < urgent.Count) ShowRequestDetail(urgent[index]);
        }

        private void LstRelated_DoubleClick(object sender, EventArgs e)
        {
            ServiceRequest selected = _listView.SelectedItems.Count > 0 ? _listView.SelectedItems[0].Tag as ServiceRequest : null;
            if (selected == null) return;

            int index = _lstRelated.SelectedIndex;
            List<ServiceRequest> related = ServiceRequestRepository.GetRelated(selected.Id);
            if (index >= 0 && index < related.Count) ShowRequestDetail(related[index]);
        }

        private void BtnNetwork_Click(object sender, EventArgs e)
        {
            using (var dlg = new ServiceNetworkForm())
                dlg.ShowDialog(this);
        }

        private static void ShowRequestDetail(ServiceRequest r)
        {
            MessageBox.Show(
                string.Format(
                    "🔧  {0}\n" +
                    "─────────────────────────────────\n" +
                    "Reference:   {1}\n" +
                    "Status:      {2}\n" +
                    "Priority:    {3}\n\n" +
                    "Category:    {4}\n" +
                    "Location:    {5}\n" +
                    "Assigned to: {6}\n\n" +
                    "Submitted:   {7} ({8} days ago)\n" +
                    "Last update: {9}\n\n" +
                    "{10}",
                    r.Title, r.ReferenceNumber, r.Status, r.Priority, r.Category, r.Location, r.AssignedTeam,
                    r.DateSubmitted.ToString("dd MMM yyyy"), r.DaysOpen, r.LastUpdated.ToString("dd MMM yyyy"), r.Description),
                r.ReferenceNumber, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
