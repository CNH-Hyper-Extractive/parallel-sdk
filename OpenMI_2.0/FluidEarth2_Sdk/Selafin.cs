using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    [Serializable]
    public class Selafin
    {
        FileInfo _selafinFile;
        string _title;
        List<Int32> _nbv, _internalCodes, _counts;
        List<string> _varsStandard = new List<string>();
        List<string> _varsClandestine = new List<string>();
        DateTime _startDateTime;
        bool _validStartDateTime = false;
        int _headerRecordCount = 0;
        int _connectivityRecordCount = 1;
        int _coordinateRecordCount = 3; 
        Int32[,] _connectivityFtn1Based;
        Int32[] _boundariesFtn1Based;
        double[,] _coordinatesFtn1Based;
        float[,] _extents = new float[2, 2];
        List<float> _times;
        Endian _endian = Endian.Big;
        HeaderWidth _width = HeaderWidth.H64;
        bool _initialised = false;

        enum InternalCodeFields { Unknown1 = 0, Unknown2, Unknown3, Unknown4, Unknown5, Unknown6, 
            PlanesOnVerticalInPrismsCount,
            BoundaryPointsCount, 
            InterfacePointsCount, 
            ComputationalStatingDateTime, 
            LAST };

        enum DateTimeFields { Year = 0, Month, Day, Hour, Minute, Second, LAST };

        enum CountFields { Elements, Points, PointsPerElement, Layers, LAST };

        public Selafin(FileInfo file)
        {
            _selafinFile = file;
        }

        public FileInfo FileInfo
        {
            get { return _selafinFile; }
            set { _selafinFile = value; }
        }

        public string Name
        {
            get { return _selafinFile.Name.Substring(0, _selafinFile.Name.LastIndexOf('.')); }
        }

        void ThrowIfNotInitialised()
        {
            if (!_initialised)
                throw new InvalidOperationException("Selafin object has not been Initialised");
        }

        public List<string> VariablesStandard
        {
            get { return _varsStandard; }
        }

        public List<string> VariablesClandestine
        {
            get { ThrowIfNotInitialised();  return _varsClandestine; }
        }

        public string Title
        {
            get { ThrowIfNotInitialised(); return _title; }
        }

        public int ElementCount
        {
            get { ThrowIfNotInitialised(); return _counts[(int)CountFields.Elements]; }
        }

        public int PointsPerElementCount
        {
            get { ThrowIfNotInitialised(); return _counts[(int)CountFields.PointsPerElement]; }
        }

        public int NodeCount
        {
            get { ThrowIfNotInitialised(); return _counts[(int)CountFields.Points]; }
        }

        public int LayerCount
        {
            get { ThrowIfNotInitialised(); return _counts[(int)CountFields.Layers]; }
        }

        public bool HasStartDateTime(out DateTime start)
        {
            ThrowIfNotInitialised();  

            start = _validStartDateTime
                ? _startDateTime
                : new DateTime(1800,1,1);

            return _validStartDateTime;
        }

        public void Initialise()
        {
            _initialised = false;
            _endian = Endian.Little;
            _width = HeaderWidth.H64;

            if (!CanReadValidTitle())
            {
                _endian = Endian.Big;

                if (!CanReadValidTitle())
                {
                    _width = HeaderWidth.H32;

                    if (!CanReadValidTitle())
                    {
                        _endian = Endian.Little;

                        if (!CanReadValidTitle())
                            throw new ArgumentException("SELAFIN IMPORT: Invalid file (Endian/Machine?)");
                    }
                }
            }

            using (FileStream fs = File.OpenRead(_selafinFile.FullName))
            {                
                using (BinaryReader br = new BinaryReader(fs))
                {
                    ReadHeader(br);                    
                }
            }

            _initialised = true;
        }

        bool CanReadValidTitle()
        {
            try
            {
                using (FileStream fs = File.OpenRead(_selafinFile.FullName))
                {
                    using (BinaryReader br = new BinaryReader(fs))
                    {
                        byte[] data = null;

                        if (!NextRecord(br, _endian, _width, out data))
                            return false;

                        if (data == null || data.Count() > 80)
                            return false;

                        string title = ASCIIEncoding.ASCII.GetString(data);
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public Int32[,] ConnectivityFtn1Based
        {
            get
            {
                ThrowIfNotInitialised();  

                if (_connectivityFtn1Based == null)
                {
                    using (FileStream fs = File.OpenRead(_selafinFile.FullName))
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            ReadConnectivity(br, true);
                        }
                    }
                }

                return _connectivityFtn1Based;
            }
        }

        public Int32[] BoundariesFtn1Based
        {
            get
            {
                ThrowIfNotInitialised();  

                if (_boundariesFtn1Based == null)
                {
                    using (FileStream fs = File.OpenRead(_selafinFile.FullName))
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            ReadCoordinates(br, true);
                        }
                    }
                }

                return _boundariesFtn1Based;
            }
        }

        public double[,] CoordinatesFtn1Based
        {
            get
            {
                ThrowIfNotInitialised();  

                if (_coordinatesFtn1Based == null)
                {
                    using (FileStream fs = File.OpenRead(_selafinFile.FullName))
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            ReadCoordinates(br, true);
                        }
                    }
                }

                return _coordinatesFtn1Based;
            }
        }

        public float[] Times
        {
            get
            {
                ThrowIfNotInitialised();  

                if (_times == null)
                {
                    _times = new List<float>();

                    using (FileStream fs = File.OpenRead(_selafinFile.FullName))
                    {
                        using (BinaryReader br = new BinaryReader(fs))
                        {
                            TimeRecords(br, true);
                        }
                    }
                }

                return _times.ToArray();
            }
        }

        public float[] TimeValues(int nTime)
        {
            ThrowIfNotInitialised();  

            using (FileStream fs = File.OpenRead(_selafinFile.FullName))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    float time, minValue, maxValue;
                    return TimeValues(br, nTime, true, out time, out minValue, out maxValue);
                }
            }
        } 

        void ReadHeader(BinaryReader br)
        {
            _nbv = null;
            _varsStandard.Clear();
            _varsClandestine.Clear();
            _internalCodes = null;
            _counts = null;
            _validStartDateTime = false;
            byte[] data = null;

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Title)");

            _title = NextString(data);

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (NBV)");

            _nbv = Int32s(data, _endian);

            if (_nbv.Count != 2)
                throw new ArgumentException("SELAFIN IMPORT: nbv count != 2, " + _nbv.Count.ToString());

            _headerRecordCount = 2;

            for (int n = 0; n < _nbv[0]; ++n)
            {
                if (!NextRecord(br, _endian, _width, out data))
                    throw new ArgumentException("SELAFIN IMPORT: Invalid file read (NBV[0])");

                _varsStandard.Add(NextString(data));
            }

            for (int n = 0; n < _nbv[1]; ++n)
            {
                if (!NextRecord(br, _endian, _width, out data))
                    throw new ArgumentException("SELAFIN IMPORT: Invalid file read (NBV[1])");

                _varsClandestine.Add(NextString(data));
            }

            _headerRecordCount += _nbv[0] + _nbv[1];

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Internal Codes)");

            _internalCodes = Int32s(data, _endian);

            if (_internalCodes.Count() != (int)InternalCodeFields.LAST)
                throw new ArgumentException("SELAFIN IMPORT: InternalCodes count != " + InternalCodeFields.LAST.ToString());

            _headerRecordCount += 1;

            if (_internalCodes[(int)InternalCodeFields.ComputationalStatingDateTime] == 1)
            {
                if (!NextRecord(br, _endian, _width, out data))
                    throw new ArgumentException("SELAFIN IMPORT: Invalid file read (StartDateTime)");

                List<Int32> dt = Int32s(data, _endian);

                if (dt.Count() != (int)DateTimeFields.LAST)
                    throw new ArgumentException("SELAFIN IMPORT: StartDateTime count != " + DateTimeFields.LAST.ToString());

                _startDateTime = new DateTime(
                    dt[(int)DateTimeFields.Year], dt[(int)DateTimeFields.Month], dt[(int)DateTimeFields.Day],
                    dt[(int)DateTimeFields.Hour], dt[(int)DateTimeFields.Minute], dt[(int)DateTimeFields.Second]);

                _headerRecordCount += 1;
                _validStartDateTime = true;
            }

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Counts)");

            _counts = Int32s(data, _endian);

            if (_counts.Count() != (int)CountFields.LAST)
                throw new ArgumentException("SELAFIN IMPORT: Counts count != " + CountFields.LAST.ToString());

            _headerRecordCount += 1;
        }

        void ReadConnectivity(BinaryReader br, bool readFromStart)
        {
            _connectivityFtn1Based = null;
            _boundariesFtn1Based = null;
            byte[] data = null;

            if (readFromStart)
                for (int n = 0; n < _headerRecordCount; ++n)
                    if (!SkipRecord(br, _endian, _width))
                        throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header)");

            int nElements = _counts[(int)CountFields.Elements];
            int nPointsPerElement = _counts[(int)CountFields.PointsPerElement];
            int nPoints = _counts[(int)CountFields.Points]; 
            int nTotal = nElements * nPointsPerElement;

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Connectivity)");

            Int32[] connectivity = Int32s(data, nTotal, _endian);

            _connectivityFtn1Based = new Int32[nElements, nPointsPerElement];

            int k = -1;
            for (int i = 0; i < nElements; ++i)
                for (int j = 0; j < nPointsPerElement; ++j)
                    _connectivityFtn1Based[i, j] = connectivity[++k];
        }

        void ReadCoordinates(BinaryReader br, bool readFromStart)
        {
            _coordinatesFtn1Based = null;
            byte[] data = null;

            if (readFromStart)
                for (int n = 0; n < _headerRecordCount + _connectivityRecordCount; ++n)
                    if (!SkipRecord(br, _endian, _width))
                        throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header + Connectivity)");

            int nPoints = _counts[(int)CountFields.Points]; 
            
            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Boundaries)");
                 
            _boundariesFtn1Based = Int32s(data, nPoints, _endian);

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (X)");

            float[] x = Floats(data, nPoints, _endian, out _extents[0, 0], out _extents[1, 0]);

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Y)");

            float[] y = Floats(data, nPoints, _endian, out _extents[0, 0], out _extents[1, 0]);

            _coordinatesFtn1Based =  new double[nPoints + 1, 2];
            _coordinatesFtn1Based[0, 0] = double.MinValue;
            _coordinatesFtn1Based[0, 1] = double.MinValue;

            for (int n = 0; n < nPoints; ++n)
            {
                _coordinatesFtn1Based[n + 1, 0] = x[n];
                _coordinatesFtn1Based[n + 1, 1] = y[n];
            }            
        }

        void TimeRecords(BinaryReader br, bool readFromStart)
        {
            _times.Clear();
            byte[] data = null;

            if (readFromStart)
                for (int n = 0; n < _headerRecordCount + _connectivityRecordCount + _coordinateRecordCount; ++n)
                    if (!SkipRecord(br, _endian, _width))
                        throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header + Connectivity + Coordinates)");

            while (NextRecord(br, _endian, _width, out data))
            {
                if (_endian != Endian.Little)
                    Array.Reverse(data);

                _times.Add(BitConverter.ToSingle(data, 0));

                for (int n = 0; n < VariablesStandard.Count + VariablesClandestine.Count; ++n)
                    if (!SkipRecord(br, _endian, _width))
                        throw new ArgumentException("SELAFIN IMPORT: Invalid file read (VariablesStandard.Count + VariablesClandestine.Count)");
            }
        }

        float[] TimeValues(BinaryReader br, int nTime, bool readFromStart, out float time, out float minValue, out float maxValue)
        {
            _times.Clear();
            byte[] data = null;

            if (readFromStart)
                for (int n = 0; n < _headerRecordCount + _connectivityRecordCount + _coordinateRecordCount; ++n)
                    if (!SkipRecord(br, _endian, _width))
                        throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header + Connectivity + Coordinates)");

            for (int n = 0; n < nTime; ++n)
            {
                if (!SkipRecord(br, _endian, _width))
                    throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header + Connectivity + Coordinates) Time " + n.ToString());
                if (!SkipRecord(br, _endian, _width))
                    throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header + Connectivity + Coordinates) Values " + n.ToString());
            }

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header + Connectivity + Coordinates) Time " + nTime.ToString());

            if (_endian != Endian.Little)
                Array.Reverse(data);

            time = BitConverter.ToSingle(data, 0);

            if (!NextRecord(br, _endian, _width, out data))
                throw new ArgumentException("SELAFIN IMPORT: Invalid file read (Header + Connectivity + Coordinates) Time " + nTime.ToString());

            return Floats(data, _counts[(int)CountFields.Points], _endian, out minValue, out maxValue);
        }

        static string NextString(byte[] record)
        {
            return ASCIIEncoding.ASCII.GetString(record); 
        }

        enum Endian { Little = 0, Big }

        static List<Int32> Int32s(byte[] record, Endian endian)
        {
            List<Int32> values = new List<int>();

            int size = sizeof(Int32);
            byte[] part = new byte[size];

            for (int n = 0; n < record.Length; n += size)
            {
                Array.Copy(record, n, part, 0, size);

                if (endian != Endian.Little)
                    part = part.Reverse().ToArray();

                values.Add(BitConverter.ToInt32(part, 0));
            }

            return values;
        }

        static Int32[] Int32s(byte[] record, int nCount, Endian endian)
        {
            int size = sizeof(Int32);

            if (nCount*size > record.Length)
                throw new ArgumentException("SELAFIN IMPORT: record too small, " + record.Length.ToString());

            byte[] part = new byte[size];
            Int32[] values = new Int32[nCount]; 

            for (int n = 0; n < nCount; ++n)
            {
                Array.Copy(record, n*size, part, 0, size);

                if (endian != Endian.Little)
                    part = part.Reverse().ToArray();

                values[n] = BitConverter.ToInt32(part, 0);
            }

            return values;
        }

        static float[] Floats(byte[] record, int nCount, Endian endian, out float min, out float max)
        {
            int size = sizeof(float);

            if (nCount * size > record.Length)
                throw new ArgumentException("SELAFIN IMPORT: record too small, " + record.Length.ToString());

            byte[] part = new byte[size];
            float[] values = new float[nCount];
            min = 0;
            max = 0;

            for (int n = 0; n < nCount; ++n)
            {
                Array.Copy(record, n * size, part, 0, size);

                if (endian != Endian.Little)
                    part = part.Reverse().ToArray();

                values[n] = BitConverter.ToSingle(part, 0);

                if (n == 0)
                {
                    min = values[n];
                    max = values[n];
                }
                else if (values[n] < min)
                    min = values[n];
                else if (values[n] > max)
                    max = values[n];
            }

            return values;
        }

        enum HeaderWidth { H32 = 2, H64 = 4, }

        static bool NextRecord(BinaryReader br, Endian endian, HeaderWidth width, out byte[] data)
        {
            data = null;

            byte[] b1 = new byte[(int)width];
            byte[] b2 = new byte[(int)width];                

            if (br.Read(b1, 0, (int)width) < (int)width)
                return false;

            if (endian != Endian.Little)
                b1 = b1.Reverse().ToArray();

            Int32 n1 = BitConverter.ToInt32(b1, 0);

            data = br.ReadBytes(n1);

            if (br.Read(b2, 0, (int)width) < (int)width)
                return false;

            if (endian != Endian.Little)
                b2 = b2.Reverse().ToArray();

            Int32 n2 = BitConverter.ToInt32(b2, 0);

            if (n1 != n2)
                throw new ArgumentException("FORTRAN RECORD READ: Record end/start missmatch");

            return true;
        }

        static bool SkipRecord(BinaryReader br, Endian endian, HeaderWidth width)
        {
            byte[] b1 = new byte[(int)width];
            byte[] b2 = new byte[(int)width];

            if (br.Read(b1, 0, (int)width) < (int)width)
                return false;

            if (endian != Endian.Little)
                b1 = b1.Reverse().ToArray();

            Int32 n1 = BitConverter.ToInt32(b1, 0);

            br.ReadBytes(n1);

            if (br.Read(b2, 0, (int)width) < (int)width)
                return false;

            if (endian != Endian.Little)
                b2 = b2.Reverse().ToArray();

            Int32 n2 = BitConverter.ToInt32(b2, 0);

            if (n1 != n2)
                throw new ArgumentException("FORTRAN RECORD READ: Record end/start missmatch");

            return true;
        }

        public XElement XTableHeader()
        {
            StringBuilder data = new StringBuilder();
            data.AppendLine("Title," + Title);
            data.AppendLine("Standard Variable Count," + VariablesStandard.Count.ToString());
            data.AppendLine("Clandestine Variable Count," + VariablesClandestine.Count.ToString());
            if (_validStartDateTime)
                data.AppendLine("Start Date Time," + _startDateTime.ToString("u"));
            else
                data.AppendLine("Start Date Time");
            data.AppendLine("Element Count," + ElementCount.ToString());
            data.AppendLine("Points per Element Count," + PointsPerElementCount.ToString());
            data.AppendLine("Point Count," + NodeCount.ToString());
            data.AppendLine("Layer Count," + LayerCount.ToString());
            data.AppendLine("Time Record Count," + Times.Length.ToString());

            return new XElement("table",
                new XElement("column",
                    new XAttribute("readOnly", "true")),
                new XElement("column",
                    new XAttribute("readOnly", "true")),
                new XElement("data",
                    new XCData(data.ToString())));
        }

        public XElement XTableConnectivity()
        {
            StringBuilder data = new StringBuilder();
            StringBuilder line = new StringBuilder();

            for (int i = 0; i < ElementCount; ++i)
            {
                line.Remove(0, line.Length); 

                for (int j = 0; j < PointsPerElementCount; ++j)
                    line.Append(ConnectivityFtn1Based[i, j].ToString() + ",");

                data.AppendLine(line.ToString());
            }

            List<XElement> xColumns = new List<XElement>();

            for (int j = 0; j < PointsPerElementCount; ++j)
                xColumns.Add(
                    new XElement("column", 
                        new XAttribute("readOnly", "true"), 
                        new XAttribute("type", "tInt")));

            return new XElement("table",
                new XAttribute("includeRowNumbers", "true"),
                xColumns,
                new XElement("data",
                    new XCData(data.ToString())));
        }

        public XElement XTableCoordinates()
        {
            StringBuilder data = new StringBuilder();
            StringBuilder line = new StringBuilder();

            data.AppendLine("Boundary,X,Y");

            for (int i = 1; i < NodeCount + 1; ++i)
            {
                line.Remove(0, line.Length);

                line.Append(BoundariesFtn1Based[i - 1].ToString() + ","); 
                line.Append(CoordinatesFtn1Based[i, 0].ToString() + ",");
                line.Append(CoordinatesFtn1Based[i, 1].ToString());

                data.AppendLine(line.ToString());
            }

            List<XElement> xColumns = new List<XElement>();

            xColumns.Add( // Boundary
                new XElement("column",
                    new XAttribute("readOnly", "true"),
                    new XAttribute("type", "tInt"))); 
            xColumns.Add( // X
                new XElement("column",
                    new XAttribute("readOnly", "true"),
                    new XAttribute("type", "tDouble"))); // Really float!
            xColumns.Add( // Y
                new XElement("column",
                    new XAttribute("readOnly", "true"),
                    new XAttribute("type", "tDouble"))); // Really float!

            return new XElement("table",
                new XAttribute("includeRowNumbers", "true"),
                xColumns,
                new XElement("data",
                    new XAttribute("headerRow", "true"), 
                    new XCData(data.ToString())));
        }

        public XElement XTableVariablesStandard()
        {
            StringBuilder data = new StringBuilder();

            for (int i = 0; i < VariablesStandard.Count; ++i)
                data.AppendLine(VariablesStandard[i]);

            return new XElement("table",
                new XAttribute("includeRowNumbers", "true"),
                new XElement("column",
                    new XAttribute("readOnly", "true")),
                new XElement("data",
                    new XCData(data.ToString())));
        }

        public XElement XTableVariablesClandestine()
        {
            StringBuilder data = new StringBuilder();

            for (int i = 0; i < VariablesClandestine.Count; ++i)
                data.AppendLine(VariablesClandestine[i]);

            return new XElement("table",
                new XAttribute("includeRowNumbers", "true"),
                new XElement("column",
                    new XAttribute("readOnly", "true")),
                new XElement("data",
                    new XCData(data.ToString())));
        }

        public XElement XTableTimeRecords()
        {
            StringBuilder data = new StringBuilder();

            for (int i = 0; i < Times.Length; ++i)
                data.AppendLine(Times[i].ToString());

            return new XElement("table",
                new XAttribute("includeRowNumbers", "true"),
                new XElement("column",
                    new XAttribute("readOnly", "true"),
                    new XAttribute("type", "tDouble")), // really float
                new XElement("data",
                    new XCData(data.ToString())));
        }

        public XElement XTableTimeValues(int nTime)
        {
            StringBuilder data = new StringBuilder();
            StringBuilder line = new StringBuilder();

            // Header

            for (int n = 0; n < VariablesStandard.Count; ++n)
                line.Append(VariablesStandard[n] + ",");
            for (int n = 0; n < VariablesClandestine.Count; ++n)
                line.Append(VariablesClandestine[n] + ",");

            data.AppendLine(line.ToString());

            // Body

            float[] values = TimeValues(nTime);

            StringBuilder[] lines = new StringBuilder[NodeCount];

            for (int i = 0; i < NodeCount; ++i)
                lines[i] = new StringBuilder();

            int nVariables = VariablesStandard.Count + VariablesClandestine.Count;

            int k = -1;
            for (int j = 0; j < nVariables; ++j)
                for (int i = 0; i < NodeCount; ++i)
                    lines[i].Append(values[++k].ToString() + ",");

            for (int i = 0; i < NodeCount; ++i)
                data.AppendLine(lines[i].ToString());

            List<XElement> xColumns = new List<XElement>();

            for (int n = 0; n < nVariables; ++n)
                xColumns.Add( // Variable
                    new XElement("column",
                        new XAttribute("readOnly", "true"),
                        new XAttribute("type", "tDouble"))); // Really float!

            return new XElement("table",
                new XAttribute("includeRowNumbers", "true"),
                xColumns,
                new XElement("data",
                    new XAttribute("headerRow", "true"),
                    new XCData(data.ToString())));
        }
        /*
        public List<Geometry.PointIndexed> Points()
        {
            List<Geometry.PointIndexed> points = new List<Geometry.PointIndexed>();

            for (int n = 0; n < NodeCount; ++n)
                points.Add(
                    new Geometry.PointIndexed(
                        n + 1, 
                        CoordinatesFtn1Based[n + 1, 0],
                        CoordinatesFtn1Based[n + 1, 1],
                        -1.0)); // No S

            return points;
        }

        public List<Geometry.Edge> Edges()
        {
            List<Geometry.Edge> edges = new List<Geometry.Edge>(ElementCount);

            for (int nE = 0; nE < ElementCount; ++nE)
            {
                for (int nP = 0; nP < PointsPerElementCount - 1; ++nP)
                    edges.Add(new Geometry.Edge(ConnectivityFtn1Based[nE, nP], ConnectivityFtn1Based[nE, nP + 1]));

                edges.Add(new Geometry.Edge(ConnectivityFtn1Based[nE, PointsPerElementCount - 1], ConnectivityFtn1Based[nE, 0]));
            }

            return edges.Distinct().ToList();
        }

        public List<Geometry.Polygon> Elements()
        {
            List<Geometry.Polygon> elements = new List<Geometry.Polygon>(ElementCount);
            List<Geometry.Edge> edges = new List<Geometry.Edge>(PointsPerElementCount + 1);

            DotSpatial.Data.Shape shape;
            int nNode;

            for (int nE = 0; nE < ElementCount; ++nE)
            {
                edges.Clear();

                shape = new DotSpatial.Data.Shape(DotSpatial.Topology.FeatureType.Polygon);

                for (int nP = 0; nP < PointsPerElementCount - 1; ++nP)
                {
                    nNode = ConnectivityFtn1Based[nE, nP];
                    edges.Add(new Geometry.Edge(ConnectivityFtn1Based[nE, nP], ConnectivityFtn1Based[nE, nP + 1]));
                }

                edges.Add(new Geometry.Edge(ConnectivityFtn1Based[nE, PointsPerElementCount - 1], ConnectivityFtn1Based[nE, 0]));

                elements.Add(new Geometry.Polygon(edges, CoordinatesFtn1Based));
            }

            return elements;
        }

        public void Perimeters(out List<Geometry.Polygon> polygons, out List<Geometry.Polyline> polylines)
        {
            List<List<Geometry.Edge>> polygonEdges = new List<List<Geometry.Edge>>();
            List<List<Geometry.Edge>> polylineEdges = new List<List<Geometry.Edge>>();

            List<int>[] nodeElementsFtn1Based = new List<int>[NodeCount + 1];
            int node;

            for (int nE = 0; nE < ElementCount; ++nE)
            {
                for (int j = 0; j < PointsPerElementCount; ++j)
                {
                    node = ConnectivityFtn1Based[nE, j];

                    if (nodeElementsFtn1Based[node] == null)
                        nodeElementsFtn1Based[node] = new List<int>(PointsPerElementCount);

                    nodeElementsFtn1Based[node].Add(nE + 1);
                }
            }

            HashSet<Geometry.Edge> edgeSet = new HashSet<Geometry.Edge>(new Geometry.Edge());
            int node1, node2, nN2, face1;
            Geometry.Edge edge;

            for (int nE = 0; nE < ElementCount; ++nE)
            {
                for (int nN = 0; nN < PointsPerElementCount; ++nN)
                {
                    nN2 = nN < PointsPerElementCount - 1 ? nN + 1 : 0;

                    node1 = ConnectivityFtn1Based[nE, nN];
                    node2 = ConnectivityFtn1Based[nE, nN2];
                    face1 = nE + 1;

                    edge = new Geometry.Edge(node1, node2, face1, -1);

                    if (edgeSet.Add(edge))
                    {
                        // New edge find other face

                        foreach (int element in nodeElementsFtn1Based[node1])
                        {
                            if (element == nE + 1)
                                continue;

                            for (int nN3 = 0; nN3 < PointsPerElementCount; ++nN3)
                            {
                                if (ConnectivityFtn1Based[element - 1, nN3] == node2)
                                {
                                    edge.Face2 = element;
                                    break;
                                }
                            }

                            if (edge.Face2 > -1)
                                break;
                        }
                    }
                }
            }

            List<Geometry.Edge> edges = edgeSet.Where(e => e.Face2 == -1).ToList();

            List<Geometry.Edge> cycle;
            bool closed;

            int nEdge;

            while (edges.Count > 0)
            {
                cycle = new List<Geometry.Edge>();
                nEdge = 0; ;

                while (nEdge > -1)
                {
                    cycle.Add(edges[nEdge]);
                    edges[nEdge] = null;

                    nEdge = edges.FindIndex(e => e != null && e.Node1 == cycle[cycle.Count - 1].Node2);
                }

                closed = cycle[0].Node1 == cycle[cycle.Count - 1].Node2;

                if (closed)
                    polygonEdges.Add(cycle);
                else
                {
                    foreach (List<Geometry.Edge> spline in polylineEdges)
                    {
                        if (cycle[cycle.Count - 1].Node2 == spline[0].Node1)
                        {
                            cycle.AddRange(spline);
                            polylineEdges.Remove(spline);
                            break;
                        }
                    }

                    closed = cycle[0].Node1 == cycle[cycle.Count - 1].Node2;

                    if (closed)
                        polygonEdges.Add(cycle);
                    else
                        polylineEdges.Add(cycle);
                }

                edges.RemoveAll(e => e == null);
            }

            polygons = polygonEdges
                .Select(p => new Geometry.Polygon(p, CoordinatesFtn1Based))
                .ToList();

            polylines = polylineEdges
                .Select(p => new Geometry.Polyline(p, CoordinatesFtn1Based))
                .ToList();
        }

        public static Geometry.Polygon PolygonFromPoint(List<Geometry.Polygon> polygons, int pointIndex)
        {
            foreach (var polygon in polygons)
                if (polygon.Points.Where(p => p.Index == pointIndex).Count() > 0)
                    return polygon;

            return null;
        }
         */
    }
}

