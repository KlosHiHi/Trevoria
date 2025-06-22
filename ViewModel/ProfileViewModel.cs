using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Trevoria.ViewModel
{
    class ProfileViewModel
    {
        private UserProfile _userProfile;

        public event PropertyChangedEventHandler PropertyChanged;

        public UserProfile UserProfile
        {
            get => _userProfile;
            set
            {
                _userProfile = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UserName));
                OnPropertyChanged(nameof(UserTitle));
                OnPropertyChanged(nameof(UserBio));
                OnPropertyChanged(nameof(UserAvatar));
                OnPropertyChanged(nameof(Stats));
            }
        }

        public string UserName => UserProfile?.UserName;
        public string UserTitle => UserProfile?.UserTitle;
        public string UserBio => UserProfile?.UserBio;
        public ImageSource UserAvatar => UserProfile?.UserAvatar;
        public IEnumerable<StatItem> Stats => UserProfile?.Stats;

        public ProfileViewModel()
        {

            LoadProfileData();
        }

        private void LoadProfileData()
        {
            UserProfile = new UserProfile
            {
                UserName = "Иван Иванов",
                UserTitle = "Обычный русский парень",
                UserBio = "Люблю прогулки на свежем воздухе и приключения",
                UserAvatar = ImageSource.FromFile("example_logo.jpg"),
                Stats = new List<StatItem>
            {
                new StatItem { Name = "Маршруты", Value = "5" },
                new StatItem { Name = "Километры", Value = "4.34K" },
                new StatItem { Name = "Посещено мест", Value = "13" }
            },
            };
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
