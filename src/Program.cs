// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, obtain it at https://mozilla.org/MPL/2.0/.
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

[assembly: AssemblyTitle("Apple Studio Display — яркость")]
[assembly: AssemblyDescription("Локальный регулятор аппаратной яркости Apple Studio Display")]
[assembly: AssemblyVersion("1.0.0.0")]

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new BrightnessWindow());
    }
}

internal sealed class BrightnessWindow : Form
{
    private readonly TrackBar slider = new TrackBar();
    private readonly NumericUpDown percent = new NumericUpDown();
    private readonly Label largeValue = new Label();
    private readonly Label status = new Label();
    private readonly Button darker = new Button();
    private readonly Button lighter = new Button();
    private readonly Button refresh = new Button();
    private readonly Timer delay = new Timer();
    private bool syncing;
    private bool dragging;
    private bool busy;
    private bool connected;
    private int verifiedPercent = 60;

    public BrightnessWindow()
    {
        Text = "Apple Studio Display — яркость";
        ClientSize = new Size(520, 340);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(248, 249, 251);
        ForeColor = Color.FromArgb(32, 36, 44);
        Icon = SystemIcons.Application;

        var title = new Label {
            Text = "Apple Studio Display",
            Font = new Font("Segoe UI Semibold", 16F),
            Location = new Point(24, 22), Size = new Size(470, 32)
        };
        var hint = new Label {
            Text = "Передвиньте ползунок или укажите яркость в процентах.",
            Location = new Point(26, 60), Size = new Size(470, 25)
        };
        largeValue.Text = "—";
        largeValue.Font = new Font("Segoe UI Semibold", 32F);
        largeValue.TextAlign = ContentAlignment.MiddleCenter;
        largeValue.Location = new Point(24, 92);
        largeValue.Size = new Size(472, 65);

        slider.Minimum = 1;
        slider.Maximum = 100;
        slider.Value = 60;
        slider.TickFrequency = 10;
        slider.SmallChange = 1;
        slider.LargeChange = 5;
        slider.AutoSize = false;
        slider.Location = new Point(26, 162);
        slider.Size = new Size(468, 46);
        slider.AccessibleName = "Яркость монитора, от 1 до 100 процентов";
        slider.TabIndex = 0;

        var low = new Label { Text = "1%", Location = new Point(34, 204), Size = new Size(70, 22) };
        var high = new Label { Text = "100%", TextAlign = ContentAlignment.TopRight,
            Location = new Point(423, 204), Size = new Size(60, 22) };

        darker.Text = "−5%";
        darker.Location = new Point(26, 239);
        darker.Size = new Size(77, 36);
        darker.TabIndex = 2;
        darker.AccessibleName = "Уменьшить яркость на 5 процентов";

        percent.Minimum = 1;
        percent.Maximum = 100;
        percent.Value = 60;
        percent.TextAlign = HorizontalAlignment.Center;
        percent.Location = new Point(119, 243);
        percent.Size = new Size(83, 30);
        percent.TabIndex = 1;
        percent.AccessibleName = "Яркость в процентах";
        var percentSign = new Label { Text = "%", Location = new Point(209, 245), Size = new Size(25, 25) };

        lighter.Text = "+5%";
        lighter.Location = new Point(240, 239);
        lighter.Size = new Size(77, 36);
        lighter.TabIndex = 3;
        lighter.AccessibleName = "Увеличить яркость на 5 процентов";

        refresh.Text = "Обновить";
        refresh.Location = new Point(371, 239);
        refresh.Size = new Size(123, 36);
        refresh.TabIndex = 4;
        refresh.AccessibleName = "Прочитать текущую яркость монитора";

        status.Text = "Подключение к монитору…";
        status.ForeColor = Color.FromArgb(92, 99, 111);
        status.Location = new Point(26, 292);
        status.Size = new Size(468, 40);
        status.AccessibleName = "Состояние монитора";

        Controls.AddRange(new Control[] { title, hint, largeValue, slider, low, high,
            darker, percent, percentSign, lighter, refresh, status });

        delay.Interval = 450;
        delay.Tick += async delegate { delay.Stop(); await ApplyBrightness(); };
        slider.MouseDown += delegate { dragging = true; delay.Stop(); };
        slider.MouseUp += delegate { dragging = false; QueueChange(); };
        slider.ValueChanged += delegate {
            if (syncing) return;
            SyncControls(slider.Value);
            if (!dragging) QueueChange();
        };
        percent.ValueChanged += delegate {
            if (syncing) return;
            SyncControls((int)percent.Value);
            QueueChange();
        };
        darker.Click += delegate { SyncControls(Math.Max(1, slider.Value - 5)); QueueChange(); };
        lighter.Click += delegate { SyncControls(Math.Min(100, slider.Value + 5)); QueueChange(); };
        refresh.Click += async delegate { await ReadBrightness(); };
        Shown += async delegate { await ReadBrightness(); };
        FormClosed += delegate { delay.Stop(); delay.Dispose(); };
        SetEnabled(false);
    }

