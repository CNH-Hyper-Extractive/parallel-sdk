using System.Linq;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValidationComponent : ValidationBase
    {
        IBaseLinkableComponent _component;

        public ValidationComponent(IBaseLinkableComponent component)
            : base(component)
        {
            _component = component;
        }

        public override bool DoValidation(ITime getValuesAt)
        {
            try
            {
                foreach (var v in _component.Validate())
                    ProcessValidateMessage(v);

                if (_component.Status == LinkableComponentStatus.Invalid)
                    AddError = "Status == LinkableComponentStatus.Invalid";

                int nProviders = _component.Inputs.Aggregate(0, (sum, o) => o.Provider == null ? sum : sum + 1);
                int nConsumers = _component.Outputs.Aggregate(0, (sum, o) => sum + o.Consumers.Count);

                AddDetail = string.Format("Provider count: {0}", nProviders);
                AddDetail = string.Format("Consumer count: {0}", nConsumers);

                if (_component is ITimeSpaceComponent)
                {
                    if (getValuesAt == null)
                        AddWarning = "Cannot validate TimeHorizon as composition time not provided";
                    else
                    {
                        var extent = ((ITimeSpaceComponent)_component).TimeExtent;

                        if (double.IsInfinity(extent.TimeHorizon.StampAsModifiedJulianDay))
                            AddError = "Invalid/Unspecified TimeHorizon " + extent.TimeHorizon.ToString();
                        else
                        {
                            if (extent.TimeHorizon.StampAsModifiedJulianDay > getValuesAt.StampAsModifiedJulianDay)
                                AddError = "Component time horizon does not start until after requested composition run time.";

                            if (!double.IsInfinity(extent.TimeHorizon.DurationInDays))
                            {
                                var end = extent.TimeHorizon.StampAsModifiedJulianDay + extent.TimeHorizon.DurationInDays;

                                if (end < getValuesAt.StampAsModifiedJulianDay)
                                    AddWarning = "Time horizon ends before end of composition run time.";
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                AddError = Sdk.Utilities.Xml.Persist(e).ToString();
            }

            return _errors.Count == 0;
        }
    }
}
