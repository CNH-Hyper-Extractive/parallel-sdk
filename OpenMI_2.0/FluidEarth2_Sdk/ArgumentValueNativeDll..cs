using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.ParametersNativeDll within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueNativeDll : ArgumentValueBase<ParametersNativeDll>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueNativeDll()
        { }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueNativeDll(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueNativeDll(ParametersNativeDll value, bool isReadOnly)
            : base(value, isReadOnly)
        { }

        /// <summary>
        /// Provide value suitable for display by UI as a Caption
        /// </summary>
        /// <returns>Caption</returns>
        public override string ToString()
        {
            return Value != null ? ((ParametersNativeDll)Value).NativeDll_ImplementingNetAssembly.ToString() : string.Empty;
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

            return ((ParametersNativeDll)Value).Validate(out message);
        }

        /// <summary>
        /// Break up persisted value into component parts
        /// </summary>
        /// <param name="valueAsString">String to split</param>
        /// <param name="face">interface</param>
        /// <param name="externalType">External Type</param>
        /// <param name="debuggerLaunch">Launch debugger in remoting server</param>
        /// <returns>True if split OK</returns
        public static bool Split(string valueAsString, out string face, out string externalType, out string debuggerLaunch)
        {
            face = string.Empty;
            externalType = string.Empty;
            debuggerLaunch = string.Empty;

            var parts = valueAsString.Split('%');

            if (parts.Length != 4)
                return false;

            face = parts[1];
            externalType = parts[2];
            debuggerLaunch = parts[3];

            return true;
        }

        /// <summary>
        /// Join string values into single string
        /// </summary>
        /// <param name="face">interface</param>
        /// <param name="externalType">External Type</param>
        /// <param name="debuggerLaunch">Launch debugger in remoting server</param>
        /// <returns>ValueAsString</returns>
        public static string Join(string face, string externalType, string debuggerLaunch)
        {
            return string.Format("NativeDll%{0}%{1}%{2}",
                face,
                externalType,
                debuggerLaunch);
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
                    parsed = new ParametersNativeDll();
                    return true;
                }

                parsed = null;

                string sFace, sExternalType, sDebuggerLaunch;

                if (!Split(value, out sFace, out sExternalType, out sDebuggerLaunch))
                    return false;

                var face = (ParametersNativeDll.Interface)Enum.Parse(typeof(ParametersNativeDll.Interface), sFace);

                IExternalType type = null;

                if (sExternalType.Trim() != string.Empty)
                {
                    var arg = new ArgumentValueExternalType();
                    arg.ValueAsString = sExternalType;
                    type = arg.Value;
                }

                var debuggerLaunch = bool.Parse(sDebuggerLaunch);

                parsed = new ParametersNativeDll(type, face, null, debuggerLaunch);

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

                var native = (ParametersNativeDll)value;

                var externalTypeValueAsString = string.Empty;

                if (native.NativeDll_ImplementingNetAssembly != null)
                    externalTypeValueAsString = new ArgumentValueExternalType(native.NativeDll_ImplementingNetAssembly, false)
                        .ValueAsString;

                persisted = Join(
                    native.ImplementsInterface.ToString(), 
                    externalTypeValueAsString, 
                    native.DebuggerLaunch.ToString());

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
