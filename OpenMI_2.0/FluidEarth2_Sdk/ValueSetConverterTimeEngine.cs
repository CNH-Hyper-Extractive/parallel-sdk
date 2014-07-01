using System;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public abstract class ValueSetConverterTimeEngineBase<TType>
        : ValueSetConverterTimeRecordBase<TType>, IValueSetConverterTimeEngine
    {
        public abstract void SetEngineValues(IEngine iEngine, TimeRecord<TType> record);
        public abstract TimeRecord<TType> GetEngineValues(IEngine iEngine, ITime time);

        protected TType _missingValue;
        protected string _engineVariable;

        protected int _elementCount = 0;
        protected int _vectorLength = 1;
        protected bool _elementValueCountConstant = true;
        protected int _elementValueCount = 1;
        protected int[] _elementValueCounts = null;

        public ValueSetConverterTimeEngineBase()
        { }

        public ValueSetConverterTimeEngineBase(string engineVariable, TType missingValue, int elementCount, InterpolationTemporal interpolation)
            : base(interpolation)
        {
            _missingValue = missingValue;
            _engineVariable = engineVariable;
            _elementCount = elementCount;
        }

        public ValueSetConverterTimeEngineBase(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public string EngineVariable
        {
            get { return _engineVariable; }
        }

        public virtual void ToEngine(IEngine iEngine, IBaseValueSet iValueSet)
        {
            Contract.Requires(iValueSet != null, "iValueSet != null");

            var iEngineTime = iEngine as IEngineTime;

            if (iEngineTime == null)
                throw new Exception("IEngine not IEngineTime");

            var engineTime = new Time(iEngineTime.GetCurrentTime());

            if (iValueSet is ITimeSpaceValueSet)
                SetCache(engineTime, iValueSet as ITimeSpaceValueSet);
            else
                SetCache(iValueSet);

            if (_cache.Count < 1)
                throw new Exception("Input not provided with any required input values");

            if (_cache.Count > 1)
                throw new Exception("Engine cannot deal with multiple input time periods");

            var record = _cache.Last();

            if (HasItemChangedEvents)
                SendItemChangedEvent(string.Format("{0}.ToEngine({1})", EngineVariable, engineTime.ToString()));

            SetEngineValues(iEngine, record);
        }

        public virtual void CacheEngineValues(IEngine iEngine)
        {
            var iEngineTime = iEngine as IEngineTime;

            if (iEngineTime == null)
                throw new Exception("IEngine not IEngineTime");

            var time = new Time(iEngineTime.GetCurrentTime());

            if (_cache.Count > 0)
            {
                if (time.StampAsModifiedJulianDay < _cache.Last().Time.StampAsModifiedJulianDay)
                    throw new Exception(string.Format("Engine moving back in time, {0} < {1}",
                        time.ToString(), _cache.Last().Time.ToString()));
                else if (time.StampAsModifiedJulianDay == _cache.Last().Time.StampAsModifiedJulianDay)
                    _cache.RemoveAt(_cache.Count - 1);
            }

            var record = GetEngineValues(iEngine, time);

            if (HasItemChangedEvents)
                SendItemChangedEvent(string.Format("Cached from engine at {0}", time.ToString()));

            _cache.Add(record);

            if (_counts[(int)Counts.CacheMaxSize] < _cache.Count)
                _counts[(int)Counts.CacheMaxSize] = _cache.Count;
        }

        public int ElementCount { get { return _elementCount; } }
        public int VectorLength { get { return _vectorLength; } }

        public bool ElementValueCountConstant { get { return _elementValueCountConstant; } }
        public int ElementValueCount { get { return _elementValueCount; } }
        public int[] ElementValueCounts { get { return _elementValueCounts; } }

        public int ValueArrayLength 
        {
            get 
            {
                if (ElementValueCountConstant)
                    return ElementCount * ElementValueCount * VectorLength;

                int elementValueCounts = ElementValueCounts.Sum();

                return ElementCount * elementValueCounts * VectorLength;
            }
        }

        public new const string XName = "ValueSetConverterTimeEngine";

        public const string XMissingValue = "missingValue";
        public const string XEngineVariable = "engineVariable";
        public const string XElementCount = "elementCount";
        public const string XVectorLength = "vectorLength";
        public const string XElementValueCountConstant = "elementValueCountConstant";
        public const string XElementValueCount = "elementValueCount";
        public const string XElementValueCounts = "ElementValueCounts";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            _missingValue = ToValue(Utilities.Xml.GetAttribute(xElement, XMissingValue));

            _engineVariable = Utilities.Xml.GetAttribute(xElement, XEngineVariable);

            _elementCount = int.Parse(Utilities.Xml.GetAttribute(xElement, XElementCount));
            _vectorLength = int.Parse(Utilities.Xml.GetAttribute(xElement, XVectorLength));
            _elementValueCountConstant = bool.Parse(Utilities.Xml.GetAttribute(xElement, XElementValueCountConstant));
            _elementValueCount = int.Parse(Utilities.Xml.GetAttribute(xElement, XElementValueCount));

            if (!_elementValueCountConstant)
            {
                var csv = xElement.Element(XElementValueCounts).Value;

                _elementValueCounts = csv
                    .Split(',')
                    .Select(v => int.Parse(v))
                    .ToArray();
            }
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                new XAttribute(XEngineVariable, _engineVariable),
                new XAttribute(XElementCount, _elementCount.ToString()),
                new XAttribute(XVectorLength, _vectorLength.ToString()),
                new XAttribute(XElementValueCountConstant, _elementValueCountConstant.ToString()),
                new XAttribute(XElementValueCount, _elementValueCount.ToString()),
                new XAttribute(XMissingValue, _missingValue.ToString()),
                base.Persist(accessor));

            if (!_elementValueCountConstant)
            {
                var csv = _elementValueCounts
                    .Aggregate(new StringBuilder(), (sb, i) => sb.Append(i + ","));

                xml.Add(new XElement(XElementValueCounts, csv));
            }

            return xml;
        }
    }
}
