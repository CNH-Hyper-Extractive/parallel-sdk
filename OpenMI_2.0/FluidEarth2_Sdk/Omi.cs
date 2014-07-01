using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using System.Diagnostics;
using OpenMI.Standard2;
 
namespace FluidEarth2.Sdk
{
    public static class Omi
    {
        public static class Component
        {
            public enum Version { Unknown = 0, One, OneFluidEarth, Two, }

            public static bool Instantiate(
                ExternalType componentType,
                SupportedPlatforms platforms,
                List<IReport> reports,
                out IBaseLinkableComponent component)
            {
                Contract.Requires(componentType != null, "componentType != null");
                Contract.Requires(reports != null, "reports != null");

                component = null;

                Type type;

                try
                {
                    component = componentType
                        .CreateInstance(out type) as IBaseLinkableComponent;
                }
                catch (System.Exception e)
                {
                    reports.Add(Report.Error(Report.ResourceIds.Instantiation,
                        "Cannot instantiate IBaseLinkableComponent", e.Message));
                    return false;
                }

                if (component == null)
                {
                    reports.Add(Report.Error(Report.ResourceIds.Instantiation,
                        "Cannot instantiate IBaseLinkableComponent", type.ToString()));
                    return false;
                }

                return true;
            }

            public static bool OmiArgumentValuesSet(
                IBaseLinkableComponent component,
                List<Argument> arguments,
                IDocumentAccessor accessor,
                List<IReport> reports)
            {
                foreach (var argument in arguments)
                {
                    var arg = component.Arguments
                        .Where(a => a.Id == argument.Key)
                        .SingleOrDefault();

                    if (arg == null)
                        arg = component.Arguments
                            .Where(a => a.Caption == argument.Key)
                            .SingleOrDefault();

                    if (arg != null)
                        ArgumentValueSet(arg, argument, reports, accessor);
                    else
                        reports.Add(Report.Warning(Report.ResourceIds.InvalidOmiArgumentKey,
                            "Cannot find component argument from OMI Key", argument.Key));
                }

                return !reports.Any(r => r.Severity == ReportSeverity.Error);
            }

            public static Version GetVersion(XElement xLinkableComponent)
            {
                var ns = xLinkableComponent.Name.Namespace;

                if (ns == NamespaceOpenMIv2)
                    return Version.Two;

                var ns1 = Omi.Component.NamespaceOpenMIv1;

                if (ns != ns1)
                    return Version.Unknown;

                Contract.Requires(xLinkableComponent.Name == ns1.GetName("LinkableComponent"),
                    "xLinkableComponent.Name == Omi.Component.NamespaceOpenMIv1.GetName(\"LinkableComponent\")");

                var xArguments = xLinkableComponent
                    .Elements(ns1.GetName("Arguments"))
                    .SingleOrDefault();

                if (xArguments == null)
                    return Version.One;

                var args1 = Utilities.Standard1.Arguments1(xArguments, ns1, null);

                if (args1.Any(a => a.Key.Contains("FluidEarth") || a.Key.Contains("OpenWEB")))
                    return Version.OneFluidEarth;

                return Version.One;
            }

            public static bool GetArgumentsV1(
                XElement xLinkableComponent,
                IDocumentAccessor accessor,
                out List<Argument> argumentsOmi,
                List<IReport> reports)
            {
                Contract.Requires(reports != null, "reports != null");

                argumentsOmi = new List<Argument>();

                var ns = xLinkableComponent.Name.Namespace;

                if (!Validates(xLinkableComponent, Xsd.ComponentV1, reports))
                    return false;

                var xArgs = xLinkableComponent.Elements(NamespaceOpenMIv1.GetName(XArguments)).SingleOrDefault();
                var xArguments = xArgs != null
                    ? xArgs.Elements(NamespaceOpenMIv1.GetName(XArgument))
                    : null;

                if (xArguments != null)
                    argumentsOmi.AddRange(xArguments
                        .Select(x => new Argument(x, accessor)));

                return true;
            }

