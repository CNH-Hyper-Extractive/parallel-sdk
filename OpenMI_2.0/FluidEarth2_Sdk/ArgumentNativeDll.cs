using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.ParametersNativeDll
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueNativeDll.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentNativeDll : ArgumentReferenceType<ArgumentValueNativeDll>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentNativeDll()
        {
            DefaultValue = new ArgumentValueNativeDll(new ParametersNativeDll(), false);
            Value = new ArgumentValueNativeDll(new ParametersNativeDll(), false);
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentNativeDll(IIdentifiable identity, ParametersNativeDll value)
            : base(identity, new ArgumentValueNativeDll(value, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentNativeDll(IIdentifiable identity, ParametersNativeDll value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueNativeDll(value, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentNativeDll(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueNativeDll(), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentNativeDll(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public ParametersNativeDll ParametersNativeDll
        {
            get { return ((ArgumentValueNativeDll)Value).Value; }
            set
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueNativeDll(value, IsReadOnly);
            }
        }

        /// <summary>
        /// Modify potential ValueAsString argument to be relative
        /// </summary>
        /// <param name="valueAsString">Value to modify</param>
        /// <param name="uri">Uri to resolve relative value, might be null</param>
        /// <returns>Relative value or original value</returns>
        public override string MakeRelative(string valueAsString, Uri uri)
        {
            if (valueAsString == null || valueAsString.Trim() == string.Empty)
                return string.Empty;

            try
            {
                if (uri == null || !uri.IsAbsoluteUri)
                    return base.MakeRelative(valueAsString, uri);

                string face, externalType, debugLaunch;

                if (!ArgumentValueNativeDll.Split(valueAsString, out face, out externalType, out debugLaunch))
                    return valueAsString;

                var arg = new ArgumentExternalType();
                externalType = arg.MakeRelative(externalType, uri);

                return ArgumentValueNativeDll.Join(face, externalType, debugLaunch);
            }
            catch (System.Exception)
            {
                return valueAsString; // Default to doing nothing
            }
        }

        /// <summary>
        /// Modify potential ValueAsString argument to be absolute
        /// </summary>
        /// <param name="valueAsString">Value to modify</param>
        /// <param name="uri">Uri to resolve relative value, might be null</param>
        /// <returns>Absolute value or original value</returns>
        public override string MakeAbsolute(string valueAsString, Uri uri)
        {
            if (valueAsString == null || valueAsString.Trim() == string.Empty)
                return string.Empty;

            try
            {
                if (uri == null || !uri.IsAbsoluteUri)
                    return base.MakeAbsolute(valueAsString, uri);

                string face, externalType, debugLaunch;

                if (!ArgumentValueNativeDll.Split(valueAsString, out face, out externalType, out debugLaunch))
                    return valueAsString;

                var arg = new ArgumentExternalType();
                externalType = arg.MakeAbsolute(externalType, uri);

                return ArgumentValueNativeDll.Join(face, externalType, debugLaunch);
            }
            catch (System.Exception)
            {
                return valueAsString; // Default to doing nothing
            }
        }
    }
}
