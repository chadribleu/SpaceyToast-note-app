using System;

namespace SpaceyToast.Models
{
    public class MonthCalendarItemClickedEventArgs : EventArgs
    {
        private MonthViewInfo _mvi;
        public MonthViewInfo CalendarInfo
        {
            get { return _mvi; }
        }

        public MonthCalendarItemClickedEventArgs(MonthViewInfo monthViewInfo)
        {
            _mvi = monthViewInfo;
        }
    }
}
