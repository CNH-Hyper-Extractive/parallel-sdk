using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public interface IInputSurrogate
    {
        IBaseInput InputOriginal { get; }
    }

    /// <summary>
    /// Inputs can only have one Provider so if we want to emulate a number of potenial
    /// input links (with Output -> Input correct connectivity) we need to emulate
    /// the true Input using this.
    /// </summary>
    public class InputSurrogate : BaseInput, IInputSurrogate
    {
        IBaseInput _inputOriginal;

        public InputSurrogate(IBaseInput input)
        {
            _inputOriginal = input;

            if (input == null)
            {
                SetDescribes(new Describes("Unspecified Input"));
                ValueDefinition = new ValueDefinition();
                Component = null;
            }
            else
            {
                SetDescribes(input);

                ValueDefinition = input.ValueDefinition;
                Component = input.Component;
            }
        }

        public IBaseInput InputOriginal
        {
            get { return _inputOriginal; }
        }

        public override bool IsValid(out string whyNot)
        {
            whyNot = "Is surrogate input, always invalid, user must replace with valid input item before runtime.";
            return false;
        }

        public override bool CanConnect(IBaseExchangeItem proposed, out string whyNot)
        {
            whyNot = "Can connect to anything, but cannot be used at runtime.";
            return true;
        }

        protected override void SetValuesImplementation(IBaseValueSet values)
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }

        protected override IBaseValueSet GetValuesImplementation()
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }
    }

    /// <summary>
    /// Inputs can only have one Provider so if we want to emulate a number of potenial
    /// input links (with Output -> Input correct connectivity) we need to emulate
    /// the true Input using this.
    /// </summary>
    public class InputSpatialSurrogate : BaseInputSpaceTime, IInputSurrogate
    {
        ITimeSpaceInput _inputOriginal;

        public InputSpatialSurrogate(ITimeSpaceInput input)
        {
            _inputOriginal = input;

            if (input == null)
            {
                SetDescribes(new Describes("Unspecified Input"));
                ValueDefinition = new ValueDefinition();
                SpatialDefinition = new SpatialDefinition();
                Component = null;
            }
            else
            {
                SetDescribes(input);

                ValueDefinition = input.ValueDefinition;
                Component = input.Component;

                var inputTime = input as BaseInputSpaceTime;

                SpatialDefinition = inputTime != null
                    ? inputTime.SpatialDefinition
                    : new SpatialDefinition();
            }
        }

        public IBaseInput InputOriginal
        {
            get { return _inputOriginal; }
        }

        public override bool IsValid(out string whyNot)
        {
            whyNot = "Is surrogate input, always invalid, user must replace with valid input item before runtime.";
            return false;
        }

        public override bool CanConnect(IBaseExchangeItem proposed, out string whyNot)
        {
            whyNot = "Can connect to anything, but cannot be used at runtime.";
            return true;
        }

        protected override void SetValuesImplementation(IBaseValueSet values)
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }

        protected override IBaseValueSet GetValuesImplementation()
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }

        protected override void SetValuesTimeImplementation(ITimeSpaceValueSet values)
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }

        protected override ITimeSpaceValueSet GetValuesTimeImplementation()
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }
    }
}
