using LiteMonitor.Common;
using LiteMonitor.src.Core;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;

namespace LiteMonitor
{
    /// <summary>
    /// 任务栏渲染器（仅负责绘制，不再负责布局）
    /// </summary>
    public static class TaskbarRenderer
    {
        private static readonly Settings _settings = Settings.Load();

        // 浅色主题
        private static readonly Color LABEL_LIGHT = Color.FromArgb(20, 20, 20);
        private static readonly Color SAFE_LIGHT = Color.FromArgb(0x00, 0x80, 0x40);
        private static readonly Color WARN_LIGHT = Color.FromArgb(0xB5, 0x75, 0x00);
        private static readonly Color CRIT_LIGHT = Color.FromArgb(0xC0, 0x30, 0x30);

        // 深色主题
        private static readonly Color LABEL_DARK = Color.White;
        private static readonly Color SAFE_DARK = Color.FromArgb(0x66, 0xFF, 0x99);
        private static readonly Color WARN_DARK = Color.FromArgb(0xFF, 0xD6, 0x66);
        private static readonly Color CRIT_DARK = Color.FromArgb(0xFF, 0x66, 0x66);

        private static bool IsSystemLight()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return (int)(key?.GetValue("SystemUsesLightTheme", 1) ?? 1) != 0;
            }
            catch { return true; }
        }

        public static void Render(Graphics g, List<Column> cols)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            bool light = IsSystemLight();

            foreach (var col in cols)
            {
                if (col.BoundsTop != Rectangle.Empty && col.Top != null)
                    DrawItem(g, col.Top, col.BoundsTop, light);

                if (col.BoundsBottom != Rectangle.Empty && col.Bottom != null)
                    DrawItem(g, col.Bottom, col.BoundsBottom, light);
            }
        }

        private static void DrawItem(Graphics g, MetricItem item, Rectangle rc, bool light)
        {
            string label = LanguageManager.T($"Short.{item.Key}");
            string value = UIUtils.FormatHorizontalValue(
                               UIUtils.FormatValue(item.Key, item.DisplayValue)
                           );

            var font = new Font(
                _settings.TaskbarFontFamily,
                _settings.TaskbarFontSize,
                _settings.TaskbarFontBold ? FontStyle.Bold : FontStyle.Regular
            );

            Color labelColor = light ? LABEL_LIGHT : LABEL_DARK;
            Color valueColor = PickColor(item.Key, item.DisplayValue, light);

            // Label 左对齐
            TextRenderer.DrawText(
                g, label, font, rc, labelColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding |
                TextFormatFlags.NoClipping
            );

            // Value 右对齐
            TextRenderer.DrawText(
                g, value, font, rc, valueColor,
                TextFormatFlags.Right |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding |
                TextFormatFlags.NoClipping
            );
        }

        private static Color PickColor(string key, double v, bool light)
        {
            if (double.IsNaN(v)) return light ? LABEL_LIGHT : LABEL_DARK;

            var (warn, crit) = UIUtils.GetThresholds(key, ThemeManager.Current);

            if (key.StartsWith("NET") || key.StartsWith("DISK"))
                v /= 1024.0;

            if (v >= crit) return light ? CRIT_LIGHT : CRIT_DARK;
            if (v >= warn) return light ? WARN_LIGHT : WARN_DARK;
            return light ? SAFE_LIGHT : SAFE_DARK;
        }
    }
}
