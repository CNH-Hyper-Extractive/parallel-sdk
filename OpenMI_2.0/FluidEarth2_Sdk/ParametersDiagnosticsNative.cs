using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ParametersDiagnosticsNative : ParametersDiagnostics
    {
        bool _launchDebugger;

        bool _includeTimings;
        bool _includeCalls;
        bool _includeStatistics;

        FileInfo _log;
        bool _logServer;

        public bool LaunchDebugger
        {
            get { return _launchDebugger; }
            set { _launchDebugger = value; }
        }

        public bool IncludeTimings
        {
            get { return _includeTimings; }
            set { _includeTimings = value; }
        }

        public bool IncludeCalls
        {
            get { return _includeCalls; }
            set { _includeCalls = value; }
        }

        public bool IncludeStatistics
        {
            get { return _includeStatistics; }
            set { _includeStatistics = value; }
        }

        public FileInfo Log
        {
            get { return _log; }
            set { _log = value; }
        }

        public bool LogServer
        {
            get { return _logServer; }
            set { _logServer = value; }
        }

        public new string ValueAsString
        {
            get
            {
                var sb = new StringBuilder(base.ValueAsString);

                if (LaunchDebugger)
                    sb.Append("~Debug");

                if (IncludeTimings)
                    sb.Append("~Timings");
                if (IncludeCalls)
                    sb.Append("~Calls");
                if (IncludeStatistics)
                    sb.Append("~Stats");

                if (LogServer)
                    sb.Append("~LogServer");

                if (Log != null)
                    sb.Append("~Log=" + Log.FullName);

                return sb
                    .ToString()
                    .TrimStart('~');
            }

            set
            {
                LaunchDebugger = false;
                IncludeTimings = false;
                IncludeCalls = false;
                IncludeStatistics = false;
                Log = null;
                LogServer = false; 
                
                base.ValueAsString = value;

                var options = value.Split('~');

                LaunchDebugger = options.Contains("Debug");

                IncludeTimings = options.Contains("Timings");
                IncludeCalls = options.Contains("Calls");
                IncludeStatistics = options.Contains("Stats");

                LogServer = options.Contains("LogServer");

                var log = options
                    .Where(o => o.Trim().StartsWith("Log="))
                    .SingleOrDefault();

                if (log != null)
                    Log = new FileInfo(log.Substring(4));
            }
        }

        public ParametersDiagnosticsNative()
        { }

        public ParametersDiagnosticsNative(bool traceStatus = false, bool traceExchangeItems = false)
        {
            TraceStatus = traceStatus;
            TraceExchangeItems = traceExchangeItems;
        }

        public ParametersDiagnosticsNative(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public new const string XName = "ParametersDiagnosticsEngine";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            base.Initialise(xElement, accessor);

            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            ValueAsString = xElement.Value;
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName, base.Persist(accessor));
        }
    }
}
