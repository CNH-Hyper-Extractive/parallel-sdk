using System;
using System.Collections.Generic;
using System.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class InterceptionEngine : InterceptionBase, IEngine
    {
        protected IEngine _engine;

        public InterceptionEngine(IEngine engine, IEnumerable<IIntercept> intercepts, bool active)
            : base(intercepts, active)
        {
            Contract.Requires(engine != null, "engine != null");

            _engine = engine;
        }

        public IEngine AgregatedEngine
        {
            get { return _engine; }
        }

        #region IEngine

        public string Ping()
        {
            if (!_active)
                return _engine.Ping();

            try
            {
                DoStart("Ping");

                return DoValue(_engine.Ping());
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                return "Exception: " + exception.Message;
            }
            finally
            {
                DoFinally();
            }
        }

        public void Initialise(string initialisingText)
        {
            if (!_active)
            {
                _engine.Initialise(initialisingText);
                return;
            }

            try
            {
                DoStart("Initialise", initialisingText);

                _engine.Initialise(initialisingText);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetArgument(string key, string value)
        {
            if (!_active)
            {
                _engine.SetArgument(key, value);
                return;
            }

            try
            {
                DoStart("SetArgument", key, value);

                _engine.SetArgument(key, value);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetInput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
        {
            if (!_active)
            {
                _engine.SetInput(engineVariable, elementCount, elementValueCount, vectorLength);
                return;
            }

            try
            {
                DoStart("SetInput1", engineVariable, elementCount, elementValueCount, vectorLength);

                _engine.SetInput(engineVariable, elementCount, elementValueCount, vectorLength);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetInput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
        {
            if (!_active)
            {
                _engine.SetInput(engineVariable, elementCount, elementValueCounts, vectorLength);
                return;
            }

            try
            {
                DoStart("SetInput2", engineVariable, elementCount, elementValueCounts, vectorLength);

                _engine.SetInput(engineVariable, elementCount, elementValueCounts, vectorLength);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetOutput(string engineVariable, int elementCount, int elementValueCount, int vectorLength)
        {
            if (!_active)
            {
                _engine.SetOutput(engineVariable, elementCount, elementValueCount, vectorLength);
                return;
            }

            try
            {
                DoStart("SetOutput1", engineVariable, elementCount, elementValueCount, vectorLength);

                _engine.SetOutput(engineVariable, elementCount, elementValueCount, vectorLength);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetOutput(string engineVariable, int elementCount, int[] elementValueCounts, int vectorLength)
        {
            if (!_active)
            {
                _engine.SetOutput(engineVariable, elementCount, elementValueCounts, vectorLength);
                return;
            }

            try
            {
                DoStart("SetOutput2", engineVariable, elementCount, elementValueCounts, vectorLength);

                _engine.SetOutput(engineVariable, elementCount, elementValueCounts, vectorLength);
            }
            finally
            {
                DoFinally();
            }
        }

        public void Prepare()
        {
            if (!_active)
            {
                _engine.Prepare();
                return;
            }

            try
            {
                DoStart("Prepare");

                _engine.Prepare();
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetStrings(string engineVariable, string missingValue, string[] values)
        {
            if (!_active)
            {
                _engine.SetStrings(engineVariable, missingValue, values);
                return;
            }

            try
            {
                DoStart("SetStrings", engineVariable, missingValue, values);

                _engine.SetStrings(engineVariable, missingValue, values);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetInt32s(string engineVariable, int missingValue, int[] values)
        {
            if (!_active)
            {
                _engine.SetInt32s(engineVariable, missingValue, values);
                return;
            }

            try
            {
                DoStart("SetInt32s", engineVariable, missingValue, values);

                _engine.SetInt32s(engineVariable, missingValue, values);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetDoubles(string engineVariable, double missingValue, double[] values)
        {
            if (!_active)
            {
                _engine.SetDoubles(engineVariable, missingValue, values);
                return;
            }

            try
            {
                DoStart("SetDoubles", engineVariable, missingValue, values);

                _engine.SetDoubles(engineVariable, missingValue, values);
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetBooleans(string engineVariable, bool missingValue, bool[] values)
        {
            if (!_active)
            {
                _engine.SetBooleans(engineVariable, missingValue, values);
                return;
            }

            try
            {
                DoStart("SetBooleans", engineVariable, missingValue, values);

                _engine.SetBooleans(engineVariable, missingValue, values);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public void Update()
        {
            if (!_active)
            {
                _engine.Update();
                return;
            }

            try
            {
                DoStart("Update");

                _engine.Update();
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        public string[] GetStrings(string engineVariable, string missingValue)
        {
            if (!_active)
                return _engine.GetStrings(engineVariable, missingValue);

            try
            {
                DoStart("GetStrings", engineVariable, missingValue);

                return DoValue(_engine.GetStrings(engineVariable, missingValue));
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                throw exception; // never reached if DoCatch(...) throws
            }
            finally
            {
                DoFinally();
            }
        }

        public int[] GetInt32s(string engineVariable, int missingValue)
        {
            if (!_active)
                return _engine.GetInt32s(engineVariable, missingValue);

            try
            {
                DoStart("GetInt32s", engineVariable, missingValue);

                return DoValue(_engine.GetInt32s(engineVariable, missingValue));
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                throw exception; // never reached if DoCatch(...) throws
            }
            finally
            {
                DoFinally();
            }
        }

        public double[] GetDoubles(string engineVariable, double missingValue)
        {
            if (!_active)
                return _engine.GetDoubles(engineVariable, missingValue);

            try
            {
                DoStart("GetDoubles", engineVariable, missingValue);

                return DoValue(_engine.GetDoubles(engineVariable, missingValue));
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                throw exception; // never reached if DoCatch(...) throws
            }
            finally
            {
                DoFinally();
            }
        }

        public bool[] GetBooleans(string engineVariable, bool missingValue)
        {
            if (!_active)
                return _engine.GetBooleans(engineVariable, missingValue);

            try
            {
                DoStart("GetBooleans", engineVariable, missingValue);

                return DoValue(_engine.GetBooleans(engineVariable, missingValue));
            }
            finally
            {
                DoFinally();
            }
        }

        public void Finish()
        {
            if (!_active)
            {
                _engine.Finish();
                return;
            }

            try
            {
                DoStart("Finish");

                _engine.Finish();
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();

                var report = DoFinalReport();

                var to = WriteTo.None;

                var values = Enum
                    .GetValues(typeof(WriteTo))
                    .Cast<WriteTo>();

                foreach (var w in values)
                    foreach (var i in _intercepts)
                        if ((w & i.To) != 0)
                            to |= w;

                var streams = _intercepts.SelectMany(i => i.Streams);

                Utilities.Diagnostics.WriteLine(report, to, streams);
            }
        }

        public void Dispose()
        {
            if (!_active)
            {
                _engine.Dispose();
                return;
            }

            try
            {
                DoStart("Dispose");

                _engine.Dispose();
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
            }
            finally
            {
                DoFinally();
            }
        }

        #endregion
    }
}