            const string XLinkableComponent = "LinkableComponent";
            const string XAssembly = "Assembly";
            const string XType = "Type";
            const string XArguments = "Arguments";
            const string XArgument = "Argument";
            const string XPlatforms = "Platforms";

            public static bool Parse( 
                XElement xLinkableComponent,
                IDocumentAccessor accessor,
                List<IReport> reports,
                out ExternalType component,
                out SupportedPlatforms platforms,
                out List<Argument> argumentsOmi)
            {
                Contract.Requires(reports != null, "reports != null");

                component = null;
                platforms = SupportedPlatforms.Unknown;
                argumentsOmi = new List<Argument>();

                var ns = xLinkableComponent.Name.Namespace;

                if (!Validates(xLinkableComponent, Xsd.ComponentV2, reports))
                    return false;

                var xArgs = xLinkableComponent.Elements(NamespaceOpenMIv2.GetName(XArguments)).SingleOrDefault();
                var xArguments = xArgs != null
                    ? xArgs.Elements(NamespaceOpenMIv2.GetName(XArgument))
                    : null;

                var xPlatforms = xLinkableComponent.Elements(NamespaceOpenMIv2.GetName(XPlatforms)).SingleOrDefault();

                var assembly = Utilities.Xml.GetAttribute(xLinkableComponent, XAssembly);
                var type = Utilities.Xml.GetAttribute(xLinkableComponent, XType);

                AssemblyName assemblyName;
                Uri codeBaseUri = null;

                if (!ExternalType.AssemblyNameGet(assembly, codeBaseUri, type, reports, accessor, out assemblyName))
                    return false;

                component = new ExternalType(accessor);
                component.Initialise(assemblyName, type);

                if (xArguments != null)
                    argumentsOmi.AddRange(xArguments
                        .Select(x => new Argument(x, accessor)));

                platforms = Platforms.Parse(xPlatforms, accessor);

                return true;
            }

            public static XElement Persist(
                IBaseLinkableComponent component,
                SupportedPlatforms platforms,
                IDocumentAccessor accessor)
            {
                // Original intention was to filter out arguments that were read only or 
                // set explicitly to their default values to simplify what user sees by reducing
                // verbosity. However, components that aggregate other components might need to process
                // all the aggregated component arguments before those aggregated components ever get
                // instantiated (when defaults etc. would normally become visible).
                // Hence, welcome verbosity my DEAR old friend.

                Contract.Requires(component != null, "component != null");

                var args = component
                    .Arguments
                    .Select(a => ArgumentValueGet(a, accessor));

                return Persist(component, platforms, args, accessor);
            }

            public static XElement Persist(
                IBaseLinkableComponent component,
                SupportedPlatforms platforms,
                IEnumerable<Argument> argumentsOmi,
                IDocumentAccessor accessor)
            {
                Contract.Requires(component != null, "component != null");
                Contract.Requires(argumentsOmi != null, "arguments != null");

                var location = new Uri(component.GetType().Assembly.Location);

                if (accessor != null && accessor.Uri != null)
                    location = accessor.Uri.MakeRelativeUri(location);

                return new XElement(NamespaceOpenMIv2 + "LinkableComponent",
                    new XAttribute("Type", component.GetType().ToString()),
                    new XAttribute("Assembly", location.ToString()),
                    new XElement(NamespaceOpenMIv2 + "Arguments",
                        argumentsOmi.Select(a => a.Persist(accessor))),
                    Platforms.Persist(platforms, accessor));
            }

            public static bool IsUri(Type type)
            {
                return type.ToString() == typeof(Uri).ToString()
                    || type.ToString() == typeof(DirectoryInfo).ToString()
                    || type.ToString() == typeof(FileInfo).ToString();       
            }

