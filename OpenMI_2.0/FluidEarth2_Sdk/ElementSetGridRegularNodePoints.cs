using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
	public class ElementSetGridRegularPoints : ElementSet, IElementSetProposed, IPersistence
	{
        public IList<IArgument> Arguments { get; private set; }
        public enum Args { GridParameters = 0, ArgCount };

		public ParametersGridRegular GridParameters { get; private set; }
		public bool FastN { get; private set; }
		public Located PointsLocated { get; private set; }

		public enum Located { CellCentre = 0, Node, }

		public ElementSetGridRegularPoints()
            : base(new SpatialDefinition(new Describes("Regular Grid Points", "Point set specified on a regular grid"), -1), ElementType.Point)
		{
			GridParameters = new ParametersGridRegular();

            Arguments = new IArgument[] {
                new ArgumentGridRegular(new Identity("Grid Parameters"), GridParameters),
                }.ToList();

            ElementType = ElementType.Point;
		}

		public ElementSetGridRegularPoints(
			ParametersGridRegular grid,
			Located location,
			bool fastN)
			: base(new SpatialDefinition(new Describes("Regular Grid Points", "Point set specified on a regular grid"), -1), ElementType.Point)
		{
			GridParameters = grid.Clone() as ParametersGridRegular;
			PointsLocated = location;
			FastN = fastN;

			ElementCount = PointsLocated == Located.CellCentre
				? grid.CellCountX * grid.CellCountY
				: (grid.CellCountX + 1) * (grid.CellCountY + 1);

			Caption = string.Format("Regular Grid {0}x{1} ({2}, {3})", 
				GridParameters.CellCountX, GridParameters.CellCountY,
				PointsLocated.ToString(), ElementCount.ToString());

            Arguments = new IArgument[] {
                new ArgumentGridRegular(new Identity("Grid Parameters"), GridParameters),
                }.ToList();

            ElementType = ElementType.Point;
		}

		int n(int index)
		{
			return FastN
				? index % NX
				: index / NY;
		}

		int m(int index)
		{
			return FastN
				? index / NX
				: index % NY;
		}

		public int NX
		{
			get
			{
				return PointsLocated == Located.CellCentre
					? GridParameters.CellCountX
					: GridParameters.CellCountX + 1;
			}
		}

		public int NY
		{
			get
			{
				return PointsLocated == Located.CellCentre
					? GridParameters.CellCountY
					: GridParameters.CellCountY + 1;
			}
		}

		public override IIdentifiable GetElementId(int index)
		{
			return new Identity(index.ToString(), 
				string.Format("[{0},{1}]", n(index) + 1, m(index) + 1), 
				"Regular mesh index");
		}

        public override int GetElementIndex(IIdentifiable elementId)
        {
            return int.Parse(elementId.Id);
        }

		public override int GetVertexCount(int elementIndex)
		{
			return 1;
		}

		public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
		{
			return GridParameters.Origin.Value1 + n(elementIndex) * GridParameters.DeltaX;
		}

		public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
		{
			return GridParameters.Origin.Value2 + m(elementIndex) * GridParameters.DeltaY;
		}

		public const string XName = "ElementSetGridRegularNodePoints";

		public void Initialise(XElement xElement, IDocumentAccessor accessor)
		{
			xElement = Persistence.ThisOrSingleChild(XName, xElement);

			ISpatialDefinition spatial;
			bool hasZ, hasM;
			ElementType = Persistence.ElementSet.Parse(xElement, accessor, out spatial, out hasZ, out hasM);
			SetSpatial(spatial);
			HasZ = hasZ;
			HasM = hasM;

            Arguments = Persistence.Arguments
                .Parse(xElement, accessor)
                .ToList();

			GridParameters = new ParametersGridRegular();
			GridParameters.Initialise(xElement, accessor);

            ((ArgumentValueGridRegular)Arguments[0]).Value = GridParameters;

			FastN = Utilities.Xml.GetAttribute(xElement, "fastN", true);

            ElementCount = PointsLocated == Located.CellCentre
                ? GridParameters.CellCountX * GridParameters.CellCountY
                : (GridParameters.CellCountX + 1) * (GridParameters.CellCountY + 1);
		}

		public XElement Persist(IDocumentAccessor accessor)
		{
			return new XElement(XName,
				Persistence.ElementSet.Persist(this, accessor),
				GridParameters.Persist(accessor),
				new XAttribute("fastN", FastN.ToString()),
                Persistence.Arguments.Persist(Arguments, accessor));
		}

		/// <summary>
		/// Clone object via constructor for OpenMI interface.
		/// See that constructor comments for details of cloning process.
		/// </summary>
		/// <returns>cloned object</returns>
		public override object Clone()
		{
			return new ElementSetGridRegularPoints(GridParameters, PointsLocated, FastN);
		}

		public bool UpdateGeometryAvailable(IElementSet elementSetEdits)
		{
			return elementSetEdits is ElementSetGridRegularPoints;
		}

		public void UpdateGeometry(IElementSet elementSetEdits)
		{
			Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

			var es = elementSetEdits as ElementSetGridRegularPoints;

			GridParameters = es.GridParameters.Clone() as ParametersGridRegular;
			FastN = es.FastN;
			PointsLocated = es.PointsLocated;

            ElementCount = PointsLocated == Located.CellCentre
                ? GridParameters.CellCountX * GridParameters.CellCountY
                : (GridParameters.CellCountX + 1) * (GridParameters.CellCountY + 1);

            Version = elementSetEdits.Version;
            ElementType = elementSetEdits.ElementType;
            SpatialReferenceSystemWkt = elementSetEdits.SpatialReferenceSystemWkt;
		}

        public void Initialise()
        {
            var parameters = Arguments[0] as ArgumentGridRegular;

            if (parameters.Value == null)
                return;

            var argValue = parameters.Value as ArgumentValueGridRegular;

            if (argValue == null)
                return;

            GridParameters = argValue.Value as ParametersGridRegular;

            ElementCount = PointsLocated == Located.CellCentre
                ? GridParameters.CellCountX * GridParameters.CellCountY
                : (GridParameters.CellCountX + 1) * (GridParameters.CellCountY + 1);
        }
    }
}
