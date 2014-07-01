using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public abstract class ValueSetConverterTimeRecordBase<TType>
        : ValueSetConverterBase<TType>, IValueSetConverterTimeRecord<TType>
    {
        protected enum Counts { GetValueSetLatest = 0, GetValueSetAt, CacheMaxSize, LAST };

        /// <summary>
        /// Temporal interpolation is potentially required when a component is requested for values and
        /// cannot supply them as it is waiting for data from another component. In OpenMI pull runs
        /// this cannot be allowed, the component must always provide values when requested. Hence,
        /// the component will have to provide values based on previous values it has stored. 
        /// </summary>
        public enum InterpolationTemporal 
        { 
            /// <summary>
            /// No interpolation, just return last value stored from last update.
            /// </summary>
            NoneUseLast = 0, 
            /// <summary>
            /// Return lowest value of bracketing values.
            /// </summary>
            Lower, 
            /// <summary>
            /// Return highest value of bracketing values.
            /// </summary>
            Upper, 
            /// <summary>
            /// Return arithmetic mean of bracketing values.
            /// </summary>
            Mean, 
            /// <summary>
            /// Linear interpolate between bracketing values, extrapolate in time if required.
            /// </summary>
            Linear, 
            /// <summary>
            /// Linear interpolate between bracketing values, if extrapolation would be required at as for Lower.
            /// </summary>
            LinearNoExtrapolation, 
        }

        protected int[] _counts = new int[(int)Counts.LAST];
        protected InterpolationTemporal _interpolation = InterpolationTemporal.NoneUseLast;

        protected List<TimeRecord<TType>> _cache = new List<TimeRecord<TType>>();

        public ValueSetConverterTimeRecordBase()
        { }

        public ValueSetConverterTimeRecordBase(InterpolationTemporal interpolation)
        {
            _interpolation = interpolation;
        }

        public ValueSetConverterTimeRecordBase(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public virtual IEnumerable<TType> LinearInterpolation(TimeRecord<TType> below, TimeRecord<TType> above, double factor)
        {
            Debug.Assert(_interpolation == InterpolationTemporal.NoneUseLast);
            throw new Exception("No temporal interpolation available for this TType");
        }

        public override IBaseValueSet GetValueSetLatest()
        {
            ++_counts[(int)Counts.GetValueSetLatest];

            if (_cache.Count == 0)
                return null;

            return ToValueSet(_cache.Last().Values);
        }

        public ITimeSet TimeSet
        {
            get
            {
                if (_cache.Count < 1)
                    return null;

                var horizon = new Time(
                    _cache.First().Time.StampAsModifiedJulianDay,
                    _cache.Last().Time.StampAsModifiedJulianDay + _cache.Last().Time.DurationInDays);

                var timeSet = new TimeSet(horizon);
                timeSet.SetTimes(_cache.Select(r => r.Time).Cast<ITime>());

                return timeSet;
            }
        }

        public virtual bool CanGetValueSetWithoutExtrapolationAt(ITime at)
        {
            if (at == null)
                throw new Exception("at == null");

            if (_cache == null || _cache.Count == 0)
                return false;

            return Time.EndTime(_cache.Last().Time) >= Time.EndTime(at);
        }

        public virtual bool CanGetValueSetWithoutExtrapolationAt(ITimeSet at)
        {
            return CanGetValueSetWithoutExtrapolationAt(at.Times.LastOrDefault());
        }

        public virtual ITimeSpaceValueSet GetValueSetAt(ITime at)
        {
            ++_counts[(int)Counts.GetValueSetAt];

            List<string> eventArgMessages = new List<string>();

            var record = GetRecordAt(at, eventArgMessages);

            if (HasItemChangedEvents)
            {
                var args = new BaseExchangeItemChangeEventArgs(ExchangeItem,
                    string.Format("{0}.Convertor.GetValueSetAt({1})",
                        ExchangeItem.Caption, at.ToString()));

                args.Messages.AddRange(eventArgMessages);

                SendItemChangedEvent(args);
            }

            return new ValueSetTimeRecord<TType>(record);
        }

        public virtual ITimeSpaceValueSet GetValueSetAt(ITimeSet at)
        {
            ++_counts[(int)Counts.GetValueSetAt];

            List<string> eventArgMessages = new List<string>();
            
            var records = at
                .Times
                .Select(t => GetRecordAt(t, eventArgMessages));

            if (HasItemChangedEvents)
            {
                var args = new BaseExchangeItemChangeEventArgs(ExchangeItem,
                    string.Format("{0}.Convertor.GetValueSetAt({1})",
                        ExchangeItem.Caption, ((at.Times != null && at.Times.Count > 0) ?
                            at.Times[0].StampAsModifiedJulianDay.ToString() : at.ToString())));

                args.Messages.AddRange(eventArgMessages);

                SendItemChangedEvent(args);
            }

            return new ValueSetTimeRecord<TType>(records);
        }

        TimeRecord<TType> InterpolateTime(ITime at, InterpolationTemporal interpolation, List<string> eventArgMessages)
        {
            if (_cache.Count == 0)
                throw new Exception("No time records to interpolate");

            if (interpolation == InterpolationTemporal.NoneUseLast)
            {
                eventArgMessages.Add(
                    "No interpolation requested, returning last values in cache at " 
                    + _cache.Last().Time.ToString());

                return new TimeRecord<TType>(at, _cache.Last().Values);
            }

            eventArgMessages.Add("Temporal interpolation request at " + at.ToString());

            if (_cache.Count == 1)
            {
                eventArgMessages.Add(
                    "Single time record only in cache, no interpolation, used values at " 
                    + _cache[0].Time.ToString());

                return new TimeRecord<TType>(at, _cache[0].Values);
            }

            TimeRecord<TType> record;

            int nAbove = _cache.FindIndex(r => r.Time.StampAsModifiedJulianDay >= at.StampAsModifiedJulianDay);

            if (nAbove == 0)
            {
                eventArgMessages.Add(
                    "All time records in cache above requested time, no interpolation, used values at first time above at " 
                    + _cache[0].Time.ToString());

                return new TimeRecord<TType>(at, _cache[0].Values);
            }

            if (interpolation == InterpolationTemporal.Lower)
            {
                record = new TimeRecord<TType>(at, _cache[nAbove - 1].Values);

                eventArgMessages.Add(string.Format(
                    "Interpolation.Lower, cache index {0}, time {1}",
                        nAbove - 1, _cache[nAbove - 1].Time.ToString()));
            }
            else if (interpolation == InterpolationTemporal.Upper
                || typeof(TType) != typeof(double))
            {
                record = new TimeRecord<TType>(at, _cache[nAbove].Values);

                eventArgMessages.Add(string.Format(
                    "Interpolation.Lower, cache index {0}, time {1}",
                        nAbove, _cache[nAbove].Time.ToString()));
            }
            else
            {
                double t;

                if (interpolation == InterpolationTemporal.Mean)
                {
                    t = 0.5;

                    eventArgMessages.Add("Interpolation.Mean, t = 0.5");
                }
                else
                    t = (at.StampAsModifiedJulianDay - _cache[nAbove - 1].Time.StampAsModifiedJulianDay)
                        / (_cache[nAbove].Time.StampAsModifiedJulianDay - _cache[nAbove - 1].Time.StampAsModifiedJulianDay);

                if (interpolation == InterpolationTemporal.LinearNoExtrapolation && t > 1.0)
                {
                    record = new TimeRecord<TType>(at, _cache[nAbove].Values);

                    eventArgMessages.Add(string.Format(
                        "Interpolation.LinearNoExtrapolation, t = {0} > 0 so t = 1.0, cache index {1}, time {2}",
                            t, nAbove, _cache[nAbove].Time.ToString()));
                }
                else
                {
                    record = new TimeRecord<TType>(at, LinearInterpolation(_cache[nAbove - 1], _cache[nAbove], t).ToArray());

                    eventArgMessages.Add(string.Format(
                        "Interpolation.Linear, t = {0}, cache indexs {1}...{2}, times {3}...{4}",
                            t, nAbove - 1, nAbove, _cache[nAbove - 1].Time.ToString(), _cache[nAbove].Time.ToString()));
                }
            }

            return record;
        }

        public override void EmptyCaches(ITime upto)
        {
            int count = _cache.Count;

            // Leave minimum of two records so linear interpolation is always possible

            while (_cache.Count > 2 && _cache[0].Time.StampAsModifiedJulianDay < upto.StampAsModifiedJulianDay)
                _cache.RemoveAt(0);

            if (HasItemChangedEvents && _cache.Count != count)
                SendItemChangedEvent(string.Format("Cleared cache upto {0}, removed {1} records",
                    upto.ToString(), count - _cache.Count));
        }


        public virtual void CacheRecords(IEnumerable<TimeRecord<TType>> values)
        {
            _cache.AddRange(values);
        }

        public virtual TimeRecord<TType> GetRecordAt(ITime at, List<string> eventArgMessages)
        {
            if (at == null)
            {
                eventArgMessages.Add("No time specified");
                return null;
            }

            if (_cache.Count == 0)
            {
                eventArgMessages.Add("Cache empty");
                return null;
            }

            var record = InterpolateTime(at, _interpolation, eventArgMessages);

            return record;
        }

        public override IBaseValueSet GetCache()
        {
            return new ValueSetTimeRecord<TType>(_cache);
        }

        public override void SetCache(ITime at, ITimeSpaceValueSet values)
        {
            Contract.Requires(at != null, "at != null");
            Contract.Requires(values != null, "values != null");

            var values2d = values.Values2D;

            if (values2d.Count != 1)
                throw new Exception("values.Values2D.Count != 1");

            _cache = new List<TimeRecord<TType>>();
            _cache.Add(new TimeRecord<TType>(at, values2d[0].Cast<TType>()));
        }

        public override void SetCache(ITimeSet at, ITimeSpaceValueSet values)
        {
            Contract.Requires(at != null, "at != null");
            Contract.Requires(values != null, "values != null");

            var values2d = values.Values2D;

            if (values2d.Count != at.Times.Count)
                throw new Exception("values.Values2D.Count != at.Times.Count");

            _cache = at
                .Times
                .Select((t, n) => new TimeRecord<TType>(t, values2d[n].Cast<TType>()))
                .ToList();
        }

        public override void SetCache(IBaseValueSet values)
        {
            throw new Exception("Time based convertor, must use ITimeSpaceValueSet versions of SetCache(...)");
        }

        public IEnumerable<TimeRecord<TType>> CacheClone()
        {
            return _cache
                .Select(r => new TimeRecord<TType>(r));
        }

        public override string DiagnosticSummary()
        {
            string component = ExchangeItem.Component != null ? ExchangeItem.Component.Caption : "Orphan";

            var sb = new StringBuilder("ValueSetConvertor Diagnostic Summary");
            sb.AppendLine();
            sb.AppendLine(string.Format("\t\tGetValueSetLatest: {0}", _counts[(int)Counts.GetValueSetLatest]));
            sb.AppendLine(string.Format("\t\tGetValueSetAt: {0}", _counts[(int)Counts.GetValueSetAt]));
            sb.AppendLine(string.Format("\t\tCacheMaxSize: {0}", _counts[(int)Counts.CacheMaxSize]));

            return sb.ToString();
        }

        public override IBaseValueSet ToValueSet(IEnumerable<TType> values)
        {
            throw new NotImplementedException("Temporal class");
        }

        public override IEnumerable<TType> ToArray(IBaseValueSet values)
        {
            throw new NotImplementedException("Temporal class");
        }

        public new const string XName = "ValueSetConverterTimeBase";
        public const string XInterpolation = "Interpolation";
        public const string XRecord = "Record";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            _interpolation = (InterpolationTemporal)Enum.Parse(typeof(InterpolationTemporal),
                Utilities.Xml.GetAttribute(xElement, XInterpolation));

            _cache = new List<TimeRecord<TType>>();

            ITime time;
            TType[] values;

            foreach (var xRecord in xElement.Elements(XRecord))
            {
                time = Persistence.Time.Parse(xRecord, accessor);

                values = xRecord.Value
                    .Split(',')
                    .Select(v => ToValue(v))
                    .ToArray();

                _cache.Add(new TimeRecord<TType>(time, values));
            }
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                new XAttribute(XInterpolation, _interpolation.ToString()),
                base.Persist(accessor));

            foreach (var record in _cache)
            {
                var csv = record
                    .Values
                    .Select(v => ToString(v))
                    .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                    .ToString();

                xml.Add(new XElement(XRecord,
                    Persistence.Time.Persist(record.Time, accessor),
                    csv));
            }

            return xml;
        }
    }
}
