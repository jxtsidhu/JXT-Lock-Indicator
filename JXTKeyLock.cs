// JXT KEY LOCK INDICATOR - Caps / Num / Scroll lock on-screen indicator
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;

[assembly: AssemblyTitle("JXT KEY LOCK INDICATOR")]
[assembly: AssemblyDescription("Caps Lock, Num Lock and Scroll Lock on-screen indicator")]
[assembly: AssemblyCompany("JXT SIDHU")]
[assembly: AssemblyProduct("JXT KEY LOCK INDICATOR")]
[assembly: AssemblyCopyright("Copyright (c) JXT SIDHU")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

static class Program {
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    public static EventWaitHandle ShowEvt;
    static bool shown;
    public static void Log(Exception ex, bool box) {
        string f = Path.Combine(Cfg.Dir, "error.log");
        try { Directory.CreateDirectory(Cfg.Dir); File.AppendAllText(f, DateTime.Now + "\r\n" + ex + "\r\n\r\n"); } catch { }
        if (box && !shown) {
            shown = true;
            MessageBox.Show(Cfg.App + " hit a problem:\n\n" + ex.Message + "\n\nWhere:\n" + string.Join("\n", (ex.StackTrace ?? "").Split(new[] { '\n' }, 4), 0, Math.Min(3, (ex.StackTrace ?? "").Split(new[] { '\n' }, 4).Length)) + "\n\nDetails saved to:\n" + f, Cfg.App, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
    public static bool Startup;
    [STAThread] static void Main(string[] args) {
        try { SetProcessDPIAware(); } catch { }
        bool created;
        ShowEvt = new EventWaitHandle(false, EventResetMode.AutoReset, "JXT_KEY_LOCK_INDICATOR_SHOW");
        using (var m = new Mutex(true, "JXT_KEY_LOCK_INDICATOR_SINGLE", out created)) {
            if (!created) { ShowEvt.Set(); return; }   // already running: just open its settings window
            Startup = Array.IndexOf(args, "--startup") >= 0;
            if (Startup) Thread.Sleep(2500);            // let the taskbar/tray finish loading at logon
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => Log(e.Exception, true);
            try { Application.Run(new AppCtx()); } catch (Exception ex) { Log(ex, true); }
        }
    }
}

static class G {
    public static GraphicsPath RR(Rectangle r, int rad) {
        var p = new GraphicsPath(); int d = Math.Max(2, rad * 2);
        p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseAllFigures(); return p;
    }
    public static readonly Color Green = Color.FromArgb(46, 204, 113), Gray = Color.FromArgb(62, 66, 78), Dark = Color.FromArgb(24, 26, 32);
}

class Cfg {
    public const string App = "JXT KEY LOCK INDICATOR", Publisher = "JXT SIDHU", Ver = "1.1.0";
    public static string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), App);
    public static string FilePath = Path.Combine(Dir, "settings.ini");
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    public static readonly string[] DefLabels = { "CAPS", "NUM", "SCROLL" };

    public bool[] Enable, Pin; public string[] Labels; public Color[] Col;   // Col: ON, OFF, ON text, OFF text
    public int Mode, Pos, Scale, Opacity, FlashSecs, X, Y, Radius, Margin, Layout, Monitor;
    public bool Drag, Auto, ShowState, Glow, Sound; public string Font;

    public Cfg() { Apply(new Dictionary<string, string>()); }
    public void Reset() { bool a = Auto; Apply(new Dictionary<string, string>()); Auto = a; }
    public void Apply(Dictionary<string, string> d) {
        Func<string, int, int> gi = (k, def) => { int n; return d.ContainsKey(k) && int.TryParse(d[k], out n) ? n : def; };
        Enable = new bool[3]; Pin = new bool[3]; Labels = new string[3]; Col = new Color[4];
        int[] dc = { unchecked((int)0xFF2ECC71), unchecked((int)0xFF2C2F3A), unchecked((int)0xFFFFFFFF), unchecked((int)0xFFBEC3CE) };
        for (int i = 0; i < 3; i++) {
            Enable[i] = gi("En" + i, 1) == 1; Pin[i] = gi("Pin" + i, i == 0 ? 1 : 0) == 1;
            string l; Labels[i] = d.TryGetValue("Lbl" + i, out l) && l.Length > 0 ? l : DefLabels[i];
        }
        for (int i = 0; i < 4; i++) Col[i] = Color.FromArgb(255, Color.FromArgb(gi("Col" + i, dc[i])));
        Mode = gi("Mode", 2); Pos = gi("Pos", 5); Scale = gi("Scale", 100); Opacity = gi("Opacity", 92);
        FlashSecs = gi("Flash", 2); X = gi("X", 100); Y = gi("Y", 100); Drag = gi("Drag", 0) == 1;
        Radius = gi("Radius", 13); Margin = gi("Margin", 6); Layout = gi("Layout", 0); Monitor = gi("Monitor", 0);
        ShowState = gi("ShowState", 1) == 1; Glow = gi("Glow", 1) == 1; Sound = gi("Sound", 0) == 1; Auto = gi("Auto", 1) == 1;
        string f; Font = d.TryGetValue("Font", out f) && f.Length > 0 ? f : "Segoe UI";
    }
    public static Cfg Load(out bool first) {
        var c = new Cfg(); first = !File.Exists(FilePath);
        if (first) return c;
        try {
            var d = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(FilePath)) { int i = line.IndexOf('='); if (i > 0) d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim(); }
            c.Apply(d);
        } catch { }
        return c;
    }
    public List<string> Lines() {
        var l = new List<string>();
        for (int i = 0; i < 3; i++) { l.Add("En" + i + "=" + (Enable[i] ? 1 : 0)); l.Add("Pin" + i + "=" + (Pin[i] ? 1 : 0)); l.Add("Lbl" + i + "=" + Labels[i]); }
        for (int i = 0; i < 4; i++) l.Add("Col" + i + "=" + Col[i].ToArgb());
        l.Add("Mode=" + Mode); l.Add("Pos=" + Pos); l.Add("Scale=" + Scale); l.Add("Opacity=" + Opacity); l.Add("Flash=" + FlashSecs);
        l.Add("X=" + X); l.Add("Y=" + Y); l.Add("Drag=" + (Drag ? 1 : 0)); l.Add("Radius=" + Radius); l.Add("Margin=" + Margin);
        l.Add("Layout=" + Layout); l.Add("Monitor=" + Monitor); l.Add("ShowState=" + (ShowState ? 1 : 0)); l.Add("Glow=" + (Glow ? 1 : 0));
        l.Add("Sound=" + (Sound ? 1 : 0)); l.Add("Auto=" + (Auto ? 1 : 0)); l.Add("Font=" + Font);
        return l;
    }
    public string Sig() { return string.Join(";", Lines().ToArray()); }
    public void Save() { try { Directory.CreateDirectory(Dir); File.WriteAllLines(FilePath, Lines().ToArray()); } catch { } }

    // Registers (or removes) the app to start hidden with Windows. Called at EVERY launch so a moved/rebuilt exe or a
    // disabled Startup-Apps switch heals itself.
    public static void SetAuto(bool on) {
        try {
            using (var k = Registry.CurrentUser.CreateSubKey(RunKey)) {
                if (on) k.SetValue(App, "\"" + Application.ExecutablePath + "\" --startup"); else k.DeleteValue(App, false);
            }
            using (var k = Registry.CurrentUser.CreateSubKey(ApprovedKey)) {
                if (on) k.SetValue(App, new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary); else k.DeleteValue(App, false);
            }
        } catch { }
    }
}

// Crisp, anti-aliased, per-pixel-alpha indicator: always on top, click-through, never steals focus
class Overlay : Form {
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr h, int i, int v);
    [DllImport("user32.dll")] static extern bool UpdateLayeredWindow(IntPtr h, IntPtr dst, ref Pt pd, ref Sz sz, IntPtr src, ref Pt ps, int key, ref Bf bf, int flags);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr o);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
    [StructLayout(LayoutKind.Sequential)] struct Pt { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] struct Sz { public int W, H; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] struct Bf { public byte Op, Flags, Alpha, Fmt; }

    static readonly string[] Names = { "CAPS", "NUM", "SCROLL" };
    Cfg c; bool[] st = new bool[3]; DateTime flashUntil = DateTime.MinValue;
    List<int> vis = new List<int>(); string lastKey = ""; bool dragging; Point dragStart;
    public event Action Moved;

    public Overlay(Cfg cfg) {
        c = cfg; FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual; TopMost = true;
    }
    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams {
        get {
            var p = base.CreateParams;
            p.ExStyle |= 0x80000 | 0x08000000 | 0x80 | 0x8;   // layered, noactivate, toolwindow, topmost
            if (c == null || !c.Drag) p.ExStyle |= 0x20;     // click-through (c can be null while the base Form is still being constructed)
            return p;
        }
    }
    void ClickThrough(bool on) {
        int ex = GetWindowLong(Handle, -20);
        SetWindowLong(Handle, -20, on ? (ex | 0x20) : (ex & ~0x20));
    }
    public void Apply(bool[] s, bool changed) {
        st = s;
        if (changed) flashUntil = DateTime.Now.AddSeconds(c.FlashSecs);
        bool flash = DateTime.Now < flashUntil;
        vis.Clear();
        for (int i = 0; i < 3; i++)
            if (c.Enable[i] && (c.Pin[i] || c.Mode == 0 || (c.Mode == 1 && st[i]) || (c.Mode != 0 && flash))) vis.Add(i);
        if (vis.Count == 0) { if (Visible) Hide(); lastKey = ""; return; }

        string key = string.Join(",", vis) + "|" + c.Sig() + "|" + st[0] + st[1] + st[2];
        if (key == lastKey && Visible) return;
        lastKey = key;
        if (!Visible) Show();
        ClickThrough(!c.Drag);
        Cursor = c.Drag ? Cursors.SizeAll : Cursors.Default;
        Render();
    }
    float dpi = 1f;
    static Color Mix(Color a, Color b, float t) {
        return Color.FromArgb(255, (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }
    Bitmap Draw(float s) {
        int gap = (int)(8 * s), pad = (int)(14 * s), rad = (int)(c.Radius * s), n = vis.Count, ch = (int)((c.ShowState ? 44 : 32) * s);
        float tw = 0;
        using (var mb = new Bitmap(1, 1)) using (var mg = Graphics.FromImage(mb)) using (var mf = new Font(c.Font, 13f * s, FontStyle.Bold, GraphicsUnit.Pixel))
            foreach (int i in vis) tw = Math.Max(tw, mg.MeasureString(c.Labels[i], mf, 4000, StringFormat.GenericTypographic).Width);
        int cw = (int)Math.Max(96 * s, 34 * s + tw + 14 * s);
        bool vert = c.Layout == 1;
        int w = vert ? pad * 2 + cw : pad * 2 + n * cw + (n - 1) * gap;
        int h = vert ? pad * 2 + n * ch + (n - 1) * gap : pad * 2 + ch;
        var bmp = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
        using (var g = Graphics.FromImage(bmp))
        using (var f1 = new Font(c.Font, 13f * s, FontStyle.Bold, GraphicsUnit.Pixel))
        using (var f2 = new Font(c.Font, 10f * s, FontStyle.Regular, GraphicsUnit.Pixel))
        using (var sf = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap }) {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; g.Clear(Color.Transparent);
            for (int k = 0; k < n; k++) {
                int i = vis[k]; bool on = st[i];
                var r = vert ? new Rectangle(pad, pad + k * (ch + gap), cw, ch) : new Rectangle(pad + k * (cw + gap), pad, cw, ch);
                Color fill = on ? c.Col[0] : c.Col[1], tc = on ? c.Col[2] : c.Col[3];
                if (c.Glow) {
                    var rs = r; if (!on) rs.Offset(0, (int)(2 * s));
                    for (int j = 6; j >= 1; j--)
                        using (var p = G.RR(rs, rad))
                        using (var pen = new Pen(on ? Color.FromArgb(10, c.Col[0]) : Color.FromArgb(9, 0, 0, 0), j * 2 * s) { LineJoin = LineJoin.Round })
                            g.DrawPath(pen, p);
                }
                using (var p = G.RR(r, rad)) {
                    using (var br = new LinearGradientBrush(r, Mix(fill, Color.White, on ? .12f : .08f), Mix(fill, Color.Black, on ? .25f : .2f), 90f))
                        g.FillPath(br, p);
                    using (var pen = new Pen(Color.FromArgb(on ? 90 : 45, 255, 255, 255), Math.Max(1f, s))) g.DrawPath(pen, p);
                }
                float d = 10 * s, lx = r.X + 15 * s, ly = r.Y + (r.Height - d) / 2f;
                if (on) using (var gb = new SolidBrush(Color.FromArgb(70, tc))) g.FillEllipse(gb, lx - 3 * s, ly - 3 * s, d + 6 * s, d + 6 * s);
                using (var lb = new SolidBrush(on ? tc : Mix(fill, Color.White, .35f))) g.FillEllipse(lb, lx, ly, d, d);
                using (var t1 = new SolidBrush(tc)) using (var t2 = new SolidBrush(Color.FromArgb(on ? 215 : 170, tc))) {
                    g.DrawString(c.Labels[i], f1, t1, r.X + 34 * s, c.ShowState ? r.Y + 6 * s : r.Y + (r.Height - f1.Height) / 2f, sf);
                    if (c.ShowState) g.DrawString(on ? "ON" : "OFF", f2, t2, r.X + 34 * s, r.Y + 24 * s, sf);
                }
            }
        }
        return bmp;
    }
    void Render() {
        using (var g0 = Graphics.FromHwnd(IntPtr.Zero)) dpi = g0.DpiX / 96f;
        using (var bmp = Draw(c.Scale / 100f * dpi)) {
            Size = bmp.Size; Place();
            IntPtr sdc = GetDC(IntPtr.Zero), mdc = CreateCompatibleDC(sdc), hb = bmp.GetHbitmap(Color.FromArgb(0)), old = SelectObject(mdc, hb);
            var sz = new Sz { W = bmp.Width, H = bmp.Height }; var ps = new Pt(); var pd = new Pt { X = Left, Y = Top };
            var bf = new Bf { Op = 0, Flags = 0, Alpha = (byte)(255 * c.Opacity / 100), Fmt = 1 };
            bool ok = UpdateLayeredWindow(Handle, sdc, ref pd, ref sz, mdc, ref ps, 0, ref bf, 2);
            SelectObject(mdc, old); DeleteObject(hb); DeleteDC(mdc); ReleaseDC(IntPtr.Zero, sdc);
            if (!ok) throw new Exception("UpdateLayeredWindow failed");
        }
    }
    void Place() {
        var scr = Screen.AllScreens; var wa = scr[Math.Min(Math.Max(0, c.Monitor), scr.Length - 1)].WorkingArea;
        int m = (int)(c.Margin * dpi), x, y;
        switch (c.Pos) {
            case 0: x = wa.Left + m; y = wa.Top + m; break;
            case 1: x = wa.Left + (wa.Width - Width) / 2; y = wa.Top + m; break;
            case 2: x = wa.Right - Width - m; y = wa.Top + m; break;
            case 3: x = wa.Left + m; y = wa.Bottom - Height - m; break;
            case 4: x = wa.Left + (wa.Width - Width) / 2; y = wa.Bottom - Height - m; break;
            case 6: x = wa.Left + (wa.Width - Width) / 2; y = wa.Top + (wa.Height - Height) / 2; break;
            case 7:
                var vs = SystemInformation.VirtualScreen;
                x = Math.Max(vs.Left, Math.Min(c.X, vs.Right - Width)); y = Math.Max(vs.Top, Math.Min(c.Y, vs.Bottom - Height)); break;
            default: x = wa.Right - Width - m; y = wa.Bottom - Height - m; break;
        }
        Location = new Point(x, y);
    }
    protected override void OnMouseDown(MouseEventArgs e) { if (c.Drag && e.Button == MouseButtons.Left) { dragging = true; dragStart = e.Location; } }
    protected override void OnMouseMove(MouseEventArgs e) { if (dragging) Location = new Point(Left + e.X - dragStart.X, Top + e.Y - dragStart.Y); }
    protected override void OnMouseUp(MouseEventArgs e) {
        if (!dragging) return; dragging = false;
        c.Pos = 7; c.X = Left; c.Y = Top; c.Save(); lastKey = ""; if (Moved != null) Moved();
    }
}

