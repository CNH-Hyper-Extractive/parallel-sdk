
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;
using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;

namespace FluidEarth2.Sdk
{
    public static partial class Utilities
    {
        public static bool OpenLink(string address) 
        {
            try 
            {
                int plat = (int) Environment.OSVersion.Platform;

                if ((plat != 4) && (plat != 128)) {
                    // Use Microsoft's way of opening sites
                    Process.Start(address);
                } 
                else 
                {
                    // We're on Unix, try gnome-open (used by GNOME), then open
                    // (used my MacOS), then Firefox or Konqueror browsers (our last
                    // hope).
                    string cmdline = String.Format("gnome-open {0} || open {0} || "+
                        "firefox {0} || mozilla-firefox {0} || konqueror {0}", address);
                    Process proc = Process.Start (cmdline);
 
                    // Sleep some time to wait for the shell to return in case of error
                    System.Threading.Thread.Sleep(250);
 
                    // If the exit code is zero or the process is still running then
                    // apparently we have been successful.
                    return (!proc.HasExited || proc.ExitCode == 0);
                }
            } 
            catch (Exception e) 
            {
                Trace.TraceError(address);
                Trace.TraceError(e.Message);
                return false;
            }

            return false;
        }

        public static bool IsProbablyWikiText(string value)
        {
            string v = value.TrimStart();

            if (v.Length < 1)
                return false;

            if (v[0] == '*' || v[0] == '#' || v[0] == '=')
                return true;

            return false;
        }

        public static IEnumerable<TType[]> Slice<TType>(IEnumerable<TType> source, int n)
        {
            do
            {
                yield return source.Take(n).ToArray();
                source = source.Skip(n);
            }
            while (source.Count() > 0);
        }

        public static string MakeCodeBase(string value, Uri baseUri)
        {
            Uri uri = new Uri(value, UriKind.RelativeOrAbsolute);

            if (uri.IsAbsoluteUri)
                return uri.AbsolutePath; // just need location not query etc annotations

            Uri absolute;

            if (baseUri != null && Uri.TryCreate(baseUri, uri, out absolute))
                return absolute.AbsolutePath;

            return value;
        }

        public static Uri NewUniqueUri(Uri baseUri, string name)
        {
            return NewUniqueUri(baseUri, name);
        }

        public static Uri NewUniqueUri(Uri baseUri, string name, string extension = null)
        {
            name = name.Replace(' ', '_');

            if (extension == null)
                extension = string.Empty;

            if (extension.Length > 0 && extension[0] != '.')
                extension = "." + extension;

            string basepath;

            if (UriIsExistingFolderPath(baseUri, out basepath))
            {
                return new Uri(Path.Combine(basepath, name + extension));
            }

            if (UriIsFilePath(baseUri, out basepath))
            {
                return new Uri(Path.Combine(Path.GetDirectoryName(basepath), name + extension));
            }

            throw new Exception("Only implemented for file/folder based Uri's");
        }

        public class FuncComparer<T> : IComparer<T>
        {
            private readonly Comparison<T> comparison;

            public FuncComparer(Comparison<T> comparison)
            {
                this.comparison = comparison;
            }
            public int Compare(T x, T y)
            {
                return comparison(x, y);
            }
        }

        public static List<TimeRecord<TType>> ToRecords<TType>(ITimeSet timeSet, ITimeSpaceValueSet vst)
        {
            if (vst == null)
                return new List<TimeRecord<TType>>();

            if (typeof(TType) != vst.ValueType)
                throw new Exception(string.Format("{0} != {1}",
                    typeof(TType).ToString(), vst.ValueType.ToString()));

            return timeSet
                .Times
                .Select((t, n) => 
                    new TimeRecord<TType>(t,
                        vst.GetElementValuesForTime(n).Cast<TType>().ToArray()))
                .ToList();
        }

        public static List<TType> ToList<TType>(IBaseValueSet vs)
        {
            Stack<int> indices = new Stack<int>();
            List<TType> values = new List<TType>();

            ToListRecursive(vs, indices, values);

            return values;
        }

        static void ToListRecursive<TType>(IBaseValueSet vs, Stack<int> indices, List<TType> values)
        {
            if (indices.Count == vs.NumberOfIndices)
            {
                values.Add((TType)vs.GetValue(indices.ToArray()));
                return;
            }

            for (int n = 0; n < vs.GetIndexCount(indices.ToArray()); ++n)
            {
                indices.Push(n);
                ToListRecursive(vs, indices, values);
                indices.Pop();
            }
        }

        public static void ToValueSet<TType>(List<TType> values, IBaseValueSet vs)
        {
            Stack<int> indices = new Stack<int>();

            ToValueSetRecursive(values, indices, vs, -1);
        }

