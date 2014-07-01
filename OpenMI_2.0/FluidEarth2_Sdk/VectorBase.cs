using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public abstract class VectorBase<TType> : IVector, IPersistence
        where TType : IConvertible
    {
        public abstract IVector New(string values);

        TType[] _values;

        public VectorBase(int rank)
        {
            _values = new TType[rank];
            _values.Initialize();
        }

        public VectorBase(int rank, TType[] values)
        {
            _values = new TType[rank];

            values.CopyTo(_values, 0);
        }

        public VectorBase(int rank, TType[] values, int offSet)
        {
            _values = new TType[rank];

            Array.Copy(values, offSet, _values, 0, 2);
        }

        public VectorBase(int rank, string values)
        {
            _values = new TType[rank];

            Initialise(values);
        }

        public void Initialise(string values)
        {
            var split = values.Split('|');

            Contract.Requires(_values.Length == split.Length,
                 "{0} values separated by | not \"{1}\"", _values.Length.ToString(), values);

            for (int n = 0; n < _values.Length; ++n)
                _values[n] = (TType)Convert.ChangeType(split[n], typeof(TType));
        }

        public void Initialise(TType value)
        {
            for (int n = 0; n < _values.Length; ++n)
                _values[n] = value;
        }

        public override string ToString()
        {
            return _values
                .Select(c => c.ToString())
                .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + "|"))
                .ToString()
                .TrimEnd('|');
        }

        public VectorBase(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public TType[] Values
        {
            get { return _values; }
            set
            {
                if (value.Count() != _values.Count())
                    throw new Exception(string.Format(
                        "value.Count() != _values.Count(), {0} != {1}",
                        value.Count(), _values.Count()));

                value.CopyTo(_values, 0);
            }
        }

        public const string XName = "Vector";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            if (typeof(TType).ToString() != Utilities.Xml.GetAttribute(xElement, "type"))
                throw new Exception(string.Format(
                    "Type miss-match, {0} != {1}",
                    typeof(TType).ToString(), Utilities.Xml.GetAttribute(xElement, "type")));

            Values = xElement.Value
                .Split(',')
                .Select(v => (TType)Convert.ChangeType(v, typeof(TType)))
                .ToArray();
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            var csv = Values
                .Select(c => Convert.ToString(c))
                .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                .ToString()
                .TrimEnd(',');

            return new XElement(XName,
                new XAttribute("type", typeof(TType).ToString()),
                csv);
        }
    }
}
