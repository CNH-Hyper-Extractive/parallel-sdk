using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public static class Persistence
    {
        public const string XPersistence = "Persistence";

        public static TType Parse<TType>(XName xName, XElement xElement, IDocumentAccessor accessor)
            where TType : class
        {
            xElement = ThisOrSingleChild(xName, xElement);

            if (xElement.Elements().Count() == 0)
                return null;

            var xType = xElement
                .Elements("ExternalType")
                .SingleOrDefault();

            var externalType = new ExternalType();
            externalType.Initialise(xType, accessor);

            Type type;
            var instance = externalType.CreateInstance(out type);

            if (instance == null)
                throw new Exception(string.Format("CreateInstance created {0} not {1}",
                    type.ToString(), typeof(TType).ToString()));

            var iPersistance = instance as IPersistence;

            if (iPersistance != null)
                iPersistance.Initialise(xElement.Elements(XPersistence).Single(), accessor);
            else if (instance is IValueDefinition)
            {
                // CoreStandard2 items do not support IPersistence so specific code required
                // A third party IValueDefinition will be persisted as its equivalent CoreStandard2

                if (instance is IQuantity)
                    instance = Persistence.Quantity.Parse(xElement, accessor);
                else if (instance is IQuality)
                    instance = Persistence.Quality.Parse(xElement, accessor);
                else
                    instance = Persistence.ValueDefinition.Parse(xElement, accessor);
            }
            else if (instance is ISpatialDefinition)
                instance = Persistence.SpatialDefinition.Parse(xElement, accessor);

            return instance as TType;
        }

        public static XElement Persist<TType>(XName xName, TType value, IDocumentAccessor accessor)
            where TType : class
        {
            if (value == null)
                return new XElement(xName, "null");

            var externalType = new ExternalType();
            externalType.Initialise(value.GetType());

            var xml = new XElement(xName,
                externalType.Persist(accessor));

            var iPersistance = value as IPersistence;

            if (iPersistance != null)
                xml.Add(new XElement(XPersistence, iPersistance.Persist(accessor)));
            else if (value is IValueDefinition)
            {
                // CoreStandard2 items do not support IPersistence so specific code required
                // A third party IValueDefinition will be persisted as its equivalent CoreStandard2
                if (value is IQuantity)
                    xml.Add(Persistence.Quantity.Persist(value as IQuantity, accessor));
                else if (value is IQuality)
                    xml.Add(Persistence.Quality.Persist(value as IQuality, accessor));
                else
                    xml.Add(Persistence.ValueDefinition.Persist(value as IValueDefinition, accessor));
            }
            else if (value is ISpatialDefinition)
                xml.Add(Persistence.SpatialDefinition.Persist(value as ISpatialDefinition, accessor));

            return xml;
        }

        public static class Values<TType>
            where TType : IConvertible
        {
            public const string XName = "Values";

            public static TType[] Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                if (xElement.Value.Trim() == string.Empty)
                    return new TType[] { };

                return xElement
                    .Value
                    .Split(',')
                    .Select(s => (TType)Convert.ChangeType(s, typeof(TType)))
                    .ToArray();
            }

            public static XElement Persist(TType[] values)
            {
                if (values == null)
                    return new XElement(XName);

                return new XElement(XName, values
                    .Aggregate(new StringBuilder(), (sb, v) => sb.Append((sb.Length > 0 ? "," : "") + Convert.ToString(v)))
                    .ToString().TrimEnd());
            }
        }

        public static XElement ThisOrSingleChild(XName name, XElement xElement)
        {
            return ThisOrSingleChild(name, xElement, false);
        }

        public static XElement ThisOrSingleChild(XName name, XElement xElement, bool allowNull)
        {
            if (xElement == null && allowNull)
                return null;

            Contract.Requires(xElement != null, "xElement != null");

            if (xElement.Name != name)
                xElement = xElement.Elements(name).SingleOrDefault();

            if (xElement == null && allowNull)
                return null;

            Contract.Requires(xElement != null, "xElement != null");

            return xElement;
        }

        public static class Describes
        {
            public const string XName = "Describes";

            public static IDescribable Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                return new FluidEarth2.Sdk.CoreStandard2.Describes(
                    Utilities.Xml.GetAttribute(xElement, "caption"),
                    xElement.Value);
            }

            public static XElement Persist(IDescribable iDescribable, IDocumentAccessor accessor)
            {
                return new XElement(Describes.XName,
                    new XAttribute("caption", iDescribable.Caption),
                    iDescribable.Description);
            }
        }

        public static class Identity
        {
            public const string XName = "Identity";

            public static IIdentifiable Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var describes = Describes.Parse(xElement.Element(Describes.XName), accessor);

                return new FluidEarth2.Sdk.CoreStandard2.Identity(Utilities.Xml.GetAttribute(xElement, "id"),
                    describes.Caption, describes.Description);
            }

            public static XElement Persist(IIdentifiable iIdentifiable, IDocumentAccessor accessor)
            {
                return new XElement(Identity.XName,
                    new XAttribute("id", iIdentifiable.Id),
                    Describes.Persist(iIdentifiable, accessor));
            }
        }

        public static class Argument<TType>
        {
            public const string XName = "ArgumentGeneric";

            public static CoreStandard2.Argument<TType> Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var arg = new CoreStandard2.Argument<TType>();

                arg.SetIdentity(Identity.Parse(xElement.Element(Identity.XName), accessor));
                arg.IsOptional = Utilities.Xml.GetAttribute(xElement, "isOptional", false);
                arg.IsReadOnly = Utilities.Xml.GetAttribute(xElement, "isReadOnly", false);

                object parsed;

                string value = xElement.Elements("Value").Single().Value;

                if (!arg.TryParse(value, out parsed))
                    throw new Exception("Cannot parse Value: " + value);

                arg.Value = parsed;

                value = xElement.Elements("DefaultValue").Single().Value;

                if (!arg.TryParse(value, out parsed))
                    throw new Exception("Cannot parse DefaultValue: " + value);

                arg.DefaultValue = parsed;

                foreach (var possible in xElement.Elements("PossibleValue"))
                {
                    if (!arg.TryParse(possible.Value, out parsed))
                        throw new Exception("Cannot parse PossibleValue: " + possible.Value);

                    arg.PossibleValues.Add(parsed);
                }

                return arg;
            }

            public static XElement Persist(CoreStandard2.Argument<TType> arg, IDocumentAccessor accessor)
            {
                var xml = new XElement(XName,
                    new XAttribute("isOptional", arg.IsOptional.ToString()),
                    new XAttribute("isReadOnly", arg.IsReadOnly.ToString()),
                    Persistence.Identity.Persist(arg, accessor),
                    new XElement("Value", arg.ValueAsString),
                    new XElement("DefaultValue", arg.ValueAsString));

                string persisted;

                foreach (var possible in arg.PossibleValues)
                    if (arg.TryPersist(possible, out persisted))
                        xml.Add("PossibleValue", persisted);

                return xml;
            }
        }

        public static class Argument
        {
            public const string XName = "Argument";

            public static IArgument Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var externalType = new ExternalType(xElement, accessor);

                Type type;
                var obj = externalType.CreateInstance(out type);
                var arg = obj as IArgument;

                var identity = Identity.Parse(xElement.Element(Identity.XName), accessor);

                arg.Caption = identity.Caption;
                arg.Description = identity.Description;

                var id = identity.Id;
                var isOptional = Utilities.Xml.GetAttribute(xElement, "isOptional", false);
                var isReadOnly = Utilities.Xml.GetAttribute(xElement, "isReadOnly", false);

                var value = xElement.Elements("Value").Single().Value;
                var valueDefault = xElement.Elements("DefaultValue").Single().Value;
                var valuePossibles = xElement.Elements("PossibleValue").Select(v => v.Value);

                foreach (var possible in valuePossibles)
                {
                    arg.ValueAsString = possible;
                    arg.PossibleValues.Add(arg.Value);
                }

                arg.ValueAsString = valueDefault;
                var defaultValue = arg.Value;

                arg.ValueAsString = value;

                var coreEdit = arg as IArgumentProposed;

                if (coreEdit != null)
                    coreEdit.Set(id, isOptional, isReadOnly, defaultValue);

                return arg;
            }

            public static XElement Persist(IArgument arg, IDocumentAccessor accessor)
            {
                var value = arg.ValueAsString;

                arg.Value = arg.DefaultValue;
                var valueDefault = arg.ValueAsString;

                var valuePossibles = new List<string>();

                foreach (var possible in arg.PossibleValues)
                {
                    arg.Value = possible;
                    valuePossibles.Add(arg.ValueAsString);
                }

                arg.ValueAsString = value;

                var externalType = new ExternalType(arg.GetType());

                return new XElement(XName,
                    new XAttribute("isOptional", arg.IsOptional.ToString()),
                    new XAttribute("isReadOnly", arg.IsReadOnly.ToString()),
                    Persistence.Identity.Persist(arg, accessor),
                    externalType.Persist(accessor),
                    new XElement("Value", value),
                    new XElement("DefaultValue", valueDefault),
                    valuePossibles.Select(p => new XElement("PossibleValue", p)));
            }
        }

        public static class Arguments
        {
            public const string XName = "Arguments";

            public static IEnumerable<IArgument> Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement, true);

                if (xElement == null)
                    return new IArgument[] { };

                var arguments = xElement
                    .Elements(Argument.XName)
                    .Select(a => Argument.Parse(a, accessor))
                    .ToList();

                arguments.AddRange(xElement
                    .Elements("IArgument")
                    .Select(a => Parse<IArgument>("IArgument", a, accessor)));

                return arguments;
            }

            public static XElement Persist(IEnumerable<IArgument> arguments, IDocumentAccessor accessor)
            {
                var xml = new XElement(XName);

                foreach (var arg in arguments)
                {
                    if (arg is IPersistence)
                        xml.Add(Persist<IArgument>("IArgument", arg, accessor));
                    else
                        xml.Add(Argument.Persist(arg, accessor));
                }

                return xml;
            }
        }

        public static class ArgumentValues
        {
            public const string XName = "ArgumentValues";
            public const string XName2 = "ArgumentValue";

            public static Dictionary<string, string> Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement, true);

                if (xElement == null)
                    return new Dictionary<string, string>();

                return xElement
                    .Elements(XName2)
                    .ToDictionary(k => Utilities.Xml.GetAttribute(k, "id"), v => v.Value);
            }

            public static XElement Persist(IEnumerable<IArgument> iArguments, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    iArguments.Select(a => new XElement(XName2, 
                        new XAttribute("id", a.Id), a.ValueAsString)));
            }
        }

        public static class PlatformsChi
        {
            public const string XName = "Platforms";

            public static SupportedPlatforms Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement, true);

                if (xElement == null)
                    return SupportedPlatforms.Unknown;

                return Parse(xElement.Value);
            }

            public static XElement Persist(SupportedPlatforms platforms, IDocumentAccessor accessor)
            {
                return new XElement(XName, Persist(platforms));
            }

            static string Persist(SupportedPlatforms platforms)
            {
                var sb = new StringBuilder();

                foreach (var v in Enum.GetValues(typeof(SupportedPlatforms)).Cast<SupportedPlatforms>())
                    if ((platforms & v) != 0)
                        sb.Append("|" + v.ToString());

                return sb.ToString().TrimStart('|');
            }

            static SupportedPlatforms Parse(string platforms)
            {
                if (platforms.Trim() == string.Empty)
                    return SupportedPlatforms.Unknown;

                var supported = SupportedPlatforms.Unknown;

                foreach (var s in platforms.Split('|'))
                {
                    var p = (SupportedPlatforms)Enum.Parse(typeof(SupportedPlatforms), s);

                    supported |= p;
                }

                return supported;
            }
        }

        public static class ValueType
        {
            public static bool TryParse(Type type, string value, out object parsed)
            {
                if (type == null)
                    throw new Exception("Argument ValueType unset");

                parsed = null;

                try
                {
                    if (string.Equals(value, "null", StringComparison.CurrentCultureIgnoreCase))
                        return true;

                    if (type.IsEnum)
                    {
                        parsed = Enum.Parse(type, value);
                        return true;
                    }

                    if (typeof(IConvertible).IsAssignableFrom(type))
                    {
                        parsed = Convert.ChangeType(value, type);
                        return true;
                    }

                    if (typeof(IArgumentValue).IsAssignableFrom(type))
                    {
                        ExternalType externalType = new ExternalType();
                        externalType.Initialise(type);

                        Type t;
                        IArgumentValue arg = externalType.CreateInstance(out t) 
                            as IArgumentValue;

                        arg.ValueAsString = value;

                        parsed = arg;
                        return true;
                    }

                    if (type.ToString() == typeof(FileInfo).ToString())
                    {
                        parsed = new FileInfo(value);
                        return true;
                    }

                    if (type.ToString() == typeof(DirectoryInfo).ToString())
                    {
                        parsed = new DirectoryInfo(value);
                        return true;
                    }

                    if (type.ToString() == typeof(Uri).ToString())
                    {
                        parsed = new Uri(value);
                        return true;
                    }
                }
                catch (System.Exception)
                { }

                return false;
            }

            public static bool TryPersist(Type type, object value, out string persisted)
            {
                if (type == null)
                    throw new Exception("Argument ValueType unset");

                persisted = string.Empty;

                try
                {
                    if (value == null)
                    {
                        persisted = "null";
                        return true;
                    }

                    if (type.IsEnum)
                    {
                        persisted = value.ToString();
                        return true;
                    }

                    if (typeof(IConvertible).IsAssignableFrom(type))
                    {
                        persisted = (string)Convert.ChangeType(value, typeof(string));
                        return true;
                    }

                    if (typeof(IArgumentValue).IsAssignableFrom(type))
                    {
                        persisted = ((IArgumentValue)value).ValueAsString;
                        return true;
                    }

                    if (type.ToString() == typeof(FileInfo).ToString())
                    {
                        persisted = ((FileInfo)value).FullName;
                        return true;
                    }

                    if (type.ToString() == typeof(DirectoryInfo).ToString())
                    {
                        persisted = ((DirectoryInfo)value).FullName;
                        return true;
                    }

                    if (type.ToString() == typeof(Uri).ToString())
                    {
                        persisted = ((Uri)value).LocalPath;
                        return true;
                    }
                }
                catch (System.Exception)
                { }

                return false;
            }
        }

        public static class Dimension
        {
            public const string XName = "Dimension";

            public static IDimension Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var dimension = new CoreStandard2.Dimension();

                string key, value;
                DimensionBase dim;

                foreach (XElement xPower in xElement.Elements("Power"))
                {
                    key = Utilities.Xml.GetAttribute(xPower, "key");
                    value = Utilities.Xml.GetAttribute(xPower, "value");
                    dim = (DimensionBase)Enum.Parse(typeof(DimensionBase), key);

                    dimension.SetPower(dim, Convert.ToDouble(value));
                }

                return dimension;
            }

            public static XElement Persist(IDimension iDimension, IDocumentAccessor accessor)
            {
                XElement xDimension = new XElement(XName);

                double value;
                DimensionBase dim;

                for (int n = 0; n <= (int)DimensionBase.Currency; ++n)
                {
                    dim = (DimensionBase)n;
                    value = iDimension.GetPower(dim);

                    if (value != 0.0)
                        xDimension.Add(
                            new XElement("Power",
                                new XAttribute("key", dim.ToString()),
                                new XAttribute("value", Convert.ToString(value))));             
                }

                return xDimension;
            }
        }

        public static class Unit
        {
            public const string XName = "Unit";

            public static IUnit Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var describes = Describes.Parse(xElement.Element(Describes.XName), accessor);
                var dimension = Dimension.Parse(xElement.Element(Dimension.XName), accessor);
                var factor = double.Parse(Utilities.Xml.GetAttribute(xElement, "factor"));
                var offset = double.Parse(Utilities.Xml.GetAttribute(xElement, "offset"));

                return new FluidEarth2.Sdk.CoreStandard2.Unit(describes, dimension, factor, offset);
            }

            public static XElement Persist(IUnit iUnit, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    new XAttribute("factor", iUnit.ConversionFactorToSI.ToString()),
                    new XAttribute("offset", iUnit.OffSetToSI.ToString()),
                    Dimension.Persist(iUnit.Dimension, accessor),
                    Describes.Persist(iUnit, accessor));
            }
        }

        public static class ValueDefinitionImp
        {
            public const string XName = "IValueDefinition";

            public static IValueDefinition Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);
                Type type = Type.GetType(xElement.Element("ValueType").Value);

                var xMissing = ThisOrSingleChild("MissingDataValue", xElement);
                var xMissingPersistence = ThisOrSingleChild("IPersistence", xMissing, true);  

                object missingDataValue;

                if (xMissingPersistence != null)
                    missingDataValue = Persistence.Parse<IPersistence>("IPersistence", xMissingPersistence, accessor);
                else
                {
                    missingDataValue = Convert.ChangeType(xMissing.Value, type);
                }

                var description = Describes.Parse(xElement, accessor);
                return new FluidEarth2.Sdk.CoreStandard2.ValueDefinition(description, type, missingDataValue);
            }

            public static XElement Persist(IValueDefinition iValueDefinition, IDocumentAccessor accessor)
            {
                var xMissing = new XElement("MissingDataValue");

                if (iValueDefinition.MissingDataValue is IConvertible)
                    xMissing.Add(Convert.ChangeType(iValueDefinition.MissingDataValue, typeof(string)));
                else if (iValueDefinition.MissingDataValue is IPersistence)
                    xMissing.Add(Persistence.Persist<IPersistence>(
                        "IPersistence", iValueDefinition.MissingDataValue as IPersistence, accessor));
                else
                    Contract.Requires(false, "iValueDefinition.MissingDataValue is neither IConvertible or IPersistence");

                return new XElement(XName,
                    new XElement("ValueType", iValueDefinition.ValueType.ToString()),
                    xMissing,
                    Describes.Persist(iValueDefinition, accessor));
            }
        }

        public static class Quantity
        {
            public const string XName = "Quantity";

            public static IQuantity Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var unit = Unit.Parse(xElement.Element(Unit.XName), accessor);
                var valueDefinition = ValueDefinitionImp.Parse(xElement, accessor);

                return new FluidEarth2.Sdk.CoreStandard2.Quantity(valueDefinition, unit);
            }

            public static XElement Persist(IQuantity iQuantity, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    Unit.Persist(iQuantity.Unit, accessor),
                    ValueDefinitionImp.Persist(iQuantity, accessor));
            }
        }

        public static class Category
        {
            public const string XName = "Category";

            public static ICategory Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var describes = Describes.Parse(xElement.Element(Describes.XName), accessor);

                ExternalType externalType = new ExternalType(accessor);
                externalType.Initialise(xElement.Element("ExternalType"), accessor);

                Type type;
                externalType.CreateInstance(out type);

                object parsed = null;

                if (!ValueType.TryParse(type, xElement.Value, out parsed))
                    parsed = null;

                return new FluidEarth2.Sdk.CoreStandard2.Category(describes, parsed);
            }

            public static XElement Persist(ICategory iCategory, IDocumentAccessor accessor)
            {
                ExternalType externalType = new ExternalType(accessor);
                externalType.Initialise(iCategory.GetType());

                string persisted = string.Empty;

                if (!ValueType.TryPersist(iCategory.GetType(), iCategory.Value, out persisted))
                    persisted = string.Empty;

                return new XElement(XName,
                    Describes.Persist(iCategory, accessor),
                    externalType.Persist(accessor),
                    iCategory.Value.ToString(),
                    persisted);
            }
        }

        public static class Quality
        {
            public const string XName = "Quality";

            public static IQuality Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var isOrdered = Utilities.Xml.GetAttribute(xElement, "isOrdered", false);
                var valueDefinition = ValueDefinitionImp.Parse(xElement, accessor);

                var categories = xElement
                    .Elements(Category.XName)
                    .Select(c => Category.Parse(c, accessor));

                return new FluidEarth2.Sdk.CoreStandard2.Quality(valueDefinition, categories, isOrdered);
            }

            public static XElement Persist(IQuality iQuality, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    new XAttribute("isOrdered", iQuality.IsOrdered.ToString()),
                    iQuality.Categories.Select(c => Category.Persist(c, accessor)),
                    ValueDefinitionImp.Persist(iQuality, accessor));
            }
        }

        public static class ValueDefinition
        {
            public const string XName = "IValueDefinition";

            public static IValueDefinition Parse(XElement xElement, IDocumentAccessor accessor)
            {
                var xQuantity = ThisOrSingleChild(Quantity.XName, xElement, true);

                if (xQuantity != null)
                    return Quantity.Parse(xQuantity, accessor);

                var xQuality = ThisOrSingleChild(Quality.XName, xElement, true);

                if (xQuality != null)
                    return Quantity.Parse(xQuality, accessor);

                xElement = ThisOrSingleChild(XName, xElement);

                // Create using default constructor and if derived from IPersistence
                // then call IPersistence.Initialise(xElement, accessor)

                return ValueDefinitionImp.Parse(xElement, accessor);
            }

            public static XElement Persist(IValueDefinition definition, IDocumentAccessor accessor)
            {
                if (definition is IPersistence)
                    return Persist<IValueDefinition>(XName, definition, accessor);
                else if (definition is CoreStandard2.Quantity)
                    return Quantity.Persist((CoreStandard2.Quantity)definition, accessor);
                else if (definition is CoreStandard2.Quality)
                    return Quality.Persist((CoreStandard2.Quality)definition, accessor);

                // Persist enough information to recreate using default constructor

                return ValueDefinitionImp.Persist(definition, accessor);
            }
        }

        public static class SpatialDefinition
        {
            public const string XName = "ISpatialDefinition";

            static CoreStandard2.SpatialDefinition ParseCoreStandard2(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var describes = Describes.Parse(xElement, accessor);
                var elementCount = int.Parse(Utilities.Xml.GetAttribute(xElement, "elementCount"));
                var version = int.Parse(Utilities.Xml.GetAttribute(xElement, "version"));

                var xWkt = ThisOrSingleChild("WellKnownType", xElement);
                var wkt = xWkt.Value;

                return new CoreStandard2.SpatialDefinition(describes, elementCount, wkt, version);
            }

            static XElement PersistCoreStandard2(CoreStandard2.SpatialDefinition spatial, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    Describes.Persist(spatial, accessor),
                    new XAttribute("CoreStandard2", true.ToString()),
                    new XAttribute("elementCount", spatial.ElementCount.ToString()),
                    new XAttribute("version", spatial.Version.ToString()),
                    new XElement("WellKnownType", spatial.SpatialReferenceSystemWkt));
            }

            public static ISpatialDefinition Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                // CoreStandard2 does not implement IPersistence so use custom method in this class

                if (Utilities.Xml.GetAttribute(xElement, "CoreStandard2", false))
                    return ParseCoreStandard2(xElement, accessor);

                // Create using default constructor and if derived from IPersistence
                // then call IPersistence.Initialise(xElement, accessor)

                return Parse<ISpatialDefinition>(XName, xElement, accessor);
            }

            public static XElement Persist(ISpatialDefinition spatial, IDocumentAccessor accessor)
            {
                if (spatial is IPersistence)
                    return Persist<ISpatialDefinition>(XName, spatial, accessor);
                else if (spatial is CoreStandard2.SpatialDefinition)
                    return PersistCoreStandard2((CoreStandard2.SpatialDefinition)spatial, accessor);
                
                // Persist enough information to recreate using default constructor

                return Persist<ISpatialDefinition>(XName, spatial, accessor);
            }
        }

        public static class ElementSet
        {
            public const string XName = "ElementSet";

            public static ElementType Parse(XElement xElement, IDocumentAccessor accessor, out ISpatialDefinition spatial, out bool hasZ, out bool hasM)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                spatial = SpatialDefinition.Parse(xElement, accessor);

                hasZ = Utilities.Xml.GetAttribute(xElement, "hasZ", false);
                hasM = Utilities.Xml.GetAttribute(xElement, "hasM", false);

                return (ElementType)Enum.Parse(typeof(ElementType), 
                    Utilities.Xml.GetAttribute(xElement, "elementType"));            
            }

            public static XElement Persist(IElementSet iElementSet, IDocumentAccessor accessor)
            {
                var spatial = new CoreStandard2.SpatialDefinition(iElementSet);

                return new XElement(XName,
                    SpatialDefinition.Persist(spatial, accessor),
                    new XAttribute("elementType", iElementSet.ElementType.ToString()),
                    new XAttribute("hasZ", iElementSet.HasZ.ToString()),
                    new XAttribute("hasM", iElementSet.HasM.ToString()));
            }
        }

        public static class Time
        {
            public const string XName = "Time";

            public static CoreStandard2.Time Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement, true);

                if (!xElement.HasAttributes)
                    return null;

                var start = double.Parse(Utilities.Xml.GetAttribute(xElement, "start"));
                var duration = double.Parse(Utilities.Xml.GetAttribute(xElement, "duration"));
                var end = CoreStandard2.Time.EndTime(start, duration);

                return new CoreStandard2.Time(start, end);
            }

            public static XElement Persist(ITime time, IDocumentAccessor accessor)
            {
                if (time == null)
                    return new XElement(XName);

                return new XElement(XName,
                    new XAttribute("start", time.StampAsModifiedJulianDay.ToString()),
                    new XAttribute("duration", time.DurationInDays.ToString()));
            }
        }

        public static class TimeSet
        {
            public const string XName = "TimeSet";

            public static CoreStandard2.TimeSet Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement, true);

                if (!xElement.HasAttributes)
                    return null;

                var timeSet = new CoreStandard2.TimeSet();

                timeSet.SetHasDurations(
                    bool.Parse(Utilities.Xml.GetAttribute(xElement, "hasDurations", Boolean.FalseString)));
                timeSet.SetOffsetFromUtcInHours(
                    double.Parse(Utilities.Xml.GetAttribute(xElement, "offsetFromUtcInHours", "0.0")));
                timeSet.SetTimeHorizon(
                    Time.Parse(xElement.Elements("TimeHorizon").Single().Elements("Time").Single(), accessor));
                timeSet.SetTimes(xElement
                    .Elements("Times").Single()
                    .Elements("Time")
                    .Select(t => Time.Parse(t, accessor) as ITime));

                return timeSet;
            }

            public static XElement Persist(ITimeSet timeSet, IDocumentAccessor accessor)
            {
                if (timeSet == null)
                    return new XElement(XName);

                return new XElement(XName,
                    new XAttribute("hasDurations", timeSet.HasDurations.ToString()),
                    new XAttribute("offsetFromUtcInHours", timeSet.OffsetFromUtcInHours.ToString()),
                    new XElement("TimeHorizon", Time.Persist(timeSet.TimeHorizon, accessor)),
                    new XElement("Times", timeSet.Times.Select(t => Time.Persist(t, accessor))));
            }
        }

        public static class Inputs
        {
            public const string XName = "Inputs";
            public const string XBase = "IBaseInput";

            public static IEnumerable<IBaseInput> Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement, true);

                if (xElement == null)
                    return new IBaseInput[] {};

                return xElement
                    .Elements(XBase)
                    .Select(x => Parse<IBaseInput>(XBase, x, accessor));
            }

            public static XElement Persist(IEnumerable<IBaseInput> inputs, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    inputs.Select(e => Persist<IBaseInput>(XBase, e, accessor)));
            }
        }

        public static class Outputs
        {
            public const string XName = "Outputs";
            public const string XBase = "IBaseOutput";

            public static IEnumerable<IBaseOutput> Parse(XElement xElement, IDocumentAccessor accessor)
            {
                xElement = ThisOrSingleChild(XName, xElement, true);

                if (xElement == null)
                    return new IBaseOutput[] { };

                return xElement
                    .Elements(XBase)
                    .Select(x => Parse<IBaseOutput>(XBase, x, accessor));
            }

            public static XElement Persist(IEnumerable<IBaseOutput> inputs, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    inputs.Select(e => Persist<IBaseOutput>(XBase, e, accessor)));
            }
        }

        public static class AdaptedOutputs
        {
            public const string XName = "AdaptedOutputs";
            public const string XBase = "AdaptedOutput";

            public static ILinkage Parse(XElement xElement, IDocumentAccessor accessor, IBaseOutput adaptee, IBaseInput target)
            {
                Contract.Requires(adaptee != null, "adaptee != null");
                Contract.Requires(target != null, "target != null");

                xElement = ThisOrSingleChild(XName, xElement, true);

                if (xElement == null)
                {
                    adaptee.AddConsumer(target);
                    return new Linkage(target);
                }

                foreach (var xAdapter in xElement.Elements(XBase))
                {
                    var factoryType = new ExternalType(xAdapter, accessor);

                    Type type;
                    var factory = factoryType.CreateInstance(out type) 
                        as IAdaptedOutputFactory;

                    Contract.Requires(factory != null, "factory != null");

                    var identity = Identity.Parse(xAdapter, accessor);

                    var adapter = factory.CreateAdaptedOutput(identity, adaptee, null);

                    Contract.Requires(adapter != null, "adapter != null");

                    foreach (var kv in ArgumentValues.Parse(xAdapter, accessor))
                        adapter
                            .Arguments
                            .Where(a => a.Id == kv.Key)
                            .Single()
                            .ValueAsString = kv.Value;

                    if (!adaptee.AdaptedOutputs.Contains(adapter))
                        adaptee.AddAdaptedOutput(adapter);

                    adaptee = adapter;
                }

                adaptee.AddConsumer(target);

                return new Linkage(target);
            }

            public static XElement Persist(ILinkage linkage, IDocumentAccessor accessor, 
                IEnumerable<IAdaptedOutputFactory> factories)
            {
                var xml = new XElement(XName);

                IAdaptedOutputFactory factory;

                var adapters = linkage.Adapters.Reverse();

                foreach (var adapter in adapters)
                {
                    factory = null;

                    if (adapter is IBaseAdaptedOutputProposed)
                        factory = ((IBaseAdaptedOutputProposed)adapter).Factory;
                    else
                    {
                        // UGLY, thats why we need IBaseAdaptedOutputProposed into standard

                        var fs = factories.TakeWhile(f 
                            => f.GetAvailableAdaptedOutputIds(adapter.Adaptee, null)
                                .Where(i => i.Id == adapter.Id).Count() > 0);
                            
                        // This could often fail, which is why we need IBaseAdaptedOutputProposed into standard 

                        Contract.Requires(fs.Count() == 1, 
                            "Cannot deduce correct factory for persisting output");

                        factory = fs.First();
                    }

                    Contract.Requires(factory != null, "factory != null");

                    var factoryType = new ExternalType(factory);

                    xml.Add(new XElement(XBase,
                        Identity.Persist(adapter, accessor),
                        factoryType.Persist(accessor),
                        ArgumentValues.Persist(adapter.Arguments, accessor)));
                }

                return xml;
            }
        }

        public static class BaseComponent
        {
            public const string XName = "BaseComponent";

            public static IIdentifiable Parse(
                XElement xElement,
                IDocumentAccessor accessor,
                out IEnumerable<IArgument> arguments,
                out IEnumerable<IBaseInput> inputs,
                out IEnumerable<IBaseOutput> outputs)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                arguments = Arguments.Parse(xElement, accessor);

                inputs = Inputs.Parse(xElement, accessor);
                outputs = Outputs.Parse(xElement, accessor);

                var x = xElement
                    .Elements("Extent")
                    .SingleOrDefault();

                return Identity.Parse(xElement.Element(Identity.XName), accessor);
            }

            public static XElement Persist(IBaseLinkableComponent component, IDocumentAccessor accessor)
            {
                return new XElement(XName,
                    Identity.Persist(component, accessor),
                    Arguments.Persist(component.Arguments, accessor),
                    Inputs.Persist(component.Inputs, accessor),
                    Outputs.Persist(component.Outputs, accessor));
            }
        }

        public static class BaseComponentTime
        {
            public const string XName = "BaseComponentTime";

            public static IIdentifiable Parse(
                XElement xElement,
                IDocumentAccessor accessor,
                out IEnumerable<IArgument> arguments,
                out IEnumerable<IBaseInput> inputs,
                out IEnumerable<IBaseOutput> outputs,
                out CoreStandard2.TimeSet extent)
            {
                xElement = ThisOrSingleChild(XName, xElement);

                var x = xElement
                    .Elements("Extent")
                    .SingleOrDefault();

                extent = x != null
                    ? TimeSet.Parse(x, accessor)
                    : null;

                return BaseComponent.Parse(xElement, accessor, out arguments, out inputs, out outputs);
            }

            public static XElement Persist(IBaseLinkableComponent component, IDocumentAccessor accessor)
            {
                var componentTime = component as ITimeSpaceComponent;

                if (componentTime == null)
                    throw new Exception("component NOT ITimeSpaceComponent");

                return new XElement(XName,
                    BaseComponent.Persist(component, accessor),
                    TimeSet.Persist(componentTime.TimeExtent, accessor));
            }
        }
    }
}
