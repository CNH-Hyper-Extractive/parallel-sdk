
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class DocumentExistingFile : IDocumentAccessor
    {
        Uri _uri;
        XDocument _document;

        public DocumentExistingFile()
        {
            _uri = Utilities.AssemblyUri(Assembly.GetExecutingAssembly());
        }

        public DocumentExistingFile(Assembly relativeTo, string relative)
        {
            Contract.Requires(relativeTo != null, "relativeTo != null");
            Contract.Requires(relative != null, "relative != null");

            var uri = Utilities.AssemblyUri(relativeTo);
            _uri = new System.Uri(uri, relative);
        }

        public DocumentExistingFile(Uri uri)
        {
            Contract.Requires(uri != null, "uri != null");

            _uri = uri;
        }

        public DocumentExistingFile(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);
        }

        #region Persistence

        public const string XName = "DocumentExistingFile";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _uri = new Uri(xElement.Element("Uri").Value);
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                new XElement("Uri", _uri.AbsoluteUri));
        }

        #endregion

        public Uri Uri
        {
            get { return _uri; }
            set {_uri = value; }
        }

        public string LocalPath
        {
            get 
            {
                if (_uri == null)
                    return string.Empty;

                // Change %20's into spaces
                return Uri.UnescapeDataString(_uri.LocalPath); 
            }
        }

        public virtual bool UpdateUriFromOpenRequest()
        {
            return _uri != null && File.Exists(_uri.LocalPath);
        }

        public XDocument Open()
        {
            Contract.Requires(_uri != null, "_uri != null");
            Contract.Requires(File.Exists(_uri.LocalPath), "File.Exists({0})", _uri.LocalPath);

            if (_document != null)
                return _document;

            _document = XDocument.Load(_uri.LocalPath);

            return _document;
        }

        public XDocument Open(Uri uri)
        {
            _uri = uri;

            return Open();
        }
    }

    public class DocumentSavableFile : IDocumentAccessorSavable
    {
        Uri _uri;
        XDocument _document;

        public DocumentSavableFile()
        { }

        public DocumentSavableFile(Assembly relativeTo, string relative)
        {
            Contract.Requires(relativeTo != null, "relativeTo != null");
            Contract.Requires(relative != null, "relative != null");

            var uri = Utilities.AssemblyUri(relativeTo);
            _uri = new System.Uri(uri, relative);
        }

        public DocumentSavableFile(Uri fileUri)
        {
            string filepath;
            Contract.Requires(fileUri != null, "fileUri != null");
            Contract.Requires(Utilities.UriIsFilePath(fileUri, out filepath), "{0} is not a file", fileUri.ToString());

            _uri = fileUri;

            Trace.TraceInformation("DocumentFileReadOnly.Uri " + _uri.AbsoluteUri);
        }

        public DocumentSavableFile(XElement xElement, IDocumentAccessor accessor)
        {
            Initialise(xElement, accessor);

            string filepath;
            Contract.Requires(_uri != null, "fileUri != null");
            Contract.Requires(Utilities.UriIsFilePath(_uri, out filepath), "{0} is not a file", _uri.ToString());
        }

        #region Persistence

        public const string XName = "DocumentExistingFile";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _uri = new Uri(xElement.Element("Uri").Value);
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                new XElement("Uri", _uri.AbsoluteUri));
        }

        #endregion

        public Uri Uri
        {
            get { return _uri; }
            set
            {
                Contract.Requires(_uri != null, "_uri != null");
                Contract.Requires(File.Exists(_uri.LocalPath), "File.Exists({0})", _uri.LocalPath);

                _uri = value;
            }
        }

        public string LocalPath
        {
            get
            {
                if (_uri == null)
                    return string.Empty;

                // Change %20's into spaces
                return Uri.UnescapeDataString(_uri.LocalPath);
            }
        }

        public virtual bool UpdateUriFromOpenRequest()
        {
            return _uri != null && File.Exists(_uri.LocalPath);
        }

        public XDocument Open()
        {
            Contract.Requires(_uri != null, "_uri != null");
            Contract.Requires(File.Exists(_uri.LocalPath), "File.Exists({0})", _uri.LocalPath);

            if (_document != null)
                return _document;

            _document = XDocument.Load(_uri.LocalPath);

            return _document;
        }

        public XDocument Open(Uri uri)
        {
            _uri = uri;

            return Open();
        }

        public bool UpdateUriFromSaveAsRequest()
        {
            throw new NotImplementedException("Class has no UI interaction");
        }

        public bool Save(XDocument xDocument)
        {
            xDocument.Save(_uri.LocalPath);

            return true;
        }

        public Reply DiscardUnsavedEdits()
        {
            throw new NotImplementedException("Class has no UI interaction");
        }
    }
}
