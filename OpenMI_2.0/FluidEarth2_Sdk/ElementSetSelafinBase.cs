using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public abstract class ElementSetSelafinBase : ElementSet, IElementSetProposed, IPersistence
    {
        public IList<IArgument> Arguments { get; private set; }
        public enum Args { SelafinFile = 0, };

        protected Selafin Selafin { get; private set; }
        protected bool Initialised { get; private set; }

        public ElementSetSelafinBase(ISpatialDefinition spatial, ElementType elementType, FileInfo selafinFileInfo = null)
            : base(spatial, elementType)
        {
            Arguments = new IArgument[] {
                new ArgumentFile(new Identity("Selafin file"), selafinFileInfo),
                }.ToList();

            Selafin = new Selafin(selafinFileInfo);
        }

        public void Initialise()
        {
            var selafin = Arguments[0] as ArgumentValueFile;

            if (selafin.Value != null)
            {
                Selafin.FileInfo = (FileInfo)selafin.Value;
                Selafin.Initialise();
                ElementCount = Selafin.NodeCount;
            }

            Initialised = true;
        }

        public const string XName = "ElementSetSelafinElements";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            ISpatialDefinition spatial;
            bool hasZ, hasM;
            ElementType = Persistence.ElementSet.Parse(xElement, accessor, out spatial, out hasZ, out hasM);
            SetSpatial(spatial);
            HasZ = hasZ;
            HasM = hasM;

            string relative = xElement.Value;

            var uri = new Uri(accessor.Uri, relative);

            var selafin = new FileInfo(uri.LocalPath);

            Selafin = new Selafin(selafin);

           var argIdSelafin =  Utilities.Xml.GetAttribute(xElement, "argIdSelafin");

           Arguments = new IArgument[] {
                new ArgumentFile(new Identity(argIdSelafin, "Selafin file"), selafin),
                }.ToList();

            Initialise();
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            if (Selafin.FileInfo == null || Selafin.FileInfo.FullName == null)
                return new XElement(XName, new XAttribute("argIdSelafin", Arguments[0].Id));

            var relative = new Uri(Selafin.FileInfo.FullName);

            var uri = new Uri(accessor.Uri, relative);

            return new XElement(XName, uri.LocalPath,
                new XAttribute("argIdSelafin", Arguments[0].Id));
        }

        public bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return elementSetEdits is ElementSetSelafinBase;
        }

        public void UpdateGeometry(IElementSet elementSetEdits)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            var e = elementSetEdits as ElementSetSelafinBase;

            Selafin = new Selafin(e.Selafin.FileInfo);
            Initialised = false;

            Version = elementSetEdits.Version;
            ElementType = elementSetEdits.ElementType;
            SpatialReferenceSystemWkt = elementSetEdits.SpatialReferenceSystemWkt;
            ElementCount = elementSetEdits.ElementCount;
        }
    }
}