        static void ToValueSetRecursive<TType>(List<TType> values, Stack<int> indices, IBaseValueSet vs, int offset)
        {
            if (indices.Count == vs.NumberOfIndices - 1)
            {
                ++offset;

                if (offset >= values.Count)
                    throw new IndexOutOfRangeException(string.Format("{0} >= {1}",
                        offset.ToString(), values.Count.ToString()));

                vs.SetValue(indices.ToArray(), offset);
                return;
            }

            for (int n = 0; n < vs.GetIndexCount(indices.ToArray()); ++n)
            {
                indices.Push(n);
                ToValueSetRecursive(values, indices, vs, offset);
                indices.Pop();
            }
        }

        public static bool Validate<TType>(string text, out TType value, out string errorMessage)
            where TType : IConvertible
        {
            errorMessage = string.Empty;

            try
            {
                value = (TType)Convert.ChangeType(text, typeof(TType));
            }
            catch (System.Exception)
            {
                value = default(TType);
                errorMessage = string.Format("Value not recognised as a viable {0}", typeof(TType).ToString());
                return false;
            }

            return true;
        }

        public enum Compare { None = 0, LessThan, LessThanOrEqual, Equal, GreaterThenOrEqual, GreaterThan, }

        public static bool Validate<TType>(string text, Compare compare, TType limit, out string errorMessage)
            where TType : IConvertible, IComparable
        {
            TType value;

            if (!Validate(text, out value, out errorMessage))
                return false;

            switch (compare)
            {
                case Compare.None:
                    return true;
                case Compare.LessThan:
                    if (value.CompareTo(limit) < 0)
                        return true;
                    else
                    {
                        errorMessage = string.Format("Value {0} must be less than {1}", value, limit);
                        return false;
                    }
                case Compare.LessThanOrEqual:
                    if (value.CompareTo(limit) <= 0)
                        return true;
                    else
                    {
                        errorMessage = string.Format("Value {0} must be less than or equal to {1}", value, limit);
                        return false;
                    }
                case Compare.Equal:
                    if (value.CompareTo(limit) == 0)
                        return true;
                    else
                    {
                        errorMessage = string.Format("Value {0} must be equal to {1}", value, limit);
                        return false;
                    }
                case Compare.GreaterThenOrEqual:
                    if (value.CompareTo(limit) >= 0)
                        return true;
                    else
                    {
                        errorMessage = string.Format("Value {0} must be greater than or equal to {1}", value, limit);
                        return false;
                    }
                case Compare.GreaterThan:
                    if (value.CompareTo(limit) > 0)
                        return true;
                    else
                    {
                        errorMessage = string.Format("Value {0} must be greater than {1}", value, limit);
                        return false;
                    }
                default:
                    throw new NotImplementedException(compare.ToString());
            }
        }

        public class Describer
        {
            public readonly IDescribable Describes;

            public Describer(IDescribable describes)
            {
                Describes = describes;
            }

            public override string ToString()
            {
                return Describes == null
                    ? string.Empty : Describes.Caption;
            }
        }

        public static string DetailsAsWikiText(IDescribable describes)
        {
            var sb = new StringBuilder();

            sb.AppendLine("= " + describes.Caption);

            var identity = describes as IIdentifiable;

            if (identity != null)
            {
                sb.AppendLine("* Id:");
                sb.AppendLine("** " + identity.Id);
            }

            if (IsProbablyWikiText(describes.Description))
                sb.AppendLine(describes.Description);
            else
            {
                sb.AppendLine("{{{");
                sb.AppendLine(describes.Description);
                sb.AppendLine("}}}");
            }

            AddDetailsAsWikiText(new ExternalType(describes.GetType()), sb);

            return sb.ToString();
        }

        public static void AddDetailsAsWikiText(IExternalType iExternalType, StringBuilder sb)
        {
            sb.AppendLine("== External Type");
            sb.AppendLine("* Caption:");
            sb.AppendLine("** " + iExternalType.Caption);
            sb.AppendLine("* Id:");
            sb.AppendLine("** " + iExternalType.Id);
            sb.AppendLine("* Type:");
            sb.AppendLine("** " + iExternalType.TypeName);
            sb.AppendLine("* Assembly:");
            sb.AppendLine("** " + iExternalType.AssemblyName);
            sb.AppendLine("* Url:");
            sb.AppendLine("** " + iExternalType.Url.AbsoluteUri);
            sb.AppendLine("* Is Instantiated:");
            sb.AppendLine("** " + iExternalType.IsInstantiated.ToString());

            sb.AppendLine(string.Format("=== Description"));
            
            if (IsProbablyWikiText(iExternalType.Description))
                sb.AppendLine(iExternalType.Description);
            else
            {
                sb.AppendLine("{{{");
                sb.AppendLine(iExternalType.Description);
                sb.AppendLine("}}}");
            }
        }

