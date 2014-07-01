using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.TimeInterval
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentValueTimeInterval<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueParametersRemoting.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentTimeInterval : ArgumentReferenceType<ArgumentValueTimeInterval>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentTimeInterval()
        {
            DefaultValue = new ArgumentValueTimeInterval(new TimeInterval(), false);
            Value = new ArgumentValueTimeInterval(new TimeInterval(), false);    
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentTimeInterval(IIdentifiable identity, TimeInterval value)
            : base(identity, new ArgumentValueTimeInterval(value, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentTimeInterval(IIdentifiable identity, TimeInterval value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueTimeInterval(value, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentTimeInterval(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueTimeInterval(), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentTimeInterval(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public TimeInterval TimeInterval
        {
            get { return ((ArgumentValueTimeInterval)Value).Value; }
            set
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueTimeInterval(value, IsReadOnly);
            }
        }
    }
}
