using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.ParametersLimitedValueLimits<TType>
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueLimitedValue<TType>.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentLimitedValue<TType> : ArgumentReferenceType<ArgumentValueLimitedValue<TType>>
        where TType : IConvertible, IComparable<TType>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentLimitedValue()
        {
            Value = default(ArgumentValueLimitedValue<TType>);
            DefaultValue = default(ArgumentValueLimitedValue<TType>);
        }

        /// <summary>
        /// Constructor from identity and value  limits
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="limits">Value Limits</param>
        public ArgumentLimitedValue(IIdentifiable identity, TType value, ParametersLimitedValueLimits<TType> limits)
            : base(identity, new ArgumentValueLimitedValue<TType>(value, limits, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="limits">Value Limits</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentLimitedValue(IIdentifiable identity, TType value, ParametersLimitedValueLimits<TType> limits, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueLimitedValue<TType>(value, limits, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentLimitedValue(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public TType LimitedValue
        {
            get { return ((ArgumentValueLimitedValue<TType>)Value).Value; }
            set
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueLimitedValue<TType>(value, ((ArgumentValueLimitedValue<TType>)Value).Limits, IsReadOnly);
            }
        }
    }
}