        public static IValueSetConverter GetValueSetConverter(IBaseExchangeItem item)
        {
            Contract.Requires(item != null, "IBaseExchangeItem != null");
            Contract.Requires(item.ValueDefinition != null, "IBaseExchangeItem.ValueDefinition != null");

            var spatial = Utilities.AsSpatialDefinition(item);

            Contract.Requires(spatial != null, "IBaseExchangeItem.SpatialDefinition != null");

            int elementCount = spatial.ElementCount; 

            var elementSet = Utilities.AsElementSet(item);

            var valueType = item.ValueDefinition.ValueType;

            IValueSetConverter convertor = null;

            if (valueType.ToString() == typeof(string).ToString())
            {
                convertor = new ValueSetConverterTimeEngineString(item.Caption,
                    elementCount, (string)item.ValueDefinition.MissingDataValue);
            }
            else if (valueType.ToString() == typeof(bool).ToString())
            {
                convertor = new ValueSetConverterTimeEngineBoolean(item.Caption,
                    (bool)item.ValueDefinition.MissingDataValue, elementCount);
            }
            else if (valueType.ToString() == typeof(Int32).ToString())
            {
                convertor = new ValueSetConverterTimeEngineInt32(item.Caption,
                    (Int32)item.ValueDefinition.MissingDataValue, elementCount,
                    ValueSetConverterTimeRecordBase<int>.InterpolationTemporal.Lower);
            }
            else if (valueType.ToString() == typeof(double).ToString())
            {
                convertor = new ValueSetConverterTimeEngineDouble(item.Caption,
                    (double)item.ValueDefinition.MissingDataValue,
                    elementCount, 1,
                    ValueSetConverterTimeRecordBase<double>.InterpolationTemporal.Linear);
            }
            else if (valueType.ToString() == typeof(Vector2d<double>).ToString())
            {
                convertor = new ValueSetConverterTimeEngineDoubleVector2d(item.Caption,
                    (Vector2d<double>)item.ValueDefinition.MissingDataValue,
                    elementCount, 1,
                    ValueSetConverterTimeRecordBase<Vector2d<double>>.InterpolationTemporal.Linear);
            }
            else if (valueType.ToString() == typeof(Vector3d<double>).ToString())
            {
                convertor = new ValueSetConverterTimeEngineDoubleVector3d(item.Caption,
                    (Vector3d<double>)item.ValueDefinition.MissingDataValue,
                    elementCount, 1,
                    ValueSetConverterTimeRecordBase<Vector3d<double>>.InterpolationTemporal.Linear);
            }
            else
                throw new Exception("Engine does not implement type " + valueType.ToString());

            convertor.ExchangeItem = item;

            return convertor;
        }

        public static ISpatialDefinition AsSpatialDefinition(IBaseExchangeItem item)
        {
            if (item is IBaseInput && (IBaseInput)item is ITimeSpaceInput)
                return ((ITimeSpaceInput)item).SpatialDefinition;

            if (item is IBaseOutput && (IBaseOutput)item is ITimeSpaceOutput)
                return ((ITimeSpaceOutput)item).SpatialDefinition;

            return null;
        }

        public static IElementSet AsElementSet(IBaseExchangeItem item)
        {
            return AsSpatialDefinition(item) as IElementSet;
        }

        public static bool IsValid(string prefix, IBaseExchangeItem item, Type requiredType, out string whyNot)
        {
            Contract.Requires(item != null, "IBaseExchangeItem != null");

            if (item.ValueDefinition == null)
            {
                whyNot = string.Format(
                    "{0} {1} ValueDefinition == null",
                    prefix, item.Caption);

                return false;
            }

            if (item.ValueDefinition.ValueType != requiredType)
            {
                whyNot = string.Format(
                    "{0} {1}, ValueDefinition ValueType miss-match, {2} != {3}",
                    prefix, item.Caption,
                    item.ValueDefinition.ValueType.ToString(), requiredType.ToString());

                return false;
            }

            whyNot = string.Empty;
            return true;
        }

        public static bool IsValid(string prefix, IElementSet elementSet, ElementType requiredType, out string whyNot)
        {
            if (elementSet == null)
            {
                whyNot = string.Format(
                    "{0} does not implement ElementSet",
                    prefix);

                return false;
            }

            if (elementSet.ElementType != requiredType)
            {
                whyNot = string.Format(
                    "{0} ElementSet ElementType miss-match, {1} != {2}",
                    prefix,
                    elementSet.ElementType.ToString(), requiredType.ToString());

                return false;
            }

            whyNot = string.Empty;
            return true;
        }

        public static string CsvTo<TType>(IEnumerable<TType> values, char separator = ',', char existingCommaSubstitution = '|')
            where TType : IConvertible
        {
            return values
                .Aggregate(new StringBuilder(), (sb, v)
                    => sb.Append(Convert.ToString(v).Replace(separator, existingCommaSubstitution) + separator))
                .ToString()
                .TrimEnd(separator);
        }

