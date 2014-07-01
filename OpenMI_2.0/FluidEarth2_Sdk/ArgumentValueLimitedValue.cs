using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.ParametersLimitedValueLimits<TType> within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueLimitedValue<TType> : ArgumentValueBase<TType>
        where TType : IConvertible, IComparable<TType>
    {
        /// <summary>
        /// Limiting values for Value
        /// </summary>
        ParametersLimitedValueLimits<TType> _limits;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueLimitedValue()
        {
            _limits = new ParametersLimitedValueLimits<TType>();

            Value = default(TType);
            ValueAsString = (string)Convert.ChangeType(Value, typeof(string));
        }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="limits">Limits on Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueLimitedValue(TType value, ParametersLimitedValueLimits<TType> limits, bool isReadOnly)
            : base(value, isReadOnly)
        {
            _limits = limits;
        }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueLimitedValue(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Limiting values for Value
        /// </summary>
        public ParametersLimitedValueLimits<TType> Limits
        {
            get { return _limits; }
        }

        /// <summary>
        /// Persistence XML root element name
        /// </summary>
        public new const string XName = "ArgumentValueLimitedValue";

        /// <summary>
        /// Initialise instance created from default constructor.
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            _limits = new ParametersLimitedValueLimits<TType>(xElement, accessor);
        }

        /// <summary>
        /// Persist derived state to XML
        /// </summary>
        /// <param name="accessor">Details on intended XML providence, for resolving relative paths, might be null</param>
        /// <returns>Persisted state as XML</returns>
        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                _limits.Persist(accessor),
                base.Persist(accessor));
        }

        /// <summary>
        /// Validate Value and get information to present to user in UI about values state.
        /// </summary>
        /// <param name="message">Additional information pertinent to Validation state</param>
        /// <returns>Validation state</returns>
        public override EValidation Validate(out string message)
        {
            return _limits.Validate(_value, out message);
        }
    }
}
