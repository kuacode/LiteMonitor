using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.SystemServices;
using LiteMonitor.src.UI.Controls;

namespace LiteMonitor.src.UI.SettingsPage
{
    public class SystemHardwarPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false; 
        private string _originalLanguage; 

        public SystemHardwarPage()
        {
            this.BackColor = UIColors.MainBg;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(0);
            _container = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };
            this.Controls.Add(_container);
        }
    
        public override void OnShow()
        {
            base.OnShow(); // 1. 刷新数据 (从Config读取最新值)
            if (Config == null || _isLoaded) return;
            
            _container.SuspendLayout();
            _container.Controls.Clear();
            
            _originalLanguage = Config.Language; // 记录初始语言

            CreateSystemCard();   
            CreateCalibrationCard();
            CreateSourceCard();   

            _container.ResumeLayout();
            _isLoaded = true;
        }

        private void CreateSystemCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.SystemSettings"));
            
            // 1. 语言选择
            var cmbLang = new LiteComboBox();
            string langDir = Path.Combine(AppContext.BaseDirectory, "resources/lang");
            if (Directory.Exists(langDir)) {
                foreach (var file in Directory.EnumerateFiles(langDir, "*.json")) {
                    string code = Path.GetFileNameWithoutExtension(file);
                    cmbLang.Items.Add(code.ToUpper());
                }
            }
            BindCombo(cmbLang, 
                () => string.IsNullOrEmpty(Config.Language) ? "EN" : Config.Language.ToUpper(),
                v => Config.Language = (v == "AUTO") ? "" : v.ToLower());
                
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Language"), cmbLang));

            // 2. 开机自启
            var chkAutoStart = new LiteCheck(false, LanguageManager.T("Menu.Enable"));
            BindCheck(chkAutoStart, () => Config.AutoStart, v => Config.AutoStart = v);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.AutoStart"), chkAutoStart));

            // 3. 隐藏托盘图标
            var chkHideTray = new LiteCheck(false, LanguageManager.T("Menu.Enable"));
            BindCheck(chkHideTray, 
                () => Config.HideTrayIcon, 
                v => Config.HideTrayIcon = v);
                
            chkHideTray.CheckedChanged += (s, e) => EnsureSafeVisibility(null, chkHideTray, null);
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.HideTrayIcon"), chkHideTray));

            AddGroupToPage(group);
        }

        private void CreateCalibrationCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.Calibration"));
            string suffix = " (" + LanguageManager.T("Menu.MaxLimits") + ")";

            void AddCalibItem(string title, string unit, Func<float> get, Action<float> set)
            {
                // ★★★ 修改开始：使用 LiteNumberInput ★★★
                // 宽度 60，无前缀，无特殊颜色(默认灰色)
                var input = new LiteNumberInput("0", unit, "", 60);
                // ★★★ 修改结束 ★★★
                
                BindDouble(input, () => get(), v => set((float)v)); 
                group.AddItem(new LiteSettingsItem(title + suffix, input));
            }

            AddCalibItem(LanguageManager.T("Items.CPU.Power"), "W", 
                () => Config.RecordedMaxCpuPower, v => Config.RecordedMaxCpuPower = v);

            AddCalibItem(LanguageManager.T("Items.CPU.Clock"), "MHz", 
                () => Config.RecordedMaxCpuClock, v => Config.RecordedMaxCpuClock = v);

            AddCalibItem(LanguageManager.T("Items.GPU.Power"), "W", 
                () => Config.RecordedMaxGpuPower, v => Config.RecordedMaxGpuPower = v);

            AddCalibItem(LanguageManager.T("Items.GPU.Clock"), "MHz", 
                () => Config.RecordedMaxGpuClock, v => Config.RecordedMaxGpuClock = v);

            group.AddFullItem(new LiteNote(LanguageManager.T("Menu.CalibrationTip"), 0));
            AddGroupToPage(group);
        }

        private void CreateSourceCard()
        {
            var group = new LiteSettingsGroup(LanguageManager.T("Menu.HardwareSettings"));
            string strAuto = LanguageManager.T("Menu.Auto"); 

            // 1. 磁盘源
            var cmbDisk = new LiteComboBox();
            cmbDisk.Items.Add(strAuto); 
            foreach (var d in HardwareMonitor.ListAllDisks()) cmbDisk.Items.Add(d);
            
            BindCombo(cmbDisk, 
                () => string.IsNullOrEmpty(Config.PreferredDisk) ? strAuto : Config.PreferredDisk,
                v => Config.PreferredDisk = (v == strAuto) ? "" : v);
            
            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.DiskSource"), cmbDisk));

            // 2. 网络源
            var cmbNet = new LiteComboBox();
            cmbNet.Items.Add(strAuto);
            foreach (var n in HardwareMonitor.ListAllNetworks()) cmbNet.Items.Add(n);
            
            BindCombo(cmbNet, 
                () => string.IsNullOrEmpty(Config.PreferredNetwork) ? strAuto : Config.PreferredNetwork,
                v => Config.PreferredNetwork = (v == strAuto) ? "" : v);

            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.NetworkSource"), cmbNet));

            // 3. 刷新率
            var cmbRefresh = new LiteComboBox();
            int[] rates = { 100, 200, 300, 500, 600, 700, 800, 1000, 1500, 2000, 3000 };
            foreach (var r in rates) cmbRefresh.Items.Add(r + " ms");
            
            BindCombo(cmbRefresh,
                () => Config.RefreshMs + " ms",
                v => {
                    int val = UIUtils.ParseInt(v);
                    Config.RefreshMs = val < 50 ? 1000 : val;
                });

            group.AddItem(new LiteSettingsItem(LanguageManager.T("Menu.Refresh"), cmbRefresh));
            AddGroupToPage(group);
        }

        private void AddGroupToPage(LiteSettingsGroup group)
        {
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            wrapper.Controls.Add(group);
            _container.Controls.Add(wrapper);
            _container.Controls.SetChildIndex(wrapper, 0);
        }

        public override void Save()
        {
            if (!_isLoaded) return;
            // 1. 执行基类保存 (将 UI 值写入 Config 对象)
            base.Save(); 

            // 2. 应用语言变更 (如果变了)
            // ★ 必须最先执行！因为语言改变可能触发完全的重绘
            if (_originalLanguage != Config.Language) {
                AppActions.ApplyLanguage(Config, this.UI, this.MainForm);
                _originalLanguage = Config.Language; 
            }

            // 3. 应用开机自启和可见性
            AppActions.ApplyAutoStart(Config);
            AppActions.ApplyVisibility(Config, this.MainForm);
            
            // 4. 应用硬件源变更 (磁盘/网络选择)
            AppActions.ApplyMonitorLayout(this.UI, this.MainForm);

            // 5. ★★★ 核心修复：应用刷新率 ★★★
            // 刷新率 (RefreshMs) 属于 ThemeAndLayout 的管辖范围 (负责重启 Timer)
            // 必须显式调用它，否则刷新率不会立即生效！
            AppActions.ApplyThemeAndLayout(Config, this.UI, this.MainForm);
        }
    }
}