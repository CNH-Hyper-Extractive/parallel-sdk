using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeLambda<TType>
        : ValueSetConverterTimeRecordBase<TType>
        where TType : IConvertible
    {
        LambdaCalc _lambda;
        string _lambdaExpression;
        ITime _timeCallFirst, _timeCallLast;
        int _recordLength;

        TimeSet _timeSet = new TimeSet();

        public ValueSetConverterTimeLambda()
        { }

        public ValueSetConverterTimeLambda(string lambdaExpression, int recordLength)
        {
            _lambdaExpression = lambdaExpression;
            _recordLength = recordLength;

            var exp = "(at, duration, index, recordLength) => " + _lambdaExpression;

            _lambda = new LambdaCalc(exp);
        }

        public ValueSetConverterTimeLambda(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override TimeRecord<TType> GetRecordAt(ITime at, List<string> eventArgMessages)
        {
            if (at == null)
            {
                eventArgMessages.Add("No time specified");
                return null;
            }

            var message = string.Format("Lambda convertor call at {0} for {1}",
                at.ToString(), _lambda.ToString());

            Trace.TraceInformation(message);

            eventArgMessages.Add(message);

            if (_timeCallFirst == null)
                _timeCallFirst = at;

            _timeCallLast = at;

            // duration in s
            double duration = 86400.0 * (at.StampAsModifiedJulianDay - _timeCallFirst.StampAsModifiedJulianDay);

            double value;
            TType[] values = new TType[_recordLength];

            for (int index = 0; index < _recordLength; ++index)
            {
                value = _lambda.Function(at.StampAsModifiedJulianDay, duration, index, _recordLength);

                Trace.TraceInformation(string.Format("Value[{0}] = {1}", index, value.ToString()));

                values[index] = (TType)Convert.ChangeType(value, typeof(TType));
            }

            return new TimeRecord<TType>(at, values);
        }

        public override void EmptyCaches(ITime upto)
        {
            _timeSet = new TimeSet();
        }

        public override IBaseValueSet GetCache()
        {
            return GetValueSetAt(_timeSet);
        }

        public override IBaseValueSet GetValueSetLatest()
        {
            return GetValueSetAt(_timeSet);
        }

        public override void SetCache(IBaseValueSet values)
        {
            throw new Exception(
                "ValueSetConverterTimeLambda creates correct values on the fly as required so cannot set cache explicitly.");
        }

        public override string ToString(TType value)
        {
            return Convert.ToString(value);
        }

        public override TType ToValue(string value)
        {
            return (TType)Convert.ChangeType(value, typeof(TType));
        }

        public override IBaseValueSet ToValueSet(IEnumerable<TType> values)
        {
            throw new NotImplementedException("Temporal class");
        }

        public override IEnumerable<TType> ToArray(IBaseValueSet values)
        {
            throw new NotImplementedException("Temporal class");
        }

        public new ITimeSet TimeSet
        {
            get { return _timeSet; }
            set { _timeSet = new TimeSet(value); }
        }

        public override bool CanGetValueSetWithoutExtrapolationAt(ITime at)
        {
            return true;
        }

        public override bool CanGetValueSetWithoutExtrapolationAt(ITimeSet at)
        {
            return true;
        }

        public IEnumerable<TType> LinearInterpolation(IEnumerable<TType> below, IEnumerable<TType> above, double factor)
        {
            throw new Exception(
                "ValueSetConverterTimeLambda does not interpolate, creates correct values on the fly as required.");
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeLambda<TType>(_lambdaExpression, _recordLength);

            c.TimeSet = new TimeSet(TimeSet);

            return c;
        }

        public new const string XName = "ValueSetConverterTimeLambda";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            _recordLength = int.Parse(Utilities.Xml.GetAttribute(xElement, "recordLength"));

            _lambdaExpression = xElement.Element("LambdaExpression").Value;
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                new XAttribute("recordLength", _recordLength),
                new XElement("LambdaExpression", _lambdaExpression),
                base.Persist(accessor));
        }
    }
}

