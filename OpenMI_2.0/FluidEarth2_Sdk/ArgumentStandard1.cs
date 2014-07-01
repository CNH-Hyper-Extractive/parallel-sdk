using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Create a version 2.0 OpenMI.Standard.IArgument from a version 1 OpenMI.Standard.IArgument
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentStandard1 : Argument<string>, OpenMI.Standard.IArgument, IPersistence
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentStandard1()
        { }

        /// <summary>
        /// Constructor from a version 1 OpenMI.Standard.IArgument
        /// </summary>
        /// <param name="iArgument"></param>
        public ArgumentStandard1(OpenMI.Standard.IArgument iArgument)
        {
            Caption = iArgument.Key;
            Description = iArgument.Description;

            IsReadOnly = iArgument.ReadOnly;

            DefaultValue = iArgument.Value;
            Value = iArgument.Value;
        }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentStandard1(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        /// <summary>
        /// Persistence XML root element name
        /// </summary>
        public const string XName = "ArgumentStandard1";

        /// <summary>
        /// Initialise instance created from default constructor.
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            var arg = Persistence.Argument<string>.Parse(xElement, accessor);

            SetIdentity(arg);
            IsOptional = arg.IsOptional;
            IsReadOnly = arg.IsReadOnly;
            Value = arg.ValueAsString;
            DefaultValue = arg.DefaultValue;
            PossibleValues = arg.PossibleValues;
        }

        /// <summary>
        /// Persist derived state to XML
        /// </summary>
        /// <param name="accessor">Details on intended XML providence, for resolving relative paths, might be null</param>
        /// <returns>Persisted state as XML</returns>
        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.Argument<string>.Persist(this, accessor));
        }

        /// <summary>
        /// OpenMI version 1 Key attribute
        /// </summary>
        public string Key
        {
            get { return Caption; }
        }

        /// <summary>
        /// OpenMI version 1 ReadOnly attribute 
        /// </summary>
        public bool ReadOnly
        {
            get { return IsReadOnly; }
        }

        /// <summary>
        /// OpenMI version 1 Value attribute 
        /// </summary>
        public new string Value
        {
            get { return ValueAsString; }
            set { ValueAsString = value; }
        }

        public override string MakeAbsolute(string valueAsString, Uri uri)
        {
            if (valueAsString == null || valueAsString.Trim() == string.Empty)
                return string.Empty;

            if (!Key.Contains(".ArgFile.") && !Key.Contains(".ArgPath."))
                return valueAsString;

            try
            {
                if (uri == null || !uri.IsAbsoluteUri)
                    return base.MakeAbsolute(valueAsString, uri);

                var absoluteUri = new Uri(uri, valueAsString);

                // Change %20's into spaces
                return Uri.UnescapeDataString(absoluteUri.LocalPath);
            }
            catch (System.Exception)
            {
                return valueAsString; // Default to doing nothing
            }
        }

        public override string MakeRelative(string valueAsString, Uri uri)
        {
            if (valueAsString == null || valueAsString.Trim() == string.Empty)
                return string.Empty; 
            
            if (!Key.Contains(".ArgFile.") && !Key.Contains(".ArgPath."))
                return valueAsString;

            try
            {
                if (uri == null || !uri.IsAbsoluteUri)
                    return base.MakeRelative(valueAsString, uri);

                var uriFile = new Uri(valueAsString);

                var relativeUri = uri.MakeRelativeUri(uriFile);

                return Uri.UnescapeDataString(relativeUri.ToString());
            }
            catch (System.Exception)
            {
                return valueAsString; // Default to doing nothing
            }
        }
    }
}
