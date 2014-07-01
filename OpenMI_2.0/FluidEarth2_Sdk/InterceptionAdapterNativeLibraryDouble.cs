using System;
using System.Linq;
using System.Collections.Generic;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class InterceptionAdapterNativeLibraryDouble : InterceptionBase, IAdapterNativeLibraryDouble
    {
        protected IAdapterNativeLibraryDouble _engine;

        public const int SuccessCodeUnassigned = -666;
        public const int FailedPing = -6666;

        public InterceptionAdapterNativeLibraryDouble(
            IAdapterNativeLibraryDouble engine, IEnumerable<IIntercept> intercepts, bool active)
            : base(intercepts, active)
        {
            Contract.Requires(engine != null, "engine != null");

            _engine = engine;
        }

        public int Ping()
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
                return FailedPing;
            }
            finally
            {
                DoFinally();
            }
        }

        public string GetSuccessMessage(int successCode)
        {
            if (!_active)
                return _engine.GetSuccessMessage(successCode);

            try
            {
                DoStart("GetSuccessMessage", successCode);

                return DoValue(_engine.GetSuccessMessage(successCode));
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                return exception.Message;
            }
            finally
            {
                DoFinally();
            }
        }

        public void Initialise(string with, out int successCode)
        {
            if (!_active)
            {
                _engine.Initialise(with, out successCode);
                return;
            }

            try
            {
                DoStart("Initialise", with);

                _engine.Initialise(with, out successCode);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                successCode = SuccessCodeUnassigned;
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetArgument(string key, string value, out int successCode)
        {
            if (!_active)
            {
                _engine.SetArgument(key, value, out successCode);
                return;
            }

            try
            {
                DoStart("SetArgument", key, value);

                _engine.SetArgument(key, value, out successCode);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                successCode = SuccessCodeUnassigned;
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetGeometryCoords(WhichWay which, GeometryPassingOptions options, double[] coords, out int successCode)
        {
            if (!_active)
            {
                _engine.SetGeometryCoords(which, options, coords, out successCode);
                return;
            }

            try
            {
                DoStart("SetGeometryCoords", which, options, coords);

                _engine.SetGeometryCoords(which, options, coords, out successCode);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                successCode = SuccessCodeUnassigned;
            }
            finally
            {
                DoFinally();
            }
        }

        public void SetGeometryVertexCounts(WhichWay which, int[] counts, out int successCode)
        {
            if (!_active)
            {
                _engine.SetGeometryVertexCounts(which, counts, out successCode);
                return;
            }

            try
            {
                DoStart("SetGeometryVertexCounts", which, counts);

                _engine.SetGeometryVertexCounts(which, counts, out successCode);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                successCode = SuccessCodeUnassigned;
            }
            finally
            {
                DoFinally();
            }
        }

        public void Prepare(out int successCode)
        {
            if (!_active)
            {
                _engine.Prepare(out successCode);
                return;
            }

            try
            {
                DoStart("Prepare");

                _engine.Prepare(out successCode);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                successCode = SuccessCodeUnassigned;
            }
            finally
            {
                DoFinally();
            }
        }

        public double[] AdaptDoubles(double time, double[] adapteeValues, int adaptedLength, out int successCode)
        {
            if (!_active)
                return _engine.AdaptDoubles(time, adapteeValues, adaptedLength, out successCode);

            try
            {
                DoStart("AdaptDoubles", time, adapteeValues, adaptedLength);

                return DoValue(_engine.AdaptDoubles(time, adapteeValues, adaptedLength, out successCode));
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                successCode = SuccessCodeUnassigned;
                return new double[] { };
            }
            finally
            {
                DoFinally();
            }
        }

        public void Finish(out int successCode)
        {
            if (!_active)
            {
                _engine.Finish(out successCode);
                return;
            }

            try
            {
                DoStart("Finish");

                _engine.Finish(out successCode);
            }
            catch (System.Exception exception)
            {
                DoCatch(exception);
                successCode = SuccessCodeUnassigned;
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
    }

}
