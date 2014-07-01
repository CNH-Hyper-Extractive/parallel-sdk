
using System;

namespace FluidEarth2.Sdk
{
    public class TimeInterval
    {
        uint _days;
        uint _hours;
        uint _minutes;
        double _seconds;
        bool _millisecondLimit = true;
        bool _allowZero = false;

        public static TimeInterval FromDays(uint days)
        {
            return new TimeInterval(days, 0, 0, 0.0);
        }

        public static TimeInterval FromHours(uint hours)
        {
            return new TimeInterval(0, hours, 0, 0.0);
        }

        public static TimeInterval FromMinutes(uint minutes)
        {
            return new TimeInterval(0, 0, minutes, 0.0);
        }

        public static TimeInterval FromSeconds(double seconds)
        {
            return new TimeInterval(0, 0, 0, seconds);
        }

        public TimeInterval()
        { 
        }

        public TimeInterval(uint days, uint hours, uint minutes, double seconds)
        {
            Contract.Requires(hours < 24, "hours < 24");
            Contract.Requires(minutes < 60, "minutes < 60");
            Contract.Requires(seconds < 60.0, "seconds < 60.0");

            _days = days;
            _hours = hours;
            _minutes = minutes;
            _seconds = seconds;

            RoundSeconds();
        }

        public TimeInterval(double days)
        {
            _days = (uint)days;          
            _seconds = 24.0 * 60.0 * 60.0 * (days - _days);

            _hours = (uint)(_seconds / (60.0 * 60.0));
            _seconds -= _hours * 60.0 * 60.0;

            _minutes = (uint)(_seconds / 60.0);

            _seconds -= 60.0 * _minutes;

            RoundSeconds();
        }

        public uint Days
        {
            get { return _days; }
        }

        public uint Minutes
        {
            get { return _minutes; }
        }

        public uint Hours
        {
            get { return _hours; }
        }

        public double Seconds
        {
            get { return _seconds; }
        }

        public bool AllowZero
        {
            get { return _allowZero; }
            set { _allowZero = value; }
        }

        public bool MillisecondLimit
        {
            get { return _millisecondLimit; }
            set { _millisecondLimit = value; }
        }

        public double TotalSeconds
        {
            get { return _seconds + 60.0 * (_minutes + 60.0 * (_hours + 24.0 * _days)); }
        }

        public double TotalDays
        {
            get { return TotalSeconds / (24.0 * 60.0 * 60.0); }
        }

        void RoundSeconds()
        {
            if (_millisecondLimit)
            {
                _seconds = Math.Round(1.0e3 * _seconds) * 1.0e-3;

                if (_seconds == 60.0)
                {
                    ++_minutes;
                    _seconds = 0;
                }
            }
        }

        public override string ToString()
        {
            return string.Format(_millisecondLimit
                ? "{0} days + {1}:{2}:{3:0.000}" 
                : "{0} days + {1}:{2}:{3}",
                _days, _hours, _minutes, _seconds); 
        }
    }
}
