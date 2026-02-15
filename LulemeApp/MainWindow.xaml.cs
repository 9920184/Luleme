using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LulemeApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private void Settings_Close(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is MainViewModel vm) vm.IsSettingsVisible = false;
        }

        private void Calendar_Close(object sender, MouseButtonEventArgs e)
        {
            if (this.DataContext is MainViewModel vm) vm.IsCalendarVisible = false;
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        // 1. 窗口加载/属性变化时：高亮所有打过卡的日期
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // 只有当日历可见时才刷新，节省性能
            if (this.DataContext is MainViewModel vm && vm.IsCalendarVisible && HistoryCalendar != null)
            {
                HistoryCalendar.SelectedDates.Clear();
                foreach (var dateStr in vm.CurrentUser.CheckInHistory)
                {
                    if (DateTime.TryParse(dateStr, out DateTime date))
                    {
                        HistoryCalendar.SelectedDates.Add(date);
                    }
                }
                // 重置下方显示文字
                vm.SelectedDateThought = "点击日期查看记录...";
            }
        }

        // 2. 关键修复：点击日历日期时触发
        private void HistoryCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            // 确保 ViewModel 存在且用户选了日期
            if (this.DataContext is MainViewModel vm && HistoryCalendar.SelectedDate.HasValue)
            {
                // 获取选中的日期，转为 "yyyy-MM-dd" 格式字符串
                DateTime selectedDate = HistoryCalendar.SelectedDate.Value;
                string key = selectedDate.ToString("yyyy-MM-dd");

                // 查找字典里有没有这一天的感想
                if (vm.CurrentUser.DailyThoughts.ContainsKey(key))
                {
                    vm.SelectedDateThought = vm.CurrentUser.DailyThoughts[key];
                }
                else
                {
                    // 如果没感想，判断一下这天有没有打卡
                    bool isCheckedIn = vm.CurrentUser.CheckInHistory.Contains(key);
                    if (isCheckedIn)
                        vm.SelectedDateThought = $"📅 {key}\n已打卡，但未记录感想。";
                    else
                        vm.SelectedDateThought = $"📅 {key}\n当天未打卡。";
                }
            }
        }
    }
}