
using System;
using System.Diagnostics;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Basic implementation of FluidEarth2.Sdk.Interfaces.IArgumentValue
    /// 
    /// If implemented provide means for editing a OpenMI.Standard2.IArgument within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// <typeparam name="TType">Value Type</typeparam>
    /// License: \ref rBsd3Clause
    public class ArgumentValueBase<TType> : IArgumentValue
    {
        /// <summary>
        /// Event handler for changes in Value in UI
        /// </summary>
        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// Value
        /// </summary>
        protected TType _value;
        /// <summary>
        /// Can UI edit value
        /// </summary>
        bool _isReadOnly;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueBase()
        {
            _value = default(TType);
        }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueBase(TType value, bool isReadOnly)
        {
            _value = value;
            _isReadOnly = isReadOnly;
        }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueBase(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Validate Value and get information to present to user in UI about values state.
        /// </summary>
        /// <param name="message">Additional information pertinent to Validation state</param>
        /// <returns>Validation state</returns>
        public virtual EValidation Validate(out string message)
        {
            if (_value == null)
            {
                message = "null";
                return EValidation.Error;
            }

            message = ToString();

            return EValidation.Valid;
        }

        /// <summary>
        /// Can UI change the value?
        /// </summary>
        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set { _isReadOnly = value; }
        }

        /// <summary>
        /// Edit value, typically through dedicated dialog
        /// Requires overriding by UI code
        /// </summary>
        /// <returns>true if value changed by edit action</returns>
        public virtual bool Edit()
        {
            throw new NotImplementedException("should not be called, should be overruled by ArgumentTypes.dll or other UI library");
        }

        /// <summary>
        /// Persistence XML root element name
        /// </summary>
        public const string XName = "ArgumentValue";

        /// <summary>
        /// Initialise instance created from default constructor.
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _isReadOnly = Utilities.Xml.GetAttribute(xElement, "isReadOnly", false);

            ValueAsString = xElement.Value;
        }

        /// <summary>
        /// Persist derived state to XML
        /// </summary>
        /// <param name="accessor">Details on intended XML providence, for resolving relative paths, might be null</param>
        /// <returns>Persisted state as XML</returns>
        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                new XAttribute("isReadOnly", _isReadOnly),
                ValueAsString);
        }

        /// <summary>
        /// Value
        /// </summary>
        public TType Value
        {
            get { return _value; }
            set
            {
                var old = ValueChanged != null
                    ? ValueAsString
                    : null;

                _value = value;

                if (ValueChanged != null)
                {
                    string v = ValueAsString;

                    if (v != old)
                        ValueChanged(this, new ValueChangedEventArgs(old, v));
                }
            }
        }

        /// <summary>
        /// Value parsing to/from string
        /// </summary>
        public string ValueAsString
        {
            get
            {
                string persisted;

                if (TryPersist(_value, out persisted))
                    return persisted;

                return string.Empty;
            }

            set
            {
                string old = ValueChanged != null
                    ? ValueAsString : null;

                object parsed;

                if (TryParse(value, out parsed))
                    _value = (TType)parsed;
                else
                    throw new Exception(
                        string.Format("Cannot parse argument string Value: \"{0}\"", value));

                if (ValueChanged != null)
                {
                    string v = ValueAsString;

                    if (v != old)
                        ValueChanged(this, new ValueChangedEventArgs(old, v));
                }
            }
        }

        /// <summary>
        /// Provide value suitable for display by UI as a Caption
        /// </summary>
        /// <returns>Caption</returns>
        public override string ToString()
        {
            return _value != null ? _value.ToString() : string.Empty;
        }

        /// <summary>
        /// Try and parse value from string
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="parsed">Parsed value if successful</param>
        /// <returns>True if parsed OK</returns>
        public virtual bool TryParse(string value, out object parsed)
        {
            try
            {
                if (string.Equals(value, "null", StringComparison.CurrentCultureIgnoreCase))
                {
                    parsed = default(TType);
                    return true;
                }

                if (typeof(TType).IsEnum)
                {
                    parsed = (TType)Enum.Parse(typeof(TType), value);
                    return true;
                }

                if (typeof(IConvertible).IsAssignableFrom(typeof(TType)))
                {
                    parsed = (TType)Convert.ChangeType(value, typeof(TType));
                    return true;
                }
            }
            catch (System.Exception)
            { }

            parsed = default(TType);
            return false;
        }

        /// <summary>
        /// Try and parse value to string
        /// </summary>
        /// <param name="value">Value to parse</param>
        /// <param name="persisted">Parsed value if successful</param>
        /// <returns>True if parsed OK</returns>
        public virtual bool TryPersist(object value, out string persisted)
        {
            persisted = string.Empty;

            try
            {
                if (value == null)
                {
                    persisted = "null";
                    return true;
                }

                if (typeof(TType).IsEnum)
                {
                    persisted = value.ToString();
                    return true;
                }

                if (typeof(IConvertible).IsAssignableFrom(typeof(TType)))
                {
                    persisted = (string)Convert.ChangeType(value, typeof(string));
                    return true;
                }
            }
            catch (System.Exception)
            { }

            return false;
        }

        /// <summary>
        /// Specific System.EventArgs for value change events
        /// </summary>
        public class ValueChangedEventArgs : EventArgs
        {
            public readonly string ValueAsStringOld;
            public readonly string ValueAsStringNew;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="valueAsStringOld">Value before changed</param>
            /// <param name="valueAsStringNew">Value after changed</param>
            public ValueChangedEventArgs(string valueAsStringOld, string valueAsStringNew)
            {
                ValueAsStringOld = valueAsStringOld;
                ValueAsStringNew = valueAsStringNew;
            }
        }
    }
}
