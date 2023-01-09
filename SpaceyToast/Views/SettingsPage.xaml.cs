using SpaceyToast.Source;
using SpaceyToast.Source.User;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SpaceyToast.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            RadioBtnLightTheme.Checked += RadioBtnLightTheme_Checked;
            RadioBtnDarkTheme.Checked += RadioBtnDarkTheme_Checked;
            RadioBtnDefaultTheme.Checked += RadioBtnDefaultTheme_Checked;

            ElementTheme theme = SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentTheme;
            RadioBtnLightTheme.IsChecked = theme == ElementTheme.Light;
            RadioBtnDarkTheme.IsChecked = theme == ElementTheme.Dark;
            RadioBtnDefaultTheme.IsChecked = theme == ElementTheme.Default;
        }

        private void RadioBtnDefaultTheme_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetTheme(ElementTheme.Default);
        }

        private void RadioBtnLightTheme_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetTheme(ElementTheme.Light);
        }

        private void RadioBtnDarkTheme_Checked(object sender, RoutedEventArgs e)
        {
            ThemeManager.Instance.SetTheme(ElementTheme.Dark);
        }

        // TODO: OneDrive Implementation
        // - Sign in to OneDrive
        // - PUT / GET User files (see README.md for more information about user-specific files)
        // OnLaunch() => If synchronization is enabled, get files from the Cloud if they're the most recent ones.
        // OnSuspending() => Save files on the cloud?
        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
