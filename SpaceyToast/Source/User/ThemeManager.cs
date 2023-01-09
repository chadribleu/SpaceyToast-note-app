using Windows.UI.Xaml;

namespace SpaceyToast.Source.User
{
    public class ThemeManager
    {
        private static object _padlock = new object();
        private static ThemeManager _instance;
        private ThemeManager() { }

        public static ThemeManager Instance
        {
            get
            { 
                lock (_padlock)
                {
                    if (_instance == null) 
                    {
                        _instance = new ThemeManager();
                    }
                    return _instance;
                }
            }
        }

        public void SetTheme(ElementTheme theme)
        {
            (Window.Current.Content as FrameworkElement).RequestedTheme = theme;
            SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentTheme = theme;
        }
    }
}
