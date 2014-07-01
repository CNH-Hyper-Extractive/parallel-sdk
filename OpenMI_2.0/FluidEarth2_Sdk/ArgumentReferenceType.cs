using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Specialise FluidEarth2.Sdk.CoreStandard2.Argument<TType> for value types that
    /// implement FluidEarth2.Sdk.Interfaces.IArgumentValue.
    /// </summary>
    /// <typeparam name="TType">TType that implement IArgumentValue</typeparam>
    /// License: \ref rBsd3Clause
    public class ArgumentReferenceType<TType> : CoreStandard2.Argument<TType>, IPersistence
        where TType : class, IArgumentValue
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentReferenceType()
        {
            Value = default(TType);
            DefaultValue = default(TType);
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentReferenceType(IIdentifiable identity, TType value)
            : base(identity, value, false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentReferenceType(IIdentifiable identity, TType value, bool isOptional, bool isReadOnly)
            : base(identity, value, isReadOnly, isOptional)
        { }

        /// <summary>
        /// Cloning constructor
        /// </summary>
        /// <param name="iArgument">Argument to clone</param>
        public ArgumentReferenceType(IArgument iArgument)
            : base(iArgument)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentReferenceType(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Persistence XML root element name
        /// </summary>
        public const string XName = "ArgumentReferenceType";

        /// <summary>
        /// Initialise instance created from default constructor.
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            SetIdentity(Persistence.Identity.Parse(xElement.Element(Persistence.Identity.XName), accessor));

            IsOptional = Utilities.Xml.GetAttribute(xElement, "isOptional", false);
            IsReadOnly = Utilities.Xml.GetAttribute(xElement, "isReadOnly", false);

            Value = Persistence.Parse<TType>("Value", xElement, accessor);
            DefaultValue = Persistence.Parse<TType>("DefaultValue", xElement, accessor); ;

            _possibleValues = xElement
                .Elements("PossibleValue")
                .Select(p => (object)Persistence.Parse<TType>("PossibleValue", p, accessor))
                .ToList();
        }

        /// <summary>
        /// Persist derived state to XML
        /// </summary>
        /// <param name="accessor">Details on intended XML providence, for resolving relative paths, might be null</param>
        /// <returns>Persisted state as XML</returns>
        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                new XAttribute("isOptional", IsOptional.ToString()),
                new XAttribute("isReadOnly", IsReadOnly.ToString()),
                Persistence.Identity.Persist(this, accessor),
                Persistence.Persist<TType>("Value", (TType)Value, accessor),
                Persistence.Persist<TType>("DefaultValue", (TType)DefaultValue, accessor),
                PossibleValues.Select(p => Persistence.Persist<TType>("PossibleValue", (TType)p, accessor)));
        }

        /// <summary>
        /// Set the Default value from string
        /// </summary>
        /// <param name="valueDefault">Default value as string</param>
        /// <returns>True if OK</returns>
        public bool SetValueDefaultAsString(string valueDefault)
        {
            object parsed;
            bool ok = TryParse(valueDefault, out parsed);
            if (ok)
                DefaultValue = parsed;
            return ok;
        }

        /// <summary>
        /// Override ToString with suitable default, used by UI as Caption
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0} = {1} [{2}]", Caption, ValueAsString, ValueType);
        }

        /// <summary>
        /// Set possible values for enumeration Value types
        /// </summary>
        /// <param name="values">Possible values</param>
        public void AddPossibleValuesAsStrings(IEnumerable<string> values)
        {
            object o;

            foreach(var s in values)
                PossibleValues.Add(TryParse(s, out o) ? o : null);
        }

        /// <summary>
        /// Persist possible values to string collection
        /// </summary>
        /// <returns>String collection of parsed possible values</returns>
        public List<string> GetPossibleValuesAsStrings()
        {
            var values = new List<string>();
            string s;

            foreach (var o in PossibleValues)
                values.Add(TryPersist(o, out s) ? s : string.Empty);

            return values;
        }

        /// <summary>
        /// Try and parse value from string
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="parsed">Parsed value if successful</param>
        /// <returns>True if parsed OK</returns>
        public override bool TryParse(string value, out object parsed)
        {
            try
            {
                if (value == string.Empty)
                {
                    parsed = default(TType);

                    if (parsed != null)
                        return true;
                }

                Type type;
                var xt = new ExternalType(typeof(TType));
                parsed = xt.CreateInstance(out type);

                ((IArgumentValue)parsed).ValueAsString = value;

                return true;
            }
            catch (System.Exception)
            {
                parsed = null;
                return false;
            }
       }

        /// <summary>
        /// Try and parse value to string
        /// </summary>
        /// <param name="value">Value to parse</param>
        /// <param name="persisted">Parsed value if successful</param>
        /// <returns>True if parsed OK</returns>
        public override bool TryPersist(object value, out string persisted)
        {
            try
            {
                if (value == null)
                {
                    persisted = string.Empty;
                    return true;
                }

                persisted = ((IArgumentValue)value).ValueAsString;
                return true;
            }
            catch (System.Exception)
            {
                persisted = string.Empty;
                return false;
            }
        }
    }
}

