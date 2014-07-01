
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class Argument<TType> : CoreStandard2.Argument<TType>, IPersistence
        where TType : IConvertible
    {
        public Argument()
        {}

        public Argument(IIdentifiable identity, TType value)
            : base(identity, value, false, false)
        { }

        public Argument(IIdentifiable identity, TType value, bool isOptional, bool isReadOnly)
            : base(identity, value, isReadOnly, isOptional)
        { }

        public Argument(IArgument iArgument)
            : base(iArgument)
        { }

        public Argument(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public const string XName = "Argument";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            SetIdentity(Persistence.Identity.Parse(xElement.Element(Persistence.Identity.XName), accessor));

            IsOptional = Utilities.Xml.GetAttribute(xElement, "isOptional", false);
            IsReadOnly = Utilities.Xml.GetAttribute(xElement, "isReadOnly", false);

            ValueAsString = xElement.Element("ValueDefault").Value;
            DefaultValue = Value;

            ValueAsString = xElement.Element("Value").Value;

            var values = xElement
                .Elements("ValuePossible")
                .Select(v => v.Value);

            if (values.Count() > 0)
                AddPossibleValuesAsStrings(values);
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                new XAttribute("isOptional", IsOptional.ToString()),
                new XAttribute("isReadOnly", IsReadOnly.ToString()),
                Persistence.Identity.Persist(this, accessor),
                new XElement("Value", ValueAsString),
                new XElement("ValueDefault", ValueAsString));

            if (PossibleValues.Count > 0)
                xml.Add(GetPossibleValuesAsStrings()
                    .Select(p => new XElement("ValuePossible", p)));

            return xml;
        }

        public bool SetValueDefaultAsString(string valueDefault)
        {
            object parsed;
            bool ok = TryParse(valueDefault, out parsed);
            if (ok)
                DefaultValue = parsed;
            return ok;
        }

        public override string ToString()
        {
            return string.Format("{0} = {1} [{2}]", Caption, ValueAsString, ValueType);
        }

        public void AddPossibleValuesAsStrings(IEnumerable<string> values)
        {
            object o;

            foreach(var s in values)
                PossibleValues.Add(TryParse(s, out o) ? o : null);
        }

        public List<string> GetPossibleValuesAsStrings()
        {
            var values = new List<string>();
            string s;

            foreach (var o in PossibleValues)
                values.Add(TryPersist(o, out s) ? s : string.Empty);

            return values;
        }

        protected override bool TryParse(string value, out object parsed)
        {
            try
            {
                if (typeof(IArgumentValue).IsAssignableFrom(ValueType))
                {
                    Type type;
                    var xt = new ExternalType(typeof(TType));
                    parsed = xt.CreateInstance(out type);

                    ((IArgumentValue)parsed).ValueAsString = value;

                    return true;
                }

                if (ValueType.ToString() == typeof(FileInfo).ToString())
                {
                    parsed = new FileInfo(value);
                    return true;
                }

                if (ValueType.ToString() == typeof(DirectoryInfo).ToString())
                {
                    parsed = new DirectoryInfo(value);
                    return true;
                }

                if (ValueType.ToString() == typeof(Uri).ToString())
                {
                    parsed = new Uri(value);
                    return true;
                }
            }
            catch (System.Exception)
            {}

            return base.TryParse(value, out parsed);
       }

        protected override bool TryPersist(object value, out string persisted)
        {
            try
            {
                if (typeof(IArgumentValue).IsAssignableFrom(ValueType))
                {
                    persisted = ((IArgumentValue)value).ValueAsString;
                    return true;
                }

                if (ValueType.ToString() == typeof(FileInfo).ToString())
                {
                    persisted = ((FileInfo)value).FullName;
                    return true;
                }

                if (ValueType.ToString() == typeof(DirectoryInfo).ToString())
                {
                    persisted = ((DirectoryInfo)value).FullName;
                    return true;
                }

                if (ValueType.ToString() == typeof(Uri).ToString())
                {
                    persisted = ((Uri)value).LocalPath;
                    return true;
                }
            }
            catch (System.Exception)
            {}

            return base.TryPersist(value, out persisted);
        }
    }
}
