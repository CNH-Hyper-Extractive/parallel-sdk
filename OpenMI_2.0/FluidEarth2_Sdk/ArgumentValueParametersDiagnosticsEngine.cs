using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.ParametersDiagnosticsEngine within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueParametersDiagnosticsEngine : ArgumentValueBase<ParametersDiagnosticsNative>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueParametersDiagnosticsEngine()
        {
            Value = new ParametersDiagnosticsNative();

            string valueAsString;

            if (TryPersist(Value, out valueAsString))
                ValueAsString = valueAsString;
        }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueParametersDiagnosticsEngine(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueParametersDiagnosticsEngine(ParametersDiagnosticsNative interval, bool isReadOnly)
            : base(interval, isReadOnly)
        { }

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
                var remoting = new ParametersDiagnosticsNative();
                remoting.ValueAsString = value;

                parsed = remoting;

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

                var remoting = (ParametersDiagnosticsNative)Value;

                persisted = remoting.ValueAsString;

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
