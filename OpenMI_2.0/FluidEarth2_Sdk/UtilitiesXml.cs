
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public static partial class Utilities
    {
        public static class Xml
        {
            public static void ValidElement(XElement element, string elementName)
            {
                if (element.Name.LocalName != elementName)
                    throw new Exception(string.Format("<{0}/> != <{1}/>",
                        element.Name.LocalName, elementName));
            }

            public static string GetAttribute(XElement element, string attributeName)
            {
                XAttribute a = element.Attributes(attributeName).SingleOrDefault();

                if (a == null)
                    throw new Exception(string.Format("Attribute {0} missing from element {1}", attributeName, element.ToString()));

                return a.Value;
            }

            public static string GetAttribute(XElement element, string attributeName, string defaultValue)
            {
                XAttribute xAttribute = element.Attributes(attributeName).SingleOrDefault();

                return xAttribute != null ? xAttribute.Value : defaultValue;
            }

            public static bool GetAttribute(XElement element, string attributeName, bool defaultValue)
            {
                XAttribute xAttribute = element.Attributes(attributeName).SingleOrDefault();

                return xAttribute != null ? bool.Parse(xAttribute.Value) : defaultValue;
            }

            public static int GetAttribute(XElement element, string attributeName, int defaultValue)
            {
                XAttribute xAttribute = element.Attributes(attributeName).SingleOrDefault();

                return xAttribute != null ? int.Parse(xAttribute.Value) : defaultValue;
            }

            public static string[] GetAttributeCsv(XElement element, string attributeName)
            {
                return GetAttributeCsv(element, attributeName, null);
            }

            public static string[] GetAttributeCsv(XElement element, string attributeName, int? expectedLength)
            {
                string value = GetAttribute(element, attributeName);
                string[] values = value.Trim().Split(',');

                if (expectedLength != null && values.Length != expectedLength)
                    throw new Exception(string.Format("Attribute {0} conversion to csv length missmatch {1} != {2}",
                        attributeName, values.Length, expectedLength));

                return values;
            }

            public static XElement ModifiedJulianDayToXml(string element, double modifiedJulianDay)
            {
                return ModifiedJulianDayToXml(element, modifiedJulianDay, "F8");
            }

            public static XElement ModifiedJulianDayToXml(string element, double modifiedJulianDay, string precision)
            {
                return new XElement(element,
                    new XAttribute("modifiedJulianDay", modifiedJulianDay.ToString(precision)),
                    Time.ToDateTimeString(modifiedJulianDay));
            }

            public static XElement Persist(System.Exception exception)
            {
                if (exception == null)
                    return new XElement("Exception",
                        new XAttribute("type", exception.GetType()),
                        new XAttribute("time", DateTime.Now.ToString("u")),
                        new XAttribute("message", "Unknown, exception == null"));

                if (exception.Message == null)
                    return new XElement("Exception",
                        new XAttribute("type", exception.GetType()),
                        new XAttribute("time", DateTime.Now.ToString("u")),
                        new XAttribute("message", "null"));

                var xml = new XElement("Exception",
                    new XAttribute("type", exception.GetType()),
                    new XAttribute("time", DateTime.Now.ToString("u")),
                    new XAttribute("message", exception.Message));

                if (exception.TargetSite != null)
                    xml.Add(new XAttribute("target", exception.TargetSite));
                if (exception.Source != null)
                    xml.Add(new XAttribute("source", exception.Source));
                if (exception.StackTrace != null)
                    xml.Add(new XCData(exception.StackTrace));

                if (exception.InnerException != null)
                    xml.Add(Persist(exception.InnerException));

                if (exception is ReflectionTypeLoadException)
                {
                    var load = new XElement("ReflectionTypeLoadExceptions");

                    foreach (var e in ((ReflectionTypeLoadException)exception).LoaderExceptions)
                        load.Add(Persist(e));

                    xml.Add(load);
                }

                return xml;
            }

            public static XElement PersistArgumentOmi(XNamespace ns, IArgument arg, IDocumentAccessor accessor)
            {
                if (accessor != null && arg.ValueType.ToString() == typeof(Uri).ToString())
                    return new XElement(ns + "argument",
                        new XAttribute("id", arg.Id),
                        new XAttribute("value", accessor.Uri.MakeRelativeUri((Uri)arg.Value)));

                return new XElement(ns + "argument",
                    new XAttribute("id", arg.Id),
                    new XAttribute("value", arg.ValueAsString));
            }

            public static XNamespace NamespaceFluidEarthPipistrelle = "http://FluidEarth2.svn.sourceforge.net/viewvc/fluidearth/trunk/PipistrelleConsole/Pipistrelle.xsd";

            public static void ParseCachedComponent(
                IBaseLinkableComponent component, 
                XElement xCachedComponent,
                IDocumentAccessor accessor,
                out IIdentifiable identity,
                out IEnumerable<IArgument> arguments,
                out IEnumerable<IBaseInput> inputs,
                out IEnumerable<IBaseOutput> outputs)
            {
                ValidElement(xCachedComponent, "Cache");

                identity = Persistence.Identity.Parse(
                    xCachedComponent.Element(Persistence.Identity.XName), accessor);

                arguments = Persistence.Arguments.Parse(xCachedComponent, accessor);
                                     
                inputs = xCachedComponent
                    .Elements("IBaseInput")
                    .Select(e => Persistence.Parse<IBaseInput>("IBaseInput", e, accessor));

                outputs = xCachedComponent
                    .Elements("IBaseOutput")
                    .Select(e => Persistence.Parse<IBaseOutput>("IBaseOutput", e, accessor));
            }

            public static string WikiTextLog(XElement xLog)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("= Session Log");
                sb.AppendLine("* " + xLog.Attribute("time").Value);

                foreach (XElement e in xLog.Elements())
                {
                    switch (e.Name.LocalName)
                    {
                        case "Trace":
                            sb.AppendLine(WikiTextTrace(e, 1));
                            break;
                        case "Exception":
                            sb.AppendLine(WikiTextException(e, 1));
                            break;
                        default:
                            throw new NotImplementedException(e.Name.LocalName);
                    }
                }

                return sb.ToString();
            }

            public static string WikiTextTrace(XElement xTrace, int depth)
            {
                string header = "=";
                for (int n = 0; n < depth; ++n)
                    header += "=";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(header + " Trace");
                sb.AppendLine("{{{");

                foreach (XCData x in xTrace.Nodes().OfType<XCData>())
                    sb.AppendLine(x.Value);

                sb.AppendLine("}}}");

                return sb.ToString();
            }
            
            public static string WikiTextException(XElement xException, int depth)
            {
                XAttribute attr = xException.Attributes("time").SingleOrDefault();
                string time = attr != null ? attr.Value : string.Empty;
                attr = xException.Attributes("type").SingleOrDefault();
                string type = attr != null ? attr.Value : string.Empty;
                attr = xException.Attributes("message").SingleOrDefault();
                string message = attr != null ? attr.Value : string.Empty;
                attr = xException.Attributes("source").SingleOrDefault();
                string source = attr != null ? attr.Value : string.Empty;
                attr = xException.Attributes("target").SingleOrDefault();
                string target = attr != null ? attr.Value : string.Empty;

                XCData data = xException
                    .Nodes()
                    .OfType<XCData>()
                    .SingleOrDefault();

                string stack = data != null ? data.Value : string.Empty;

                string header = "=";
                for (int n = 0; n < depth; ++n)
                    header += "=";

                StringBuilder sb = new StringBuilder();

                sb.AppendLine(header + " Exception");
                sb.AppendLine(message); 
                sb.AppendLine("* Thrown");
                sb.AppendLine("** " + time);
                sb.AppendLine("* Type");
                sb.AppendLine("** " + type);
                sb.AppendLine("* Source");
                sb.AppendLine("** " + source);
                sb.AppendLine("* Target");
                sb.AppendLine("** " + target);
                sb.AppendLine("* Stack");
                string s = stack.ToString().Replace(" in ", "\r\n\tin ");
                sb.AppendLine("{{{");
                sb.AppendLine(s);
                sb.AppendLine("}}}");

                foreach (XElement inner in xException.Elements("Exception"))
                    sb.AppendLine(WikiTextException(inner, depth + 1));

                XElement load = xException
                    .Elements("ReflectionTypeLoadExceptions")
                    .SingleOrDefault();

                if (load != null)
                {
                    sb.AppendLine(header + "= Load Exceptions");

                    foreach (XElement inner in load.Elements("Exception"))
                        sb.AppendLine(WikiTextException(inner, depth + 1));
                }

                return sb.ToString();
            }

            public static IEnumerable<int> ParseValueAsInts(XElement xElement, char deliminator)
            {
                if (xElement.Value == null)
                    throw new Exception("Value null for element " + xElement.Name.LocalName);

                return xElement
                    .Value
                    .Split(deliminator)
                    .Select(s => Convert.ToInt32(s));
            }

            public static IEnumerable<double> ParseValueAsDoubles(XElement xElement, char deliminator)
            {
                if (xElement.Value == null)
                    throw new Exception("Value null for element " + xElement.Name.LocalName);

                return xElement
                    .Value
                    .Split(deliminator)
                    .Select(s => Convert.ToDouble(s));
            }

            public static string CommaReplace(string text)
            {
                return text.Replace(",", ";");
            }

            public static string CommaRestore(string text)
            {
                return text;
                //return text.Replace("&188;", ",");
            }

            public static string DescriptionCellValue(string text)
            {
                if (text == null || text.Trim() == string.Empty)
                    return string.Empty;

                text = text.Split('\n')[0].TrimEnd('\r');

                if (text.Length < 20)
                    return CommaReplace(text);

                return CommaReplace(text).Substring(0, 17) + "...";
            }
        }
    }
}
