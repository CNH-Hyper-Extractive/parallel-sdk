using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a System.Uri within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueUri : ArgumentValueBase<Uri>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueUri()
        { }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueUri(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueUri(Uri value, bool isReadOnly)
            : base(value, isReadOnly)
        { }

        /// <summary>
        /// Provide value suitable for display by UI as a Caption
        /// </summary>
        /// <returns>Caption</returns>
        public override string ToString()
        {
            return Value != null ? ((Uri)Value).Segments.Last() : string.Empty;
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

            var uri = (Uri)Value;

            if (!File.Exists(uri.LocalPath))
            {
                message = "Uri.LocalPath not found";
                return EValidation.Warning;
            }

            message = string.Format("{0}\r\n{1}\r\n{2}",
                uri.Segments.Last(), uri.LocalPath, uri.AbsoluteUri);

            return EValidation.Valid;
        }

        /// <summary>
        /// Try and parse value from string
        /// </summary>
        /// <param name="value">String to parse</param>
        /// <param name="parsed">Parsed value if successful</param>
        /// <returns>True if parsed OK</returns>
        public override bool TryParse(string value, out object parsed)
        {
            try
            {
                if (value.Trim() == string.Empty)
                {
                    parsed = null;
                    return true;
                }

                Uri uri;

                if (!Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out uri))
                    uri = null;

                parsed = uri;
                return true;
            }
            catch (System.Exception)
            {
                parsed = null;
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

                var uri = (Uri)value;

                persisted = uri.ToString();

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
