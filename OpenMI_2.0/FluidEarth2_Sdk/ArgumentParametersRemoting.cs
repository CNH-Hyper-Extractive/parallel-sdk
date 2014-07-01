using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.ParametersRemoting
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueParametersRemoting.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentParametersRemoting : ArgumentReferenceType<ArgumentValueParametersRemoting>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentParametersRemoting()
        {
            DefaultValue = new ArgumentValueParametersRemoting(new ParametersRemoting(), false);
            Value = new ArgumentValueParametersRemoting(new ParametersRemoting(), false);
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentParametersRemoting(IIdentifiable identity, ParametersRemoting value)
            : base(identity, new ArgumentValueParametersRemoting(value, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentParametersRemoting(IIdentifiable identity, ParametersRemoting value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueParametersRemoting(value, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentParametersRemoting(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueParametersRemoting(), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentParametersRemoting(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public ParametersRemoting ParametersRemoting
        {
            get { return ((ArgumentValueParametersRemoting)Value).Value; }
            set
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueParametersRemoting(value, IsReadOnly);
            }
        }
    }
}
