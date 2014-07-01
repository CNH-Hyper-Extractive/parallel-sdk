using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.TimeInterval within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueTimeInterval : ArgumentValueBase<TimeInterval>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueTimeInterval()
        { }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueTimeInterval(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueTimeInterval(TimeInterval interval, bool isReadOnly)
            : base(interval, isReadOnly)
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

            var interval = (TimeInterval)Value;

            if (interval.Hours > 23)
            {
                message = "Hours value must be less than 24";
                return EValidation.Error;
            }

            if (interval.Minutes > 59)
            {
                message = "Minutes value must be less than 60";
                return EValidation.Error;
            }

            if (interval.Seconds > 59.0)
            {
                message = "Seconds value must be less than 60.0";
                return EValidation.Error;
            }

            if (interval.Seconds < 0.0)
            {
                message = "Seconds value must not be negative";
                return EValidation.Error;
            }

            if (!interval.AllowZero && interval.TotalSeconds == 0.0)
            {
                message = "Time Interval must be greater than 0.0";
                return EValidation.Error;
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
                string[] parts = value.Split('^');

                var days = (uint)Convert.ChangeType(parts[0], typeof(uint));
                var hours = (uint)Convert.ChangeType(parts[1], typeof(uint));
                var minutes = (uint)Convert.ChangeType(parts[2], typeof(uint));
                var seconds = (double)Convert.ChangeType(parts[3], typeof(double));
                var millisecondLimit = (bool)Convert.ChangeType(parts[4], typeof(bool));
                var allowZero = (bool)Convert.ChangeType(parts[5], typeof(bool));

                var interval = new TimeInterval(days, hours, minutes, seconds);
                interval.MillisecondLimit = millisecondLimit;
                interval.AllowZero = allowZero;

                parsed = interval;

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
                var interval = (TimeInterval)Value;

                persisted = string.Format(interval.MillisecondLimit
                    ? "{0}^{1}^{2}^{3:0.000}^{4}^{5}" : "{0}^{1}^{2}^{3}^{4}^{5}",
                    interval.Days, interval.Hours, interval.Minutes, interval.Seconds,
                    interval.MillisecondLimit, interval.AllowZero);

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