        public static IEnumerable<TType> CsvFrom<TType>(string csv, char separator = ',', char existingCommaSubstitution = '|')
            where TType : IConvertible
        {
            return csv
                .Split(separator)
                .Select(c
                    => (TType)Convert.ChangeType(c.Replace(existingCommaSubstitution, separator), typeof(TType)));
        }

        static public Uri AssemblyUri(Assembly assembly)
        {
            var uri = new UriBuilder(assembly.CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return new Uri(Path.GetFullPath(path));
        }

        static public string LocalPath(string path, IDocumentAccessor accessor)
        {
            Uri relativeToUri;

            if (accessor != null && accessor.Uri != null)
                relativeToUri = accessor.Uri;
            else
                relativeToUri = AssemblyUri(Assembly.GetExecutingAssembly());

            var absoluteUri = new Uri(relativeToUri, path);

            // Change %20's into spaces
            return Uri.UnescapeDataString(absoluteUri.LocalPath);
        }

        /// <summary>
        /// Is Uri a physical absolute file path, if so return that path
        /// The file does not have to exists BUT the containing folder MUST.
        /// Uri's that start with file:: can be files OR folders
        /// </summary>
        /// <param name="uri">Uri to test</param>
        /// <param name="filepath">file path or string.Empty if fails</param>
        /// <returns>true if file with path</returns>
        public static bool UriIsFilePath(Uri uri, out string filepath)
        {
            if (UriIsExistingFolderPath(uri, out filepath))
                return false;

            filepath = string.Empty;

            if (uri == null || !uri.IsFile || !uri.IsAbsoluteUri)
                return false;

            filepath = Uri.UnescapeDataString(uri.LocalPath);

            return true;
        }

        /// <summary>
        /// Is Uri a physical absolute existing folder path, if so return that path
        /// Uri's that start with file:: can be files OR folders
        /// </summary>
        /// <param name="uri">Uri to test</param>
        /// <param name="filepath">folder path or string.Empty if fails</param>
        /// <returns>true if folder with path</returns>
        public static bool UriIsExistingFolderPath(Uri uri, out string folderpath)
        {
            folderpath = string.Empty;

            try
            {
                if (uri == null || !uri.IsFile || !uri.IsAbsoluteUri)
                    return false;

                // Could be file of folder, just know begins with file::

                folderpath = Uri.UnescapeDataString(uri.LocalPath);

                return folderpath != null && Directory.Exists(folderpath);
            }
            catch (System.Exception)
            { }

            return false;
        }

        public static string NewLocalFilePath(string name, string ext)
        {
            name = name.Replace(' ', '_');

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            var path = new FileInfo(Utilities.AssemblyUri(
                    Assembly.GetExecutingAssembly()).LocalPath)
                .DirectoryName;

            int n = 0;
            string f;

            do
            {
                var file = string.Format("{0}{1}.{2}", name, ++n, ext);
                f = Path.Combine(path, file);
            }
            while (File.Exists(f));

            return f;
        }

        public class ExchangeItemEvent
        {
            WriteTo _to;
            string _caption;
            HashSet<Stream> _streams = new HashSet<Stream>();

            public EventHandler<ExchangeItemChangeEventArgs> NewHandler
            {
                get
                {
                    return new EventHandler<ExchangeItemChangeEventArgs>(Changed);
                }
            }

            public ExchangeItemEvent(string caption, WriteTo to, IEnumerable<Stream> streams = null)
            {
                Contract.Requires(caption != null, "caption != null");

                _to = to;
                _caption = caption;

                if (streams != null)
                    _streams = new HashSet<Stream>(streams);
            }

            public string Caption
            {
                get { return _caption; }
                set { _caption = value; }
            }

            public WriteTo To
            {
                get { return _to; }
                set { _to = value; }
            }

            public HashSet<Stream> Streams
            {
                get { return _streams; }
            }

            public void Changed(object sender, ExchangeItemChangeEventArgs e)
            {
                if (To == WriteTo.None)
                    return;

                string component = e.ExchangeItem.Component != null ? e.ExchangeItem.Component.Caption : "Orphan";

                var sb = new StringBuilder(
                    string.Format("Event Item: {0}", e.Message));

                var e2 = e as BaseExchangeItemChangeEventArgs;

                if (e2 != null)
                {
                    for (int n = 0; n < e2.Messages.Count; ++n)
                    {
                        sb.AppendLine();
                        sb.Append(n == 0 ? "\t" + e2.Messages[n] : "\t\t" + e2.Messages[n]);
                    }
                }

                Utilities.Diagnostics.WriteLine(
                    Utilities.Diagnostics.DatedLine(Caption, sb.ToString()),
                    To, Streams);
            }
        }
    }
}
