using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.ExternalType
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueExternalType.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentExternalType : ArgumentReferenceType<ArgumentValueExternalType>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentExternalType()
        {
            DefaultValue = new ArgumentValueExternalType(new ExternalType(), false);
            Value = new ArgumentValueExternalType(new ExternalType(), false);
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentExternalType(IIdentifiable identity, ExternalType value)
            : base(identity, new ArgumentValueExternalType(value, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentExternalType(IIdentifiable identity, ExternalType value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueExternalType(value, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentExternalType(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueExternalType(new ExternalType(), false), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentExternalType(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public IExternalType ExternalType
        {
            get { return ((ArgumentValueExternalType)Value).Value; }
            set 
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueExternalType(value, IsReadOnly);
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

                string typename, fullname, codebase;

                if (!ArgumentValueExternalType.Split(valueAsString, out typename, out fullname, out codebase))
                    return valueAsString;

                var uriCodebase = new Uri(codebase);

                var relativeUri = uri.MakeRelativeUri(uriCodebase);

                codebase = Uri.UnescapeDataString(relativeUri.ToString());

                var persisted = string.Empty;

                return ArgumentValueExternalType.Join(typename, fullname, codebase);  
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

                string typename, fullname, codebase;

                if (!ArgumentValueExternalType.Split(valueAsString, out typename, out fullname, out codebase))
                    return valueAsString;

                var absoluteUri = new Uri(uri, codebase);

                codebase = Uri.UnescapeDataString(absoluteUri.LocalPath);

                return ArgumentValueExternalType.Join(typename, fullname, codebase);
            }
            catch (System.Exception)
            {
                return valueAsString; // Default to doing nothing
            }
        }
    }
}
