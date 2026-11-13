using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Municipal_App.Models;

namespace Municipal_App
{
    public class MainMenuForm : Form
    {
        private Panel _headerPanel;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _dateLabel;
        private FlowLayoutPanel _contentPanel;
        private TableLayoutPanel _cardsPanel;
        private Button _btnReportIssues;
        private Button _btnLocalEvents;
        private Button _btnServiceStatus;
        private Button _btnViewReports;
        private Panel _footerPanel;
        private Label _pointsLabel;
        private Label _levelLabel;
        private PictureBox _badgeIcon;
        private Button _btnExit;

        private static readonly Color ThemeGreen    = Color.FromArgb(0, 128, 96);
        private static readonly Color ThemeDarkGreen = Color.FromArgb(0, 96, 72);
        private static readonly Color ThemeGray     = Color.FromArgb(180, 180, 180);
        private static readonly Color ThemeLight    = Color.FromArgb(245, 247, 245);

        public MainMenuForm()
        {
            InitializeUI();
            RefreshEngagement();
        }

        private void InitializeUI()
        {
            Text = "Municipal Services — Citizen Portal";
            Size = new Size(960, 600);
            MinimumSize = new Size(960, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = ThemeLight;
            Font = new Font("Segoe UI", 9f);

            BuildHeader();
            BuildFooter();   // footer must be added before Fill content
            BuildContent();
            Resize += MainMenuForm_Resize;

            // Ensure the header stays visible above the content panel
            if (_headerPanel != null)
            {
                // Move header to front of z-order so nothing in the content covers it
                Controls.SetChildIndex(_headerPanel, 0);
                _headerPanel.BringToFront();
            }

            // Re-sync once the form's auto-scaling has fully settled, in case the
            // initial ClientSize read during construction was still stale.
            Shown += (s, e) =>
            {
                SyncDateLabelPosition();
                SyncContentPanelBounds();
            };
        }

        private void BuildHeader()
        {
            // Header: white background with logo/title on left and date on right
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 12)
            };

            var logo = new Panel { Size = new Size(72, 72), BackColor = Color.White, Location = new Point(20, 20) };
            // App logo, loaded from Resources\logo.png (copied to the output directory on build)
            var logoPic = new PictureBox
            {
                Size = new Size(72, 72),
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            string logoPath = Path.Combine(Application.StartupPath, "Resources", "logo.png");
            if (File.Exists(logoPath))
            {
                logoPic.Image = Image.FromFile(logoPath);
            }
            logo.Controls.Add(logoPic);
            _headerPanel.Controls.Add(logo);

            _titleLabel = new Label
            {
                Text = "Community Service System",
                ForeColor = Color.FromArgb(15, 76, 136),
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(110, 20)
            };

            _subtitleLabel = new Label
            {
                Text = "Working together for a better community",
                ForeColor = Color.FromArgb(120, 120, 120),
                Font = new Font("Segoe UI", 10f),
                AutoSize = true,
                Location = new Point(112, 52)
            };

            _dateLabel = new Label
            {
                Text = DateTime.Now.ToString("dd MMM yyyy\nhh:mm tt"),
                TextAlign = ContentAlignment.TopRight,
                ForeColor = Color.FromArgb(100, 100, 100),
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Size = new Size(140, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_subtitleLabel);
            _headerPanel.Controls.Add(_dateLabel);
            Controls.Add(_headerPanel);

            // Reading ClientSize.Width here (before the form's font-based auto-scaling
            // has settled) produces a bogus, oversized value, which pushed the date label
            // far outside the header's visible bounds. Positioning it relative to the
            // header panel's own resolved width — after layout — fixes that.
            SyncDateLabelPosition();
        }

        private void SyncDateLabelPosition()
        {
            if (_headerPanel == null || _dateLabel == null) return;
            _dateLabel.Left = Math.Max(_headerPanel.ClientSize.Width - _dateLabel.Width - 20, 0);
            _dateLabel.Top = 28;
        }

        private void BuildContent()
        {
            // Content area with welcome text and three cards.
            // Dock=Fill does not reliably account for the header panel's reserved
            // top space in this control tree (same issue found/fixed in
            // ReportIssueForm) — it was rendering starting at Y=0, hiding the
            // welcome label entirely underneath the opaque header. Sizing/positioning
            // it manually (see SyncContentPanelBounds) avoids that.
            _contentPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.None,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(24, 18, 24, 18),
                BackColor = Color.White
            };

            var welcome = new Label
            {
                Text = "Welcome!\n\nPlease select an option from the menu below.",
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 40, 40),
                AutoSize = true,
                Margin = new Padding(6, 6, 6, 18)
            };
            _contentPanel.Controls.Add(welcome);

