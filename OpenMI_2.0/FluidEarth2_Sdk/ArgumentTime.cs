
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.CoreStandard2.Time
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueTime.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentTime : ArgumentReferenceType<ArgumentValueTime>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentTime()
        {
            DefaultValue = new ArgumentValueTime(new Time(), false);
            Value = new ArgumentValueTime(new Time(), false);               
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentTime(IIdentifiable identity, Time value)
            : base(identity, new ArgumentValueTime(value, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentTime(IIdentifiable identity, Time value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueTime(value, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentTime(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueTime(), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentTime(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public Time Time
        {
            get { return ((ArgumentValueTime)Value).Value; }
            set
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueTime(value, IsReadOnly);
            }
        }
    }
}
