#if DEPRECATED
using System;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverter<TType> : ValueSetConverterBase
        where TType : EngineValueTypes.IEngineType
    {
        TType[] _cache;

        public ValueSetConverter()
        {
        }

        public ValueSetConverter(string engineVariable, IValueSetPacking packing)
            : base(engineVariable, packing)
        {
            _cache = new TType[packing.ValueArrayLength];
        }

        public ValueSetConverter(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override IBaseValueSet GetValueSetLatest()
        {
            if (PackingInformation.ElementValueCountConstant && PackingInformation.ElementValueCount == 1) // Single valued
            {
                return new ValueSetElementSingleValued<TType>(PackingInformation.ElementCount, _cache);
            }
            else if (PackingInformation.ElementValueCountConstant) // Multi valued constant
            {
                return new ValueSetElementMultiValued<TType>(PackingInformation.ElementCount, PackingInformation.ElementValueCount, _cache);
            }
            else // Multi valued with variable element value lengths
            {
                return new ValueSetElementMultiMixedValued<TType>(PackingInformation.ElementValueCounts, _cache);
            }
        }

        public override void ToEngine(IEngine iEngine, IBaseValueSet iValueSet)
        {
            if (iValueSet is IValueSetSpace<TType>)
                ((IValueSetSpace<TType>)iValueSet).ToArray.CopyTo(_cache, 0);
            else
                throw new NotImplementedException("ToEngine(...) for type: " + typeof(TType).ToString());

            if (HasItemChangedEvents)
                SendItemChangedEvent("Sent to engine");

            // IEngine is simple C style API so generics break down here

            if (typeof(TType) == typeof(EngineValueTypes.String))
            {
                var values = EngineValueTypes.String.ToStrings(_cache.Cast<EngineValueTypes.String>());
                iEngine.SetStrings(EngineVariable, Convert.ToString(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Int32))
            {
                var values = EngineValueTypes.Int32.ToInt32s(_cache.Cast<EngineValueTypes.Int32>());
                iEngine.SetInt32s(EngineVariable, Convert.ToInt32(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
            {
                var values = EngineValueTypes.Boolean.ToBools(_cache.Cast<EngineValueTypes.Boolean>());
                iEngine.SetBooleans(EngineVariable, Convert.ToBoolean(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double))
            {
                var values = EngineValueTypes.Double.ToDoubles(_cache.Cast<EngineValueTypes.Double>());
                iEngine.SetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
            {
                var values = EngineValueTypes.Double2d.ToDoubles(_cache.Cast<EngineValueTypes.Double2d>());
                iEngine.SetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue), values.ToArray());
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
            {
                var values = EngineValueTypes.Double3d.ToDoubles(_cache.Cast<EngineValueTypes.Double3d>());
                iEngine.SetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue), values.ToArray());
            }
            else
                throw new NotImplementedException(typeof(TType).ToString());
        }

        public override void CacheEngineValues(IEngine iEngine)
        {
            // IEngine is simple C style API so generics break down here

            if (typeof(TType) == typeof(EngineValueTypes.String))
            {
                var values = iEngine.GetStrings(EngineVariable, Convert.ToString(PackingInformation.MissingValue));
                _cache = EngineValueTypes.String.FromStrings(values).Cast<TType>().ToArray();
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Int32))
            {
                var values = iEngine.GetInt32s(EngineVariable, Convert.ToInt32(PackingInformation.MissingValue));
                _cache = EngineValueTypes.Int32.FromInt32s(values).Cast<TType>().ToArray();
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
            {
                var values = iEngine.GetBooleans(EngineVariable, Convert.ToBoolean(PackingInformation.MissingValue));
                _cache = EngineValueTypes.Boolean.FromBools(values).Cast<TType>().ToArray();
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double))
            {
                var values = iEngine.GetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue));
                _cache = EngineValueTypes.Double.FromDoubles(values).Cast<TType>().ToArray();
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
            {
                var values = iEngine.GetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue));
                _cache = EngineValueTypes.Double2d.FromDoubles(values).Cast<TType>().ToArray();
            }
            else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
            {
                var values = iEngine.GetDoubles(EngineVariable, Convert.ToDouble(PackingInformation.MissingValue));
                _cache = EngineValueTypes.Double3d.FromDoubles(values).Cast<TType>().ToArray();
            }  
            else
                throw new NotImplementedException(typeof(TType).ToString());

            if (HasItemChangedEvents)
                SendItemChangedEvent("Cached from engine");
        }

        public override void EmptyCaches(ITime upto)
        {
        }

        public override IBaseValueSet GetCache()
        {
            return GetValueSetLatest();
        }

        public override object Clone()
        {
            var v = new ValueSetConverter<TType>(EngineVariable, PackingInformation);
            _cache.CopyTo(v._cache, 0);
            return v;
        }

        public new const string XName = "ValueSetConverter";
        public const string XValues = "Values";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            var xValues = Persistence.ThisOrSingleChild(XValues, xElement);

            if (typeof(TType) == typeof(EngineValueTypes.String))
                _cache = EngineValueTypes.String.FromCsv(xValues.Value).Cast<TType>().ToArray();
            else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
                _cache = EngineValueTypes.Boolean.FromCsv(xValues.Value).Cast<TType>().ToArray();
            else if (typeof(TType) == typeof(EngineValueTypes.Int32))
                _cache = EngineValueTypes.Int32.FromCsv(xValues.Value).Cast<TType>().ToArray();
            else if (typeof(TType) == typeof(EngineValueTypes.Double))
                _cache = EngineValueTypes.Double.FromCsv(xValues.Value).Cast<TType>().ToArray();
            else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
                _cache = EngineValueTypes.Double2d.FromCsv(xValues.Value).Cast<TType>().ToArray();
            else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
                _cache = EngineValueTypes.Double3d.FromCsv(xValues.Value).Cast<TType>().ToArray();
            else
                throw new NotImplementedException(typeof(TType).ToString());
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            var xml = base.Persist(accessor);

            string csv = null;

            if (typeof(TType) == typeof(EngineValueTypes.String))
                csv = EngineValueTypes.String.ToCsv(_cache.Cast<EngineValueTypes.String>());
            else if (typeof(TType) == typeof(EngineValueTypes.Boolean))
                csv = EngineValueTypes.Boolean.ToCsv(_cache.Cast<EngineValueTypes.Boolean>());
            else if (typeof(TType) == typeof(EngineValueTypes.Int32))
                csv = EngineValueTypes.Int32.ToCsv(_cache.Cast<EngineValueTypes.Int32>());
            else if (typeof(TType) == typeof(EngineValueTypes.Double))
                csv = EngineValueTypes.Double.ToCsv(_cache.Cast<EngineValueTypes.Double>());
            else if (typeof(TType) == typeof(EngineValueTypes.Double2d))
                csv = EngineValueTypes.Double2d.ToCsv(_cache.Cast<EngineValueTypes.Double2d>());
            else if (typeof(TType) == typeof(EngineValueTypes.Double3d))
                csv = EngineValueTypes.Double3d.ToCsv(_cache.Cast<EngineValueTypes.Double3d>());
            else
                throw new NotImplementedException(typeof(TType).ToString());

            return new XElement(XName, xml, 
                new XElement(XValues, csv));
        }
    }
}
#endif