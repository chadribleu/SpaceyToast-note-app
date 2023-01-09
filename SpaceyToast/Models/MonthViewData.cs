using SpaceyToast.Enums;
using SpaceyToast.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceyToast.Models
{
    public struct MonthViewInfo
    {
        public int Day;
        public int Year;
        public string Month;
    }

    public class MonthViewData : ViewModelBase
    {
        private string _month;
        public string Month
        {
            get { return _month; }
            set 
            { 
                _month = value;
                NotifyPropertyChanged(nameof(Month));
            }
        }

        private int _year;
        public int Year
        {
            get { return _year; }
            set
            {
                _year = value;
                NotifyPropertyChanged(nameof(Year));
            }
        }

        private List<int> _days;
        public List<int> Days
        {
            get
            {
                if (_days == null)
                {
                    _days = new List<int>();
                }
                return _days;
            }
            set
            {
                _days = value;
                NotifyPropertyChanged(nameof(Days));
            }
        }

        public MonthViewData(Months month, int year)
        {
            Month = month.ToString();
            Year = year;
            Days = Enumerable.Range(1, DateTime.DaysInMonth(year, (int)month)).ToList();
        }
    }
}
