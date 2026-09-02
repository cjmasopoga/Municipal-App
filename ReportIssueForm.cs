using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using Municipal_App.Models;

namespace Municipal_App
{
    public class ReportIssueForm : Form
    {
        private Panel _headerPanel;
        private Label _titleLabel;
        private Label _stepLabel;
        private ProgressBar _progressBar;
        private Label _progressLabel;
        private Label _locationLabel;
        private TextBox _locationBox;
        private Label _categoryLabel;
        private ComboBox _categoryCombo;
        private Label _descriptionLabel;
        private RichTextBox _descriptionBox;
        private Label _charCountLabel;
        private Label _attachLabel;
        private Label _attachStatus;
        private ListBox _attachList;
        private Button _btnAttach;
        private Button _btnRemoveAttach;
        private Label _attachHint;
        private Panel _engagementPanel;
        private Label _engagementLabel;
        private ProgressBar _pointsBar;
        private Button _btnSubmit;
        private Button _btnSubmitInline;
        private Button _btnBack;
        private Panel _bottomPanel;

        private static readonly Color ThemeGreen     = Color.FromArgb(0, 128, 96);
        private static readonly Color ThemeDarkGreen  = Color.FromArgb(0, 96, 72);
        private static readonly Color ThemeLight      = Color.FromArgb(245, 247, 245);
        private static readonly Color ThemeAccent     = Color.FromArgb(255, 152, 0);

        private readonly List<string> _attachedFiles = new List<string>();

        // Placeholder texts for controls (simple cue implementation)
        private const string LocationPlaceholder    = "Enter the location of the reported issue...";
        private const string DescriptionPlaceholder = "Provide a detailed description of the issue...";

        public ReportIssueForm()
        {
            InitializeUI();
            WireEvents();
            UpdateProgress();
        }

        private string GetLocationText()
        {
            if (_locationBox == null) return string.Empty;
            var t = _locationBox.Text ?? string.Empty;
            if (t == LocationPlaceholder) return string.Empty;
            return t.Trim();
        }

        private string GetDescriptionText()
        {
            if (_descriptionBox == null) return string.Empty;
            var t = _descriptionBox.Text ?? string.Empty;
            if (t == DescriptionPlaceholder) return string.Empty;
            return t.Trim();
        }

        private void InitializeUI()
        {
            Text = "Report an Issue";
            Size = new Size(720, 740);
            MinimumSize = new Size(640, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ThemeLight;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.Sizable;

            // ── Left navigation (mockup-style) ─────────────────────────────
            var leftNav = new Panel
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(8)
            };

            var navStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false
            };

            Button MakeNav(string text, bool active = false)
            {
                var b = new Button
                {
                    Text = text,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Height = 48,
                    Width = 164,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = active ? Color.FromArgb(230, 243, 255) : Color.Transparent,
                    ForeColor = Color.FromArgb(40, 40, 40),
                    Font = new Font("Segoe UI", 10f),
                    Margin = new Padding(6, 6, 6, 0)
                };
                b.FlatAppearance.BorderSize = 0;
                return b;
            }

            navStack.Controls.Add(MakeNav("\u2302  Main Menu"));
            navStack.Controls.Add(MakeNav("\u270F  Report an Issue", true));
            navStack.Controls.Add(MakeNav("\u25A3  My Reports"));
            navStack.Controls.Add(MakeNav("\u25A1  Track Report"));
            navStack.Controls.Add(MakeNav("\u2753  Help"));
            navStack.Controls.Add(MakeNav("\u24D8  About"));

            // spacer and user info at bottom
            var spacer = new Panel { Height = 12, Dock = DockStyle.Top }; navStack.Controls.Add(spacer);
            var userPanel = new Panel { Height = 64, Dock = DockStyle.Bottom };
            var userLabel = new Label { Text = "John Doe\nVersion 1.0.0", AutoSize = false, Height = 40, Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9f) };
            userPanel.Controls.Add(userLabel);
            leftNav.Controls.Add(navStack);
            leftNav.Controls.Add(userPanel);
            Controls.Add(leftNav);

