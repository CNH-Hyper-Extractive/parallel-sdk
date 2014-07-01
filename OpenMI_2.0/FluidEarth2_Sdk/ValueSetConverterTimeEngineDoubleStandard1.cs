using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard;
using OpenMI.Standard2.TimeSpace;
using ITime2 = OpenMI.Standard2.TimeSpace.ITime;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Caches all values received from the component1 as they arrive
    /// Then on request from a component2 provides values interpolating
    /// cached records as appropriate.
    /// 
    /// The user must set the _updateLinkTimeIncrement as no way
    /// of knowing what the net componet1 timestep will be using just Standard1.
    /// Hence, the values returned by component1 could have been interpolated to
    /// match with _updateLinkTimeIncrement intervals. Then a requesting component2
    /// might require interpolation from this cache.
    /// Hence, care in choice of _updateLinkTimeIncrement is required to maintain
    /// both accuracy and efficiency.
    /// </summary>
    internal class ValueSetConverterTimeEngineDoubleStandard1 : ValueSetConverterTimeEngineDouble
    {
        ILink _link;
        LinkableComponentV1OpenMI.EngineProxy _engineProxy;

        public ValueSetConverterTimeEngineDoubleStandard1()
        { }

        public ValueSetConverterTimeEngineDoubleStandard1(string engineVariable, double missingValue, int elementCount)
            : base(engineVariable, missingValue, elementCount, InterpolationTemporal.Linear)
        { }

        public ValueSetConverterTimeEngineDoubleStandard1(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Initialise unpersisted variables
        /// </summary>
        /// <param name="engineProxy"></param>
        /// <param name="link"></param>
        public void SetRuntime(LinkableComponentV1OpenMI.EngineProxy engineProxy, ILink link)
        {
            _engineProxy = engineProxy;
            _link = link;
        }

        public override void CacheEngineValues(IEngine iEngine)
        {
            // Am expecting this to be called just once to initialise values
            // prior to pull run

            var iEngineTime = iEngine as IEngineTime;

            if (iEngineTime == null)
                throw new Exception("IEngine not IEngineTime");

            var at = new Time(iEngineTime.GetCurrentTime());

            if (_cache.Count > 0)
            {
                if (at.StampAsModifiedJulianDay < _cache.Last().Time.StampAsModifiedJulianDay)
                    throw new Exception(string.Format("Engine moving back in time, {0} < {1}",
                        at.ToString(), _cache.Last().Time.ToString()));
                else if (at.StampAsModifiedJulianDay == _cache.Last().Time.StampAsModifiedJulianDay)
                    _cache.RemoveAt(_cache.Count - 1);
            }

            var vs = _link.SourceComponent.GetValues(Utilities.Standard1.ToTime1(at), _link.ID);

            var record = ToTimeRecord(at, vs, _missingValue);

            if (HasItemChangedEvents)
                SendItemChangedEvent(string.Format("Standard1.ValueSetConvertorTarget: Cached from v1.link at {0}", at.ToString()));

            _cache.Add(record);

            if (_counts[(int)Counts.CacheMaxSize] < _cache.Count)
                _counts[(int)Counts.CacheMaxSize] = _cache.Count;
        }

        static TimeRecord<double> ToTimeRecord(ITime2 time2, IValueSet vs, double missingValue)
        {
            var scalarSet = vs as IScalarSet;

            if (scalarSet == null)
                throw new Exception("Vector set should be using ValueSetConverterTimeEngineDoubleVector3dStandard1");

            var values = new double[scalarSet.Count];

            for (int n = 0; n < scalarSet.Count; ++n)
                values[n] = scalarSet.IsValid(n)
                    ? scalarSet.GetScalar(n)
                    : missingValue;

            return new TimeRecord<double>(time2, values);
        }

        public override TimeRecord<double> GetRecordAt(ITime2 at, List<string> eventArgMessages)
        {
            if (at.DurationInDays > 0)
                throw new Exception();

            var vs = _engineProxy.GetComponent1Values(at.StampAsModifiedJulianDay, _link.ID);

            var record = ToTimeRecord(at, vs, _missingValue);

            _cache.Add(record);

            if (_cache.Count > 0)
                ((Utilities.Standard1.DummyComponent1Target)_link.TargetComponent).EarliestInputTime
                    = new Utilities.Standard1.TimeStamp(_cache[0].Time.StampAsModifiedJulianDay);

            return base.GetRecordAt(at, eventArgMessages);
        }

        void CacheSourceGetValues(IEnumerable<ITime2> at)
        {
            var input2 = ExchangeItem as InputSpaceTime;

            if (input2 == null)
                throw new Exception("Component input is null");

            if (input2.Provider == null)
                throw new Exception("Component input has no connected provider");

            var item = input2 as ITimeSpaceExchangeItem;

            item.TimeSet.Times.Clear();

            foreach (ITime2 t in at)
                item.TimeSet.Times.Add(t);

            // Require to explicitly use ITimeSpaceOutput interface as
            // IBaseOutput also has a GetValues(input2)

            var provider = input2.Provider as ITimeSpaceOutput;

            var vs = provider != null
                ? provider.GetValues(input2)
                : input2.Provider.GetValues(input2);

            if (typeof(double) != vs.ValueType)
                throw new Exception(string.Format("{0} != {1}",
                    typeof(double).ToString(), vs.ValueType.ToString()));

            var vst = vs as ITimeSpaceValueSet;

            if (vst != null)
                _cache.AddRange(Utilities.ToRecords<double>(item.TimeSet, vst));
            else
            {
                Debug.Assert(at.Count() == 1);

                ITime2 t = at.Count() > 0 ? at.First() : null;

                _cache.Add(new TimeRecord<double>(t, vs));
            }

            if (HasItemChangedEvents)
                SendItemChangedEvent("ValueSetConverterTimeEngineDoubleVector3dStandard1: Cached inputs for v1.link");

            if (HasItemChangedEvents)
                SendItemChangedEvent("Cached input");
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineDoubleStandard1(EngineVariable, _missingValue, _elementCount);

            c.SetRuntime(_engineProxy, _link);

            c._cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineDoubleStandard1";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                base.Persist(accessor));
        }
    }
}