            public static void ArgumentValueSet(IArgument arg, Argument argOmi, List<IReport> reports, IDocumentAccessor accessor)
            {
                Contract.Requires(arg != null, "arg != null");
                Contract.Requires(argOmi != null, "argValue != null");
                Contract.Requires(reports != null, "reports != null");

                string valueAsString = argOmi.Value;

                try
                {
                    if (arg is IArgumentProposed)
                        valueAsString = ((IArgumentProposed)arg).MakeAbsolute(
                            valueAsString,
                            accessor != null ? accessor.Uri : null);
                    else
                    {
                        var isUri = IsUri(arg.ValueType);

                        if (isUri && accessor != null && accessor.Uri != null)
                        {
                            // Type is not FluidEarth but is recognised as being 
                            // something we can resolve potential relative path issues

                            valueAsString = new Uri(accessor.Uri, valueAsString).LocalPath;
                        }
                    }

                    arg.ValueAsString = valueAsString;
                }
                catch (System.Exception e)
                {
                    reports.Add(Report.Error(Report.ResourceIds.SetArgumentValueAsString,
                        "Cannot set argument value using ValueAsString",
                        string.Format("\"{0}\".ValueAsString = \"{1}\", {2}",
                        arg.Id, valueAsString, e.Message)));
                }
            }

            public static Argument ArgumentValueGet(IArgument arg, IDocumentAccessor accessor)
            {
                Contract.Requires(arg != null, "arg != null");

                string valueAsString = arg.ValueAsString;

                if (arg is IArgumentProposed)
                    valueAsString = ((IArgumentProposed)arg).MakeRelative(
                        valueAsString,
                        accessor != null ? accessor.Uri : null);
                else
                {
                    var isUri = IsUri(arg.ValueType);

                    if (isUri && accessor != null && accessor.Uri != null && arg.Value != null)
                    {
                        // Type is not FluidEarth but is recognised as being 
                        // something we can resolve potential relative path issues

                        valueAsString = accessor.Uri
                            .MakeRelativeUri((Uri)arg.Value).LocalPath;
                    }
                }

                var key = arg is ArgumentStandard1 ? arg.Caption : arg.Id;

                return new Argument(key, valueAsString, arg.IsReadOnly);
            }

            static bool ComponentAssemblyNameGetWrapped(Uri containerUri, string type, List<IReport> reports, out AssemblyName assemblyName)
            {
                Contract.Requires(containerUri != null, "containerUri != null");
                Contract.Requires(type != null, "type != null");
                Contract.Requires(reports != null, "reports != null");

                reports.Add(Report.Error(Report.ResourceIds.ToImplement,
                    "TODO: Wrap native code component",
                    "TODO: Return a .NET wrapped OMI native code component"));

                assemblyName = null;

                return false;
            }

            public static XNamespace NamespaceOpenMIv2 = "http://www.openmi.org/v2_0"; 
            public static XNamespace NamespaceOpenMIv1 = "http://www.openmi.org/LinkableComponent.xsd";

            public static bool Validates(XElement xml, Stream xsd, List<IReport> reports)
            {
                Contract.Requires(xml != null, "xml != null");
                Contract.Requires(reports != null, "reports != null");

                var validator = new Validator();

                var ok = validator.ValidatesAgainstSchema(new XDocument(xml), xsd);

                reports.Add(validator.ValidationReport);

                return ok;
            }
        }

        public class Argument : IPersistence
        {
            public string Key { get; private set; }
            public string Value { get; private set; }
            public bool ReadOnly { get; private set; }

            public Argument(string key, string valueAsString, bool isReadOnly)
            {
                Key = key;
                Value = valueAsString;
                ReadOnly = isReadOnly;
            }

            public Argument(XElement xElement, IDocumentAccessor accessor)
            {
                Initialise(xElement, accessor);
            }

            public void Initialise(XElement xElement, IDocumentAccessor accessor)
            {
                Contract.Requires(xElement != null, "xElement != null");

                Key = Utilities.Xml.GetAttribute(xElement, "Key");
                Value = Utilities.Xml.GetAttribute(xElement, "Value");
                ReadOnly = Utilities.Xml.GetAttribute(xElement, "ReadOnly", false);
            }

