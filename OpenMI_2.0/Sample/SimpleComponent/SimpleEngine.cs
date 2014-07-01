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
using System.Diagnostics;
using System.Threading;
using FluidEarth2.Sdk;
using FluidEarth2.Sdk.Interfaces;

namespace KState.SimpleComponent
{
    public class SimpleEngine : BaseEngineTime
    {
        private string _caption;
        private double _timeDelta;

        public override void Initialise(string initialisingXml, IDocumentAccessor accessor)
        {
            base.Initialise(initialisingXml, accessor);

            _caption = ArgumentCaption;

            _timeDelta = (double)Argument(SimpleComponent.GetArgumentIdentity(
                SimpleComponent.ConsumerArgs.TimeDelta)).Value;
        }

        public override void SetInput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
        {
            base.SetInput(engineVariable, elementCount, elementValueCount, vectorLength);

            if (vectorLength != 1)
                throw new Exception(string.Format("SetInput({0},...); Vector length = {1}, expected {2}",
                    engineVariable, vectorLength, 1));

            switch (engineVariable)
            {
                case "_A":
                    if (elementCount != 1)
                        throw new Exception(string.Format("SetInput({0},...); Element count = {1}, expected {2}",
                            engineVariable, elementCount, 1));
                    if (elementValueCount != 1)
                        throw new Exception(string.Format("SetInput({0},...); Element value count = {1}, expected {2}",
                            engineVariable, elementValueCount, 1));
                    break;
                case "_B":
                    if (elementCount != 1)
                        throw new Exception(string.Format("SetInput({0},...); Element count = {1}, expected {2}",
                            engineVariable, elementCount, 1));
                    if (elementValueCount != 1)
                        throw new Exception(string.Format("SetInput({0},...); Element value count = {1}, expected {2}",
                            engineVariable, elementValueCount, 1));
                    break;
                case "_C":
                    if (elementCount != 1)
                        throw new Exception(string.Format("SetInput({0},...); Element count = {1}, expected {2}",
                            engineVariable, elementCount, 1));
                    if (elementValueCount != 1)
                        throw new Exception(string.Format("SetInput({0},...); Element value count = {1}, expected {2}",
                            engineVariable, elementValueCount, 1));
                    break;

                default:
                    throw new NotImplementedException(engineVariable);
            }
        }

        public override void SetOutput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
        {
            base.SetOutput(engineVariable, elementCount, elementValueCount, vectorLength);

            if (vectorLength != 1)
                throw new Exception(string.Format("SetOutput({0},...); Vector length = {1}, expected {2}",
                    engineVariable, vectorLength, 1));

            switch (engineVariable)
            {
                case "_X":
                    if (elementCount != 1)
                        throw new Exception(string.Format("SetOutput({0},...); Element count = {1}, expected {2}",
                            engineVariable, elementCount, 1));
                    if (elementValueCount != 1)
                        throw new Exception(string.Format("SetOutput({0},...); Element value count = {1}, expected {2}",
                            engineVariable, elementValueCount, 1));
                    break;
                case "_Y":
                    if (elementCount != 1)
                        throw new Exception(string.Format("SetOutput({0},...); Element count = {1}, expected {2}",
                            engineVariable, elementCount, 1));
                    if (elementValueCount != 1)
                        throw new Exception(string.Format("SetOutput({0},...); Element value count = {1}, expected {2}",
                            engineVariable, elementValueCount, 1));
                    break;
                case "_Z":
                    if (elementCount != 1)
                        throw new Exception(string.Format("SetOutput({0},...); Element count = {1}, expected {2}",
                            engineVariable, elementCount, 1));
                    if (elementValueCount != 1)
                        throw new Exception(string.Format("SetOutput({0},...); Element value count = {1}, expected {2}",
                            engineVariable, elementValueCount, 1));
                    break;

                default:
                    throw new NotImplementedException(engineVariable);
            }
        }

        public override void Prepare()
        {
            base.Prepare();
        }

        public override double Update(double timeCurrent)
        {
            Trace.TraceInformation("{0} {1} Update Begin {2}", DateTime.Now, _caption, timeCurrent);
            timeCurrent += _timeDelta;
            Thread.Sleep(5000);
            Trace.TraceInformation("{0} {1} Update End {2}", DateTime.Now, _caption, timeCurrent);
            return timeCurrent;
        }

        public override void Finish()
        {
            base.Finish();
        }

        public override void SetDoubles(string engineVariable, double missingValue, double[] values)
        {
            switch (engineVariable)
            {
                case "_A":
                    if (values == null || values.Length != 1)
                        throw new Exception(string.Format("Invalid input value length for {0}. Expected {1} was {2}",
                            engineVariable, 1, values.Length));
                    break;
                case "_B":
                    if (values == null || values.Length != 1)
                        throw new Exception(string.Format("Invalid input value length for {0}. Expected {1} was {2}",
                            engineVariable, 1, values.Length));
                    break;
                case "_C":
                    if (values == null || values.Length != 1)
                        throw new Exception(string.Format("Invalid input value length for {0}. Expected {1} was {2}",
                            engineVariable, 1, values.Length));
                    break;

                default:
                    throw new NotImplementedException(engineVariable);
            }
        }

        public override double[] GetDoubles(string engineVariable, double missingValue)
        {
            double[] values;

            switch (engineVariable)
            {
                case "_X":
                    values = new double[1];
                    return values;
                case "_Y":
                    values = new double[1];
                    return values;
                case "_Z":
                    values = new double[1];
                    return values;

                default:
                    throw new NotImplementedException(engineVariable);
            }
        }
    }
}