class SettingsForm : Form {
    Cfg c; Action changed, exit; bool loading; float k;
    CheckBox[] en = new CheckBox[3], pin = new CheckBox[3]; TextBox[] lbl = new TextBox[3]; Button[] cb = new Button[4];
    ComboBox mode, pos, layout, fontCb, monCb; NumericUpDown flash; TrackBar size, op, rad, mar;
    CheckBox drag, auto, showState, glow, sound; Label sizeL, opL, radL, marL;
    TableLayoutPanel tl;
    static readonly string[] Fonts = { "Segoe UI", "Bahnschrift", "Arial", "Calibri", "Consolas", "Verdana", "Tahoma", "Trebuchet MS" };

    int S(int v) { return (int)Math.Round(v * k); }
    void Row(Control x, bool stretch) { x.Anchor = stretch ? (AnchorStyles.Left | AnchorStyles.Right) : AnchorStyles.Left; tl.Controls.Add(x); }
    void Sec(string t) { Row(new Label { Text = t, AutoSize = true, Font = new Font("Segoe UI Semibold", 10.5f), ForeColor = G.Dark, Margin = new Padding(0, S(14), 0, S(4)) }, false); }
    CheckBox Chk(string t) { var b = new CheckBox { Text = t, AutoSize = true, Margin = new Padding(0, S(3), 0, S(3)) }; b.CheckedChanged += (s, e) => Push(); return b; }
    ComboBox Combo(params string[] items) {
        var b = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, S(4)) };
        b.Items.AddRange(items); b.SelectedIndexChanged += (s, e) => Push(); Row(b, true); return b;
    }
    TrackBar Track(int min, int max) {
        var t = new TrackBar { Minimum = min, Maximum = max, TickFrequency = 10, Margin = new Padding(0, 0, 0, S(2)) };
        t.ValueChanged += (s, e) => Push(); Row(t, true); return t;
    }
    Label Dim(string t, Padding m) { return new Label { Text = t, AutoSize = true, ForeColor = Color.FromArgb(90, 95, 108), Margin = m }; }
    Button Btn(string t) { return new Button { Text = t, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(S(8), S(3), S(8), S(3)) }; }
    TabPage Tab(string title) {
        var p = new TabPage(title) { BackColor = Color.White, AutoScroll = true };
        tl = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(S(14), 0, S(14), S(14)) };
        tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); p.Controls.Add(tl); return p;
    }
    TableLayoutPanel Grid2() {
        var g = new TableLayoutPanel { AutoSize = true, ColumnCount = 2 };
        g.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Row(g, true); return g;
    }

    public SettingsForm(Cfg cfg, Action onChange, Action onExit) {
        loading = true;
        using (var g0 = Graphics.FromHwnd(IntPtr.Zero)) k = g0.DpiX / 96f;
        c = cfg; changed = onChange; exit = onExit;
        AutoScaleMode = AutoScaleMode.None;
        Text = Cfg.App + "  -  by " + Cfg.Publisher; Font = new Font("Segoe UI", 9f); StartPosition = FormStartPosition.CenterScreen; BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true;
        ClientSize = new Size(S(450), S(700)); MinimumSize = new Size(S(400), S(360));
        Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };

        var hd = new Panel { Dock = DockStyle.Top, Height = S(64), BackColor = G.Dark };
        hd.Controls.Add(new Label { Text = Cfg.App, ForeColor = Color.White, Font = new Font("Segoe UI", 14f, FontStyle.Bold), AutoSize = true, Location = new Point(S(16), S(9)) });
        hd.Controls.Add(new Label { Text = "Caps / Num / Scroll lock status  |  by " + Cfg.Publisher, ForeColor = Color.FromArgb(160, 165, 175), AutoSize = true, Location = new Point(S(18), S(38)) });

        // ---------- General ----------
        var gen = Tab("General");
        string[] names = { "Caps Lock", "Num Lock", "Scroll Lock" };
        Sec("Indicators");
        var grid = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 3 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        for (int i = 0; i < 3; i++) { en[i] = Chk("Show " + names[i]); pin[i] = Chk("Always visible"); grid.Controls.Add(en[i], 0, i); grid.Controls.Add(pin[i], 1, i); }
        Row(grid, true);
        Sec("When to show");
        mode = Combo("Always show all indicators", "Show only while the lock is ON", "Flash briefly when a lock is toggled");
        var fl = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        fl.Controls.Add(Dim("Flash time (seconds)", new Padding(0, S(5), S(8), 0)));
        flash = new NumericUpDown { Minimum = 1, Maximum = 10, Width = S(60) }; flash.ValueChanged += (s, e) => Push(); fl.Controls.Add(flash);
        Row(fl, false);
        Sec("Position");
        pos = Combo("Top left", "Top center", "Top right", "Bottom left", "Bottom center", "Bottom right", "Center", "Custom (dragged)");
        drag = Chk("Unlock to drag the indicator with the mouse"); Row(drag, false);
        Sec("Appearance");
        sizeL = Dim("Size", new Padding(0)); Row(sizeL, false); size = Track(60, 160);
        opL = Dim("Opacity", new Padding(0, S(6), 0, 0)); Row(opL, false); op = Track(30, 100);
        Sec("Startup");
        auto = Chk("Start with Windows (runs hidden in the tray)"); Row(auto, false);
        var bt = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, S(10), 0, 0) };
        var hide = Btn("Hide to tray"); var quit = Btn("Exit app");
        hide.Click += (s, e) => Hide(); quit.Click += (s, e) => exit();
        bt.Controls.Add(hide); bt.Controls.Add(quit); Row(bt, false);

        // ---------- Advanced options ----------
        var adv = Tab("Advanced options");
        Sec("Colors  (click a swatch to change)");
        var cg = Grid2(); string[] cn = { "ON color", "OFF color", "ON text", "OFF text" };
        for (int i = 0; i < 4; i++) {
            int idx = i;
            cg.Controls.Add(Dim(cn[i], new Padding(0, S(6), S(16), 0)), 0, i);
            cb[i] = new Button { FlatStyle = FlatStyle.Flat, Width = S(80), Height = S(26), Margin = new Padding(0, S(2), 0, S(2)) };
            cb[i].Click += (s, e) => {
                using (var dlg = new ColorDialog { FullOpen = true, Color = c.Col[idx] })
                    if (dlg.ShowDialog(this) == DialogResult.OK) { c.Col[idx] = Color.FromArgb(255, dlg.Color); cb[idx].BackColor = c.Col[idx]; Push(); }
            };
            cg.Controls.Add(cb[i], 1, i);
        }
        Sec("Text labels");
        var lg = Grid2();
        for (int i = 0; i < 3; i++) {
            lg.Controls.Add(Dim(names[i], new Padding(0, S(6), S(16), 0)), 0, i);
            lbl[i] = new TextBox { MaxLength = 10, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            lbl[i].TextChanged += (s, e) => Push(); lg.Controls.Add(lbl[i], 1, i);
        }
        Sec("Style");
        Row(Dim("Layout", new Padding(0)), false); layout = Combo("Horizontal (side by side)", "Vertical (stacked)");
        Row(Dim("Font", new Padding(0)), false); fontCb = Combo(Fonts);
        showState = Chk("Show ON / OFF text under the name"); Row(showState, false);
        glow = Chk("Glow and shadow effects"); Row(glow, false);
        radL = Dim("Corner roundness", new Padding(0, S(6), 0, 0)); Row(radL, false); rad = Track(0, 24);
        marL = Dim("Distance from screen edge", new Padding(0, S(6), 0, 0)); Row(marL, false); mar = Track(0, 80);
        Sec("Display");
        Row(Dim("Show on monitor", new Padding(0)), false);
        var sn = new List<string>(); var scr = Screen.AllScreens;
        for (int i = 0; i < scr.Length; i++) sn.Add("Display " + (i + 1) + (scr[i].Primary ? " (primary)" : ""));
        monCb = Combo(sn.ToArray());
        Sec("Behavior");
        sound = Chk("Play a sound when a lock changes"); Row(sound, false);
        var reset = Btn("Reset advanced options to defaults"); reset.Margin = new Padding(0, S(14), 0, 0);
        reset.Click += (s, e) => {
            if (MessageBox.Show(this, "Reset all colors, labels and style options to defaults?", Cfg.App, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { c.Reset(); c.Save(); LoadUI(); changed(); }
        };
        Row(reset, false);

        // ---------- About ----------
        var about = Tab("About");
        Row(new Label { Text = Cfg.App, AutoSize = true, Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = G.Dark, Margin = new Padding(0, S(16), 0, S(4)) }, false);
        Row(Dim("Version " + Cfg.Ver, new Padding(0, 0, 0, S(2))), false);
        Row(Dim("Publisher: " + Cfg.Publisher, new Padding(0, 0, 0, S(12))), false);
        var tip = Dim("The app runs silently in the system tray (near the clock - click the ^ arrow if you can't see it). Click the tray icon to open these settings. It starts automatically with Windows.", new Padding(0, 0, 0, S(12)));
        tip.MaximumSize = new Size(S(380), 0); Row(tip, false);
        var of = Btn("Open settings folder"); of.Click += (s, e) => { try { Directory.CreateDirectory(Cfg.Dir); Process.Start("explorer.exe", "\"" + Cfg.Dir + "\""); } catch { } };
        Row(of, false);

        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(S(14), S(4)) };
        tabs.TabPages.Add(gen); tabs.TabPages.Add(adv); tabs.TabPages.Add(about);
        Controls.Add(tabs); Controls.Add(hd);   // fill first, header second
        LoadUI();
    }
    void Labels() {
        sizeL.Text = "Size: " + size.Value + "%"; opL.Text = "Opacity: " + op.Value + "%";
        radL.Text = "Corner roundness: " + rad.Value; marL.Text = "Distance from screen edge: " + mar.Value + " px";
    }
    public void LoadUI() {
        loading = true;
        for (int i = 0; i < 3; i++) { en[i].Checked = c.Enable[i]; pin[i].Checked = c.Pin[i]; lbl[i].Text = c.Labels[i]; }
        for (int i = 0; i < 4; i++) cb[i].BackColor = c.Col[i];
        mode.SelectedIndex = c.Mode; pos.SelectedIndex = c.Pos; flash.Value = Math.Max(1, Math.Min(10, c.FlashSecs));
        size.Value = Math.Max(60, Math.Min(160, c.Scale)); op.Value = Math.Max(30, Math.Min(100, c.Opacity));
        rad.Value = Math.Max(0, Math.Min(24, c.Radius)); mar.Value = Math.Max(0, Math.Min(80, c.Margin));
        layout.SelectedIndex = Math.Max(0, Math.Min(1, c.Layout));
        fontCb.SelectedItem = c.Font; if (fontCb.SelectedIndex < 0) fontCb.SelectedIndex = 0;
        monCb.SelectedIndex = Math.Max(0, Math.Min(monCb.Items.Count - 1, c.Monitor));
        drag.Checked = c.Drag; auto.Checked = c.Auto; showState.Checked = c.ShowState; glow.Checked = c.Glow; sound.Checked = c.Sound;
        Labels();
        loading = false;
    }
    void Push() {
        if (loading) return;
        for (int i = 0; i < 3; i++) {
            c.Enable[i] = en[i].Checked; c.Pin[i] = pin[i].Checked;
            string t = lbl[i].Text.Trim(); c.Labels[i] = t.Length > 0 ? t : Cfg.DefLabels[i];
        }
        c.Mode = mode.SelectedIndex; c.Pos = pos.SelectedIndex; c.FlashSecs = (int)flash.Value;
        c.Scale = size.Value; c.Opacity = op.Value; c.Radius = rad.Value; c.Margin = mar.Value; c.Layout = layout.SelectedIndex;
        c.Font = (string)fontCb.SelectedItem ?? c.Font; c.Monitor = Math.Max(0, monCb.SelectedIndex);
        c.Drag = drag.Checked; c.ShowState = showState.Checked; c.Glow = glow.Checked; c.Sound = sound.Checked;
        bool oa = c.Auto; c.Auto = auto.Checked; if (oa != c.Auto) Cfg.SetAuto(c.Auto);
        Labels(); c.Save(); changed();
    }
    protected override void OnFormClosing(FormClosingEventArgs e) {
        if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
        base.OnFormClosing(e);
    }
}

