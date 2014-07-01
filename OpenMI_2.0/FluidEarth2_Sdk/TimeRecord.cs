using System;
using System.Collections.Generic;
using System.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using System.Xml.Linq;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class TimeRecord<TType> : ICloneable
    {
        protected Time _time;
        protected TType[] _values;

        public TimeRecord()
        {}

        public TimeRecord(ITime time)
        {
            _time = new Time(time);
            _values = new TType[] {};
        }

        public TimeRecord(ITime time, TType[] values)
        {
            _time = new Time(time);
            _values = values;
        }

        public TimeRecord(ITime time, IEnumerable<TType> values)
        {
            _time = new Time(time);
            _values = values.ToArray();
        }

        public TimeRecord(TimeRecord<TType> record)
        {
            _time = record.Time;
            _values = record.Values;
        }

        public TimeRecord(ITime time, IBaseValueSet vs)
        {
            if (typeof(TType) != vs.ValueType)
                throw new Exception(string.Format("{0} != {1}",
                    typeof(TType).ToString(), vs.ValueType.ToString()));

            _time = new Time(time);
            _values = Utilities.ToList<TType>(vs)
                .ToArray();
        }

        public Time Time
        {
            get { return _time; }
            set { _time = value; }
        }

        public TType[] Values
        {
            get { return _values; }
            set { _values = value; }
        }

        public object Clone()
        {
            var r = new TimeRecord<TType>(_time);
            _values.CopyTo(r._values, 0);
            return r;
        }
    }
}
