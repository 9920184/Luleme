using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LulemeApp
{
    // 命令类
    public class SimpleCommand : ICommand
    {
        private readonly Action<object?> _execute;
        public SimpleCommand(Action<object?> execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute(parameter);
        // 消除警告
        public event EventHandler? CanExecuteChanged { add { CommandManager.RequerySuggested += value; } remove { CommandManager.RequerySuggested -= value; } }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public User CurrentUser { get; set; }
        public ObservableCollection<User> Friends { get; set; }

        public ICommand CheckInCommand { get; }
        public ICommand ToggleSettingsCommand { get; }
        public ICommand ToggleCalendarCommand { get; }
        public ICommand SaveThoughtCommand { get; }
        public ICommand OpenGithubCommand { get; }

        private string _msg = "准备就绪";
        public string StatusMessage { get => _msg; set { _msg = value; OnPropertyChanged(); } }

        private bool _isSettings;
        public bool IsSettingsVisible { get => _isSettings; set { _isSettings = value; OnPropertyChanged(); } }

        private bool _isCalendar;
        public bool IsCalendarVisible { get => _isCalendar; set { _isCalendar = value; OnPropertyChanged(); } }

        private bool _isCoop;
        public bool IsCoopMode { get => _isCoop; set { _isCoop = value; OnPropertyChanged(); StatusMessage = value ? "好友一起🦌已开启 x1.5" : "单人模式"; } }

        private string _currentInputThought = "";
        public string CurrentInputThought { get => _currentInputThought; set { _currentInputThought = value; OnPropertyChanged(); } }

        // === 关键属性：绑定到界面显示的感想 ===
        private string _selectedDateThought = "点击日期查看感想...";
        public string SelectedDateThought
        {
            get => _selectedDateThought;
            set
            {
                _selectedDateThought = value;
                OnPropertyChanged(); // 必须有这一句，界面才会变！
            }
        }

        public MainViewModel()
        {
            CurrentUser = User.Load();
            CurrentUser.CalculateLevel();

            // 如果今天有感想，自动填入输入框
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (CurrentUser.DailyThoughts.ContainsKey(today))
                CurrentInputThought = CurrentUser.DailyThoughts[today];
            // 模拟好友数据（带等级和积分）
            Friends = new ObservableCollection<User> {
                new User { Name = "阿拉·佐巴扬", Points = 8500, AvatarUrl="https://bkimg.cdn.bcebos.com/pic/3801213fb80e7bec54e7ad90e474ae389b504fc23d35?x-bce-process=image/format,f_auto/quality,Q_70/resize,m_lfit,limit_1,w_536" },
                new User { Name = "东北雨姐", Points = 3400, AvatarUrl="https://bkimg.cdn.bcebos.com/smart/cdbf6c81800a19d8bc3e1118c0a3958ba61ea8d3e82e-bkimg-process,v_1,rw_1,rh_1,maxl_216,pad_1,color_ffffff?x-bce-process=image/format,f_auto" },
                new User { Name = "樱羽艾玛", Points = 0, AvatarUrl="https://i0.hdslb.com/bfs/garb/item/78092814ea94f18e14734cde17c7c7715dc800bf.png@500w_500h.avif" },
                new User { Name = "勒布朗·詹姆斯", Points = 15000, AvatarUrl="https://bkimg.cdn.bcebos.com/smart/fc1f4134970a304e251f38ff2193b086c9177f3ef638-bkimg-process,v_1,rw_1,rh_1,maxl_216,pad_1,color_ffffff?x-bce-process=image/format,f_auto" },
                
            };
            foreach (var f in Friends) f.CalculateLevel(); // 计算好友等级

            // 打卡命令
            CheckInCommand = new SimpleCommand(_ => {
                int slot = DateTime.Now.Hour < 12 ? 0 : DateTime.Now.Hour < 18 ? 1 : 2;
                if (CurrentUser.DailyCheckInStatus[slot]) { StatusMessage = "该时段已打卡"; return; }

                int p = (int)(100 * (IsCoopMode ? 1.5 : 1.0));
                CurrentUser.Points += p;
                CurrentUser.DailyCheckInStatus[slot] = true;

                // 记录历史日期
                if (!CurrentUser.CheckInHistory.Contains(today))
                    CurrentUser.CheckInHistory.Add(today);

                CurrentUser.Save();
                StatusMessage = $"打卡成功 +{p}";
                OnPropertyChanged(nameof(CurrentUser));
            });

            // 保存感想命令
            SaveThoughtCommand = new SimpleCommand(_ => {
                if (string.IsNullOrWhiteSpace(CurrentInputThought)) return;

                string dateKey = DateTime.Now.ToString("yyyy-MM-dd");

                // 存入字典
                if (CurrentUser.DailyThoughts.ContainsKey(dateKey))
                    CurrentUser.DailyThoughts[dateKey] = CurrentInputThought;
                else
                    CurrentUser.DailyThoughts.Add(dateKey, CurrentInputThought);

                CurrentUser.Save();
                StatusMessage = "感想已保存";

                // 如果日历正打开且选中了今天，立即刷新显示
                SelectedDateThought = CurrentInputThought;
            });

            ToggleSettingsCommand = new SimpleCommand(_ => IsSettingsVisible = !IsSettingsVisible);
            ToggleCalendarCommand = new SimpleCommand(_ => IsCalendarVisible = !IsCalendarVisible);
            OpenGithubCommand = new SimpleCommand(_ => { try { Process.Start(new ProcessStartInfo { FileName = "https://github.com/9920184", UseShellExecute = true }); } catch { } });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        // 简化的通知方法
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}