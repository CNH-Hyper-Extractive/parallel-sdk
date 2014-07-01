using System;
using System.Reflection;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.Interfaces.IExternalType within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueExternalType : ArgumentValueBase<IExternalType>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueExternalType()
        { }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueExternalType(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        public ArgumentValueExternalType(IExternalType externalType, bool isReadOnly)
            : base(externalType, isReadOnly)
        { }

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

            var external = (ExternalType)Value;

            return external.Validate(out message);
        }

        /// <summary>
        /// Break up persisted value into component parts
        /// </summary>
        /// <param name="valueAsString"></param>
        /// <param name="typename">Type name</param>
        /// <param name="fullname">Fully qualified assembly version</param>
        /// <param name="codebase">Assembly location</param>
        /// <returns>OK if split OK</returns>
        public static bool Split(string valueAsString, out string typename, out string fullname, out string codebase)
        {
            typename = string.Empty;
            fullname = string.Empty;
            codebase = string.Empty;

            var parts = valueAsString.Split('^');

            if (parts.Length != 4)
                return false;

            typename = parts[1].Replace('~', ',');
            fullname = parts[2].Replace('~', ',');
            codebase = parts[3].Replace('~', ',');

            return true;
        }

        /// <summary>
        /// Join string values into single string
        /// </summary>
        /// <param name="valueAsString"></param>
        /// <param name="typename">Type name</param>
        /// <param name="fullname">Fully qualified assembly version</param>
        /// <param name="codebase">Assembly location</param>
        /// <returns>ValueAsString</returns>
        public static string Join(string typename, string fullname, string codebase)
        {
            return string.Format("ExternalType^{0}^{1}^{2}",
                typename, fullname, codebase);
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
                parsed = null;

                string typename, fullname, codebase;

                if (!Split(value, out typename, out fullname, out codebase))
                    return false;

                var externalType = new ExternalType();

                var assemblyName = fullname.Trim() != string.Empty
                    ? new AssemblyName(fullname)
                    : new AssemblyName();
              
                if (codebase != string.Empty)
                {
                    // There are issues with setting CodeBase but not full name, see MSN help for
                    // AssemblyName::CodeBase. However, as a argument, expectation is that user
                    // will resolve using UI with codebase value there as a hint.

                    assemblyName.CodeBase = codebase;
                }

                externalType.Initialise(assemblyName, typename);

                parsed = externalType;

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
                var external = (ExternalType)Value;

                var typename = external.TypeName != null
                    ? external.TypeName.Replace(',', '~')
                    : string.Empty;

                var fullname = string.Empty;
                var codebase = string.Empty;

                if (external.AssemblyName != null)
                {
                    if (external.AssemblyName.FullName != null)
                        fullname = external.AssemblyName.FullName.Replace(',', '~');

                    if (external.AssemblyName.CodeBase != null)
                        codebase = external.AssemblyName.CodeBase.Replace(',', '~');
                }

                persisted = Join(typename, fullname, codebase);

                return true;
            }
            catch (System.Exception)
            {
                persisted = null;
                return false;
            }
        }

        /// <summary>
        /// Persistence XML root element name
        /// </summary>
        public new const string XName = "ArgumentValueExternalType";

        /// <summary>
        /// Initialise instance created from default constructor.
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            Value = new ExternalType(xElement, accessor);
        }

        /// <summary>
        /// Persist derived state to XML
        /// </summary>
        /// <param name="accessor">Details on intended XML providence, for resolving relative paths, might be null</param>
        /// <returns>Persisted state as XML</returns>
        public override XElement Persist(IDocumentAccessor accessor)
        {
            var external = (ExternalType)Value;

            var xml = new XElement(XName,
                base.Persist(accessor));

            if (external != null)
                xml.Add(external.Persist(accessor));

            return xml;
        }
    }
}
