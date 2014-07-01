using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public abstract class ElementSetGridRegularBase : ElementSet, IElementSetProposed, IPersistence
    {
        public IList<IArgument> Arguments { get; private set; }
        public enum Args { GridParameters = 0, ArgCount };

        public ParametersGridRegular GridParameters { get; private set; }

        public ElementSetGridRegularBase(ParametersGridRegular grid, ISpatialDefinition spatialDefinition, ElementType elementType)
            : base(spatialDefinition, elementType)
        {
            GridParameters = grid.Clone() as ParametersGridRegular;

            Arguments = new IArgument[] {
                new ArgumentGridRegular(new Identity("Grid Parameters"), GridParameters),
                }.ToList();

            ElementType = ElementType;
        }

        public int n(int index)
        {
            return index % NX;
        }

        public int m(int index)
        {
            return index / NX;
        }

        public int NX
        {
            get
            {
                return GridParameters.CellCountX + 1;
            }
        }

        public int NY
        {
            get
            {
                return GridParameters.CellCountY + 1;
            }
        }

        public const string XName = "ElementSetGridRegularBase";

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
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName,
                Persistence.ElementSet.Persist(this, accessor),
                GridParameters.Persist(accessor),
                Persistence.Arguments.Persist(Arguments, accessor));
        }

        public bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return elementSetEdits is ElementSetGridRegularBase;
        }

        public void UpdateGeometry(IElementSet elementSetEdits)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            var es = elementSetEdits as ElementSetGridRegularBase;

            GridParameters = es.GridParameters.Clone() as ParametersGridRegular;

            Version = elementSetEdits.Version;
            ElementType = elementSetEdits.ElementType;
            SpatialReferenceSystemWkt = elementSetEdits.SpatialReferenceSystemWkt;
        }

        public virtual void Initialise()
        {
            var parameters = Arguments[0] as ArgumentGridRegular;

            if (parameters.Value == null)
                return;

            var argValue = parameters.Value as ArgumentValueGridRegular;

            if (argValue == null)
                return;

            GridParameters = argValue.Value as ParametersGridRegular;
        }
    }

    public class ElementSetGridRegularCells : ElementSetGridRegularBase
	{
        public ElementSetGridRegularCells()
            : this(new ParametersGridRegular())
		{ }

        public ElementSetGridRegularCells(ParametersGridRegular grid)
            : base(grid, 
                new SpatialDefinition(new Describes("Regular Grid Cells", "Polygon set specified as a regular grid"), -1), 
                ElementType.Polygon)
		{
            ElementCount = GridParameters.CellCountX * GridParameters.CellCountY;

			Caption = string.Format("Regular Grid Cells: {0}x{1} = {2}", 
				GridParameters.CellCountX, GridParameters.CellCountY, ElementCount.ToString());
		}

		public override IIdentifiable GetElementId(int index)
		{
			return new Identity(index.ToString(), 
				string.Format("[{0},{1}]", n(index), m(index)), 
				"Regular mesh index");
		}

        public override int GetElementIndex(IIdentifiable elementId)
        {
            return int.Parse(elementId.Id);
        }

		public override int GetVertexCount(int elementIndex)
		{
			return 4;
		}

		public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
		{
            var nX = n(elementIndex);

            if (vertexIndex == 0 || vertexIndex == 3)
                return GridParameters.Origin.Value1 + nX * GridParameters.DeltaX;
            if (vertexIndex == 1 || vertexIndex == 2)
                return GridParameters.Origin.Value1 + (nX + 1)* GridParameters.DeltaX;

            throw new IndexOutOfRangeException("vertexIndex " + vertexIndex.ToString());
		}

		public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
		{
            var nY = m(elementIndex);

            if (vertexIndex == 0 || vertexIndex == 1)
                return GridParameters.Origin.Value2 + nY * GridParameters.DeltaY;
            if (vertexIndex == 2 || vertexIndex == 3)
                return GridParameters.Origin.Value2 + (nY + 1) * GridParameters.DeltaY;

            throw new IndexOutOfRangeException("vertexIndex " + vertexIndex.ToString());
		}

        public const string XName = "ElementSetGridRegularCells";

		public void Initialise(XElement xElement, IDocumentAccessor accessor)
		{
			xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            ElementCount = (GridParameters.CellCountX + 1) * (GridParameters.CellCountY + 1);
		}

		public XElement Persist(IDocumentAccessor accessor)
		{
			return new XElement(XName, base.Persist(accessor));
		}

		/// <summary>
		/// Clone object via constructor for OpenMI interface.
		/// See that constructor comments for details of cloning process.
		/// </summary>
		/// <returns>cloned object</returns>
		public override object Clone()
        {
            return new ElementSetGridRegularCells(GridParameters);
		}

		public bool UpdateGeometryAvailable(IElementSet elementSetEdits)
		{
            return elementSetEdits is ElementSetGridRegularCells;
		}

		public void UpdateGeometry(IElementSet elementSetEdits)
		{
			Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            base.UpdateGeometry(elementSetEdits);

            ElementCount = GridParameters.CellCountX * GridParameters.CellCountY;
		}

        public override void Initialise()
        {
            base.Initialise();

            ElementCount = GridParameters.CellCountX * GridParameters.CellCountY;
        }
    }

    public class ElementSetGridRegularNodes : ElementSetGridRegularBase
    {
        public ElementSetGridRegularNodes()
            : this(new ParametersGridRegular())
        { }

        public ElementSetGridRegularNodes(ParametersGridRegular grid)
            : base(grid,
                new SpatialDefinition(new Describes("Regular Grid Nodes", "Point set specified on nodes of a regular grid"), -1),
                ElementType.Point)
        {
            ElementCount = NX * NY;

            Caption = string.Format("Regular Grid Nodes: {0}x{1} = {2}",
                NX, NY, ElementCount.ToString());
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
            return 4;
        }

        public override double GetVertexXCoordinate(int elementIndex, int vertexIndex)
        {
            var nX = n(elementIndex);

            if (vertexIndex == 0 || vertexIndex == 3)
                return GridParameters.Origin.Value1 + nX * GridParameters.DeltaX;
            if (vertexIndex == 1 || vertexIndex == 2)
                return GridParameters.Origin.Value1 + (nX + 1) * GridParameters.DeltaX;

            throw new IndexOutOfRangeException("vertexIndex " + vertexIndex.ToString());
        }

        public override double GetVertexYCoordinate(int elementIndex, int vertexIndex)
        {
            var nY = m(elementIndex);

            if (vertexIndex == 0 || vertexIndex == 1)
                return GridParameters.Origin.Value2 + nY * GridParameters.DeltaY;
            if (vertexIndex == 2 || vertexIndex == 3)
                return GridParameters.Origin.Value2 + (nY + 1) * GridParameters.DeltaY;

            throw new IndexOutOfRangeException("vertexIndex " + vertexIndex.ToString());
        }

        public const string XName = "ElementSetGridRegularCells";

        public void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            base.Initialise(xElement, accessor);

            ElementCount = (GridParameters.CellCountX + 1) * (GridParameters.CellCountY + 1);
        }

        public XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName, base.Persist(accessor));
        }

        /// <summary>
        /// Clone object via constructor for OpenMI interface.
        /// See that constructor comments for details of cloning process.
        /// </summary>
        /// <returns>cloned object</returns>
        public override object Clone()
        {
            return new ElementSetGridRegularCells(GridParameters);
        }

        public bool UpdateGeometryAvailable(IElementSet elementSetEdits)
        {
            return elementSetEdits is ElementSetGridRegularCells;
        }

        public void UpdateGeometry(IElementSet elementSetEdits)
        {
            Contract.Requires(UpdateGeometryAvailable(elementSetEdits), "updateGeometryAvailable(elementSetEdits)");

            base.UpdateGeometry(elementSetEdits);

            ElementCount = GridParameters.CellCountX * GridParameters.CellCountY;
        }

        public override void Initialise()
        {
            base.Initialise();

            ElementCount = GridParameters.CellCountX * GridParameters.CellCountY;
        }
    }
}
