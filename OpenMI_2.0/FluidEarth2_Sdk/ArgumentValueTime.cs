
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.CoreStandard2.Time within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueTime : ArgumentValueBase<Time>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueTime()
        { }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueTime(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueTime(Time time, bool isReadOnly)
            : base(time, isReadOnly)
        { }

        /// <summary>
        /// Provide value suitable for display by UI as a Caption
        /// </summary>
        /// <returns>Caption</returns>
        public override string ToString()
        {
            return Value == null ? string.Empty : ((Time)Value).ValueAsFormatedString();
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

            var time = _value as Time;

            if (time.Domain == Time.Limits.Open)
            {
                message = "Time is unbounded";
                return EValidation.Warning;
            }

            message = ToString();

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
                parsed = new Time(value);

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
                persisted = ((Time)value).ValueAsString();
                    
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
