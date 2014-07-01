using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// A OpenMi.Standard2.IArgument for value type FluidEarth2.Sdk.AssemblyLoader
    /// 
    /// Implemented as a FluidEarth2.Sdk.ArgumentReferenceType<TType> so can have a dedicated
    /// editor as a Pipistrelle plug-in. Plug-in requires the implementation of 
    /// FluidEarth2.Sdk.Interfaces.IArgumentValue which is done by 
    /// FluidEarth2.Sdk.ArgumentValueAssemblyLoader.
    /// Indeed almost all the actual specific functionality is delegated to this class.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentAssemblyLoader : ArgumentReferenceType<ArgumentValueAssemblyLoader>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentAssemblyLoader()
        {
            DefaultValue = new ArgumentValueAssemblyLoader(new AssemblyLoader(), false);
            Value = new ArgumentValueAssemblyLoader(new AssemblyLoader(), false);
        }

        /// <summary>
        /// Constructor from identity and value.
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        public ArgumentAssemblyLoader(IIdentifiable identity, AssemblyLoader value)
            : base(identity, new ArgumentValueAssemblyLoader(value, false), false, false)
        { }

        /// <summary>
        /// Fully explicit constructor 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="value">Both Value and DefaultValue</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentAssemblyLoader(IIdentifiable identity, AssemblyLoader value, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueAssemblyLoader(value, isReadOnly), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Constructor with default Value 
        /// </summary>
        /// <param name="identity">Identity</param>
        /// <param name="isOptional">Is argument optional to running component</param>
        /// <param name="isReadOnly">Can users edit the argument prior to run</param>
        public ArgumentAssemblyLoader(IIdentifiable identity, bool isOptional, bool isReadOnly)
            : base(identity, new ArgumentValueAssemblyLoader(new AssemblyLoader(), false), isReadOnly, isOptional)
        { }

        /// <summary>
        /// Persistence constructor
        /// </summary>
        /// <param name="xElement">XML</param>
        /// <param name="accessor">For resolving relative paths, might be null</param>
        public ArgumentAssemblyLoader(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Value 
        /// </summary>
        public AssemblyLoader AssemblyLoader
        {
            get { return ((ArgumentValueAssemblyLoader)Value).Value; }
            set 
            {
                // Do not change ((ArgumentValue??)Value).Value as that will skip possible events 
                Value = new ArgumentValueAssemblyLoader(value, IsReadOnly);
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

                List<string> uris, exts;

                if (!ArgumentValueAssemblyLoader.Split(valueAsString, out uris, out exts))
                    return valueAsString;

                for (int n = 0; n < uris.Count; ++n)
                {
                    var u = new Uri(uris[n]);
                    var r = uri.MakeRelativeUri(u);
                    uris[n] = Uri.UnescapeDataString(r.ToString());
                }

                return ArgumentValueAssemblyLoader.Join(uris, exts);  
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

                List<string> uris, exts;

                if (!ArgumentValueAssemblyLoader.Split(valueAsString, out uris, out exts))
                    return valueAsString;

                for (int n = 0; n < uris.Count; ++n)
                {
                    var a = new Uri(uri, uris[n]);
                    uris[n] = Uri.UnescapeDataString(a.LocalPath);
                }

                return ArgumentValueAssemblyLoader.Join(uris, exts);  
            }
            catch (System.Exception)
            {
                return valueAsString; // Default to doing nothing
            }
        }
    }
}