            var cards = new TableLayoutPanel
            {
                // Dock=Top gets recalculated (and overridden) by the FlowLayoutPanel's
                // layout engine, undoing any manual width we set. Leaving Dock=None and
                // sizing the panel explicitly (see SyncCardsWidth) is what actually sticks.
                Dock = DockStyle.None,
                AutoSize = false,
                Height = 320,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(6)
            };
            _cardsPanel = cards;
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
            cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Card factory
            Panel MakeCard(string number, string title, string desc, Color accent, bool enabled, EventHandler action)
            {
                var outer = new Panel
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(8),
                    BackColor = enabled ? Color.FromArgb(240, 247, 255) : Color.White,
                    Padding = new Padding(0)
                };
                outer.BorderStyle = BorderStyle.FixedSingle;

                var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
                outer.Controls.Add(inner);

                var iconCircle = new Panel
                {
                    Size = new Size(84, 84),
                    BackColor = enabled ? Color.FromArgb(210, 230, 250) : Color.FromArgb(235, 235, 235),
                    Location = new Point(88, 8),
                    Anchor = AnchorStyles.Top
                };
                var icon = new Label
                {
                    Text = title.Contains("Local") ? "\uD83D\uDCE2" : (title.Contains("Service") ? "\uD83D\uDCCB" : "\uD83D\uDCDD"),
                    Font = new Font("Segoe UI Emoji", 26f),
                    AutoSize = true,
                    ForeColor = accent,
                    Location = new Point(20, 18)
                };
                iconCircle.Controls.Add(icon);
                inner.Controls.Add(iconCircle);

