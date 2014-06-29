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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using KState.Util;
using Oatc.OpenMI.Sdk.Backbone;
using Oatc.OpenMI.Sdk.Wrapper;
using OpenMI.Standard;
using TimeSpan = Oatc.OpenMI.Sdk.Backbone.TimeSpan;

namespace KState.SimpleComponent
{
    [Serializable]
    internal class SimpleEngine : IEngine
    {
        private const String version = "1";
        private readonly ILinkableComponent component;
        private double _currentTime;
        private List<InputExchangeItem> _inputs = new List<InputExchangeItem>();
        private String _modelDescription;
        private String _modelId;
        private List<OutputExchangeItem> _outputs = new List<OutputExchangeItem>();
        private int _processingTime;
        private double _simulationEndTime;
        private double _simulationStartTime;
        private double _timeStepLength;
        private TraceFile _traceFile;

        public SimpleEngine(ILinkableComponent component)
        {
            this.component = component;
        }

        public void Initialize(Hashtable properties)
        {
            try
            {
                // read the element set file
                var elementSets = ElementSetReader.read("ElementSets.xml");

                // read the config file
                var filename = (String)properties["ConfigFile"];
                var componentProperties = ComponentProperties.read(filename, component, elementSets);
                var extras = componentProperties.getExtras();
                foreach (var nextKey in extras.Keys)
                    properties[nextKey] = extras[nextKey];

                // save the standard properties
                _timeStepLength = componentProperties.getTimeStepInSeconds();
                _inputs = componentProperties.getInputExchangeItems();
                _outputs = componentProperties.getOutputExchangeItems();
                _simulationStartTime = componentProperties.getStartDateTime();
                _simulationEndTime = componentProperties.getEndDateTime();
                _modelId = componentProperties.getModelId();
                _modelDescription = componentProperties.getModelDescription();

                // save any extra properties
                _processingTime = Int32.Parse((String)properties["processingTime"]);

                //var c = (SimpleComponent)component;
                //c.EnableParallel = bool.Parse((string)properties["enableParallel"]);

                // setup the log file
                _traceFile = new TraceFile(_modelId);

                _traceFile.Append("Initialize");
                _traceFile.Append("Version: " + _modelId + " v" + version);
                _traceFile.Append("TimeHorizon:" + _simulationStartTime + "-" + _simulationEndTime);
                _traceFile.Append("TimeStep:" + _timeStepLength);
                _traceFile.Append("ProcessingTime:" + _processingTime);
                //_traceFile.Append("Parallel: " + c.EnableParallel);
            }
            catch (Exception e)
            {
                _traceFile.Exception(e);
            }
        }

        public bool PerformTimeStep()
        {
            _traceFile.Append("PerformTimeStep Begin " + GetCurrentTime());

            var ct = (TimeStamp)GetCurrentTime();
            _currentTime = ct.ModifiedJulianDay + (_timeStepLength/86400.0);

            // add a delay
            Thread.Sleep(5000);


            _traceFile.Append("PerformTimeStep End " + GetCurrentTime());

            return true;
        }

        public void Finish()
        {
            _traceFile.Append("Finish");
            _traceFile.Stop();
        }

        public IValueSet GetValues(string QuantityID, string ElementSetID)
        {
            // find the element set
            IElementSet elementSet = null;
            foreach (var item in _outputs)
            {
                if (item.ElementSet.ID == ElementSetID == true)
                {
                    elementSet = item.ElementSet;
                    break;
                }
            }

            // create a value set of missing values
            var values = new double[elementSet.ElementCount];
            for (var i = 0; i < values.Length; i++)
                values[i] = GetMissingValueDefinition();

            _traceFile.Append("GetValues: " + QuantityID + "/" + ElementSetID + "/" + _currentTime + " (" + values.Length + ")");

            return new ScalarSet(values);
        }

        public void SetValues(string QuantityID, string ElementSetID, IValueSet values)
        {
            _traceFile.Append("SetValues: " + QuantityID + "/" + ElementSetID + "/" + _currentTime + " (" + values.Count + ")");
        }

        public string GetComponentID()
        {
            return GetModelID();
        }

        public string GetComponentDescription()
        {
            return GetModelDescription();
        }

        public string GetModelID()
        {
            return _modelId;
        }

        public string GetModelDescription()
        {
            return _modelDescription;
        }

        public InputExchangeItem GetInputExchangeItem(int exchangeItemIndex)
        {
            return _inputs[exchangeItemIndex];
        }

        public OutputExchangeItem GetOutputExchangeItem(int exchangeItemIndex)
        {
            return _outputs[exchangeItemIndex];
        }

        public int GetInputExchangeItemCount()
        {
            if (_inputs == null)
                return 0;
            return _inputs.Count;
        }

        public int GetOutputExchangeItemCount()
        {
            if (_outputs == null)
                return 0;
            return _outputs.Count;
        }

        public ITimeSpan GetTimeHorizon()
        {
            return new TimeSpan(new TimeStamp(_simulationStartTime), new TimeStamp(_simulationEndTime));
        }

        public void Dispose()
        {
        }

        public ITime GetCurrentTime()
        {
            if (_currentTime == 0)
                _currentTime = _simulationStartTime;
            return new TimeStamp(_currentTime);
        }

        public ITimeStamp GetEarliestNeededTime()
        {
            if (_currentTime > 0)
                return new TimeStamp(_currentTime);
            return new TimeStamp(_simulationStartTime);
        }

        public ITime GetInputTime(string QuantityID, string ElementSetID)
        {
            // need input for the time step being calculated
            return new TimeStamp(_currentTime + 1);
        }

        public double GetMissingValueDefinition()
        {
            return 999;
        }
    }
}