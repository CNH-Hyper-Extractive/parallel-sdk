using System;
using System.Xml.Linq;
using FluidEarth2.Sdk.Interfaces;

namespace FluidEarth2.Sdk
{
    public class ParametersGridRegular : IPersistence, ICloneable
    {
        public Coord2d Origin { get; set; }
        public Coord2d TopRight { get; set; }
        public double DeltaX { get; set; }
        public double DeltaY { get; set; }
        public int CellCountX { get; set; }
        public int CellCountY { get; set; }

        public ParametersGridRegular()
        {
            Origin = new Coord2d(0, 0); 
            TopRight = new Coord2d(1, 1);
            DeltaX = 1;
            DeltaY = 1;
            CellCountX = 1;
            CellCountY = 1;
        }

        public ParametersGridRegular(int cellCountX, int cellCountY, Coord2d origin, Coord2d topRight)
        {
            CellCountX = cellCountX;
            CellCountY = cellCountY;

            SetExtent(origin, topRight);
        }

        public ParametersGridRegular(int cellCountX, int cellCountY, Coord2d origin, double deltaX, double deltaY)
        {
            CellCountX = cellCountX;
            CellCountY = cellCountY;

            DeltaX = deltaX;
            DeltaY = deltaY;

            Origin = origin;
            TopRight = new Coord2d(origin.Value1 + cellCountX * deltaX, origin.Value2 + cellCountY * deltaY);

            Contract.Requires(TopRight.Value1 > Origin.Value1, "_topRight.Value1 > _origin.Value1");
            Contract.Requires(TopRight.Value2 > Origin.Value2, "_topRight.Value2 > _origin.Value2");
        }

        public void SetExtent(Coord2d origin, Coord2d topRight)
        {
            Contract.Requires(topRight.Value1 > origin.Value1, "topRight.Value1 > origin.Value1");
            Contract.Requires(topRight.Value2 > origin.Value2, "topRight.Value2 > origin.Value2");

            Origin = origin;
            TopRight = topRight;

            DeltaX = (TopRight.Value1 - Origin.Value1) / (double)CellCountX;
            DeltaY = (TopRight.Value2 - Origin.Value2) / (double)CellCountY;
        }

        public const string XName = "ParametersGridRegular";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            CellCountX = int.Parse(Utilities.Xml.GetAttribute(xElement, "cellCountX"));
            CellCountY = int.Parse(Utilities.Xml.GetAttribute(xElement, "cellCountY"));

            SetExtent(
                new Coord2d(xElement.Element("Origin"), accessor),
                new Coord2d(xElement.Element("TopRight"), accessor));
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                new XAttribute("cellCountX", CellCountX.ToString()),
                new XAttribute("cellCountY", CellCountY.ToString()),
                new XElement("Origin", Origin.Persist(accessor)),
                new XElement("TopRight", TopRight.Persist(accessor)));
        }

        public object Clone()
        {
            return new ParametersGridRegular(CellCountX, CellCountY, Origin, TopRight);
        }
    }
}