                var lblTitle = new Label
                {
                    Text = number + " " + title,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = enabled ? accent : Color.FromArgb(80,80,80),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(260, 44),
                    Location = new Point(0, 100),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                var lblDesc = new Label
                {
                    Text = desc,
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(100,100,100),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false,
                    Size = new Size(260, 40),
                    Location = new Point(0, 148),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

                var actionBtn = new Button
                {
                    Text = enabled ? "Click to continue" : "(To be implemented later)",
                    Enabled = enabled,
                    BackColor = enabled ? accent : Color.FromArgb(230,230,230),
                    ForeColor = enabled ? Color.White : Color.FromArgb(120,120,120),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(260, 36),
                    Location = new Point(0, 200),
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                actionBtn.FlatAppearance.BorderSize = 0;

                // Keep title/description/button widths in sync with the card's actual
                // available width so text is never clipped (or overflowing into the next
                // card) when the form is resized.
                void SyncChildWidths()
                {
                    int contentWidth = inner.ClientSize.Width - inner.Padding.Left - inner.Padding.Right;
                    if (contentWidth < 160) contentWidth = 160;
                    iconCircle.Left = inner.Padding.Left + (contentWidth - iconCircle.Width) / 2;
                    lblTitle.Left = 0;
                    lblTitle.Width = contentWidth;
                    lblDesc.Left = 0;
                    lblDesc.Width = contentWidth;
                    actionBtn.Left = 0;
                    actionBtn.Width = contentWidth;
                }

                if (enabled && action != null)
                {
                    actionBtn.Click += action;
                    outer.Cursor = Cursors.Hand;
                    outer.Click += action;
                    inner.Click += action;
                    icon.Click += action;
                    lblTitle.Click += action;
                    lblDesc.Click += action;
                }

                inner.Controls.Add(lblTitle);
                inner.Controls.Add(lblDesc);
                inner.Controls.Add(actionBtn);

                inner.Resize += (s, e) => SyncChildWidths();
                SyncChildWidths();

                return outer;
            }

            var card1 = MakeCard("1.", "Report Issues", "Report problems or issues in your community.", Color.FromArgb(14,100,206), true, BtnReportIssues_Click);
            var card2 = MakeCard("2.", "Local Events and Announcements", "View upcoming local events and announcements.", Color.FromArgb(0,128,96), true, BtnLocalEvents_Click);
            var card3 = MakeCard("3.", "Service Request Status", "Check the status of your service requests.", Color.FromArgb(214,118,21), true, BtnServiceStatus_Click);

            cards.Controls.Add(card1, 0, 0);
            cards.Controls.Add(card2, 1, 0);
            cards.Controls.Add(card3, 2, 0);

            _contentPanel.Controls.Add(cards);
            Controls.Add(_contentPanel);

            SyncContentPanelBounds();
            SyncCardsWidth();
        }

        private void SyncContentPanelBounds()
        {
            if (_contentPanel == null) return;
            int top = _headerPanel?.Bottom ?? 0;
            int bottom = _footerPanel?.Top ?? ClientSize.Height;
            _contentPanel.Location = new Point(0, top);
            _contentPanel.Size = new Size(ClientSize.Width, Math.Max(bottom - top, 0));
        }

        private Button CreateTaskButton(string title, string description, Color backColor, Color foreColor, bool enabled, int top)
        {
            var btn = new Button
            {
                Text = string.Format("{0}\n{1}", title, description),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f),
                ForeColor = enabled ? Color.White : foreColor,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Enabled = enabled,
                Height = 110,
                Padding = new Padding(15, 10, 10, 10),
                Cursor = enabled ? Cursors.Hand : Cursors.Default,
                UseVisualStyleBackColor = false,
                Tag = title,
                Margin = new Padding(0, 0, 0, 12)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = enabled ? ThemeDarkGreen : backColor;
            // Width is adjusted in ResizeButtons to match the content area's width

            return btn;
        }

        private void ResizeButtons()
        {
            if (_contentPanel == null) return;
            int horizontalPadding = _contentPanel.Padding.Left + _contentPanel.Padding.Right;
            int newWidth = _contentPanel.ClientSize.Width - horizontalPadding;
            if (newWidth < 300) newWidth = 300;
            foreach (Button btn in new[] { _btnReportIssues, _btnLocalEvents, _btnServiceStatus, _btnViewReports })
            {
                if (btn != null) btn.Width = newWidth;
            }
            SyncCardsWidth();
        }

        private void SyncCardsWidth()
        {
            // FlowLayoutPanel does not stretch Dock=Top children to the full available
            // width, so the cards TableLayoutPanel's width must be set explicitly to
            // match the content area, otherwise the cards (and their text) stay clipped.
            if (_contentPanel == null || _cardsPanel == null) return;
            int horizontalPadding = _contentPanel.Padding.Left + _contentPanel.Padding.Right;
            int scrollbarAllowance = _contentPanel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0;
            int newWidth = _contentPanel.ClientSize.Width - horizontalPadding - scrollbarAllowance;
            if (newWidth < 300) newWidth = 300;
            _cardsPanel.Width = newWidth;
        }

        private void BuildFooter()
        {
            // Footer: light blue bar with an info icon/message on the left and an Exit button on the right,
            // matching the mockup. Engagement info (points/level) is kept for internal state but hidden
            // from this simplified footer; RefreshEngagement() still updates the hidden labels.
            _footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(225, 238, 250),
                Padding = new Padding(20, 0, 20, 0)
            };

            var infoIcon = new Label
            {
                Text = "\u24D8",
                Font = new Font("Segoe UI", 14f),
                ForeColor = Color.FromArgb(90, 90, 90),
                AutoSize = true,
                Location = new Point(20, 18)
            };

            var messageLabel = new Label
            {
                Text = "Thank you for helping us build a better community!",
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Location = new Point(50, 20)
            };

            _btnExit = new Button
            {
                Text = "\u2192]  Exit",
                Size = new Size(96, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(ClientSize.Width - 116, 14)
            };
            _btnExit.FlatAppearance.BorderSize = 1;
            _btnExit.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnExit.Click += (s, e) => Close();

            // Hidden labels retained for engagement tracking (not shown in this footer layout)
            _badgeIcon = new PictureBox { Size = new Size(1, 1), Visible = false };
            _levelLabel = new Label { AutoSize = true, Visible = false };
            _pointsLabel = new Label { AutoSize = true, Visible = false };

            _footerPanel.Controls.Add(infoIcon);
            _footerPanel.Controls.Add(messageLabel);
            _footerPanel.Controls.Add(_btnExit);
            _footerPanel.Controls.Add(_badgeIcon);
            _footerPanel.Controls.Add(_levelLabel);
            _footerPanel.Controls.Add(_pointsLabel);
            Controls.Add(_footerPanel);
        }

        public void RefreshEngagement()
        {
            int    pts   = IssueReportRepository.TotalPoints;
            string level = IssueReportRepository.GetUserLevel();
            _levelLabel.Text  = level;
            _pointsLabel.Text = string.Format(
                "⭐ {0} points  —  Keep reporting to earn more rewards!", pts);

            // Keep the View Reports button label up-to-date with the current list count
            if (_btnViewReports != null)
                _btnViewReports.Text = string.Format(
                    "📄  View My Submitted Reports  ({0})",
                    IssueReportRepository.GetCount());
        }

        // ── Named event handlers ────────────────────────────────────────────

        private void MainMenuForm_Resize(object sender, EventArgs e)
        {
            SyncContentPanelBounds();
            ResizeButtons();
            SyncDateLabelPosition();
        }

        private void BtnReportIssues_Click(object sender, EventArgs e)
        {
            using (var form = new ReportIssueForm())
            {
                form.ShowDialog(this);
            }
            RefreshEngagement();
        }

        private void BtnViewReports_Click(object sender, EventArgs e)
        {
            using (var form = new ViewReportsForm())
            {
                form.ShowDialog(this);
            }
        }

        private void BtnLocalEvents_Click(object sender, EventArgs e)
        {
            using (var form = new LocalEventsForm())
            {
                form.ShowDialog(this);
            }
        }

        private void BtnServiceStatus_Click(object sender, EventArgs e)
        {
            using (var form = new ServiceRequestStatusForm())
            {
                form.ShowDialog(this);
            }
        }

        private void TaskButton_MouseEnter(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.Enabled)
                btn.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        }

        private void TaskButton_MouseLeave(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
                btn.Font = new Font("Segoe UI", 10f);
        }
    }
}
