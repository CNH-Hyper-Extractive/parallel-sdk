#if DEPRECATED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTime<TType> : ValueSetConverterBase, IValueSetConverterTime
        where TType : EngineValueTypes.IEngineType
    {
        protected enum Counts { ToEngine = 0, CacheEngineValues, GetValueSetLatest, GetValueSetAt, GetExtrapolatedValueSetAt, CacheMaxSize, LAST };

        protected int[] _counts = new int[(int)Counts.LAST];
        protected List<TimeRecord<TType>> _cache;
        protected Interpolation _interpolation;

        public enum Interpolation { NoneUseLast = 0, Lower, Upper, Mean, Linear, LinearNoExtrapolation, }


        public ValueSetConverterTime()
        {
        }

        public ValueSetConverterTime(string engineVariable, IValueSetPacking packing, Interpolation interpolation)
            : base(engineVariable, packing)
        {
            _interpolation = interpolation;
            _cache = new List<TimeRecord<TType>>();
        }

        public ValueSetConverterTime(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override IBaseValueSet GetValueSetLatest()
        {
            ++_counts[(int)Counts.GetValueSetLatest];

            if (_cache.Count == 0)
                return null;

            List<TimeRecord<TType>> latest = new List<TimeRecord<TType>>();

            if (_cache.Count > 0)
                latest.Add(_cache.Last());

            return Utilities.ValueSet<TType>(latest, PackingInformation);
        }

        public bool CanGetValueSetWithoutExtrapolationAt(ITime at)
        {
            if (at == null)
                throw new Exception("at == null");

            if (_cache == null || _cache.Count == 0)
                return false;

            return Time.EndTime(_cache.Last().Time) >= Time.EndTime(at);
        }

        public bool CanGetValueSetWithoutExtrapolationAt(ITimeSet at)
        {
            return CanGetValueSetWithoutExtrapolationAt(at.Times.LastOrDefault());
        }

        public virtual IBaseValueSet GetValueSetAt(ITime at)
        {
            ++_counts[(int)Counts.GetValueSetAt];
            return GetValueSetAt(at, _interpolation);
        }

        public virtual IBaseValueSet GetValueSetAt(ITimeSet at)
        {
            ++_counts[(int)Counts.GetValueSetAt];
            return GetValueSetAt(at, _interpolation);
        }

        public virtual IBaseValueSet GetExtrapolatedValueSetAt(ITime at)
        {
            ++_counts[(int)Counts.GetExtrapolatedValueSetAt];
            return GetValueSetAt(at, Interpolation.Linear);
        }

        public virtual IBaseValueSet GetExtrapolatedValueSetAt(ITimeSet at)
        {
            ++_counts[(int)Counts.GetExtrapolatedValueSetAt];
            return GetValueSetAt(at, Interpolation.Linear);
        }

        IBaseValueSet GetValueSetAt(ITime at, Interpolation interpolation)
        {
            if (_cache.Count == 0)
                return null;

            List<TimeRecord<TType>> records = new List<TimeRecord<TType>>();

            BaseExchangeItemChangeEventArgs args = null;

            if (HasItemChangedEvents)
                args = new BaseExchangeItemChangeEventArgs(
                    ExchangeItem,
                    string.Format("{0} - Get IBaseValueSet", EngineVariable));

            records.Add(InterpolateTime(at, interpolation, _cache, args));

            if (HasItemChangedEvents)
                SendItemChangedEvent(args);

            return Utilities.ValueSet<TType>(records, PackingInformation);
        }

        IBaseValueSet GetValueSetAt(ITimeSet at, Interpolation interpolation)
        {
            if (_cache.Count == 0)
                return null;

            List<TimeRecord<TType>> records = new List<TimeRecord<TType>>();

            BaseExchangeItemChangeEventArgs args = null;

            if (HasItemChangedEvents)
                args = new BaseExchangeItemChangeEventArgs(
                    ExchangeItem,
                    string.Format("{0} - Get IBaseValueSet", EngineVariable));

            foreach (ITime time in at.Times)
                records.Add(InterpolateTime(time, interpolation, _cache, args));

            if (HasItemChangedEvents)
                SendItemChangedEvent(args);

            return Utilities.ValueSet<TType>(records, PackingInformation);
        }

        static TimeRecord<TType> InterpolateTime(ITime at, Interpolation interpolation, List<TimeRecord<TType>> _cache)
        {
            return InterpolateTime(at, interpolation, _cache, null);
        }

        static TimeRecord<TType> InterpolateTime(ITime at, Interpolation interpolation, List<TimeRecord<TType>> _cache, BaseExchangeItemChangeEventArgs args)
        {
            if (_cache.Count == 0)
                throw new Exception("No time records to interpolate");

            if (interpolation == Interpolation.NoneUseLast)
            {
                if (args != null)
                    args.Add("No interpolation requested, returning last values in cache at " + _cache.Last().Time.ToString());

                return new TimeRecord<TType>(at, _cache.Last().Values);
            }

            if (args != null)
                args.Add("Temporal interpolation request at " + at.ToString());

            if (_cache.Count == 1)
            {
                if (args != null)
                    args.Add("Single time record only in cache, no interpolation, used values at " + _cache[0].Time.ToString());

                return new TimeRecord<TType>(at, _cache[0].Values);
            }

            TimeRecord<TType> record;

            int nAbove = _cache.FindIndex(r => r.Time.StampAsModifiedJulianDay >= at.StampAsModifiedJulianDay);

            if (nAbove == 0)
            {
                if (args != null)
                    args.Add("All time records in cache above requested time, no interpolation, used values at first time above at " + _cache[0].Time.ToString());

                return new TimeRecord<TType>(at, _cache[0].Values);
            }

            if (interpolation == Interpolation.Lower)
            {
                record = new TimeRecord<TType>(at, _cache[nAbove - 1].Values);

                if (args != null)
                    args.Add(string.Format("Interpolation.Lower, cache index {0}, time {1}", 
                        nAbove - 1, _cache[nAbove - 1].Time.ToString()));
            }
            else if (interpolation == Interpolation.Upper
                || typeof(TType) != typeof(double))
            {
                record = new TimeRecord<TType>(at, _cache[nAbove].Values);

                if (args != null)
                    args.Add(string.Format("Interpolation.Lower, cache index {0}, time {1}",
                        nAbove, _cache[nAbove].Time.ToString()));
            }
            else
            {
                double t;

                if (interpolation == Interpolation.Mean)
                {
                    t = 0.5;

                    if (args != null)
                        args.Add("Interpolation.Mean, t = 0.5");
                }
                else
                    t = (at.StampAsModifiedJulianDay - _cache[nAbove - 1].Time.StampAsModifiedJulianDay)
                        / (_cache[nAbove].Time.StampAsModifiedJulianDay - _cache[nAbove - 1].Time.StampAsModifiedJulianDay);

                if (interpolation == Interpolation.LinearNoExtrapolation && t > 1.0)
                {
                    record = new TimeRecord<TType>(at, _cache[nAbove].Values);

                    if (args != null)
                        args.Add(string.Format("Interpolation.LinearNoExtrapolation, t = {0} > 0 so t = 1.0, cache index {1}, time {2}",
                            t, nAbove, _cache[nAbove].Time.ToString()));
                }
                else
                {
                    record = new TimeRecord<TType>(at, LinearInterpolation(_cache[nAbove - 1].Values, _cache[nAbove].Values, t).ToArray());

                    if (args != null)
                        args.Add(string.Format("Interpolation.Linear, t = {0}, cache indexs {1}...{2}, times {3}...{4}",
                            t, nAbove - 1, nAbove, _cache[nAbove - 1].Time.ToString(), _cache[nAbove].Time.ToString()));
                }
            }

            return record;
        }

        static IEnumerable<TType> LinearInterpolation(IEnumerable<TType> below, IEnumerable<TType> above, double factor)
        {
            /*
            if (typeof(TType) == typeof(EngineValueTypes.String))
                return EngineValueTypes.String.LinearInterpolation(below, above, factor);
            if (typeof(TType) == typeof(EngineValueTypes.Boolean))
                return EngineValueTypes.Boolean.LinearInterpolation(below, above, factor);
            if (typeof(TType) == typeof(EngineValueTypes.Int32))
                return EngineValueTypes.Int32.LinearInterpolation(below, above, factor);
            if (typeof(TType) == typeof(EngineValueTypes.Double))
                return EngineValueTypes.Double.LinearInterpolation(below, above, factor);
            if (typeof(TType) == typeof(EngineValueTypes.Double2d))
                return EngineValueTypes.Double2d.LinearInterpolation(below, above, factor);
            if (typeof(TType) == typeof(EngineValueTypes.Double3d))
                return EngineValueTypes.Double3d.LinearInterpolation(below, above, factor);
            */

            throw new NotImplementedException(typeof(TType).ToString());
        }

        public override void ToEngine(IEngine iEngine, IBaseValueSet iValueSet)
        {
            ++_counts[(int)Counts.ToEngine];

            IEngineTime iEngineTime = iEngine as IEngineTime;

            if (iEngineTime == null)
                throw new Exception("IEngine not IEngineTime");
            
            if (iValueSet is IValueSetSpaceTime<TType>)
                _cache = ((IValueSetSpaceTime<TType>)iValueSet)
                    .Records
                    .Select(r => (TimeRecord<TType>)r.Clone())
                    .ToList();
            else
                throw new NotImplementedException("ToEngine(...) for type: " + typeof(TType).ToString());

            if (_cache.Count < 1)
                throw new Exception("Input not provided with any required input values");

            if (_cache.Count > 1)
                throw new Exception("Engine cannot deal with multiple input time periods");

            ITime engineTime = new Time(iEngineTime.GetCurrentTime());
            TimeRecord<TType> record = _cache.Last();

            if (!engineTime.Equals(record.Time))
                throw new Exception(string.Format("Input not provided with required input values; expected {0} received {1} ",
                    engineTime.ToString(), record.Time.ToString()));

            if (HasItemChangedEvents)
                SendItemChangedEvent(string.Format("{0}.ToEngine({1})", EngineVariable, engineTime.ToString()));

            // IEngine is simple C style API so generics break down here

            if (typeof(TType) == typeof(EngineValueTypes.String))
            {
                var values = EngineValueTypes.String.ToStrings(record.Values.Cast<EngineValueTypes.String>());
                iEngine.SetStrings(EngineVariable, Convert.ToString(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
            {
                var values = EngineValueTypes.Boolean.ToBools(record.Values.Cast<EngineValueTypes.Boolean>());
                iEngine.SetBooleans(EngineVariable, Convert.ToBoolean(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Int32))
            {
                var values = EngineValueTypes.Int32.ToInt32s(record.Values.Cast<EngineValueTypes.Int32>());
                iEngine.SetInt32s(EngineVariable, Convert.ToInt32(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double))
            {
                var values = EngineValueTypes.Double.ToDoubles(record.Values.Cast<EngineValueTypes.Double>());
                iEngine.SetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
            {
                var values = EngineValueTypes.Double2d.ToDoubles(record.Values.Cast<EngineValueTypes.Double2d>());
                iEngine.SetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
            {
                var values = EngineValueTypes.Double3d.ToDoubles(record.Values.Cast<EngineValueTypes.Double3d>());
                iEngine.SetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue), values.ToArray());
            }
            else
                throw new NotImplementedException("ToEngine(...) for type: " + typeof(TType).ToString());
        }

        public override void CacheEngineValues(IEngine iEngine)
        {
            ++_counts[(int)Counts.CacheEngineValues];

            IEngineTime iEngineTime = iEngine as IEngineTime;

            if (iEngineTime == null)
                throw new Exception("IEngine not IEngineTime");

            ITime time = new Time(iEngineTime.GetCurrentTime());

            if (_cache.Count > 0)
            {
                if (time.StampAsModifiedJulianDay < _cache.Last().Time.StampAsModifiedJulianDay)
                    throw new Exception(string.Format("Engine moving back in time, {0} < {1}",
                        time.ToString(), _cache.Last().Time.ToString()));
                else if (time.StampAsModifiedJulianDay == _cache.Last().Time.StampAsModifiedJulianDay)
                    _cache.RemoveAt(_cache.Count - 1);
            }

            // IEngine is simple C style API so generics break down here

            TimeRecord<TType> record;

            if (typeof(TType) == typeof(EngineValueTypes.String))
            {
                var values = iEngine.GetStrings(EngineVariable, Convert.ToString(PackingInformation.MissingValue));
                record = new TimeRecord<TType>(time, EngineValueTypes.String.FromStrings(values).Cast<TType>().ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
            {
                var values = iEngine.GetBooleans(EngineVariable, Convert.ToBoolean(PackingInformation.MissingValue));
                record = new TimeRecord<TType>(time, EngineValueTypes.Boolean.FromBools(values).Cast<TType>().ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Int32))
            {
                var values = iEngine.GetInt32s(EngineVariable, Convert.ToInt32(PackingInformation.MissingValue));
                record = new TimeRecord<TType>(time, EngineValueTypes.Int32.FromInt32s(values).Cast<TType>().ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double))
            {
                var values = iEngine.GetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue));
                record = new TimeRecord<TType>(time, EngineValueTypes.Double.FromDoubles(values).Cast<TType>().ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
            {
                var values = iEngine.GetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue));
                record = new TimeRecord<TType>(time, EngineValueTypes.Double2d.FromDoubles(values).Cast<TType>().ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
            {
                var values = iEngine.GetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue));
                record = new TimeRecord<TType>(time, EngineValueTypes.Double3d.FromDoubles(values).Cast<TType>().ToArray());
            }
            else
                throw new NotImplementedException("CacheEngineValues(...) for type: " + typeof(TType).ToString());

            if (HasItemChangedEvents)
                SendItemChangedEvent(string.Format("Cached from engine at {0}", time.ToString()));

            _cache.Add(record);

            if (_counts[(int)Counts.CacheMaxSize] < _cache.Count)
                _counts[(int)Counts.CacheMaxSize] = _cache.Count;
        }

        public override void EmptyCaches(ITime upto)
        {
            int count = _cache.Count;

            // Leave minimum of two records so linear interpolation is always possible

            while (_cache.Count > 1 && _cache[0].Time.StampAsModifiedJulianDay < upto.StampAsModifiedJulianDay)
                _cache.RemoveAt(0);

            if (HasItemChangedEvents && _cache.Count != count)
                SendItemChangedEvent(string.Format("Cleared cache upto {0}, removed {1} records",
                    upto.ToString(), count - _cache.Count));
        }

        public override IBaseValueSet GetCache()
        {
            return Utilities.ValueSet<TType>(_cache, PackingInformation);
        }

        public override object Clone()
        {
            ValueSetConverterTime<TType> c = PackingInformation.ElementValueCountConstant
                ? new ValueSetConverterTime<TType>(EngineVariable, PackingInformation, _interpolation)
                : new ValueSetConverterTime<TType>(EngineVariable, PackingInformation, _interpolation);

            c._cache = new List<TimeRecord<TType>>();

            foreach (TimeRecord<TType> r in _cache)
                c._cache.Add(new TimeRecord<TType>(r));

            return c;
        }

        /// <summary>
        /// The times currently cached/available
        /// </summary>
        public ITimeSet TimeSet
        {
            get 
            { 
                if (_cache.Count < 1)
                    return null;

                var horizon = new Time(
                    _cache.First().Time.StampAsModifiedJulianDay, 
                    _cache.Last().Time.StampAsModifiedJulianDay + _cache.Last().Time.DurationInDays);

                TimeSet timeSet = new TimeSet(horizon);
                timeSet.SetTimes(_cache.Select(r => r.Time).Cast<ITime>());

                return timeSet; 
            }
        }

        public override string DiagnositicSummary()
        {
            string component = ExchangeItem.Component != null ? ExchangeItem.Component.Caption : "Orphan";

            StringBuilder sb = new StringBuilder("ValueSetConvertor Diagnostic Summary");
            sb.AppendLine();
            sb.AppendLine(string.Format("\t'{0}'.'{1}' ({2})", component, ExchangeItem.Caption, EngineVariable));
            sb.AppendLine(string.Format("\t\tToEngine: {0}", _counts[(int)Counts.ToEngine]));
            sb.AppendLine(string.Format("\t\tCacheEngineValues: {0}", _counts[(int)Counts.CacheEngineValues]));
            sb.AppendLine(string.Format("\t\tGetValueSetLatest: {0}", _counts[(int)Counts.GetValueSetLatest]));
            sb.AppendLine(string.Format("\t\tGetValueSetAt: {0}", _counts[(int)Counts.GetValueSetAt]));
            sb.AppendLine(string.Format("\t\tGetExtrapolatedValueSetAt: {0}", _counts[(int)Counts.GetExtrapolatedValueSetAt]));
            sb.AppendLine(string.Format("\t\tCacheMaxSize: {0}", _counts[(int)Counts.CacheMaxSize]));

            return sb.ToString();
        }

        public new const string XName = "ValueSetConverterTime";
        public const string XInterpolation = "Interpolation";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            _interpolation = (Interpolation)Enum.Parse(typeof(Interpolation), 
                Utilities.Xml.GetAttribute(xElement, XInterpolation));

            _cache = new List<TimeRecord<TType>>();

            ITime time;
            TType[] values;

            foreach (var record in xElement.Elements("Record"))
            {
                time = Persistence.Time.Parse(record, accessor);

                if (typeof(TType) == typeof(EngineValueTypes.String))
                    values = EngineValueTypes.String.FromCsv(record.Value).Cast<TType>().ToArray();
                else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
                    values = EngineValueTypes.Boolean.FromCsv(record.Value).Cast<TType>().ToArray();
                else if (typeof(TType) == typeof(EngineValueTypes.Int32))
                    values = EngineValueTypes.Int32.FromCsv(record.Value).Cast<TType>().ToArray();
                else if (typeof(TType) == typeof(EngineValueTypes.Double))
                    values = EngineValueTypes.Double.FromCsv(record.Value).Cast<TType>().ToArray();
                else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
                    values = EngineValueTypes.Double2d.FromCsv(record.Value).Cast<TType>().ToArray();
                else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
                    values = EngineValueTypes.Double3d.FromCsv(record.Value).Cast<TType>().ToArray();
                else
                    throw new NotImplementedException(typeof(TType).ToString());

                _cache.Add(new TimeRecord<TType>(time, values));
            }
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName, 
                new XAttribute(XInterpolation, _interpolation.ToString()),                    
                base.Persist(accessor));

            string csv = null;

            foreach (var record in _cache)
            {
                if (typeof(TType) == typeof(EngineValueTypes.String))
                    csv = EngineValueTypes.String.ToCsv(record.Values.Cast<EngineValueTypes.String>());
                else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
                    csv = EngineValueTypes.Boolean.ToCsv(record.Values.Cast<EngineValueTypes.Boolean>());
                else if (typeof(TType) == typeof(EngineValueTypes.Int32))
                    csv = EngineValueTypes.Int32.ToCsv(record.Values.Cast<EngineValueTypes.Int32>());
                else if (typeof(TType) == typeof(EngineValueTypes.Double))
                    csv = EngineValueTypes.Double.ToCsv(record.Values.Cast<EngineValueTypes.Double>());
                else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
                    csv = EngineValueTypes.Double2d.ToCsv(record.Values.Cast<EngineValueTypes.Double2d>());
                else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
                    csv = EngineValueTypes.Double3d.ToCsv(record.Values.Cast<EngineValueTypes.Double3d>());
                else
                    throw new NotImplementedException(typeof(TType).ToString());

                xml.Add(new XElement("Record",
                    Persistence.Time.Persist(record.Time, accessor),
                    csv));
            }

            return xml;
        }
    }
}

#endif