using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValidationRunOptions : ValidationBase
    {
        IRunOptions _options;

        public ValidationRunOptions(IIdentifiable options)
            : base(options)
        {
            _options = options as IRunOptions;
        }

        public override bool DoValidation(ITime getValuesAt)
        {
            try
            {
                if (_options.RunType != RunType.GetValuesAt)
                    AddError = "Not a GetValuesAt run";

                if (_options.GetValuesAt_ActOn == null)
                    AddError = "No output specified to pull values on, need to set in composition View tab";

                if (_options.GetValuesAt_RunTo == null 
                    || double.IsInfinity(_options.GetValuesAt_RunTo.StampAsModifiedJulianDay))
                    AddError = "Invalid time specified to pull values too";
            }
            catch (System.Exception e)
            {
                AddError = Sdk.Utilities.Xml.Persist(e).ToString();
            }

            return _errors.Count == 0;
        }
    }
}