    private void SyncControls(int value)
    {
        value = Math.Max(1, Math.Min(100, value));
        syncing = true;
        try {
            slider.Value = value;
            percent.Value = value;
            largeValue.Text = value.ToString() + "%";
        } finally { syncing = false; }
    }

    private void SetEnabled(bool enabled)
    {
        slider.Enabled = enabled;
        percent.Enabled = enabled;
        darker.Enabled = enabled;
        lighter.Enabled = enabled;
        refresh.Enabled = !busy;
    }

    private void QueueChange()
    {
        if (syncing || busy || !connected || dragging) return;
        delay.Stop();
        if (slider.Value == verifiedPercent) return;
        status.ForeColor = Color.FromArgb(92, 99, 111);
        status.Text = "Применение " + slider.Value.ToString() + "%…";
        delay.Start();
    }

    private async Task ReadBrightness()
    {
        if (busy) return;
        delay.Stop();
        busy = true;
        SetEnabled(false);
        status.Text = "Чтение яркости…";
        try {
            var result = await Task.Run(() => StudioDisplayBrightness.Run(-1));
            if (IsDisposed) return;
            verifiedPercent = Math.Max(1, Math.Min(100, (int)Math.Round(result.After / 600.0)));
            connected = true;
            SyncControls(verifiedPercent);
            status.ForeColor = Color.FromArgb(38, 112, 68);
            status.Text = "Подключён · текущая яркость " + verifiedPercent.ToString() + "%";
        } catch (Exception) {
            if (IsDisposed) return;
            connected = false;
            status.ForeColor = Color.FromArgb(176, 52, 52);
            status.Text = "Нет связи с монитором. Проверьте USB-C и нажмите «Обновить».";
        } finally {
            busy = false;
            if (!IsDisposed) SetEnabled(connected);
        }
    }

    private async Task ApplyBrightness()
    {
        if (busy || !connected || dragging) return;
        int requested = slider.Value;
        busy = true;
        SetEnabled(false);
        try {
            var result = await Task.Run(() => StudioDisplayBrightness.Run(requested * 600));
            if (result.After != requested * 600) {
                await Task.Delay(700);
                result = await Task.Run(() => StudioDisplayBrightness.Run(-1));
            }
            if (result.After != requested * 600)
                throw new InvalidOperationException("Monitor did not confirm brightness");
            if (IsDisposed) return;
            verifiedPercent = requested;
            SyncControls(verifiedPercent);
            status.ForeColor = Color.FromArgb(38, 112, 68);
            status.Text = "Яркость " + verifiedPercent.ToString() + "% применена";
        } catch (Exception) {
            if (IsDisposed) return;
            connected = false;
            SyncControls(verifiedPercent);
            status.ForeColor = Color.FromArgb(176, 52, 52);
            status.Text = "Не удалось применить яркость. Нажмите «Обновить».";
        } finally {
            busy = false;
            if (!IsDisposed) SetEnabled(connected);
        }
    }
}
