using System;
using System.IO;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type System.IO.FileInfo
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueFile.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentFile : ArgumentReferenceType<ArgumentValueFile>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentFile()
        { }

        /// <summary>
        /// Constructor from identity.
        /// </summary>
        /// <param name="identity">Identity</param>
        public ArgumentFile(IIdentifiable identity)
            : base(identity, new ArgumentValueFile(), false, false)
        { }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentFile(IIdentifiable identity, FileInfo value)
            : base(identity, new ArgumentValueFile(value, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentFile(IIdentifiable identity, FileInfo value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueFile(value, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue from IArgumentValue</param>
        public ArgumentFile(IIdentifiable identity, ArgumentValueFile value)
            : base(identity, value, false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue from IArgumentValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentFile(IIdentifiable identity, ArgumentValueFile value, bool isOptional, bool isReadOnly)
            : base(identity, value, isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentFile(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueFile(), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentFile(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public FileInfo File
        {
            get { return ((ArgumentValueFile)Value).Value; }
            set
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueFile(value, IsReadOnly);
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
            if (uri == null || !uri.IsAbsoluteUri)
                return base.MakeRelative(valueAsString, uri);

            return ArgumentValueFile.MakeRelative(valueAsString, uri);
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

            if (uri == null || !uri.IsAbsoluteUri)
                return base.MakeAbsolute(valueAsString, uri);

            return ArgumentValueFile.MakeAbsolute(valueAsString, uri);
        }
    }
}
