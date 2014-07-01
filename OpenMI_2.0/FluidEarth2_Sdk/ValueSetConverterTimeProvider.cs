
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValueSetConverterTimeSource<TType>
        : ValueSetConverterTimeRecordBase<TType>
        where TType : IConvertible
    {
        public delegate TimeRecord<TType> CreateRecord(ITime at);

        TimeSet _timeSet = new TimeSet();
        CreateRecord _createRecord;

        public ValueSetConverterTimeSource()
        { }

        public ValueSetConverterTimeSource(CreateRecord createRecord)
        {
            _createRecord = createRecord;
        }

        public ValueSetConverterTimeSource(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
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
                "ValueSetConverterTimeSource creates correct values on the fly as required so cannot set cache explicitly.");
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

        public override TimeRecord<TType> GetRecordAt(ITime at, List<string> eventArgMessages)
        {
            if (at == null)
            {
                eventArgMessages.Add("No time specified");
                return null;
            }

            return _createRecord(at);
        }

        public IEnumerable<TType> LinearInterpolation(IEnumerable<TType> below, IEnumerable<TType> above, double factor)
        {
            throw new Exception(
                "ValueSetConverterTimeSource does not interpolate, creates correct values on the fly as required.");
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeSource<TType>(_createRecord);

            c.TimeSet = new TimeSet(TimeSet);

            return c;
        }

        public new const string XName = "ValueSetConverterTimeSource";

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

    public class ValueSetConverterTimeSourceVector2d<TType>
        : ValueSetConverterTimeRecordBase<Vector2d<TType>>
        where TType : IConvertible
    {
        public delegate TimeRecord<Vector2d<TType>> CreateRecord(ITime at);

        TimeSet _timeSet = new TimeSet();
        CreateRecord _createRecord;

        public ValueSetConverterTimeSourceVector2d()
        { }

        public ValueSetConverterTimeSourceVector2d(CreateRecord createRecord)
        {
            _createRecord = createRecord;
        }

        public ValueSetConverterTimeSourceVector2d(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
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
                "ValueSetConverterTimeSource creates correct values on the fly as required so cannot set cache explicitly.");
        }

        public override string ToString(Vector2d<TType> value)
        {
            return value.ToString();
        }

        public override Vector2d<TType> ToValue(string value)
        {
            return new Vector2d<TType>(value);
        }

        public override IBaseValueSet ToValueSet(IEnumerable<Vector2d<TType>> values)
        {
            throw new NotImplementedException("Temporal class");
        }

        public override IEnumerable<Vector2d<TType>> ToArray(IBaseValueSet values)
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

        public override TimeRecord<Vector2d<TType>> GetRecordAt(ITime at, List<string> eventArgMessages)
        {
            if (at == null)
            {
                eventArgMessages.Add("No time specified");
                return null;
            }

            return _createRecord(at);
        }

        public IEnumerable<TType> LinearInterpolation(IEnumerable<Vector2d<TType>> below, IEnumerable<Vector2d<TType>> above, double factor)
        {
            throw new Exception(
                "ValueSetConverterTimeSource does not interpolate, creates correct values on the fly as required.");
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeSourceVector2d<TType>(_createRecord);

            c.TimeSet = new TimeSet(TimeSet);

            return c;
        }

        public new const string XName = "ValueSetConverterTimeSourceVector2d";

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

    public class ValueSetConverterTimeSourceVector3d<TType>
        : ValueSetConverterTimeRecordBase<Vector3d<TType>>
        where TType : IConvertible
    {
        public delegate TimeRecord<Vector3d<TType>> CreateRecord(ITime at);

        TimeSet _timeSet = new TimeSet();
        CreateRecord _createRecord;

        public ValueSetConverterTimeSourceVector3d()
        { }

        public ValueSetConverterTimeSourceVector3d(CreateRecord createRecord)
        {
            _createRecord = createRecord;
        }

        public ValueSetConverterTimeSourceVector3d(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
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
                "ValueSetConverterTimeSource creates correct values on the fly as required so cannot set cache explicitly.");
        }

        public override string ToString(Vector3d<TType> value)
        {
            return value.ToString();
        }

        public override Vector3d<TType> ToValue(string value)
        {
            return new Vector3d<TType>(value);
        }

        public override IBaseValueSet ToValueSet(IEnumerable<Vector3d<TType>> values)
        {
            throw new NotImplementedException("Temporal class");
        }

        public override IEnumerable<Vector3d<TType>> ToArray(IBaseValueSet values)
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

        public override TimeRecord<Vector3d<TType>> GetRecordAt(ITime at, List<string> eventArgMessages)
        {
            if (at == null)
            {
                eventArgMessages.Add("No time specified");
                return null;
            }

            return _createRecord(at);
        }

        public IEnumerable<TType> LinearInterpolation(IEnumerable<Vector3d<TType>> below, IEnumerable<Vector3d<TType>> above, double factor)
        {
            throw new Exception(
                "ValueSetConverterTimeSource does not interpolate, creates correct values on the fly as required.");
        }

        public override object Clone()
        {
            var c = new ValueSetConverterTimeSourceVector3d<TType>(_createRecord);

            c.TimeSet = new TimeSet(TimeSet);

            return c;
        }

        public new const string XName = "ValueSetConverterTimeSourceVector3d";

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