using SpaceyToast.Core.Services;
using SpaceyToast.Enums;
using SpaceyToast.Models;
using SpaceyToast.Source;
using SpaceyToast.Source.Helpers;
using SpaceyToast.Source.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SpaceyToast.Views
{
    public sealed partial class CalendarPage : Page
    {
        private readonly ObservableCollection<MonthViewData> _CalendarDataList;
        private ObservableCollection<int> _AvailableYears;

        public CalendarPage()
        {
            InitializeComponent();
            _CalendarDataList = new ObservableCollection<MonthViewData>();
            MonthViewPanel.ItemsSource = _CalendarDataList;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            string username = await Helpers.GetUsername(UserInfo.FirstName);
            CalendarPageHeader.Text =
                string.Format(
                    (SaveDataManager<CurrentUserData>.Instance.SaveData.IsFirstTime ? "Welcome, " : "Welcome back, ") + "{0}.",
                    username);

            var l = Enumerable.Range(SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar.StartingYearCycle,
                SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar.MaxItems).ToList();
            _AvailableYears = new ObservableCollection<int>();
            l.ForEach(_AvailableYears.Add);

            for (int i = 1; i <= 12; i++)
            {
                _CalendarDataList.Add(new MonthViewData((Months)i,
                    SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar.CurrentYear));
            }

            // GUI update
            YearSelector.ItemsSource = _AvailableYears;
            YearSelector.SelectedIndex = SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar.ComboBoxSelectedIndex;
        }

        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            await SaveDataManager<CurrentUserData>.Instance.UpdateLocalFile();
        }

        private async void MonthViewCalendar_OnDayClicked(UserControls.MonthView.MonthViewCalendar sender, MonthCalendarItemClickedEventArgs args)
        {
            List<TagManifestData> dataList = await TagManifestManager.Instance.Get();
            DateTime dateTime = new DateTime(args.CalendarInfo.Year, (int)Enum.Parse<Months>(args.CalendarInfo.Month), args.CalendarInfo.Day);

            int dataIndex = dataList.FindIndex(d => d.DateTime == dateTime);
            if (dataIndex == -1)
            {
                TagManifestData data = new TagManifestData("", dateTime, "", string.Empty, new List<string>());
                Frame.Navigate(typeof(DrawingArea), data);
            }
            else
            {
                Frame.Navigate(typeof(DrawingArea), dataList[dataIndex]);
            }
        }

        private void YearSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = (sender as ComboBox).SelectedIndex;

            if (selectedIndex != SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar.ComboBoxSelectedIndex)
            {
                int outValue = int.MinValue;
                if (!int.TryParse(e.AddedItems[0].ToString(), out outValue))
                    return;

                SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar.CurrentYear = outValue;
                SaveDataManager<CurrentUserData>.Instance.SaveData.CurrentCalendar.ComboBoxSelectedIndex = (sender as ComboBox).SelectedIndex;
                this.Frame.Navigate(typeof(CalendarPage));
            }
        }
    }
}
