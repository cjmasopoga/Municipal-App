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
    /// Visualises the service-request relationship graph: every request is a node,
    /// every relatedness edge is drawn in light grey, and the edges belonging to the
    /// Minimum Spanning Tree (computed via Prim's algorithm) are highlighted — showing
    /// the cheapest possible network that still connects every request, useful for
    /// planning efficient field-team routes/groupings.
    /// </summary>
    public class ServiceNetworkForm : Form
    {
        private Panel _headerPanel;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Panel _drawPanel;
        private Panel _bottomPanel;
        private Button _btnClose;

        private static readonly Color ThemeAccent     = Color.FromArgb(214, 118, 21);
        private static readonly Color ThemeAccentDark = Color.FromArgb(168, 92, 14);

        private List<(ServiceRequest A, ServiceRequest B, double Weight)> _allEdges;
        private HashSet<(int, int)> _mstEdgeKeys;
        private Dictionary<int, PointF> _nodePositions;

        public ServiceNetworkForm()
        {
            BuildUI();
            PrepareGraphData();
        }

        private void BuildUI()
        {
            Text            = "Service Request Network (Graph & Minimum Spanning Tree)";
            Size            = new Size(820, 640);
            MinimumSize     = new Size(600, 440);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.Sizable;

            _headerPanel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = ThemeAccent };
            _titleLabel = new Label
            {
                Text = "🕸  Service Request Network", ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold), AutoSize = true, Location = new Point(18, 8)
            };
            _subtitleLabel = new Label
            {
                Text = "Grey lines = related requests (graph edges). Bold orange lines = Minimum Spanning Tree (Prim's algorithm).",
                ForeColor = Color.FromArgb(255, 235, 210), Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                AutoSize = true, Location = new Point(20, 38)
            };
            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_subtitleLabel);

            _drawPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _drawPanel.Paint += DrawPanel_Paint;
            _drawPanel.Resize += (s, e) => _drawPanel.Invalidate();

            _bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White, Padding = new Padding(16, 10, 16, 10) };
            _btnClose = new Button
            {
                Text = "✕  Close", Size = new Size(110, 32), FlatStyle = FlatStyle.Flat,
                BackColor = ThemeAccent, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, UseVisualStyleBackColor = false, Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(ClientSize.Width - 130, 10)
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = ThemeAccentDark;
            _btnClose.Click += (s, e) => Close();
            _bottomPanel.Controls.Add(_btnClose);
            Resize += (s, e) => _btnClose.Location = new Point(ClientSize.Width - 130, 10);

            // This dialog's control tree (Top + Fill + Bottom, no Left/Right siblings)
            // mirrors ViewReportsForm's proven-safe Dock pattern, so plain Dock works here.
            Controls.Add(_drawPanel);
            Controls.Add(_bottomPanel);
            Controls.Add(_headerPanel);
        }

        private void PrepareGraphData()
        {
            _allEdges = ServiceRequestRepository.GetAllRelationshipEdges();
            List<(ServiceRequest From, ServiceRequest To, double Weight)> mst = ServiceRequestRepository.GetServiceNetworkMst();
            _mstEdgeKeys = new HashSet<(int, int)>(mst.Select(e => (Math.Min(e.From.Id, e.To.Id), Math.Max(e.From.Id, e.To.Id))));

            List<ServiceRequest> all = ServiceRequestRepository.GetAll();
            _nodePositions = new Dictionary<int, PointF>();
            int n = Math.Max(all.Count, 1);
            for (int i = 0; i < all.Count; i++)
            {
                double angle = 2 * Math.PI * i / n - Math.PI / 2;
                _nodePositions[all[i].Id] = new PointF((float)Math.Cos(angle), (float)Math.Sin(angle));
            }
        }

        private void DrawPanel_Paint(object sender, PaintEventArgs e)
        {
            if (_nodePositions == null) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = _drawPanel.ClientSize.Width / 2f;
            float cy = _drawPanel.ClientSize.Height / 2f;
            float radius = Math.Max(Math.Min(cx, cy) - 70, 40);

            PointF Resolve(PointF unit) => new PointF(cx + unit.X * radius, cy + unit.Y * radius);

            using (var greyPen = new Pen(Color.FromArgb(70, 160, 160, 160), 1.2f))
            using (var mstPen = new Pen(ThemeAccent, 2.6f))
            {
                foreach ((ServiceRequest a, ServiceRequest b, double weight) in _allEdges)
                {
                    PointF p1 = Resolve(_nodePositions[a.Id]);
                    PointF p2 = Resolve(_nodePositions[b.Id]);
                    bool inMst = _mstEdgeKeys.Contains((Math.Min(a.Id, b.Id), Math.Max(a.Id, b.Id)));
                    e.Graphics.DrawLine(inMst ? mstPen : greyPen, p1, p2);
                }
            }

            using (var nodeBrush = new SolidBrush(ThemeAccentDark))
            using (var textBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            using (var nodeFont = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            {
                foreach (ServiceRequest request in ServiceRequestRepository.GetAll())
                {
                    PointF p = Resolve(_nodePositions[request.Id]);
                    const float r = 7f;
                    e.Graphics.FillEllipse(nodeBrush, p.X - r, p.Y - r, r * 2, r * 2);

                    SizeF textSize = e.Graphics.MeasureString(request.ReferenceNumber, nodeFont);
                    float labelOffsetX = p.X < cx ? -textSize.Width - 10 : 10;
                    e.Graphics.DrawString(request.ReferenceNumber, nodeFont, textBrush, p.X + labelOffsetX, p.Y - textSize.Height / 2);
                }
            }
        }
    }
}
