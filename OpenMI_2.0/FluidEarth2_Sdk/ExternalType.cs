
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ExternalType<TType> : ExternalType, IExternalType<TType>
        where TType : class
    {
        public ExternalType()
        { }

        public ExternalType(TType instance)
            : base(instance)
        { }

        public ExternalType(Type type)
            : base(type)
        { }

        public ExternalType(IExternalType type)
            : base(type)
        { }

        public ExternalType(string assemblyPath, string typeName)
            : base(assemblyPath, typeName)
        { }

        public ExternalType(IDocumentAccessor accessor)
            : base(accessor)
        { }

        public ExternalType(XElement xElement, IDocumentAccessor accessor)
            : base(xElement, accessor)
        { }

        public TType Instance
        {
            get 
            {
                Type type;
                return CreateInstance(out type) as TType;
            }
        }

        public bool Equals(IExternalType<TType> other)
        {
            if (other == null)
                return false;

            if (other is ExternalType)
                Equals((ExternalType)other);

            return false;
        }
    }

    public class ExternalType : Identity, IExternalType
    {
        protected object _instance;
        AssemblyName _assemblyName;
        string _typeName = string.Empty;
        IDocumentAccessor _accessor;

        public Uri Url
        {
            get 
            {
                if (_assemblyName == null)
                    return null;

                return new Uri(_assemblyName.CodeBase, UriKind.RelativeOrAbsolute); 
            }
        }

        public AssemblyName AssemblyName
        {
            get { return _assemblyName; }
            set { _assemblyName = value; }
        }

        public string TypeName
        {
            get { return _typeName; }
            set { _typeName = value != null ? value : string.Empty; }
        }

        public ExternalType()
        {
        }

        public ExternalType(object instance)
        {
            Initialise(instance.GetType());

            _instance = instance;
        }

        public ExternalType(Type type)
        {
            Initialise(type);
        }

        public ExternalType(ExternalType type)
        {
            Initialise(type);
        }

        public ExternalType(string assemblyPath, string typeName)
        {
            _accessor = new DocumentExistingFile(
                Utilities.AssemblyUri(Assembly.GetCallingAssembly()));
        
            Initialise(assemblyPath, typeName);
        }

        public ExternalType(IDocumentAccessor accessor)
        {
            _accessor = accessor;
        }

        public ExternalType(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        public override object Clone()
        {
            ExternalType xt = new ExternalType(_accessor);
            if (_assemblyName != null)
                xt.Initialise((AssemblyName)_assemblyName.Clone(), _typeName);
            return xt;
        }

        public IDocumentAccessor DocumentAccessor
        {
            get { return _accessor; }
            set { _accessor = value; } 
        }

        public bool IsInstantiated
        {
            get { return _instance != null; }
        }

        public const string XName = "ExternalType";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            if (xElement == null)
            {
                _instance = null;
                _accessor = null;
                _typeName = null;
                _assemblyName = null;
                Caption = string.Empty;

                return;
            }

            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _accessor = accessor;

            _typeName = Utilities.Xml.GetAttribute(xElement, "type");

            var xIdentity = Persistence.ThisOrSingleChild(Persistence.Identity.XName, xElement, true);

            if (xIdentity != null)
                SetIdentity(Persistence.Identity.Parse(xIdentity, accessor));
            else
                Caption = _typeName;

            var xAssemblyName = xElement
                .Elements("AssemblyName")
                .SingleOrDefault();

            var assembly = xAssemblyName != null
                ? xAssemblyName.Value
                : string.Empty;

            // Code-base
            // If relative exists use, else if original exists use, else do not specify at all.

            var xCodeBase = xElement
                .Elements("CodeBase")
                .SingleOrDefault();

            Uri codeBaseUri = null;

            if (xCodeBase != null && !xCodeBase.IsEmpty)
            {
                if (accessor != null)
                {
                    var xRelative = xCodeBase
                        .Elements("Relative")
                        .SingleOrDefault();

                    if (xRelative != null)
                    {
                        var relative = new Uri(accessor.Uri, xRelative.Value);

                        if (File.Exists(relative.LocalPath))
                            codeBaseUri = relative;
                    }
                }

                if (codeBaseUri == null)
                {
                    var xOriginal = xCodeBase
                        .Elements("Original")
                        .SingleOrDefault();

                    if (xOriginal != null)
                    {
                        var original = new Uri(xOriginal.Value);

                        if (File.Exists(original.LocalPath))
                            codeBaseUri = original;
                    }
                }
            }

            _assemblyName = null;

            var reports = new List<IReport>();

            if (AssemblyNameGet(assembly, codeBaseUri, _typeName, reports, accessor, out _assemblyName))
                Description = _assemblyName.FullName;
            else
                Description = string.Format("Cannot deduce AssemblyName from \"{0}\", \"{1}\", \"{2}\"",
                    assembly, _typeName, accessor == null || accessor.Uri == null
                        ? string.Empty : accessor.Uri.ToString());
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            var xml = new XElement(XName,
                Persistence.Identity.Persist(this, accessor),
                new XAttribute("type", _typeName));

            if (_assemblyName != null)
            {
                xml.Add(new XElement("AssemblyName", _assemblyName.FullName));

                if (_assemblyName.CodeBase != null)
                {
                    var xCodebase = new XElement("CodeBase",
                        new XElement("Original", _assemblyName.CodeBase));

                    if (accessor != null && accessor.Uri != null)
                    {
                        var codeBase = new Uri(_assemblyName.CodeBase);
                        codeBase = accessor.Uri.MakeRelativeUri(codeBase);
                    
                        xCodebase.Add(
                            new XElement("Relative", codeBase.OriginalString));
                    }

                    xml.Add(xCodebase);
                }
            }

            return xml;
        }

        public void Initialise(Type type)
        {
            Contract.Requires(type != null, "type != null");

            _instance = null;

            _assemblyName = AssemblyName.GetAssemblyName(type.Assembly.Location);
            _typeName = type.FullName;

            Caption = _typeName;
            Description = _assemblyName.FullName;

            CheckAssemblyName();
        }

        public void Initialise(IExternalType externalType)
        {
            Contract.Requires(externalType != null, "externalType != null");

            _instance = null;

            _assemblyName = externalType.AssemblyName;
            _typeName = externalType.TypeName;

            Caption = _typeName;
            Description = _assemblyName != null ?  _assemblyName.FullName : string.Empty;

            CheckAssemblyName();
        }

        public void Initialise(string assemblyLocation, string className)
        {
            _instance = null;

            if (!File.Exists(assemblyLocation))
            {
                Uri relativeTo = _accessor != null
                    ? _accessor.Uri : null;

                if (relativeTo == null)
                    relativeTo = Utilities.AssemblyUri(Assembly.GetCallingAssembly());

                var uri = new Uri(relativeTo, assemblyLocation);

                assemblyLocation = uri.LocalPath;
            }

            if (!File.Exists(assemblyLocation))
                throw new Exception(string.Format(
                    "ExternalType assemblyLocation \"{0}\" irresolvable to assembly", assemblyLocation));

            _assemblyName = AssemblyName.GetAssemblyName(assemblyLocation);

            _typeName = className;

            Caption = _typeName;
            Description = _assemblyName.FullName;

            CheckAssemblyName();
        }

        public void Initialise(AssemblyName assemblyName, string typename)
        {
            _instance = null;
            _assemblyName = assemblyName;
            _typeName = typename;

            Caption = _typeName;
            Description = _assemblyName.FullName;

            CheckAssemblyName();
        }

        public override string ToString()
        {
            return TypeName;
        }

        public static bool AssemblyNameGet(string assembly, Uri codeBaseUri, string type, List<IReport> reports, IDocumentAccessor accessor, out AssemblyName assemblyName)
        {
            Contract.Requires(assembly != null, "assembly != null");
            Contract.Requires(type != null, "type != null");
            Contract.Requires(reports != null, "reports != null");

            assemblyName = null;

            if (!File.Exists(Uri.UnescapeDataString(assembly)))
            {
                if (assembly.Trim() != string.Empty)
                {
                    try
                    {
                        assemblyName = new AssemblyName(assembly);

                        if (assemblyName != null && assemblyName.CodeBase != null)
                            return true;
                    }
                    catch (System.Exception)
                    { }
                }

                var t = Type.GetType(type, false);

                if (t != null)
                {
                    assemblyName = t.Assembly.GetName();
                    return true;
                }

                var entryAssembly = Assembly.GetEntryAssembly();

                if (entryAssembly.GetTypes().Any(x => x.FullName == type))
                {
                    assemblyName = entryAssembly.GetName();
                    return true;
                }

                var callingAssembly = Assembly.GetCallingAssembly();

                if (callingAssembly.GetTypes().Any(x => x.FullName == type))
                {
                    assemblyName = callingAssembly.GetName();
                    return true;
                }

                if (codeBaseUri != null)
                {
                    var a = Assembly.LoadFile(codeBaseUri.LocalPath);
                    assemblyName = a.GetName();
                    return true;
                }

                if (string.IsNullOrWhiteSpace(assembly))
                {
                    reports.Add(Report.Error(Report.ResourceIds.InvalidAssemblyName,
                        "Invalid AssemblyName",
                        "Is null or just whitespace"));
                    return false;
                }

                if (assembly.Contains(','))
                {
                    reports.Add(Report.Error(Report.ResourceIds.InvalidAssemblyName,
                        "Invalid AssemblyName",
                        string.Format("\"{0}\" contains commas so assumed to be an AssemblyName.FullName rather than URL."
                            + "\r\nCould not find it in GAC", assembly)));
                    return false;
                }
            }

            // Assume file based

            var localPath = Utilities.LocalPath(assembly, accessor);
  
            try
            {
                Trace.TraceInformation(string.Format("AssemblyName.GetAssemblyName({0});", localPath));

                assemblyName = AssemblyName.GetAssemblyName(localPath);

                Trace.TraceInformation(string.Format("\t= {0}", assemblyName.FullName));
            }
            catch (System.Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.GetType().ToString());

                reports.Add(Report.Error(Report.ResourceIds.InvalidUri,
                    string.Format("AssemblyName.GetAssemblyName({0}) Exception",
                    localPath),
                    e.Message));
                return false;
            }

            return true;
        }

        string CheckAssemblyName()
        {
            // Now for ... _assemblyName.ProcessorArchitecture ... documentation says ...
            // Beginning with the .NET Framework version 4, this property always returns ProcessorArchitecture.None for reference assemblies.
            // so my tests no longer provide useful knowledge (sigh)

            return string.Empty;

#if DEFUNCT
            string warning = string.Empty;

            Assembly entry = Assembly.GetEntryAssembly();

            if (entry == null)
                return warning; // cant check, maybe running in Unit test environment

            AssemblyName nameEntry = entry.GetName();
            AssemblyName nameThis = Assembly.GetExecutingAssembly().GetName();

            if (nameEntry.ProcessorArchitecture != _assemblyName.ProcessorArchitecture
                || nameThis.ProcessorArchitecture != _assemblyName.ProcessorArchitecture)
            {
                /* Entry architecture all that matters if SDK is any MSIL as 
                 * SDK will be running in required domain, hopefully!
                 */

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Processor Architecture missmatch");
                sb.AppendLine(string.Format(
                    "Processor Architecture for CodeBase \"{0}\" is \"{1}\".",
                    _assemblyName.CodeBase, _assemblyName.ProcessorArchitecture.ToString()));
                sb.AppendLine(string.Format(
                    "This is different to entry \"{0}\" architecture of \"{1}\"",
                    nameEntry.FullName,
                    nameEntry.ProcessorArchitecture.ToString()));
                sb.AppendLine(string.Format(
                    " and/or SDK \"{0}\" architecture of \"{1}\".",
                    nameThis.FullName,
                    nameThis.ProcessorArchitecture.ToString()));
                sb.Append("This might cause a BadImageFormatException exception.");
                sb.AppendLine(" If case then either recompile assembly to match this Fluid Earth platform target or use different Fluid Earth platform.");
                sb.AppendLine("Architectures are None (unknown), MSIL (neutral/any), X86 (Intel 32-bit or 'Windows on Windows' on 64-bit), IA64 (Intel 64-bit only), Amd64 (AMD 64-bit only)");
                warning = sb.ToString();

                Trace.TraceWarning(warning);
            }

            return warning;

#endif
        }

        public virtual object CreateInstance(out Type type)
        {
            if (_instance != null)
            {
                type = _instance.GetType();
                return _instance;
            }

            type = null;

            try
            {
                if (_typeName == null || _assemblyName == null)
                    return null;

                var assembly = Assembly.Load(_assemblyName);

                if (assembly == null)
                    throw new Exception("Assembly load failed: " + CheckAssemblyName());

                type = assembly.GetType(_typeName);

                if (type == null)
                {
                    var e = new StringBuilder();
                    e.AppendLine(string.Format(
                        "Type \"{0}\" not found. Available types ...", _typeName));

                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();

                        e.AppendLine(
                            assembly.GetTypes()
                            .OrderBy(t => t.FullName)
                            .Aggregate(new StringBuilder(), (sb, t) => sb.AppendLine(t.FullName)).ToString());

                        foreach (Type t in types)
                            Trace.TraceWarning(e.ToString());

                        throw new Exception(e.ToString());
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        foreach (System.Exception ex2 in ex.LoaderExceptions)
                            Trace.TraceError(ex2.Message);
                        throw;
                    }
                }

                object obj = null;

                if (type.FullName == "System.String")
                    obj = String.Empty; // no default constructor for new
                else
                    obj = type.Assembly.CreateInstance(type.FullName);

                if (obj == null)
                    throw new Exception("Cannot create instance");

                return obj;
            }
            catch (Exception exception)
            {
                throw new Exception(string.Format(
                    "Cannot create instance of {0} from assembly {1} ({2})",
                    _typeName, _assemblyName.FullName,
                    _assemblyName.CodeBase != null ? _assemblyName.CodeBase : string.Empty), 
                    exception);
            }
        }

        public EValidation Validate(out string message)
        {
            if (AssemblyName == null)
            {
                message = "AssemblyName is null";
                return EValidation.Error;
            }

            if (AssemblyName.FullName == null
                || AssemblyName.FullName == string.Empty)
            {
                message = "AssemblyName.FullName is unset";
                return EValidation.Error;
            }

            if (AssemblyName.CodeBase == null
                || AssemblyName.CodeBase == string.Empty)
            {
                message = "AssemblyName.CodeBase is unset";
                return EValidation.Warning;
            }

            Uri uriCodebase;

            if (!Uri.TryCreate(AssemblyName.CodeBase, UriKind.Absolute, out uriCodebase))
            {
                message = string.Format("AssemblyName.CodeBase \"{0}\" not valid absolute Url", AssemblyName.CodeBase);
                return EValidation.Warning;
            }

            if (!File.Exists(uriCodebase.LocalPath))
            {
                message = string.Format("AssemblyName.CodeBase \"{0}\" not found", uriCodebase.LocalPath);
                return EValidation.Warning;
            }

            if (TypeName == null
                || TypeName == string.Empty)
            {
                message = "TypeName is unset";
                return EValidation.Error;
            }

            message = string.Format("{0}\r\n{1}\r\n{2}",
                            TypeName, AssemblyName.FullName, AssemblyName.CodeBase);

            return EValidation.Valid;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is ExternalType)
                Equals((ExternalType)obj);

            return false;
        }

        public bool Equals(IExternalType other)
        {
            if ((object)other == null)
                return false;

            return _assemblyName.FullName == other.AssemblyName.FullName
                && _typeName == other.TypeName;       
        }

        public bool Equals(ExternalType other)
        {
            if ((object)other == null)
                return false;

            return _assemblyName.FullName == other._assemblyName.FullName
                && _typeName == other._typeName;
        }

        public override int GetHashCode()
        {
            return _assemblyName.FullName.GetHashCode() ^ _typeName.GetHashCode();
        }

        public static bool operator ==(ExternalType e1, ExternalType e2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(e1, e2))
                return true;

            // If one is null, but not both, return false.
            if (((object)e1 == null) || ((object)e2 == null))
                return false;

            return e1.Equals(e2);
        }

        public static bool operator !=(ExternalType e1, ExternalType e2)
        {
            return !(e1 == e2);
        }
    }
}