            public XElement Persist(IDocumentAccessor accessor)
            {
                return new XElement(Component.NamespaceOpenMIv2 + "Argument",
                    new XAttribute("Key", Key),
                    new XAttribute("Value", Value),
                    new XAttribute("ReadOnly", ReadOnly));
            }

            public override string ToString()
            {
                return Key;
            }
        }

        public static class Platforms
        {
            public static SupportedPlatforms Parse(XElement xElement, IDocumentAccessor accessor)
            {
                Contract.Requires(xElement != null, "xElement != null");

                XName XPlatform = Component.NamespaceOpenMIv2 + "Platform";

                var values = xElement
                    .Elements(XPlatform)
                    .Select(p => p.Value);
                
                var platforms = SupportedPlatforms.Unknown;

                foreach (var v in values)
                    platforms |= Parse(v);

                return platforms;
            }

            public static XElement Persist(SupportedPlatforms platforms, IDocumentAccessor accessor)
            {
                return new XElement(Component.NamespaceOpenMIv2 + "Platforms",
                    Persist(platforms).Select(value => new XElement(Component.NamespaceOpenMIv2 + "Platform", value)));
            }

            static List<string> Persist(SupportedPlatforms platforms)
            {
                var values = new List<string>();

                if ((platforms & SupportedPlatforms.Win) != 0)
                    values.Add(SupportedPlatforms.Win.ToString());
                if ((platforms & SupportedPlatforms.Win32) != 0)
                    values.Add(SupportedPlatforms.Win32.ToString());
                if ((platforms & SupportedPlatforms.Win64) != 0)
                    values.Add(SupportedPlatforms.Win64.ToString());

                if ((platforms & SupportedPlatforms.Unix) != 0)
                    values.Add(SupportedPlatforms.Unix.ToString());
                if ((platforms & SupportedPlatforms.Unix32) != 0)
                    values.Add(SupportedPlatforms.Unix32.ToString());
                if ((platforms & SupportedPlatforms.Unix64) != 0)
                    values.Add(SupportedPlatforms.Unix64.ToString());

                if ((platforms & SupportedPlatforms.Linux) != 0)
                    values.Add(SupportedPlatforms.Linux.ToString());
                if ((platforms & SupportedPlatforms.Linux32) != 0)
                    values.Add(SupportedPlatforms.Linux32.ToString());
                if ((platforms & SupportedPlatforms.Linux64) != 0)
                    values.Add(SupportedPlatforms.Linux64.ToString());

                if ((platforms & SupportedPlatforms.Mac) != 0)
                    values.Add(SupportedPlatforms.Mac.ToString());
                if ((platforms & SupportedPlatforms.Mac32) != 0)
                    values.Add(SupportedPlatforms.Mac32.ToString());
                if ((platforms & SupportedPlatforms.Mac64) != 0)
                    values.Add(SupportedPlatforms.Mac64.ToString());

                return values;
            }

            static SupportedPlatforms Parse(string platform)
            {
                platform = platform.ToLower();

                switch (platform)
                {
                    case "win":
                        return SupportedPlatforms.Win;
                    case "unix":
                        return SupportedPlatforms.Unix;
                    case "linux":
                        return SupportedPlatforms.Linux;
                    case "mac":
                        return SupportedPlatforms.Mac;
                    case "win32":
                        return SupportedPlatforms.Win32;
                    case "win64":
                        return SupportedPlatforms.Win64;
                    case "unix32":
                        return SupportedPlatforms.Unix32;
                    case "unix64":
                        return SupportedPlatforms.Unix64;
                    case "linux32":
                        return SupportedPlatforms.Linux32;
                    case "linux64":
                        return SupportedPlatforms.Linux64;
                    case "mac32":
                        return SupportedPlatforms.Mac32;
                    case "mac64":
                        return SupportedPlatforms.Mac64;
                    default:
                        throw new NotImplementedException(platform);
                }
            }
        } 
    }
}