            // ── Header ──────────────────────────────────────────────────────
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = Color.White,
                Padding = new Padding(20, 12, 20, 8)
            };

            _titleLabel = new Label
            {
                Text = "Report an Issue",
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(64, 10)
            };

            _stepLabel = new Label
            {
                Text = "Please provide details about the issue you are experiencing.",
                ForeColor = Color.FromArgb(110, 110, 110),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Location = new Point(64, 44)
            };

            var iconLabel = new Label
            {
                Text = "📝",
                Font = new Font("Segoe UI Emoji", 22f),
                ForeColor = Color.FromArgb(15, 76, 136),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(40, 40),
                Location = new Point(20, 14)
            };

            int textX = iconLabel.Right + 14;
            _titleLabel.Location = new Point(textX, _titleLabel.Location.Y);
            _stepLabel.Location = new Point(textX, _stepLabel.Location.Y);

            _headerPanel.Controls.Add(iconLabel);

            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_stepLabel);
            Controls.Add(_headerPanel);

            // ── Divider (hidden progress bar kept for internal completion tracking) ──
            // NOTE: Dock=Top here was being laid out incorrectly (ending up at X=0,Y=0,
            // ignoring the leftNav/header reserved space and covering the Location/Category
            // fields beneath it). Positioning it explicitly and keeping it in sync via
            // SyncMainLayout (see below) is reliable.
            var progressPanel = new Panel
            {
                Height = 1,
                BackColor = Color.FromArgb(225, 225, 225)
            };

            _progressLabel = new Label { AutoSize = true, Visible = false };
            _progressBar = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Visible = false };

            Controls.Add(progressPanel);

            // ── Scroll panel for fields ───────────────────────────────────────
            // NOTE: Dock=Fill here was also being laid out incorrectly (X=0,Y=0 spanning the
            // full form width/height, ignoring leftNav/header/bottomPanel). Positioned manually
            // and kept in sync via SyncMainLayout instead - see below.
            var scroll = new Panel
            {
                AutoScroll = true,
                Padding = new Padding(0),
                BackColor = ThemeLight
            };

            // Explicitly positioned fields panel (avoids TableLayoutPanel auto-size quirks
            // that were collapsing/hiding the Location and Category rows).
            // NOTE: Dock = Top does not reliably produce a scrollbar in an AutoScroll panel,
            // so we position it explicitly and set AutoScrollMinSize below instead.
            var fieldsPanel = new Panel
            {
                Location = new Point(0, 0),
                Height = 560,
                BackColor = ThemeLight
            };

            const int labelX = 30;
            const int inputX = 190;
            const int inputWidth = 440;
            int fieldsY = 20;

            _locationLabel = new Label
            {
                Text = "Location  *",
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(labelX, fieldsY + 6)
            };
            _locationBox = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(inputX, fieldsY),
                Size = new Size(inputWidth, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            // placeholder text cue
            _locationBox.Text = LocationPlaceholder;
            _locationBox.ForeColor = Color.FromArgb(150, 150, 150);
            fieldsPanel.Controls.Add(_locationLabel);
            fieldsPanel.Controls.Add(_locationBox);
            fieldsY += 60;

            _categoryLabel = new Label
            {
                Text = "Category  *",
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(labelX, fieldsY + 6)
            };
            _categoryCombo = new ComboBox
            {
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(inputX, fieldsY),
                Size = new Size(inputWidth, 32)
            };
            _categoryCombo.Items.AddRange(new object[]
            {
                "-- Select Category --",
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
            _categoryCombo.SelectedIndex = 0;
            fieldsPanel.Controls.Add(_categoryLabel);
            fieldsPanel.Controls.Add(_categoryCombo);
            fieldsY += 60;

            _descriptionLabel = new Label
            {
                Text = "Description  *",
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(labelX, fieldsY + 6)
            };

            var descriptionPanel = new Panel
            {
                Location = new Point(inputX, fieldsY),
                Size = new Size(inputWidth, 200),
                BorderStyle = BorderStyle.FixedSingle
            };

            var toolbarPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(248, 248, 248)
            };

            Button MakeToolbarButton(string text, FontStyle style)
            {
                var b = new Button
                {
                    Text = text,
                    Size = new Size(30, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(248, 248, 248),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    Font = new Font("Segoe UI", 9f, style),
                    Cursor = Cursors.Hand,
                    UseVisualStyleBackColor = false,
                    Margin = new Padding(2, 2, 0, 2)
                };
                b.FlatAppearance.BorderSize = 0;
                return b;
            }

            var btnBold = MakeToolbarButton("B", FontStyle.Bold);
            var btnItalic = MakeToolbarButton("I", FontStyle.Italic);
            var btnUnderline = MakeToolbarButton("U", FontStyle.Underline);
            var btnBullets = MakeToolbarButton("≔", FontStyle.Regular);
            var btnNumbered = MakeToolbarButton("1≡", FontStyle.Regular);
            var btnAlign = MakeToolbarButton("≡", FontStyle.Regular);

            btnBold.Click += (s, e) => ToggleSelectionFontStyle(FontStyle.Bold);
            btnItalic.Click += (s, e) => ToggleSelectionFontStyle(FontStyle.Italic);
            btnUnderline.Click += (s, e) => ToggleSelectionFontStyle(FontStyle.Underline);
            btnBullets.Click += (s, e) => { _descriptionBox.SelectionBullet = !_descriptionBox.SelectionBullet; _descriptionBox.Focus(); };
            btnNumbered.Click += (s, e) => { _descriptionBox.SelectionBullet = !_descriptionBox.SelectionBullet; _descriptionBox.Focus(); };
            btnAlign.Click += (s, e) =>
            {
                _descriptionBox.SelectionAlignment = _descriptionBox.SelectionAlignment == HorizontalAlignment.Left
                    ? HorizontalAlignment.Center : HorizontalAlignment.Left;
                _descriptionBox.Focus();
            };

            toolbarPanel.Controls.Add(btnBold);
            toolbarPanel.Controls.Add(btnItalic);
            toolbarPanel.Controls.Add(btnUnderline);
            toolbarPanel.Controls.Add(new Panel { Width = 8, Height = 1 });
            toolbarPanel.Controls.Add(btnBullets);
            toolbarPanel.Controls.Add(btnNumbered);
            toolbarPanel.Controls.Add(new Panel { Width = 8, Height = 1 });
            toolbarPanel.Controls.Add(btnAlign);

            _descriptionBox = new RichTextBox
            {
                Font = new Font("Segoe UI", 10f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                MaxLength = 500,
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0)
            };
            _descriptionBox.Text = DescriptionPlaceholder;
            _descriptionBox.ForeColor = Color.FromArgb(150, 150, 150);

            descriptionPanel.Controls.Add(_descriptionBox);
            descriptionPanel.Controls.Add(toolbarPanel);

            fieldsPanel.Controls.Add(_descriptionLabel);
            fieldsPanel.Controls.Add(descriptionPanel);
            fieldsY += 210;

            _charCountLabel = new Label { AutoSize = true, Visible = false };

            _attachLabel = new Label
            {
                Text = "Media Attachment",
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(labelX, fieldsY + 6)
            };

            _btnAttach = new Button
            {
                Text = "📎  Attach File(s)...",
                AutoSize = true,
                Padding = new Padding(10, 6, 10, 6),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 76, 136),
                Font = new Font("Segoe UI", 9.5f),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Location = new Point(inputX, fieldsY)
            };
            _btnAttach.FlatAppearance.BorderSize = 1;
            _btnAttach.FlatAppearance.BorderColor = Color.FromArgb(15, 76, 136);

            _attachStatus = new Label
            {
                Text = "No file(s) attached",
                ForeColor = Color.FromArgb(110, 110, 110),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true,
                Location = new Point(inputX + 160, fieldsY + 8)
            };

            fieldsPanel.Controls.Add(_attachLabel);
            fieldsPanel.Controls.Add(_btnAttach);
            fieldsPanel.Controls.Add(_attachStatus);
            fieldsY += 44;

            _attachHint = new Label
            {
                Text = "You can attach images or documents (e.g., .jpg, .png, .pdf, .docx)",
                ForeColor = Color.FromArgb(140, 140, 140),
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(inputX, fieldsY)
            };
            fieldsPanel.Controls.Add(_attachHint);
            fieldsY += 30;

            // hidden multi-select list retained for internal tracking of attached files
            _attachList = new ListBox { Visible = false, Height = 1 };
            _btnRemoveAttach = new Button { Visible = false };

            var divider = new Panel { Location = new Point(labelX, fieldsY), Size = new Size(inputWidth + (inputX - labelX), 1), BackColor = Color.FromArgb(225, 225, 225) };
            fieldsPanel.Controls.Add(divider);
            fieldsY += 20;

            _engagementPanel = new Panel
            {
                Location = new Point(labelX, fieldsY),
                Size = new Size(inputWidth + (inputX - labelX), 70),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 0)
            };

            var engagementTitle = new Label
            {
                Text = "Engagement",
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            };

            _engagementLabel = new Label
            {
                Text = "Keep going — fill in the fields above!",
                ForeColor = Color.FromArgb(0, 140, 60),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(120, 0)
            };

            // Tracks live completion of THIS form (Location/Category/Description),
            // not the user's overall points total — kept in sync via UpdateProgress().
            _pointsBar = new ProgressBar
            {
                Location = new Point(0, 26),
                Size = new Size(430, 20),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous,
                ForeColor = ThemeGreen
            };

            var engagementCaption = new Label
            {
                Text = "Your report helps us build a better community.",
                ForeColor = Color.FromArgb(120, 120, 120),
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = true,
                Location = new Point(0, 50)
            };

            _engagementPanel.Controls.Add(engagementTitle);
            _engagementPanel.Controls.Add(_engagementLabel);
            _engagementPanel.Controls.Add(_pointsBar);
            _engagementPanel.Controls.Add(engagementCaption);
            fieldsPanel.Controls.Add(_engagementPanel);
            fieldsY += 90;

            fieldsPanel.Height = fieldsY + 20;

            // Hidden inline submit button retained for backward-compat wiring; the mockup only shows
            // a single Submit button in the bottom bar.
            _btnSubmitInline = new Button { Visible = false };

            scroll.Controls.Add(fieldsPanel);
            // Force the scroll panel to always show a vertical scrollbar when content overflows,
            // and keep the fields panel width in sync with the visible viewport so nothing is
            // clipped or hidden off to the right.
            scroll.AutoScrollMinSize = new Size(0, fieldsPanel.Height);
            void SyncFieldsPanelWidth()
            {
                fieldsPanel.Width = Math.Max(scroll.ClientSize.Width, 1);
            }
            SyncFieldsPanelWidth();
            scroll.Resize += (s, e) => SyncFieldsPanelWidth();
            // ensure scroll starts at top so the first fields (Location/Category) are visible
            scroll.AutoScrollPosition = new Point(0, 0);

            // Bottom bar is added to Controls BEFORE the fill-docked scroll panel so it
            // reserves its space first and the scroll area correctly fills the remainder,
            // ensuring the top fields (Location/Category) are not pushed out of view.
            _bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };

            _btnBack = new Button
            {
                Text = "← Back to Main Menu",
                Size = new Size(180, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(220, 220, 220),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Dock = DockStyle.Left
            };
            _btnBack.FlatAppearance.BorderSize = 0;

            _btnSubmit = new Button
            {
                Text = "➤  Submit",
                Size = new Size(120, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(15, 76, 136),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Dock = DockStyle.Right
            };
            _btnSubmit.FlatAppearance.BorderSize = 0;
            _btnSubmit.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 60, 110);

            _bottomPanel.Controls.Add(_btnBack);
            _bottomPanel.Controls.Add(_btnSubmit);
            Controls.Add(_bottomPanel);

            // Manually keeps progressPanel/scroll positioned to the right of leftNav and
            // between header and bottomPanel. Dock=Top/Fill was unreliable here (it computed
            // bounds as if leftNav/header didn't exist, covering the Location/Category fields).
            void SyncMainLayout()
            {
                int left = leftNav.Width;
                int top = _headerPanel.Bottom;
                int availWidth = Math.Max(ClientSize.Width - left, 0);

                progressPanel.Location = new Point(left, top);
                progressPanel.Width = availWidth;

                int scrollTop = progressPanel.Bottom;
                int scrollHeight = Math.Max(_bottomPanel.Top - scrollTop, 0);
                scroll.Location = new Point(left, scrollTop);
                scroll.Size = new Size(availWidth, scrollHeight);
            }
            SyncMainLayout();
            Resize += (s, e) => SyncMainLayout();

            // On first show, make sure the top fields are visible and bottom bar is above the scroll area
            Shown += (s, e) =>
            {
                try
                {
                    SyncMainLayout();
                    _locationBox.Focus();
                    _locationBox.Select(0, 0);
                }
                catch { }
                _bottomPanel?.BringToFront();
                _headerPanel?.BringToFront();
            };
            Controls.Add(scroll);
        }
        private Label CreateFieldLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(40, 40, 40),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(30, y)
            };
        }

        private void WireEvents()
        {
            // ── Field change handlers ──────────────────────────────────────
            _locationBox.TextChanged            += LocationBox_TextChanged;
            _categoryCombo.SelectedIndexChanged += CategoryCombo_SelectedIndexChanged;
            _descriptionBox.TextChanged         += DescriptionBox_TextChanged;

            // ── Button click handlers ──────────────────────────────────────
            _btnAttach.Click            += BtnAttach_Click;
            _btnRemoveAttach.Click      += BtnRemoveAttach_Click;
            _btnSubmit.Click            += BtnSubmit_Click;
            _btnBack.Click              += BtnBack_Click;
            _attachList.DoubleClick     += AttachList_DoubleClick;

            // placeholder focus behavior
            _locationBox.GotFocus += (s, e) =>
            {
                if (_locationBox.Text == LocationPlaceholder)
                {
                    _locationBox.Text = string.Empty;
                    _locationBox.ForeColor = Color.FromArgb(40, 40, 40);
                }
            };
            _locationBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_locationBox.Text))
                {
                    _locationBox.Text = LocationPlaceholder;
                    _locationBox.ForeColor = Color.FromArgb(140, 140, 140);
                }
            };

            _descriptionBox.GotFocus += (s, e) =>
            {
                if (_descriptionBox.Text == DescriptionPlaceholder)
                {
                    _descriptionBox.Text = string.Empty;
                    _descriptionBox.ForeColor = Color.FromArgb(40, 40, 40);
                }
            };
            _descriptionBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_descriptionBox.Text))
                {
                    _descriptionBox.Text = DescriptionPlaceholder;
                    _descriptionBox.ForeColor = Color.FromArgb(140, 140, 140);
                }
            };

            // ── Keyboard handler — Escape closes the form ──────────────────
            KeyPreview = true;
            KeyDown += ReportIssueForm_KeyDown;

            // ── FormClosing — confirm if user has unsaved input ────────────
            FormClosing += ReportIssueForm_FormClosing;
        }

        // ── Named event handlers ────────────────────────────────────────────

        private void LocationBox_TextChanged(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        private void CategoryCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        private void DescriptionBox_TextChanged(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        private void ToggleSelectionFontStyle(FontStyle style)
        {
            var current = _descriptionBox.SelectionFont ?? _descriptionBox.Font;
            FontStyle newStyle = current.Style.HasFlag(style)
                ? current.Style & ~style
                : current.Style | style;
            _descriptionBox.SelectionFont = new Font(current, newStyle);
            _descriptionBox.Focus();
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AttachList_DoubleClick(object sender, EventArgs e)
        {
            if (_attachList.SelectedIndex < 0) return;

            int idx = _attachList.SelectedIndex;
            if (idx >= _attachedFiles.Count) return;

            string path = _attachedFiles[idx];
            try
            {
                var psi = new ProcessStartInfo(path) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open attachment: " + ex.Message, "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReportIssueForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void ReportIssueForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If DialogResult is already OK the user just submitted — no prompt needed
            if (DialogResult == DialogResult.OK) return;

            bool hasInput = !string.IsNullOrWhiteSpace(GetLocationText())
                         || _categoryCombo.SelectedIndex > 0
                         || !string.IsNullOrWhiteSpace(GetDescriptionText())
                         || _attachedFiles.Count > 0;

            if (hasInput)
            {
                var result = MessageBox.Show(
                    "You have unsaved information.\nAre you sure you want to go back to the main menu?",
                    "Discard Changes?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    e.Cancel = true;
            }
        }

        private void UpdateProgress()
        {
            int score = 0;
            if (!string.IsNullOrWhiteSpace(GetLocationText()))     score += 34;
            if (_categoryCombo.SelectedIndex > 0)                  score += 33;
            if (GetDescriptionText().Trim().Length > 10)           score += 33;

            score = Math.Min(score, 100);
            _progressBar.Value = score;
            _progressLabel.Text = string.Format("Form completion:  {0}%", score);

            // Keep the visible "Engagement" bar in sync with the same completion score
            // (it previously showed a static, unrelated TotalPoints value and never moved).
            if (_pointsBar != null)
            {
                _pointsBar.Value = score;
            }
            if (_engagementLabel != null)
            {
                _engagementLabel.Text = score >= 100
                    ? "Thank you for helping us improve!"
                    : string.Format("{0}% complete \u2014 keep going!", score);
            }
        }

        private void BtnAttach_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title      = "Attach a file";
                dlg.Filter     = "Images & Documents|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.pdf;*.docx;*.doc;*.txt|All Files|*.*";
                dlg.Multiselect = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    foreach (string path in dlg.FileNames)
                    {
                        if (!_attachedFiles.Contains(path))
                        {
                            _attachedFiles.Add(path);
                            _attachList.Items.Add(Path.GetFileName(path));
                            // update status label
                            _attachStatus.Text = string.Join(", ", _attachList.Items.Cast<object>());
                        }
                    }
                }
            }
        }

        private void BtnRemoveAttach_Click(object sender, EventArgs e)
        {
            for (int i = _attachList.SelectedIndices.Count - 1; i >= 0; i--)
            {
                int idx = _attachList.SelectedIndices[i];
                _attachedFiles.RemoveAt(idx);
                _attachList.Items.RemoveAt(idx);
            }
            // refresh status label
            if (_attachList.Items.Count == 0)
                _attachStatus.Text = "No file(s) attached";
            else
                _attachStatus.Text = string.Join(", ", _attachList.Items.Cast<object>());
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            string location    = GetLocationText();
            string category    = _categoryCombo.SelectedIndex > 0
                                     ? _categoryCombo.SelectedItem.ToString() : string.Empty;
            string description = GetDescriptionText();

            if (string.IsNullOrEmpty(location))
            {
                ShowError("Please enter a location for the issue.");
                _locationBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(category))
            {
                ShowError("Please select a category for the issue.");
                _categoryCombo.Focus();
                return;
            }

            if (description.Length < 10)
            {
                ShowError("Please provide a description of at least 10 characters.");
                _descriptionBox.Focus();
                return;
            }

            var report = new IssueReport
            {
                Location          = location,
                Category          = category,
                Description       = description,
                AttachedFilePaths = new List<string>(_attachedFiles)
            };

            IssueReportRepository.AddReport(report);

            int    newTotal = IssueReportRepository.TotalPoints;
            string newLevel = IssueReportRepository.GetUserLevel();
            int    count    = IssueReportRepository.GetCount();

            MessageBox.Show(
                string.Format(
                    "✔  Issue reported successfully!\n\n"  +
                    "Reference #:     {0}\n"              +
                    "Category:        {1}\n"              +
                    "Submitted:       {2}\n"              +
                    "Total reports:   {3}\n\n"            +
                    "⭐ You earned 10 points!\n"           +
                    "   Total: {4} pts  |  Level: {5}",
                    report.Id,
                    report.Category,
                    report.DateReported.ToString("dd MMM yyyy HH:mm"),
                    count,
                    newTotal,
                    newLevel),
                "Report Submitted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // ReportIssueForm
            // 
            this.ClientSize = new System.Drawing.Size(278, 244);
            this.Name = "ReportIssueForm";
            this.ResumeLayout(false);

        }
    }
}
