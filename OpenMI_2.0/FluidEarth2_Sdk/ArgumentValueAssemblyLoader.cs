using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.AssemblyLoader within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueAssemblyLoader : ArgumentValueBase<AssemblyLoader>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueAssemblyLoader()
        {
            Value = new AssemblyLoader();

            string valueAsString;
            if (TryPersist(Value, out valueAsString))
                ValueAsString = valueAsString;
        }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueAssemblyLoader(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueAssemblyLoader(AssemblyLoader value, bool isReadOnly)
            : base(value, isReadOnly)
        { }

        /// <summary>
        /// Provide value suitable for display by UI as a Caption
        /// </summary>
        /// <returns>Caption</returns>
        public override string ToString()
        {
            return string.Format("AssemblyLoader: {0}", Value != null 
                ? ((AssemblyLoader)Value).Caption : string.Empty);
        }

        /// <summary>
        /// Validate Value and get information to present to user in UI about values state.
        /// </summary>
        /// <param name="message">Additional information pertinent to Validation state</param>
        /// <returns>Validation state</returns>
        public override EValidation Validate(out string message)
        {
            var baseValidation = base.Validate(out message);

            if (baseValidation == EValidation.Error)
                return baseValidation;

            if (Value is AssemblyLoader)
                return EValidation.Valid;

            message += "!(Value is AssemblyLoader)";

            return EValidation.Error;
        }

        /// <summary>
        /// Break up persisted value into component parts
        /// </summary>
        /// <param name="valueAsString">Persisted value</param>
        /// <param name="uris">Uri as string collection</param>
        /// <param name="exts">Extensions as string collection</param>
        /// <returns>True if split OK</returns>
        public static bool Split(string valueAsString, out List<string> uris, out List<string> exts)
        {
            uris = new List<string>();
            exts = new List<string>();

            var parts = valueAsString.Split('%');

            if (parts.Length != 3 || parts[0] != "AssemblyLoader")
                return false;

            uris.AddRange(parts[1].Split('^'));
            exts.AddRange(parts[2].Split('^'));

            return true;
        }

        /// <summary>
        /// Join string values into single string
        /// </summary>
        /// <param name="uris">Uri as string collection</param>
        /// <param name="exts">Extensions as string collection</param>
        /// <returns>ValueAsString</returns>
        public static string Join(IEnumerable<string> uris, IEnumerable<string> exts)
        {
            var sUris = uris
                .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + "^"))
                .ToString()
                .TrimEnd('^');

            var sExts = exts
                .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + "^"))
                .ToString()
                .TrimEnd('^');

            return string.Format("AssemblyLoader%{0}%{1}",sUris, sExts);
        }

        /// <summary>
        /// Try and parse value from string
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="parsed">Parsed value if successful</param>
        /// <returns>True if parsed OK</returns>
        public override bool TryParse(string value, out object parsed)
        {
            parsed = null; 
            
            try
            {
                if (value.Trim() == string.Empty)
                {
                    parsed = new AssemblyLoader();
                    return true;
                }

                var loader = new AssemblyLoader();

                List<string> uris, exts;

                if (!Split(value, out uris, out exts))
                    return false;

                foreach (var uri in uris)
                    loader.AddSearchUri(new Uri(uri));

                foreach (var ext in exts)
                    loader.AddPotentialAssemblyExtension(ext);

                parsed = loader;

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Try and parse value to string
        /// </summary>
        /// <param name="value">Value to parse</param>
        /// <param name="persisted">Parsed value if successful</param>
        /// <returns>True if parsed OK</returns>
        public override bool TryPersist(object value, out string persisted)
        {
            try
            {
                if (value == null)
                {
                    persisted = string.Empty;
                    return true;
                }

                var loader = (AssemblyLoader)value;

                var sUris = loader.Uris.Select(u => u.ToString());

                persisted = Join(sUris, loader.Extensions);

                return true;
            }
            catch (System.Exception)
            {
                persisted = null;
                return false;
            }
        }
    }
}
