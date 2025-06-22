public class UserProfile
{
    public string UserName { get; set; }
    public string UserTitle { get; set; }
    public string UserBio { get; set; }
    public ImageSource UserAvatar { get; set; }
    public List<StatItem> Stats { get; set; }
    public List<SettingItem> Settings { get; set; }
}
