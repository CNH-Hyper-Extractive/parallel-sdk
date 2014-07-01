using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// This is a little bit arse, driving a time invariant class of of a time variant one, 
    /// done for quick fix, need to create a new base class.
    /// </summary>
    public class ValueSetConverterTimeInvariantEngineDouble : ValueSetConverterTimeEngineBase<double>
    {
        public ValueSetConverterTimeInvariantEngineDouble()
        { }

        public ValueSetConverterTimeInvariantEngineDouble(string engineVariable, double missingValue, int elementCount)
            : base(engineVariable, missingValue, elementCount, InterpolationTemporal.NoneUseLast)
        { }

        public ValueSetConverterTimeInvariantEngineDouble(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override void SetEngineValues(IEngine iEngine, TimeRecord<double> record)
        {
            iEngine.SetDoubles(EngineVariable, _missingValue, record.Values);
        }

        public override TimeRecord<double> GetEngineValues(IEngine iEngine, ITime time)
        {
            var values = iEngine.GetDoubles(EngineVariable, _missingValue);

            return new TimeRecord<double>(time, values);
        }

        public override string ToString(double value)
        {
            return value.ToString();
        }

        public override double ToValue(string value)
        {
            return double.Parse(value);
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeEngineDouble(EngineVariable, _missingValue, _elementCount, _elementValueCount, _interpolation);

            _cache = CacheClone()
                .ToList();

            return c;
        }

        public new const string XName = "ValueSetConverterTimeEngineDouble";

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

        public override void EmptyCaches(ITime upto)
        {
            _cache = new List<TimeRecord<double>>();
        }

        public override void SetCache(IBaseValueSet values)
        {
            _cache = new List<TimeRecord<double>>();
            _cache.Add(new TimeRecord<double>(new Time(), ToArray(values)));
        }

        public override IBaseValueSet ToValueSet(IEnumerable<double> values)
        {
            return new ValueSetArray<double>(values);
        }

        public override IEnumerable<double> ToArray(IBaseValueSet values)
        {
            return ((ValueSetArray<double>)values).Value;
        }

        class ValueSetArray<TType> : IBaseValueSet
        {
            public TType[] Value { get; set; }

            public ValueSetArray()
            { }

            public ValueSetArray(IEnumerable<TType> value)
            {
                Value = value.ToArray();
            }

            public int GetIndexCount(int[] indices)
            {
                return 0;
            }

            public object GetValue(int[] indices)
            {
                return Value;
            }

            public int NumberOfIndices
            {
                get { return 0; }
            }

            public void SetValue(int[] indices, object value)
            {
                Value = ((IEnumerable<TType>)value).ToArray();
            }

            public Type ValueType
            {
                get { return typeof(TType[]); }
            }
        }

        class ValueSet<TType> : IBaseValueSet
        {
            public TType Value { get; set; }

            public ValueSet()
            { }

            public ValueSet(TType value)
            {
                Value = value;
            }

            public int GetIndexCount(int[] indices)
            {
                return 0;
            }

            public object GetValue(int[] indices)
            {
                return Value;
            }

            public int NumberOfIndices
            {
                get { return 0; }
            }

            public void SetValue(int[] indices, object value)
            {
                Value = (TType)value;
            }

            public Type ValueType
            {
                get { return typeof(TType); }
            }
        }

    #region Time Invariant

        public override IEnumerable<double> LinearInterpolation(TimeRecord<double> below, TimeRecord<double> above, double factor)
        {
            throw new NotImplementedException("Time invariant");
        }

        public override bool CanGetValueSetWithoutExtrapolationAt(ITime at)
        {
            throw new NotImplementedException("Time invariant");
        }

        public override bool CanGetValueSetWithoutExtrapolationAt(ITimeSet at)
        {
            throw new NotImplementedException("Time invariant");
        }

        public override TimeRecord<double> GetRecordAt(ITime at, List<string> eventArgMessages)
        {
            throw new NotImplementedException("Time invariant");
        }

        public override ITimeSpaceValueSet GetValueSetAt(ITime at)
        {
            throw new NotImplementedException("Time invariant");
        }

        public override ITimeSpaceValueSet GetValueSetAt(ITimeSet at)
        {
            throw new NotImplementedException("Time invariant");
        }

        public override void SetCache(ITime at, ITimeSpaceValueSet values)
        {
            throw new NotImplementedException("Time invariant");
        }

        public override void SetCache(ITimeSet at, ITimeSpaceValueSet values)
        {
            throw new NotImplementedException("Time invariant");
        }

        #endregion Time Invariant
    }
}
