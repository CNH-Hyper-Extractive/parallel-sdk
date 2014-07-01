
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Provides means for editing a FluidEarth2.Sdk.ParametersGridRegular within
    /// Pipistrelle using a custom editor plug-in.
    /// </summary>
    /// License: \ref rBsd3Clause
    public class ArgumentValueGridRegular : ArgumentValueBase<ParametersGridRegular>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public ArgumentValueGridRegular()
        { }

        /// <summary>
        /// Constructor from XML persistence
        /// </summary>
        /// <param name="xElement">XML to initialise from</param>
        /// <param name="accessor">Details on XML providence, for resolving relative paths, might be null</param>
        public ArgumentValueGridRegular(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        /// <summary>
        /// Constructor from value and readOnly specifier
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="isReadOnly">Can UI edit value</param>
        public ArgumentValueGridRegular(ParametersGridRegular value, bool isReadOnly)
            : base(value, isReadOnly)
        { }

        /// <summary>
        /// Provide value suitable for display by UI as a Caption
        /// </summary>
        /// <returns>Caption</returns>
        public override string ToString()
        {
            if (Value == null)
                return string.Empty;

            var parameters = (ParametersGridRegular)Value;

            return string.Format(
                "Regular Grid {0}x{1}, Origin({2},{3}), Deltas({4},{5})",
                parameters.CellCountX, parameters.CellCountY, 
                parameters.Origin.Value1, parameters.Origin.Value2, 
                parameters.DeltaX, parameters.DeltaY);
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

                // Do not use ','
                string[] values = value.Split('^');

                if (values.Length != 6)
                    throw new Exception(value);

                var originX = double.Parse(values[0]);
                var originY = double.Parse(values[1]);

                var deltaX = double.Parse(values[2]);
                var deltaY = double.Parse(values[3]);

                var cellCountX = int.Parse(values[4]);
                var cellCountY = int.Parse(values[5]);

                parsed = new ParametersGridRegular(
                    cellCountX, cellCountY, 
                    new Coord2d(originX, originY), 
                    deltaX, deltaY);

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

                ParametersGridRegular parameters;

                if (value is ParametersGridRegular)
                    parameters = (ParametersGridRegular)value;
                else if (value is ArgumentValueGridRegular)
                    parameters = ((ArgumentValueGridRegular)value).Value;
                else
                    throw new Exception(string.Format("Cannot convert {0} into a ParametersGridRegular", value.GetType()));

                persisted = string.Format("{0}^{1}^{2}^{3}^{4}^{5}",
                    parameters.Origin.Value1, parameters.Origin.Value2,
                    parameters.DeltaX, parameters.DeltaY,
                    parameters.CellCountX, parameters.CellCountY);

                return true;
            }
            catch (System.Exception)
            {
                persisted = null;
                return false;
            }
        }

        /// <summary>
        /// Validate Value and get information to present to user in UI about values state.
        /// </summary>
        /// <param name="message">Additional information pertinent to Validation state</param>
        /// <returns>Validation state</returns>
        public override EValidation Validate(out string message)
        {
            if (Value == null)
            {
                message = "null";
                return EValidation.Error;
            }

            var parameters = (ParametersGridRegular)Value;

            var validation = EValidation.Valid;
            var sb = new StringBuilder();

            if (parameters.DeltaX <= 0)
            {
                sb.AppendLine(string.Format("Error: DeltaX = {0}; Value <= 0", parameters.DeltaX));
                validation = EValidation.Error;
            }

            if (parameters.DeltaY <= 0)
            {
                sb.AppendLine(string.Format("Error: DeltaY = {0}; Value <= 0", parameters.DeltaY));
                validation = EValidation.Error;
            }

            if (parameters.CellCountX <= 0)
            {
                sb.AppendLine(string.Format("Error: CellCountX = {0}; Value <= 0", parameters.CellCountX));
                validation = EValidation.Error;
            }

            if (parameters.CellCountY <= 0)
            {
                sb.AppendLine(string.Format("Error: CellCountY = {0}; Value <= 0", parameters.CellCountY));
                validation = EValidation.Error;
            }

            message = validation == EValidation.Valid
                ? ToString()
                : sb.ToString();

            return validation;
        }
    }
}
