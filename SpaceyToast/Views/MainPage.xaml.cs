using SpaceyToast.Source;
using SpaceyToast.Source.User;
using SpaceyToast.Views;
using SpaceyToast.Views.TagsPage;
using System;
using Windows.UI.Xaml;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewBackRequestedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs;
using NavigationViewDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using Page = Windows.UI.Xaml.Controls.Page;

namespace SpaceyToast
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
            Window.Current.SetTitleBar(CoreApplicationTitleBar);

            (Window.Current.Content as FrameworkElement).RequestedTheme = SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentTheme;
        }

        public bool NavView_NavigateTo(string tag, Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo transition)
        {
            Type page = null;

            switch (tag)
            {
                case "Settings":
                    page = typeof(SettingsPage);
                    break;
                case "Calendar":
                    page = typeof(CalendarPage);
                    break;
                case "Tags":
                    page = typeof(Host);
                    break;
            }

            if (MainFrame.SourcePageType != page)
            {
                MainFrame.Navigate(page, null, transition);
                MainNavView.Header = null;
                return true;
            }
            return false;
        }

        private void MainNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            NavView_NavigateTo(args.InvokedItemContainer.Tag.ToString(), 
                new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void MainNavView_Loaded(object sender, RoutedEventArgs e)
        {
            MainNavView.SelectedItem = 0;
            NavView_NavigateTo("Calendar", new Windows.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private void MainNavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (!MainFrame.CanGoBack)
            {
                return;
            }

            // Don't go back if the nav pane is overlayed.
            if (MainNavView.IsPaneOpen 
                && (MainNavView.DisplayMode == NavigationViewDisplayMode.Compact || MainNavView.DisplayMode == NavigationViewDisplayMode.Minimal))
            {
                return;
            }

            MainFrame.GoBack();
        }

        private void MainFrame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (MainFrame.SourcePageType == typeof(CalendarPage))
            {
                MainNavView.SelectedItem = CalendarNavViewItem;
            }
            else if (MainFrame.SourcePageType == typeof(Host) || MainFrame.SourcePageType == typeof(ContentView))
            {
                MainNavView.SelectedItem = TagsNavViewItem;
            }
            else if (MainFrame.SourcePageType == typeof(SettingsPage))
            {
                MainNavView.SelectedItem = SettingsNavViewItem;
            }
        }
    }
}