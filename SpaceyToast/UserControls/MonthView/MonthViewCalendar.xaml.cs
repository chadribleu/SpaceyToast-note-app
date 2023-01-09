using SpaceyToast.Models;
using Windows.UI.Xaml.Controls;

namespace SpaceyToast.UserControls.MonthView
{
    public sealed partial class MonthViewCalendar : UserControl
    {
        public MonthViewCalendar()
        {
            InitializeComponent();
        }

        private void MonthViewGrid_ItemClick(object sender, ItemClickEventArgs e)
        {
            MonthViewInfo infos;
            infos.Month = Month;
            infos.Day = (sender as GridView).Items.IndexOf(e.ClickedItem) + 1;
            infos.Year = Year;

            OnDayClicked?.Invoke(this, new MonthCalendarItemClickedEventArgs(infos));
        }
    }
}
