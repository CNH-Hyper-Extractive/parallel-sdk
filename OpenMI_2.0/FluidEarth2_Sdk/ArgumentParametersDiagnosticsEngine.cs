using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.ParametersDiagnosticsEngine
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ParametersDiagnosticsEngine.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentParametersDiagnosticsEngine : ArgumentReferenceType<ArgumentValueParametersDiagnosticsEngine>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentParametersDiagnosticsEngine()
        {
            DefaultValue = new ArgumentValueParametersDiagnosticsEngine(new ParametersDiagnosticsNative(), false);
            Value = new ArgumentValueParametersDiagnosticsEngine(new ParametersDiagnosticsNative(), false);
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentParametersDiagnosticsEngine(IIdentifiable identity, ParametersDiagnosticsNative value)
            : base(identity, new ArgumentValueParametersDiagnosticsEngine(value, false), false, false)
        {
            DefaultValue = new ArgumentValueParametersDiagnosticsEngine(new ParametersDiagnosticsNative(), false);       
        }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentParametersDiagnosticsEngine(IIdentifiable identity, ParametersDiagnosticsNative value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueParametersDiagnosticsEngine(value, isReadOnly), isReadOnly, isOptional)
        {
            DefaultValue = new ArgumentValueParametersDiagnosticsEngine(new ParametersDiagnosticsNative(), false);
        }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentParametersDiagnosticsEngine(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueParametersDiagnosticsEngine(), isReadOnly, isOptional)
        {
            DefaultValue = new ArgumentValueParametersDiagnosticsEngine(new ParametersDiagnosticsNative(), false);
        }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentParametersDiagnosticsEngine(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public ParametersDiagnosticsNative Parameters
        {
            get { return ((ArgumentValueParametersDiagnosticsEngine)Value).Value; }
            set
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueParametersDiagnosticsEngine(value, IsReadOnly);
            }
        }
    }
}
