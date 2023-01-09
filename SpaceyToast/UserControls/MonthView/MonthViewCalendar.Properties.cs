using SpaceyToast.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;

namespace SpaceyToast.UserControls.MonthView
{
    public partial class MonthViewCalendar
    {
        public int Year
        {
            get { return (int)GetValue(yearProperty); }
            set { SetValue(yearProperty, value); }
        }
        public static readonly DependencyProperty yearProperty = DependencyProperty.Register("Year", typeof(int),
            typeof(MonthViewCalendar), new PropertyMetadata(2000, new PropertyChangedCallback(OnYearChanged)));
        private static void OnYearChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as MonthViewCalendar).OnYearChanged(e);
        }
        // update days count
        private void OnYearChanged(DependencyPropertyChangedEventArgs e)
        {
            SpaceyToast.Enums.Months myValue;
            Enum.TryParse<SpaceyToast.Enums.Months>(Month, true, out myValue);
            Days = Enumerable.Range(1, DateTime.DaysInMonth((int)e.NewValue, (int)myValue)).ToList(); 
        }

        public string Month
        {
            get { return (string)GetValue(monthProperty); }
            set { SetValue(monthProperty, value); }
        }
        public static readonly DependencyProperty monthProperty = DependencyProperty.Register("Month", typeof(string),
            typeof(MonthViewCalendar), new PropertyMetadata("January", new PropertyChangedCallback(OnMonthChanged)));
        private static void OnMonthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthViewCalendar UserControl1Control = d as MonthViewCalendar;
            UserControl1Control.OnMonthChanged(e);
        }
        private void OnMonthChanged(DependencyPropertyChangedEventArgs e)
        {
            CurrentMonthName.Text = e.NewValue.ToString();
        }

        public List<int> Days
        {
            get { return (List<int>)GetValue(daysProperty); }
            set { SetValue(daysProperty, value); }
        }
        public static readonly DependencyProperty daysProperty = DependencyProperty.Register("Days", typeof(List<int>), typeof(MonthViewCalendar),
            new PropertyMetadata(new List<int>(), new PropertyChangedCallback(OnDaysChanged)));

        private static void OnDaysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MonthViewCalendar UserControl1Control = d as MonthViewCalendar;
            UserControl1Control.OnDaysChanged(e);
        }
        private void OnDaysChanged(DependencyPropertyChangedEventArgs e)
        {
            MonthViewGrid.ItemsSource = (List<int>)e.NewValue;
        }

        public delegate void OnDaysSelectedEventHandler(MonthViewCalendar sender, MonthCalendarItemClickedEventArgs args);
        public event OnDaysSelectedEventHandler OnDayClicked;
    }
}
