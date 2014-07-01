using System;
using System.IO;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a System.IO.FileInfo within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueFile : ArgumentValueBase<FileInfo>
    {
        /// <summary>
        /// File must exist to be valid
        /// </summary>
        bool _mustExist = false;
        /// <summary>
        /// File must be editable to be valid
        /// </summary>
        bool _isEditable = false;

        /// <summary>
        /// Folder filter for finding files of correct extension
        /// </summary>
        string _filter = "All files (*.*)|*.*";

        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueFile()
        { }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueFile(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueFile(FileInfo value, bool isReadOnly)
            : base(value, isReadOnly)
        { }

        public ArgumentValueFile(FileInfo value, bool isEditable, bool mustExist, string filter)
            : base(value, !isEditable)
        {
            _isEditable = isEditable;
            _mustExist = mustExist;
            _filter = filter;
        }

        /// <summary>
        /// Provide value suitable for display by UI as a Caption
        /// </summary>
        /// <returns>Caption</returns>
        public override string ToString()
        {
            return Value == null ? string.Empty : ((FileInfo)Value).Name;
        }

        /// <summary>
        /// File must be editable to be valid
        /// </summary>
        public bool IsEditable
        {
            get { return _isEditable; }
            set { _isEditable = value; }
        }

        /// <summary>
        /// File must exist to be valid
        /// </summary>
        public bool MustExist
        {
            get { return _mustExist; }
            set { _mustExist = value; }
        }

        /// <summary>
        /// Folder filter for finding files of correct extension
        /// </summary>
        public string DialogFileFilter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        /// <summary>
        /// Persistence XML root element name
        /// </summary>
        public new const string XName = "ArgumentValueFile";

        /// <summary>
        /// Initialise instance created from default constructor.
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _isEditable = Utilities.Xml.GetAttribute(xElement, "isEditable", false);
            _mustExist = Utilities.Xml.GetAttribute(xElement, "mustExist", false);

            _filter = Utilities.Xml.GetAttribute(xElement, "filter");

            base.Initialise(xElement, accessor);
        }

        /// <summary>
        /// Persist derived state to XML
        /// </summary>
        /// <param name="accessor">Details on intended XML providence, for resolving relative paths, might be null</param>
        /// <returns>Persisted state as XML</returns>
        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                new XAttribute("isEditable", _isEditable),
                new XAttribute("mustExist", _mustExist),
                new XAttribute("filter", _filter),
                base.Persist(accessor));
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

            var file = (FileInfo)Value;

            if (!file.Exists)
            {
                message = "File not found";

                return _mustExist ? EValidation.Error : EValidation.Warning;
            }

            if (_isEditable && file.IsReadOnly)
            {
                message = "File read only, required to be editable";

                return EValidation.Error;
            }

            message = string.Empty;
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

                parsed = new FileInfo(value);

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

                var file = (FileInfo)value;

                persisted = file.FullName;

                return true;
            }
            catch (System.Exception)
            {
                persisted = null;
                return false;
            }
        }

        /// <summary>
        /// Modify potential ValueAsString argument to be relative
        /// </summary>
        /// <param name="valueAsString">Value to modify</param>
        /// <param name="uri">Uri to resolve relative value, might be null</param>
        /// <returns>Relative value or original value</returns>
        public static string MakeRelative(string valueAsString, Uri uri)
        {
            if (valueAsString == null || valueAsString.Trim() == string.Empty)
                return string.Empty;

            try
            {
                if (uri == null || !uri.IsAbsoluteUri)
                    return valueAsString;

                var uriFile = new Uri(valueAsString);

                var relativeUri = uri.MakeRelativeUri(uriFile);

                return Uri.UnescapeDataString(relativeUri.ToString());
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
        public static string MakeAbsolute(string valueAsString, Uri uri)
        {
            if (valueAsString == null || valueAsString.Trim() == string.Empty)
                return string.Empty;

            try
            {
                if (uri == null || !uri.IsAbsoluteUri)
                    return valueAsString;

                var absoluteUri = new Uri(uri, valueAsString);

                // Change %20's into spaces
                return Uri.UnescapeDataString(absoluteUri.LocalPath);
            }
            catch (System.Exception)
            {
                return valueAsString; // Default to doing nothing
            }
        }
    }
}
