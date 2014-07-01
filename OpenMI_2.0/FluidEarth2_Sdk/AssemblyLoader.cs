
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
	public class AssemblyLoader : IAssemblyLoader
	{
		string _caption = string.Empty;

		List<Uri> _uris
			= new List<Uri>();
		List<string> _potentialAssemblyExtensions 
			= new List<string>();
		Dictionary<string, Assembly> _loaded 
			= new Dictionary<string, Assembly>();
		
		public static IAssemblyLoader New(string caption)
		{
			// On load will reflection will need to know folder
			// locations to search for additional dependencies

			var uri = new Uri(Assembly.GetExecutingAssembly().Location);

			var folders = new Uri[] {
				uri, // installed
			};

			return new AssemblyLoader(caption, folders);
		}

		public AssemblyLoader()
		{
			Init(null);
		}

		public AssemblyLoader(string caption, Uri[] potentialFolderLocations)
		{
			_caption = caption;

			Init(potentialFolderLocations);
		}

		public AssemblyLoader(XElement xElement, IDocumentAccessor accessor)
		{
			Initialise(xElement, accessor);
		}

		public string Caption
		{
			get { return _caption; }
			set { _caption = value; }
		}

		#region Persistence

		public const string XName = "AssemblyLoader";

		public void Initialise(XElement xElement, IDocumentAccessor accessor)
		{
			xElement = Persistence.ThisOrSingleChild(XName, xElement, true);

			if (xElement != null)
			{
				_caption = Utilities.Xml.GetAttribute(xElement, "caption");

				var uris = xElement
					.Elements("Uri")
					.Select(x => new Uri(x.Value));

				var exts = xElement
					.Elements("Extension")
					.Select(x => x.Value)
					.ToList();

				foreach (var uri in uris)
					AddSearchUri(uri);

				if (accessor != null)
					AddSearchUri(accessor.Uri);

				foreach (var ext in exts)
					AddPotentialAssemblyExtension(ext);
			}
		}

		public XElement Persist(IDocumentAccessor accessor)
		{
            if (accessor != null)
			    AddSearchUri(accessor.Uri);

			// will have to revisit this for Uri's that are not local paths
			//.NET 4.5 provides Uri.IsUnc which would be useful, don't want create our
			// own at moment.

			return new XElement(XName,
				new XAttribute("caption", _caption),
				_uris.Select(f => new XElement("Uri", Uri.UnescapeDataString(f.LocalPath))),
				_potentialAssemblyExtensions.Select(f => new XElement("Extension", f)));
		}

		#endregion

		public IEnumerable<Uri> Uris
		{
			get { return _uris; }
		}

		public IEnumerable<string> Extensions
		{
			get { return _potentialAssemblyExtensions; }
		}

		public void AddSearchUri(Uri uri)
		{
			var folderPath = string.Empty;

			if (Utilities.UriIsFilePath(uri, out folderPath))
				folderPath = Path.GetDirectoryName(folderPath);
			else if (!Utilities.UriIsExistingFolderPath(uri, out folderPath))
				return;

			if (!_uris.Any(f => f.LocalPath == folderPath))
			{
				_uris.Add(new Uri(folderPath));

				Trace.TraceInformation(string.Format(
					"AssemblyLoader({0}).AddSearchUri({1})",
					_caption, uri));
			}
		}

		public void Add(IAssemblyLoader loader)
		{
			foreach (var uri in loader.Uris)
				AddSearchUri(uri);
			foreach (var ext in loader.Extensions)
				AddPotentialAssemblyExtension(ext);
		}

		public void AddPotentialAssemblyExtension(string extension)
		{
			var ext = extension.StartsWith(".")
				? extension
				: "." + extension;

			if (!_potentialAssemblyExtensions.Contains(ext))
				_potentialAssemblyExtensions.Add(ext);
		}

		void Init(IEnumerable<Uri> uris)
		{
			_potentialAssemblyExtensions = new List<string>();
			_potentialAssemblyExtensions.Add(".dll");
			_potentialAssemblyExtensions.Add(".exe");

			_uris = new List<Uri>();

			if (uris != null)
				foreach (var uri in uris)
					AddSearchUri(uri);

			AppDomain.CurrentDomain.AssemblyResolve += FindAssemblies;
		}

		public object CreateInstance(Type type)
		{
			var obj = Activator.CreateInstance(type);

			if (obj == null)
				throw new ArgumentException("Cannot create instance " + type.FullName);

			return obj;
		}

		/// <summary>
		/// Gets the original file location of the loaded type
		/// </summary>
		/// <param name="type">Loaded type</param>
		/// <returns>FileInfo</returns>
		public static FileInfo GetOriginalCodeBase(Type type)
		{
			var code = Assembly.GetAssembly(type).CodeBase;
			return new FileInfo(new System.Uri(code).LocalPath);
		}

		public Assembly FindAssemblies(object sender, ResolveEventArgs args)
		{
			// If a linkable component loads additional assemblies OmiEd might
			// have issues resolving them
			// This will help by guessing that the additional assembly might be
			// in the same folder as the linkable component assembly and trying that.
			// Not guarantied to work but definitely does help in some cases.

			var name = new AssemblyName(args.Name).Name;

			if (name.EndsWith(".resources") || name.StartsWith("System.") || name.EndsWith(".XmlSerializers"))
			{
				// don't clutter session log with these
				return null;
			}

			Assembly assembly;
			string file;
			FileInfo fi;
			var sb = new StringBuilder();
			bool loaded = false;

			try
			{
				sb.Append(string.Format("AssemblyLoader({0}).FindAssemblies({1})",
					_caption, name));

				if (_loaded.TryGetValue(name, out assembly))
				{
					sb.AppendLine();
					sb.AppendLine(string.Format("\tPreviously loaded as({0})", assembly.EscapedCodeBase));
					return assembly;
				}

				if (args.RequestingAssembly != null)
				{
					var uri = Utilities.AssemblyUri(args.RequestingAssembly);

					AddSearchUri(uri);
				}

				var folderpath = string.Empty;

				foreach (string extension in _potentialAssemblyExtensions)
				{
					file = name;

					if (!file.EndsWith(extension))
						file = name + extension;

					foreach (var uri in _uris)
					{
						if (!Utilities.UriIsExistingFolderPath(uri, out folderpath))
							continue;

						fi = new FileInfo(Path.Combine(folderpath, file));

						if (!fi.Exists)
							continue;

						sb.AppendLine();
						sb.Append(string.Format("\tLoadFrom({0})", fi.FullName));

						assembly = Assembly.LoadFrom(fi.FullName);

						if (assembly != null)
						{
							loaded = true;

							_loaded.Add(name, assembly);

							return assembly;
						}
					}
				}

				return null;
			}
			finally
			{
				if (args.RequestingAssembly != null)
					sb.AppendFormat("\t\tRequestingAssembly\r\n\t\t\t{0}\r\n\t\t\t{1}\r\n",
						args.RequestingAssembly.FullName,
						args.RequestingAssembly.CodeBase);

				if (loaded)
					Trace.TraceInformation(sb.ToString());
				else
				{
					sb.Append(", unfound");
					Trace.TraceWarning(sb.ToString());
				}
			}
		}
	}
}



