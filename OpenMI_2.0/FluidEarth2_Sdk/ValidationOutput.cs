using FluidEarth2.Sdk.Interfaces;
using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public class ValidationSource : ValidationBase
    {
        IBaseOutput _source;

        public ValidationSource(IBaseOutput source)
            : base(source)
        {
            _source = source;
        }

        public override bool DoValidation(ITime getValuesAt)
        {
            try
            {
                if (_source.Component != null)
                    AddError = "Source has Component specified";

                if (_source.Consumers.Count == 0 && _source.AdaptedOutputs.Count == 0)
                    AddError = "Source unattached, has no consumers or adapters";

                if (!(_source is ITimeSpaceOutput))
                    AddError = "Source non temporal";
            }
            catch (System.Exception e)
            {
                AddError = Sdk.Utilities.Xml.Persist(e).ToString();
            }

            return _errors.Count == 0;
        }
    }

    public class ValidationAdapter : ValidationBase
    {
        IBaseAdaptedOutput _adapter;

        public ValidationAdapter(IBaseAdaptedOutput adapter)
            : base(adapter)
        {
            _adapter = adapter;
        }

        public override bool DoValidation(ITime getValuesAt)
        {
            try
            {
                if (_adapter.Adaptee == null)
                    AddError = "Source has Adaptee unspecified";

                if (_adapter.Consumers.Count == 0 && _adapter.AdaptedOutputs.Count == 0)
                    AddError = "Adapter unattached, has no consumers or adapters";
            }
            catch (System.Exception e)
            {
                AddError = Sdk.Utilities.Xml.Persist(e).ToString();
            }

            return _errors.Count == 0;
        }
    }
}
