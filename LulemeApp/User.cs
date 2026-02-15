using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization; // 关键引用

namespace LulemeApp
{
    public class User : INotifyPropertyChanged
    {
        // === 核心修复：增加一个“是否是本人”的标记 ===
        // JsonIgnore 防止这个属性被保存到文件里，避免逻辑混乱
        [JsonIgnore]
        public bool IsMe { get; set; } = false;

        public string Name { get; set; } = "千早爱音";
        public string AvatarUrl { get; set; } = "https://api.dicebear.com/7.x/avataaars/png?seed=Felix";

        private int _points;
        public int Points
        {
            get => _points;
            set
            {
                if (_points != value)
                {
                    _points = value;
                    CalculateLevel();
                    OnPropertyChanged();
                    Save(); // 只有 IsMe = true 才会真的保存
                }
            }
        }

        // 打卡状态
        public bool[] DailyCheckInStatus { get; set; } = new bool[3];
        public string LastCheckInDate { get; set; } = "";

        // 历史记录
        public List<string> CheckInHistory { get; set; } = new List<string>();

        // 感想记录
        public Dictionary<string, string> DailyThoughts { get; set; } = new Dictionary<string, string>();

        // 等级系统
        private int _level;
        private string _title = "🦌界萌新";
        private string _titleColor = "#FFFFFF";
        private string _glowColor = "#FFFFFF";

        public int Level { get => _level; set { _level = value; OnPropertyChanged(); } }
        public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }
        public string TitleColor { get => _titleColor; set { _titleColor = value; OnPropertyChanged(); } }
        public string GlowColor { get => _glowColor; set { _glowColor = value; OnPropertyChanged(); } }

        // 存档路径 (建议改个名，避免读取旧的错误存档)
        public static string SavePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LulemeApp", "liquid_v6_fixed.json");

        public void CalculateLevel()
        {
            Level = Points / 100;
            UpdateTitleAndColor();
        }

        public void UpdateTitleAndColor()
        {
            if (Level < 10) { Title = "🦌界萌新"; TitleColor = "#E0E0E0"; GlowColor = "#40FFFFFF"; }
            else if (Level < 30) { Title = "小有所成"; TitleColor = "#66FFCC"; GlowColor = "#4066FFCC"; }
            else if (Level < 60) { Title = "融会贯通"; TitleColor = "#33CCFF"; GlowColor = "#4033CCFF"; }
            else if (Level < 90) { Title = "出神入化"; TitleColor = "#CC66FF"; GlowColor = "#40CC66FF"; }
            else { Title = "🦌神"; TitleColor = "#FFD700"; GlowColor = "#60FFD700"; }
        }

        // === 核心修复：Save 方法加锁 ===
        public void Save()
        {
            // 只有标记为“本人”的实例，才有资格写入硬盘
            if (!IsMe) return;

            try
            {
                string dir = Path.GetDirectoryName(SavePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SavePath, JsonSerializer.Serialize(this));
            }
            catch { }
        }

        public static User Load()
        {
            User user = new User { IsMe = true }; // 默认新建一个本人

            if (File.Exists(SavePath))
            {
                try
                {
                    var loadedUser = JsonSerializer.Deserialize<User>(File.ReadAllText(SavePath));
                    if (loadedUser != null)
                    {
                        user = loadedUser;
                        user.IsMe = true; // 加载回来后，一定要重新标记为本人

                        // 处理日期跨天重置
                        string today = DateTime.Now.ToString("yyyy-MM-dd");
                        if (user.LastCheckInDate != today)
                        {
                            user.LastCheckInDate = today;
                            user.DailyCheckInStatus = new bool[3];
                        }
                    }
                }
                catch { }
            }
            else
            {
                // 如果是第一次运行，初始化一些数据
                user.LastCheckInDate = DateTime.Now.ToString("yyyy-MM-dd");
            }

            // 确保集合不为空
            if (user.CheckInHistory == null) user.CheckInHistory = new List<string>();
            if (user.DailyThoughts == null) user.DailyThoughts = new Dictionary<string, string>();

            user.CalculateLevel();
            return user;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}