class AppCtx : ApplicationContext {
    [DllImport("user32.dll")] static extern short GetKeyState(int k);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr h);
    Cfg c; Overlay ov; SettingsForm sf; NotifyIcon ni; System.Windows.Forms.Timer t;
    bool[] st = new bool[3]; IntPtr ih = IntPtr.Zero;

    public AppCtx() {
        bool firstRun; c = Cfg.Load(out firstRun);
        if (firstRun) { c.Auto = true; c.Save(); }
        Cfg.SetAuto(c.Auto);   // re-register startup on every launch (self-heal)
        ov = new Overlay(c);
        sf = new SettingsForm(c, () => ov.Apply(st, true), Quit);
        ov.Moved += () => sf.LoadUI();
        ni = new NotifyIcon { Visible = true };
        var m = new ContextMenuStrip();
        m.Items.Add("Settings...", null, (s, e) => ShowSettings());
        m.Items.Add("Exit", null, (s, e) => Quit());
        ni.ContextMenuStrip = m; ni.DoubleClick += (s, e) => ShowSettings(); ni.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowSettings(); };
        Poll(true);
        t = new System.Windows.Forms.Timer { Interval = 100 }; t.Tick += (s, e) => Poll(false); t.Start();
        if (firstRun && !Program.Startup) ShowSettings();   // otherwise stay hidden in the tray
    }
    void ShowSettings() { sf.LoadUI(); sf.Show(); sf.WindowState = FormWindowState.Normal; sf.Activate(); }
    static bool On(int vk) { return (GetKeyState(vk) & 1) != 0; }
    void Poll(bool force) {
        if (Program.ShowEvt.WaitOne(0)) ShowSettings();
        var n = new[] { On(0x14), On(0x90), On(0x91) };
        bool ch = false; for (int i = 0; i < 3; i++) if (n[i] != st[i]) ch = true;
        if (ch && !force && c.Sound) { try { System.Media.SystemSounds.Asterisk.Play(); } catch { } }
        st = n;
        try { ov.Apply(st, ch && !force); } catch (Exception ex) { Program.Log(ex, true); }
        if (ch || force) {
            ni.Text = "JXT KEY LOCK - Caps " + (st[0] ? "ON" : "OFF") + " | Num " + (st[1] ? "ON" : "OFF") + " | Scr " + (st[2] ? "ON" : "OFF");
            using (var b = new Bitmap(32, 32)) using (var g = Graphics.FromImage(b)) {
                g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                using (var p = G.RR(new Rectangle(1, 1, 30, 30), 8)) using (var br = new SolidBrush(st[0] ? G.Green : Color.FromArgb(90, 95, 108))) g.FillPath(br, p);
                using (var f = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var sf2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("A", f, Brushes.White, new RectangleF(0, 0, 32, 33), sf2);
                IntPtr old = ih; ih = b.GetHicon(); ni.Icon = Icon.FromHandle(ih); sf.Icon = ni.Icon;
                if (old != IntPtr.Zero) DestroyIcon(old);
            }
        }
    }
    void Quit() { t.Stop(); ni.Visible = false; ni.Dispose(); ov.Close(); Application.Exit(); }
}
