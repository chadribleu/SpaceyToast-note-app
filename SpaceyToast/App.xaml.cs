using SpaceyToast.Core.Services;
using SpaceyToast.Source;
using SpaceyToast.Source.User;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SpaceyToast
{
    sealed partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {

                }

                Window.Current.Content = rootFrame;
            }

            // extend application to title bar
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            var appView = ApplicationView.GetForCurrentView();
            appView.TitleBar.BackgroundColor = Colors.Transparent;
            appView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appView.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Load save data
            SaveDataManager<CurrentUserData>.Instance.SaveData = await SaveDataManager<CurrentUserData>.GetCurrent("user.json");

            // Update calendar infos
            CurrentCalendar refToCalendar = SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar;
            int currentYear = refToCalendar.CurrentYear;
            int startingYearCycle = refToCalendar.StartingYearCycle;
            int onFirstAppLaunchYear = refToCalendar.OnFirstLaunchYear;
            int maxYearRange = refToCalendar.PerCycleYearRange;
            int maxItem = refToCalendar.MaxItems;

            currentYear = DateTime.Now.Year;

            // if the app is launched for the first time, use the default settings
            if (SaveDataManager<CurrentUserData>.Instance.SaveData.IsFirstTime || startingYearCycle == -1)
            {
                onFirstAppLaunchYear = startingYearCycle = currentYear;
            }
            // How many years have passed since the last instance?
            if (currentYear + maxYearRange > startingYearCycle + refToCalendar.MaxItems)
            {
                startingYearCycle += maxYearRange;
                refToCalendar.ComboBoxSelectedIndex = maxItem - (currentYear - startingYearCycle);
            }
            else if (currentYear < startingYearCycle)
            {
                startingYearCycle = currentYear;
                refToCalendar.ComboBoxSelectedIndex = 0;
            }
            refToCalendar.CurrentYear = currentYear;
            refToCalendar.OnFirstLaunchYear = onFirstAppLaunchYear;
            refToCalendar.StartingYearCycle = startingYearCycle;
            await SaveDataManager<CurrentUserData>.Instance.UpdateLocalFile();

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                Window.Current.Activate();
            }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            SaveDataManager<CurrentUserData>.Instance.SaveData.IsFirstTime = false;
            await SaveDataManager<CurrentUserData>.Instance.UpdateLocalFile();

            // remove unused tags
            if (TagManifestManager.Instance != null)
            {
                var tagManifestData = await TagManifestManager.Instance.Get();
                var globalTags = await TagManagerService.Instance.GetGlobalTags();

                bool needUpdate = false;

                for (int i = 0; i < tagManifestData.Count - 1; ++i)
                {
                    for (int j = 0; j < tagManifestData[i].Tags.Count - 1; ++j)
                    {
                        // assuming the same number of tags are present in both sides, there is nothing to check
                        if (tagManifestData[i].Tags.Count == globalTags.Count) break;
                        
                        string current = tagManifestData[i].Tags[j];
                        if (!globalTags[i].Contains(current))
                        {
                            needUpdate = true;
                            tagManifestData[i].Tags.Remove(current);
                        }
                    }
                }

                if (needUpdate) await TagManifestManager.Instance.Update(tagManifestData);
            }

            deferral.Complete();
        }
    }
}
