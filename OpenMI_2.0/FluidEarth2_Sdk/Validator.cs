
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class Validator : IXmlValidation
    {
        List<IReport> _reports = new List<IReport>();

        void OnSchemaValidation(object sender, ValidationEventArgs e)
        {
            var severity = e.Severity == XmlSeverityType.Error
                ? ReportSeverity.Error
                : ReportSeverity.Warning;

            var r = new Report(severity,
                Report.ResourceIds.XmlSchemaValidationEvent,
                "Validation Event",
                e.Message);

            _reports.Add(r);
        }

        public IReport ValidationReport
        {
            get { return Report.Aggregate(_reports); }        
        }

        public bool ValidatesAgainstSchema(XDocument xml, Stream xsd)
        {
            Contract.Requires(xml != null, "xml != null");
            Contract.Requires(xsd != null, "xsd != null");

            _reports.Clear();

            try
            {
                var settings = new XmlReaderSettings();

                settings.ValidationFlags =
                        XmlSchemaValidationFlags.
                            ProcessIdentityConstraints |
                        XmlSchemaValidationFlags.
                            ReportValidationWarnings;

                var xr = XmlReader.Create(xsd, settings);

                var schemas = new XmlSchemaSet();
                schemas.Add(null, xr);

                xml.Validate(schemas, OnSchemaValidation);
            }
            catch (System.Exception e)
            {
                _reports.Add(Report.Error(
                    Report.ResourceIds.XmlSchemaValidation,
                    "Schema Validation System.Exception",
                    e.Message));

                return false;
            }

            return !_reports.Any(r => r.Severity == ReportSeverity.Error);
        }
    }
}
