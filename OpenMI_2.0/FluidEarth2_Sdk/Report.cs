using FluidEarth2.Sdk.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class Report : IReport
    {
        string _resourceId;
        string _caption;
        string _details;
        ReportSeverity _severity;

        public Report(ReportSeverity severity, ResourceIds id, string caption, string details)
        {
            _resourceId = id.ToString();
            _caption = caption;
            _details = details;
            _severity = severity;
        }

        public Report(System.Exception exception)
        {
            Contract.Requires(exception != null, "e != null");

            _resourceId = ResourceIds.SystemException.ToString();
            _caption = string.Format("Exception: \"{0}\"", exception.Message);
            _details = Utilities.Xml.Persist(exception).ToString();
            _severity = ReportSeverity.Error;
        }

        public Report(ReportSeverity severity, string id, string caption, string details)
        {
            _resourceId = id;
            _caption = caption;
            _details = details;
            _severity = severity;
        }

        public Report(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public const string XName = "Report";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _resourceId = Utilities.Xml.GetAttribute(xElement, "id");
            _caption = Utilities.Xml.GetAttribute(xElement, "caption");

            var severity = Utilities.Xml.GetAttribute(xElement, "severity");
            _severity = (ReportSeverity)Enum.Parse(typeof(ReportSeverity), severity);

            var xcData = xElement
                .Nodes()
                .OfType<XCData>()
                .Single();

            _details = xcData.Value;
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName, 
                new XAttribute("id", _resourceId),
                new XAttribute("severity", _severity.ToString()),
                new XAttribute("caption", _caption),
                new XCData(_details));
        }

        public string ResourceId
        {
            get { return _resourceId; }
        }

        public string Caption
        {
            get { return _caption; }
        }

        public string Details
        {
            get { return _details; }
        }

        public ReportSeverity Severity
        {
            get { return _severity; }
        }

        public string WikiSection
        {
            get
            {
                var sb = new StringBuilder();

                sb.AppendLine("== Report: " + Caption);
                sb.AppendLine(string.Format("* Severity: \"{0}\"", Severity.ToString()));
                sb.AppendLine(string.Format("* Resource ID: \"{0}\"", ResourceId));
                sb.AppendLine();

                if (Utilities.IsProbablyWikiText(Details))
                    sb.AppendLine(Details);
                else
                {
                    sb.AppendLine("{{{");
                    sb.AppendLine(Details);
                    sb.AppendLine("}}}");
                }

                /*
                var lines = Details.Split('\n');

                int nCount = lines.Count();

                if (lines.Last() == string.Empty)
                    --nCount;

                for (int n = 0; n < nCount; ++n)
                {
                    if (lines[n].EndsWith("r"))
                    {
                        sb.AppendLine(lines[n].TrimEnd('\r'));
                        sb.AppendLine();
                    }
                    else
                        sb.AppendLine(lines[n]);                    
                }
                 */

                return sb.ToString();
            }
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", Severity.ToString()[0], Caption);
        }

        /// <summary>
        /// Create an error report.
        /// </summary>
        /// <param name="key">A resource ID to be used to retrieve generic help about this report</param>
        /// <param name="caption">A short description, typically less than 100 charactors, shorter the better</param>
        /// <param name="details">Extended details, can be formatted using WikiText syntax.</param>
        /// <returns>An error report</returns>
        public static IReport Error(ResourceIds id, string caption, string details)
        {
            return new Report(ReportSeverity.Error, id, caption, details);
        }

        /// <summary>
        /// Create a warning report.
        /// </summary>
        /// <param name="key">A resource ID to be used to retrieve generic help about this report</param>
        /// <param name="caption">A short description, typically less than 100 charactors, shorter the better</param>
        /// <param name="details">Extended details, can be formatted using WikiText syntax.</param>
        /// <returns>A warning report</returns>
        public static IReport Warning(ResourceIds id, string caption, string details)
        {
            return new Report(ReportSeverity.Warning, id, caption, details);
        }

        /// <summary>
        /// Create an informative report.
        /// </summary>
        /// <param name="key">A resource ID to be used to retrieve generic help about this report</param>
        /// <param name="caption">A short description, typically less than 100 charactors, shorter the better</param>
        /// <param name="details">Extended details, can be formatted using WikiText syntax.</param>
        /// <returns>An informative report</returns>
        public static IReport Info(string caption, string details)
        {
            return new Report(ReportSeverity.Info, ResourceIds.Informative, caption, details);
        }

        /// <summary>
        /// There should be a enum member for every resource in ResourceManager HelpText.
        /// User should use this enum value.ToString() to set  
        /// </summary>
        public enum ResourceIds { Informative = 0, ToImplement,
            SystemException,
            FileMissing,
            XmlSchemaValidation, XmlSchemaValidationEvent,
            Instantiation,
            InvalidOmiArgumentKey, SetArgumentValueAsString, 
            InvalidAssemblyName, InvalidUri,
        }

        public static bool GetHelpText(IReport report, out string help)
        {
            try
            {
                help = string.Format("{0} is not a member of {1}",
                    report.ResourceId, typeof(ResourceIds).FullName);

                if (!Enum.GetNames(typeof(ResourceIds)).Contains(report.ResourceId))
                    return false;

                help = HelpText.ResourceManager.GetString(report.ResourceId, HelpText.Culture);
                return true;
            }
            catch (System.Exception e)
            {
                help = e.Message;
                return false;
            }
        }

        public static void TraceIt(IReport report)
        {
            switch (report.Severity)
            {
                case ReportSeverity.Error:
                    Trace.TraceError("{0}\r\n\t{1}", report.Caption, report.Details);
                    break;
                case ReportSeverity.Warning:
                    Trace.TraceWarning("{0}\r\n\t{1}", report.Caption, report.Details);
                    break;
                default:
                    Trace.TraceInformation("{0}\r\n\t{1}", report.Caption, report.Details);
                    break;
            }
        }

        public static IReport Aggregate(List<IReport> reports)
        {
            // Combine into single report

            var details = reports
                .Aggregate(new StringBuilder(), (sb, r) => sb.AppendLine(
                    string.Format("{0}: {1}", r.Severity.ToString()[0], r.Details)))
                .ToString();

            var nWarnings = reports.Count(r => r.Severity == ReportSeverity.Warning);
            var nErrors = reports.Count(r => r.Severity == ReportSeverity.Error);

            var severity = ReportSeverity.OK;

            if (nWarnings > 0)
                severity = ReportSeverity.Warning;
            if (nErrors > 0)
                severity = ReportSeverity.Error;

            if (severity == ReportSeverity.OK)
                return new Report(severity,
                    Report.ResourceIds.XmlSchemaValidation,
                    "OK",
                    details);

            return new Report(severity,
                Report.ResourceIds.XmlSchemaValidation,
                string.Format("Error(s) {0} Warning(s) {1}", 
                    nErrors, nWarnings),
                details);
        }
    }
}
