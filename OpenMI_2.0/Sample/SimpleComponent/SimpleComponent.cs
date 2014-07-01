//  -----------------------------------------------------------------------
//   Copyright (c) 2014 Tom Bulatewicz, Kansas State University
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy
//   of this software and associated documentation files (the "Software"), to deal
//   in the Software without restriction, including without limitation the rights
//   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//   copies of the Software, and to permit persons to whom the Software is
//   furnished to do so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all
//   copies or substantial portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//   SOFTWARE.
//  -----------------------------------------------------------------------

using System;
using System.Linq;
using FluidEarth2.Sdk;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Library;
using OpenMI.Standard2;

namespace KState.SimpleComponent
{
    public class SimpleComponent : BaseComponentTimeWithEngine
    {
        public enum ConsumerArgs
        {
            TimeDelta = 0,
        }

        public enum InputIdentity
        {
            A,
            B,
            C
        }

        public enum OutputIdentity
        {
            X,
            Y,
            Z
        }

        public const string Ns = "KState.SimpleComponent.SimpleComponent";
        public const string NsArg = Ns + ".Arg.";
        public const string NsInput = Ns + ".Input.";
        public const string NsOutput = Ns + ".Output.";

        public SimpleComponent()
            : base(new Identity(Ns, "SimpleComponent", "SimpleComponent"), new ExternalType(typeof (SimpleComponent)), new ExternalType(typeof (SimpleEngine)))
        {
            ArgumentsAddRange(new IArgument[]
            {
                new Argument<double>(GetArgumentIdentity(ConsumerArgs.TimeDelta), Time.MinutesToMJD(0.5))
            });
        }

        public static IIdentifiable GetArgumentIdentity(ConsumerArgs key)
        {
            switch (key)
            {
                case ConsumerArgs.TimeDelta:
                    return new Identity(NsArg + key, "Time delta", "Engine increments by this fixed time interval each time step");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        public IBaseInput GetInput(InputIdentity identity)
        {
            var id = GetIdentity(identity).Id;
            return Inputs
                .Where(i => i.Id == id)
                .SingleOrDefault();
        }

        public static IIdentifiable GetIdentity(InputIdentity key)
        {
            switch (key)
            {
                case InputIdentity.A:
                    return new Identity(NsInput + key, "A", "A");
                case InputIdentity.B:
                    return new Identity(NsInput + key, "B", "B");
                case InputIdentity.C:
                    return new Identity(NsInput + key, "C", "C");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        public IBaseOutput GetOutput(OutputIdentity identity)
        {
            var id = GetIdentity(identity).Id;
            return Outputs
                .Where(i => i.Id == id)
                .SingleOrDefault();
        }

        public static IIdentifiable GetIdentity(OutputIdentity key)
        {
            switch (key)
            {
                case OutputIdentity.X:
                    return new Identity(NsOutput + key, "X", "X");
                case OutputIdentity.Y:
                    return new Identity(NsOutput + key, "Y", "Y");
                case OutputIdentity.Z:
                    return new Identity(NsOutput + key, "Z", "Z");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        protected override void DoInitialise(bool reinitialising)
        {
            base.DoInitialise(reinitialising);

            if (reinitialising)
            {
                return;
            }

            var cellId = new Identity[1];
            var id = "Center";
            cellId[0] = new Identity(id, id, id);
            var cellX = new double[1];
            var cellY = new double[1];
            cellX[0] = 0.0;
            cellY[0] = 0.0;

            var spatialCenter = new SpatialDefinition(new Describes("Center", string.Format("Center ({0};{1})", cellX, cellY)), cellId.Count());
            var center = new ElementSetPoints(spatialCenter, cellId, cellX, cellY);

            // outputs
            var converterX = new ValueSetConverterTimeEngineDouble("_X", double.NegativeInfinity, 1, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear);
            var outX = new OutputSpaceTime(GetIdentity(OutputIdentity.X), QuantitiesSI.Length("X"), center, this, converterX);
            Add(outX);
            var converterY = new ValueSetConverterTimeEngineDouble("_Y", double.NegativeInfinity, 1, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear);
            var outY = new OutputSpaceTime(GetIdentity(OutputIdentity.Y), QuantitiesSI.Length("Y"), center, this, converterY);
            Add(outY);
            var converterZ = new ValueSetConverterTimeEngineDouble("_Z", double.NegativeInfinity, 1, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear);
            var outZ = new OutputSpaceTime(GetIdentity(OutputIdentity.Z), QuantitiesSI.Length("Z"), center, this, converterZ);
            Add(outZ);

            // inputs
            var converterA = new ValueSetConverterTimeEngineDouble("_A", double.NegativeInfinity, 1, ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear);
            var inA = new InputSpaceTime(GetIdentity(InputIdentity.A), QuantitiesSI.Length("A"), center, this, converterA);
            Add(inA);
            var inB = new InputSpaceTime(GetIdentity(InputIdentity.B), QuantitiesSI.Length("B"), center, this, converterA);
            Add(inB);
            var inC = new InputSpaceTime(GetIdentity(InputIdentity.C), QuantitiesSI.Length("C"), center, this, converterA);
            Add(inC);
        }
    }
}