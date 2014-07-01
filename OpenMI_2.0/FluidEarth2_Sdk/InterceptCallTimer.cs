using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class InterceptCallTimer : InterceptCallBase
    {
        bool _calledPrepare, _calledFinish;
        string _called = string.Empty;
        TimeSpan _totalProcessorTimeStart;
        TimeSpan[] _timingsProcessor;
        int[] _calls;
        DateTime _timeStart, _timePrepare, _timeFinish;

        List<string> _callNames = new List<string>(new string[] 
            {
                "Unknown", // Must be first
                "Ping", "Initialise",
                "SetArgument",
                "SetInput1", "SetInput2", "SetOutput1", "SetOutput2",
                "SetGeometryCoords", "SetGeometryVertexCounts",
                "Prepare",
                "SetStrings", "SetInt32s", "SetDoubles", "SetBooleans",
                "Update",
                "AdaptDoubles",  
                "GetStrings", "GetInt32s", "GetDoubles", "GetBooleans",
                "Finish",
                "Dispose",
                "GetCurrentTime",
                "GetSuccessMessage",
            });

        public InterceptCallTimer(ParametersDiagnostics diagnostics)
            : base(diagnostics)
        {
            _timingsProcessor = new TimeSpan[_callNames.Count];
            _calls = new int[(int)_callNames.Count];

            for (int n = 0; n < (int)_callNames.Count; ++n)
                _timingsProcessor[n] = TimeSpan.Zero;
        }

        public override void Start(string call, params object[] args)
        {
            _called = call;
            _totalProcessorTimeStart = Process.GetCurrentProcess().TotalProcessorTime;
            _timeStart = DateTime.UtcNow;

            var n = _callNames.FindIndex(s => s == _called);

            if (n < 0)
                n = 0;

            _calls[n] += 1;

            if (call == "Prepare")
            {
                _timePrepare = DateTime.UtcNow;
                _calledPrepare = true;
            }
            else if (call == "Finish")
            {
                _timeFinish = DateTime.UtcNow;
                _calledFinish = true;
            }
        }

        public override void Finally()
        {
            try
            {
                var processor = Process.GetCurrentProcess().TotalProcessorTime - _totalProcessorTimeStart;
                var elapsed = DateTime.UtcNow - _timeStart;

                var n = _callNames.FindIndex(s => s == _called);

                if (n < 0)
                    n = 0;

                _timingsProcessor[n] = _timingsProcessor[n].Add(processor);

                if (To != WriteTo.None)
                {
                    var line = string.Format("{0}: Processor = {1}, Elapsed = {2}",
                        _called.ToString(), processor.ToString("g"), elapsed.ToString("g"));

                    Utilities.Diagnostics.WriteLine(Utilities.Diagnostics.DatedLine(Caption, line), this);
                }
            }
            finally
            {
                _called = _callNames[0];
            }
        }

        public override string FinalReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("== Engine Timing Report: {0}", Caption));
            sb.AppendLine("* " + DateTime.UtcNow.ToString("u"));

            sb.AppendLine("* Time Called");

            if (_calledPrepare)
                sb.AppendLine(string.Format("** Prepare: {0}", _timePrepare.ToString("u")));
            if (_calledFinish)
                sb.AppendLine(string.Format("** Finish: {0}", _timeFinish.ToString("u")));

            if (_calledFinish)
            {
                sb.AppendLine("* Elapsed time from Prepare() to Finish()");
                var runtime = _timeFinish - _timePrepare;
                sb.AppendLine(string.Format("** {0}", runtime.ToString("g")));
            }

            sb.AppendLine("* Total Processor times and call count for all calls > 0");
            for (int n = 0; n < _callNames.Count; ++n)
            {
                if (_calls[n] == 0)
                    continue;

                sb.AppendLine(string.Format("** {0}: {1}, {2}",
                    _callNames[n], _timingsProcessor[n].ToString("g"), _calls[n].ToString()));
            }

            var s = sb.ToString();

            return s;
        }
    }
}
