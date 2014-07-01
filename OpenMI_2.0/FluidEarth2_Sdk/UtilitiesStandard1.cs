
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard;
using OpenMI.Standard2;
using IArgument1 = OpenMI.Standard.IArgument;
using IElementSet1 = OpenMI.Standard.IElementSet;
using ILinkableComponent1 = OpenMI.Standard.ILinkableComponent;
using IQuantity1 = OpenMI.Standard.IQuantity;
using ITime1 = OpenMI.Standard.ITime;
using ITime2 = OpenMI.Standard2.TimeSpace.ITime;

namespace FluidEarth2.Sdk
{
	public static partial class Utilities
	{
		public static class Standard1
		{
            public static LinkableComponentV1Base CreateWrappedComponent(ExternalType component, IEnumerable<Argument1> arguments)
            {
                var ns = Omi.Component.NamespaceOpenMIv1;

                var omi = new XElement(
                    ns + "LinkableComponent",                
                    new XAttribute("Type", component.TypeName),
                    new XAttribute("Assembly", component.Url.LocalPath),
                    new XElement(ns + "Arguments", arguments.Select(a => 
                        new XElement(ns + "Argument",
                            new XAttribute("Key", a.Key),
                            new XAttribute("Value", a.Value))))
                );

                var version = Omi.Component.Version.One;

                if (arguments.Any(a => a.Key.Contains("FluidEarth") || a.Key.Contains("OpenWEB")))
                    version = Omi.Component.Version.OneFluidEarth;

                LinkableComponentV1Base c = null;

                if (version == Omi.Component.Version.One)
                    c = new LinkableComponentV1OpenMI();
                else
                    c = new LinkableComponentV1FluidEarth();

                throw new NotImplementedException();

                // c.ArgumentV1Omi = ??;
            }

#if DEPRECATED

			public static LinkableComponentOpenMIV1Wrapper CreateWrappedComponent(ExternalType component, IEnumerable<Argument1> arguments)
			{
				Type tComponentV1;
				ILinkableComponent1 component1
					= component.CreateInstance(out tComponentV1)
					as ILinkableComponent1;

				if (component1 == null)
					throw new Exception("Cannot instantiate " + component.ToString());

				component1.Initialize(arguments.ToArray());

				bool isFluidEarthV1 = tComponentV1.BaseType != null &&
					tComponentV1.BaseType.AssemblyQualifiedName.Contains("FluidEarth.Sdk.LinkableComponent");

				return isFluidEarthV1
					? new LinkableComponentFluidEarthV1Wrapper(component1, component.DocumentAccessor, arguments.ToList())
					: new LinkableComponentOpenMIV1Wrapper(component1, component.DocumentAccessor, arguments.ToList());
			}

            public enum Component1Type { OpenMIGeneric = 0, FluidEarthEngine5, }

            public static LinkableComponentOpenMIV1Wrapper ImportComponent(Component1Type v1Type, XElement xLinkableComponentV1, IDocumentAccessor accessor)
            {
                var assembly = Utilities.Xml.GetAttribute(xLinkableComponentV1, "Assembly");
                var type = Utilities.Xml.GetAttribute(xLinkableComponentV1, "Type");
                var assemblyUri = new Uri(accessor.Uri, assembly);

                string path;

                if (!FluidEarth2.Sdk.Utilities.UriIsFilePath(assemblyUri, out path) || !File.Exists(path))
                    throw new Exception("OMI file Assembly attribute cannot be resolved to an existing file, correct OMI file and retry. Used: "
                        + assembly);

                var componentTypeV1 = new ExternalType(accessor);
                componentTypeV1.Initialise(assemblyUri.LocalPath, type);

                Type tComponentV1;
                var component1 = componentTypeV1.CreateInstance(out tComponentV1) as ILinkableComponent1;

                if (component1 == null)
                    throw new Exception("Cannot instantiate " + componentTypeV1.ToString());

                var ns1 = Omi.Component.NamespaceOpenMIv1;

                var xArguments = xLinkableComponentV1
                    .Element(ns1.GetName("Arguments"));

                var args1 = Arguments1(xArguments, ns1, accessor.Uri);

                component1.Initialize(args1.ToArray());

                switch (v1Type)
                {
                    case Component1Type.OpenMIGeneric:
                        return new LinkableComponentOpenMIV1Wrapper(component1, componentTypeV1.DocumentAccessor, args1.ToList());
                    case Component1Type.FluidEarthEngine5:
                        return new LinkableComponentFluidEarthV1Wrapper(component1, componentTypeV1.DocumentAccessor, args1.ToList());
                    default:
                        throw new NotImplementedException(v1Type.ToString());
                }
            }

            public static List<Argument1> Arguments1(XElement xArguments, XNamespace ns)
			{
				return Arguments1(xArguments, ns, null);
			}
#endif
            /// <summary>
			/// Get Arguments, assembly and type from an OMI XML
			/// </summary>
			/// <param name="omiXml">OMI XML</param>
			/// <param name="relativeTo">OMI file Path</param>
			/// <param name="assembly">ILinkableComponent Assembly</param>
			/// <param name="type">ILinkableComponent type</param>
			/// <returns>ILinkableComponent arguments</returns>
			public static List<Argument1> Arguments1(XElement xArguments, XNamespace ns, Uri relativeTo)
			{
				List<Argument1> args = new List<Argument1>();

				string key, value, description;
				bool readOnly;

				foreach (XElement xArg in xArguments.Elements(ns.GetName("Argument")))
				{
					key = Utilities.Xml.GetAttribute(xArg, "Key");
					value = Utilities.Xml.GetAttribute(xArg, "Value");
					value = ResolveArgumentValueV1(relativeTo, key, value);
					readOnly = Utilities.Xml.GetAttribute(xArg, "ReadOnly", false); 
					description = xArg.Value;

					args.Add(new Argument1(
						key,
						value,
						readOnly,
						description));
				}

				return args;
			}

			/// <summary>
			/// Resolve relative path in OMI arguments, FluidEarth version 1
			/// </summary>
			/// <param name="relativeTo">Path arguments are relative too</param>
			/// <param name="key">Argument key</param>
			/// <param name="value">argument value</param>
			/// <returns>resolved value</returns>
			public static string ResolveArgumentValueV1(Uri relativeTo, string key, string value)
			{
				if (!key.Contains(".ArgFile.") && !key.Contains(".ArgPath."))
					return value;

				if (Path.IsPathRooted(value) || relativeTo == null)
					return value;

				return new Uri(relativeTo, value).LocalPath;
			}

            #if DEPRECATED
			public static List<OpenMI.Standard2.IArgument> ToArgument2(Argument1 arg1)
			{
				string key = arg1.Key;
				string value = arg1.Value;
				string description = arg1.Description;
				bool readOnly = arg1.ReadOnly;

				string[] parts = key.Split('.');

				var args = new List<OpenMI.Standard2.IArgument>();

				bool sdk1Arg = parts[0] == "FluidEarth_SDK" || parts[0] == "OpenWEB_SDK";

				if (parts.Length == 3 && sdk1Arg)
				{
					Identity identity = new Identity(key, parts[2], description);

					switch (parts[1])
					{
						case "ArgBool":
							args.Add(new Argument<bool>(identity, 
								System.Convert.ToBoolean(value), false, readOnly));
							break;
						case "ArgInt":
							args.Add(new Argument<int>(identity, 
								System.Convert.ToInt32(value), false, readOnly));
							break;
						case "ArgDouble":
						case "ArgJulian": 
							args.Add(new Argument<double>(identity,
								System.Convert.ToDouble(value), false, readOnly));
							break;
						case "ArgResource":
						case "ArgFile":
                            var file = new ArgumentFile(identity);
                            file.IsReadOnly = readOnly;
                            file.ValueAsString = value;
                            args.Add(file);
							break;
						case "ArgPath":
                            var folder = new ArgumentFolder(identity);
                            folder.IsReadOnly = readOnly;
                            folder.ValueAsString = value;
                            args.Add(folder);
							break;
						case "ArgEnum":
							if (parts[2] == "LoggerSeverity")
							{
								switch (value)
								{
									case "Error":
									case "Warning":
										break;
									case "Event":
									case "Status":
										args.Add(new Argument<bool>(new Identity(
											"FluidEarth2.Sdk.BaseComponent.TraceStatus",
											"TraceStatus", string.Empty),
											true, false, readOnly));
										break;
									case "Progress":
									case "Debug1":
									case "Debug2":
										args.Add(new Argument<bool>(new Identity(
											"FluidEarth2.Sdk.BaseComponent.TraceStatus",
											"TraceStatus", string.Empty),
											true, false, readOnly));
										args.Add(new Argument<bool>(new Identity(
											"FluidEarth2.Sdk.BaseComponent.TraceConvertors",
											"TraceConvertors", string.Empty),
											true, false, readOnly));
										break;
									default:
										throw new NotImplementedException();
								}
							}
							break;
						default:
							args.Add(new Argument<string>(new Identity(key, key, description),
								value, false, readOnly));
							break;
					}
				}
				else if (parts.Length == 4 && sdk1Arg && parts[1] == "Remoting")
				{
					switch (parts[3])
					{
						case "Protocol":
							switch (value)
							{
								case "HTTP":
									args.Add(new Argument<RemotingProtocol>(new Identity(
										"FluidEarth2.Sdk.RemoteData.Protocol",
										"RemoteData.Protocol", string.Empty),
										RemotingProtocol.http, false, readOnly));
									break;
								case "TCP":
									args.Add(new Argument<RemotingProtocol>(new Identity(
										"FluidEarth2.Sdk.RemoteData.Protocol",
										"RemoteData.Protocol", string.Empty),
										RemotingProtocol.tcp, false, readOnly));
									break;
								case "IPC":
									args.Add(new Argument<RemotingProtocol>(new Identity(
										"FluidEarth2.Sdk.RemoteData.Protocol",
										"RemoteData.Protocol", string.Empty),
										RemotingProtocol.ipc, false, readOnly));
									break;
								default:
									throw new NotImplementedException();
							}
							break;
						case "Host":
							args.Add(new Argument<string>(new Identity(
								"FluidEarth2.Sdk.RemoteData.Host",
								"RemoteData.Host", string.Empty),
								value, false, readOnly));
							break;
						case "Port":
							args.Add(new Argument<int>(new Identity(
								"FluidEarth2.Sdk.RemoteData.Port",
								"RemoteData.Port", string.Empty),
								System.Convert.ToInt32(value), false, readOnly));
							break;
						case "UniqueId":
							args.Add(new Argument<string>(new Identity(
								key, key, description),
								value, false, readOnly));
							break;
						case "Uri":
							args.Add(new Argument<Uri>(new Identity(
								"FluidEarth2.Sdk.RemoteData.Uri",
								"RemoteData.Uri", string.Empty),
								new Uri(value), false, readOnly));
							break;
						case "ServerProcessCreate":
							args.Add(new Argument<RemotingProtocol>(new Identity(
								"FluidEarth2.Sdk.RemoteData.Protocol",
								"RemoteData.Protocol", string.Empty),
								RemotingProtocol.ipcAuto, false, readOnly));
							break;
						case "ServerConfigFile":
							args.Add(new Argument<string>(new Identity(
								key, key, description),
								value, false, readOnly));
							break;
						case "ConnectionSleep":
							args.Add(new Argument<int>(new Identity(
								"FluidEarth2.Sdk.RemoteData.ConnectionSleep",
								"RemoteData.ConnectionSleep", string.Empty),
								System.Convert.ToInt32(value), false, readOnly));
							break;
						case "TimeOut":
							args.Add(new Argument<uint>(new Identity(
								"FluidEarth2.Sdk.RemoteData.ConnectionTimeOut",
								"RemoteData.ConnectionTimeOut", string.Empty),
								System.Convert.ToUInt32(value), false, readOnly));
							break;
						default:
							args.Add(new Argument<string>(new Identity(
								key, key, description),
								value, false, readOnly));
							break;
					}
				}
				else
					args.Add(new Argument<string>(new Identity(
						key, key, description),
						value, false, readOnly));

				return args;
			}
#endif

			/// <summary>
			/// Argument is a class that contains (key,value) pairs.
			/// <para>This is a trivial implementation of OpenMI.Standard.IArgument, refer there for further details.</para>
			/// </summary>
			[Serializable]
			public class Argument1 : IArgument1
			{
				private string _key="";
				private string _value="";
				private bool _readOnly = false;
				private string _description="";

				/// <summary>
				/// Empty constructor
				/// </summary>
				public Argument1()
				{
				}

				/// <summary>
				/// Copy constructor
				/// </summary>
				/// <param name="source">Source argument to copy</param>
				public Argument1(IArgument1 source)
				{
					Key = source.Key;
					Value = source.Value;
					ReadOnly = source.ReadOnly;
					Description = source.Description;
				}

				/// <summary>
				/// Constructor
				/// </summary>
				/// <param name="key">Key</param>
				/// <param name="Value">Value</param>
				/// <param name="ReadOnly">Is argument read-only?</param>
				/// <param name="Description">Description</param>
				public Argument1(string key,string Value,bool ReadOnly,string Description)
				{
					_key = key;
					_value = Value;
					_readOnly = ReadOnly;
					_description = Description;
				}

				public Argument1(OpenMI.Standard2.IArgument arg2)
				{
					Key = arg2.Caption;
					Value = arg2.ValueAsString;
					ReadOnly = arg2.IsReadOnly;
					Description = arg2.Description;
				}

				#region IArgument Members

				///<summary>
				/// TODO: comment
				///</summary>
				public string Key
				{
					get
					{
						return _key;
					}
					set 
					{
						_key = value;
					}
				}

				///<summary>
				/// TODO: comment
				///</summary>
				public string Value
				{
					get
					{
						return _value;
					}
					set
					{
						_value = value;
					}
				}

				///<summary>
				/// TODO: comment
				///</summary>
				public bool ReadOnly
				{
					get
					{
						return _readOnly;
					}
					set
					{
						_readOnly = value;
					}
				}

				///<summary>
				/// TODO: comment
				///</summary>
				public string Description
				{
					get
					{
						return _description;
					}
					set
					{
						_description = value;
					}
				}

				#endregion

				///<summary>
				/// Check if the current instance equals another instance of this class.
				///</summary>
				///<param name="obj">The instance to compare the current instance with.</param>
				///<returns><code>true</code> if the instances are the same instance or have the same content.</returns>
				public override bool Equals(object obj) 
				{
					if (obj == null || GetType() != obj.GetType()) 
						return false;
					Argument1 d = (Argument1)obj;
					return (Value.Equals(d.Value)&&Key.Equals(d.Key));
				}

				///<summary>
				/// Get Hash Code.
				///</summary>
				///<returns>Hash Code for the current instance.</returns>
				public override int GetHashCode()
				{
					int hashCode = base.GetHashCode();
					if (_key != null) hashCode += _key.GetHashCode();
					if (_value != null) hashCode += _value.GetHashCode();
					return hashCode;
				}
			}

			public class XElementSet
			{
				public readonly string Id;
				public readonly string Index;
				public readonly string ElementType;

				public XElementSet(XElement xElement)
				{
					Id = Utilities.Xml.GetAttribute(xElement, "id").Trim();
					Index = Utilities.Xml.GetAttribute(xElement, "index").Trim();
					ElementType = Utilities.Xml.GetAttribute(xElement, "element_type").Trim();
				}
			}

			public class XQuantity
			{
				public readonly string Id;
				public readonly string Index;
				public readonly string ValueType;

				public XQuantity(XElement xElement)
				{
					Id = Utilities.Xml.GetAttribute(xElement, "id").Trim();
					Index = Utilities.Xml.GetAttribute(xElement, "index").Trim();
					ValueType = Utilities.Xml.GetAttribute(xElement, "value_type").Trim();
				}
			}

			public class ExchangeItemV1ModelXml
			{
				public readonly string Index;
				public readonly string QuantityIndex;
				public readonly string ElementSetIndex;
				public readonly Dictionary<string, string> UserVariables;
				public XQuantity Quantity = null;
				public XElementSet ElementSet = null;

				public ExchangeItemV1ModelXml(XElement xElement)
				{
					Index = Utilities.Xml.GetAttribute(xElement, "index").Trim();
					QuantityIndex = Utilities.Xml.GetAttribute(xElement, "quantity_index").Trim();
					ElementSetIndex = Utilities.Xml.GetAttribute(xElement, "element_set_index").Trim();
					UserVariables = xElement
						.Elements("user")
						.ToDictionary(
							k => Utilities.Xml.GetAttribute(k, "key").Trim(),
							v => Utilities.Xml.GetAttribute(v, "value").Trim());
				}
			}

            public static Dictionary<string, Tuple<double, double>> ReadModelFileCoords(XElement xModel)
            {
                return xModel
                        .Elements("coord")
                        .Select(x => Tuple.Create(x.Attribute("index").Value,
                            Tuple.Create(double.Parse(x.Attribute("x").Value), double.Parse(x.Attribute("y").Value))))
                        .ToDictionary(t => t.Item1, t => t.Item2);
            }

			public static void ReadModelFile(XElement xModel, 
				out Dictionary<string, ExchangeItemV1ModelXml> inputs, 
				out Dictionary<string, ExchangeItemV1ModelXml> outputs)
			{
				Dictionary<string, XElementSet> elementSets = xModel
						.Elements("element_set")
						.Select(x => new XElementSet(x))
						.ToDictionary(k => k.Index, v => v);

				Dictionary<string, XQuantity> quantities = xModel
						.Elements("quantity")
						.Select(x => new XQuantity(x))
						.ToDictionary(k => k.Index, v => v);

				inputs = xModel
						.Elements("input_exchange_item")
						.Select(x => new ExchangeItemV1ModelXml(x))
						.ToDictionary(k => k.Index, v => v);

				outputs = xModel
						.Elements("output_exchange_item")
						.Select(x => new ExchangeItemV1ModelXml(x))
						.ToDictionary(k => k.Index, v => v);

				foreach (ExchangeItemV1ModelXml e in inputs.Values)
				{
					e.Quantity = quantities[e.QuantityIndex];
					e.ElementSet = elementSets[e.ElementSetIndex];
				}

				foreach (ExchangeItemV1ModelXml e in outputs.Values)
				{
					e.Quantity = quantities[e.QuantityIndex];
					e.ElementSet = elementSets[e.ElementSetIndex];
				}
			}

			public class InternalLink : ILink
			{
				string _id = Guid.NewGuid().ToString();

				// Implementation assumes that active version 1 component does not
				// interrogate other (version2) component.

				ILinkableComponent1 _component1Source;
				ILinkableComponent1 _component1Target;

				// This implementation assumes IQuantity and IelementSet are not
				// modified during execution, if so cloning might be required for
				// dummy versions, however this is obscure, even for case of version
				// on elementSet
				IQuantity1 _quantity1;
				IElementSet1 _elementSet1;

				public InternalLink(ILinkableComponent1 component1Target, IInputExchangeItem target1, ITimeSpan timeHorizon, IValueSetConverterTime convertor, bool isVector)
				{
					_component1Source = new DummyComponent1Source(timeHorizon, convertor, isVector);
					_component1Target = component1Target;
					_quantity1 = target1.Quantity;
					_elementSet1 = target1.ElementSet;
				}

				public InternalLink(ILinkableComponent1 component1Source, IOutputExchangeItem source1, ITimeSpan timeHorizon)
				{
					_component1Source = component1Source;
					_component1Target = new DummyComponent1Target(timeHorizon);
					_quantity1 = source1.Quantity;
					_elementSet1 = source1.ElementSet;
				}

				public int DataOperationsCount
				{
					get { return 0; }
				}

				public string Description
				{
					get { return "Dummy link between version 1 and 2 components"; }
				}

				public IDataOperation GetDataOperation(int dataOperationIndex)
				{
					throw new NotImplementedException();
				}

				public string ID
				{
					get { return _id; }
				}

				public ILinkableComponent1 SourceComponent
				{
					get { return _component1Source; }
				}

				public IElementSet1 SourceElementSet
				{
					get { return _elementSet1; }
				}

				public IQuantity1 SourceQuantity
				{
					get { return _quantity1; }
				}

				public ILinkableComponent1 TargetComponent
				{
					get { return _component1Target; }
				}

				public IElementSet1 TargetElementSet
				{
					get { return _elementSet1; }
				}

				public IQuantity1 TargetQuantity
				{
					get { return _quantity1; }
				}
			}

			public class TimeStamp : ITimeStamp
			{
				double _mjd;

				public TimeStamp(double mjd)
				{
					_mjd = mjd;
				}

				public double ModifiedJulianDay
				{
					get { return _mjd; }
				}
			}

			public class TimeSpan : ITimeSpan
			{
				ITimeStamp _start;
				ITimeStamp _end;

				public TimeSpan(ITimeStamp start, ITimeStamp end)
				{
					_start = new TimeStamp(start.ModifiedJulianDay);
					_end = new TimeStamp(end.ModifiedJulianDay);
				}

				public TimeSpan(ITime2 timeHorizon)
				{
					_start = new TimeStamp(timeHorizon.StampAsModifiedJulianDay);
					_end = new TimeStamp(timeHorizon.StampAsModifiedJulianDay + timeHorizon.DurationInDays);
				}

				public ITimeStamp End
				{
					get { return _start; }
				}

				public ITimeStamp Start
				{
					get { return _end; }
				}
			}

			public static Time ToTime2(ITime1 time1)
			{
				if (time1 is ITimeStamp)
					return new Time(((ITimeStamp)time1).ModifiedJulianDay);
				
				if (time1 is ITimeSpan)
					return new Time(((ITimeSpan)time1).Start.ModifiedJulianDay, ((ITimeSpan)time1).End.ModifiedJulianDay);

				throw new NotImplementedException();
			}

			public static ITime1 ToTime1(ITime2 time2)
			{
				if (time2.DurationInDays == 0)
					return new TimeStamp(time2.StampAsModifiedJulianDay);

				return new TimeSpan(
					new TimeStamp(time2.StampAsModifiedJulianDay),
					new TimeStamp(time2.StampAsModifiedJulianDay + time2.DurationInDays));
			}

            public static string ToString(IValueSet valueSet1)
			{
				IScalarSet scalarSet = valueSet1 as IScalarSet;

				if (scalarSet == null)
					return "Method ToString(IVectorSet) not implemented yet";
				
				if (scalarSet.Count == 0)
					return "Empty";

				StringBuilder sb = new StringBuilder(
					scalarSet.GetScalar(0).ToString());

				for (int n = 1; n < scalarSet.Count; ++n)
					sb.AppendFormat(",{0}", scalarSet.GetScalar(n).ToString());

				return sb.ToString();
			}

            abstract class ValueSet : IValueSet
			{
				bool _scalar;
				protected List<double> _values;

				public ValueSet(IBaseValueSet valueSet2, bool scalar)
				{
					if (typeof(double) != valueSet2.ValueType)
						throw new Exception(string.Format("{0} != {1}",
							typeof(double).ToString(), valueSet2.ValueType.ToString()));

					_scalar = scalar;
					_values = Utilities.ToList<double>(valueSet2);
				}

				public int Count
				{
					get { return _scalar ? _values.Count : _values.Count / 3; }
				}

				public bool IsValid(int elementIndex)
				{
					return elementIndex > -1 && elementIndex < Count;
				}
			}

			class ScalarSet : ValueSet, IScalarSet
			{
				public ScalarSet(IBaseValueSet valueSet2)
					: base(valueSet2, true)
				{
				}

				public double GetScalar(int elementIndex)
				{
					if (!IsValid(elementIndex))
						throw new IndexOutOfRangeException(string.Format("{0} >= {1}",
							elementIndex.ToString(), Count.ToString()));

					return _values[elementIndex];
				}
			}

			class VectorSet : ValueSet, IVectorSet
			{
				public VectorSet(IBaseValueSet valueSet2)
					: base(valueSet2, false)
				{
				}

				public OpenMI.Standard.IVector GetVector(int elementIndex)
				{
					if (!IsValid(elementIndex))
						throw new IndexOutOfRangeException(string.Format("{0} >= {1}",
							elementIndex.ToString(), Count.ToString()));

					return new Vector(3 * elementIndex, _values);
				}

				class Vector : OpenMI.Standard.IVector
				{
					double _x, _y, _z;

					public Vector(int offset, List<double> _values)
					{
						_x = _values[offset];
						_y = _values[offset + 1];
						_z = _values[offset + 2];
					}

					public double XComponent
					{
						get { return _x; }
					}

					public double YComponent
					{
						get { return _y; }
					}

					public double ZComponent
					{
						get { return _z; }
					}
				}
			}

			internal class DummyComponent1Base : ILinkableComponent1
			{
				string _componentId = Guid.NewGuid().ToString();
				string _modelId = Guid.NewGuid().ToString();
				ITimeSpan _timeHorizon;
				ITimeStamp _earliestInputTime;

				public DummyComponent1Base(ITimeSpan timeHorizon)
				{
					_timeHorizon = timeHorizon;
					_earliestInputTime = _timeHorizon.Start;
				}

				public void AddLink(ILink link)
				{
					throw new NotImplementedException();
				}

				public string ComponentDescription
				{
					get { return "Dummy component for internal links between openMI.Startdard1 and OpenMI.Standard2 components"; }
				}

				public string ComponentID
				{
					get { return _componentId; }
				}

				public string ModelDescription
				{
					get { return "Dummy component for internal links between openMI.Startdard1 and OpenMI.Standard2 components"; }
				}

				public string ModelID
				{
					get { return _modelId; }
				}

				public ITimeSpan TimeHorizon
				{
					get { return _timeHorizon; }
				}

				public ITimeStamp EarliestInputTime
				{
					get { return _earliestInputTime; }
					set { _earliestInputTime = value; }
				}

				public void Dispose()
				{
				}

				public void Finish()
				{
				}

				public IInputExchangeItem GetInputExchangeItem(int inputExchangeItemIndex)
				{
					throw new NotImplementedException();
				}

				public IOutputExchangeItem GetOutputExchangeItem(int outputExchangeItemIndex)
				{
					throw new NotImplementedException();
				}

				public virtual IValueSet GetValues(ITime1 time, string linkID)
				{
					throw new NotImplementedException();
				}

				public void Initialize(IArgument1[] properties)
				{
				}

				public int InputExchangeItemCount
				{
					get { throw new NotImplementedException(); }
				}

				public int OutputExchangeItemCount
				{
					get { throw new NotImplementedException(); }
				}

				public void Prepare()
				{
				}

				public void RemoveLink(string linkID)
				{
					throw new NotImplementedException();
				}

				public string Validate()
				{
					return string.Empty;
				}

				public EventType GetPublishedEventType(int providedEventTypeIndex)
				{
					throw new NotImplementedException();
				}

				public int GetPublishedEventTypeCount()
				{
					throw new NotImplementedException();
				}

				public void SendEvent(IEvent Event)
				{
					throw new NotImplementedException();
				}

				public void Subscribe(IListener listener, EventType eventType)
				{
					throw new NotImplementedException();
				}

				public void UnSubscribe(IListener listener, EventType eventType)
				{
					throw new NotImplementedException();
				}
			}

			internal class DummyComponent1Target : DummyComponent1Base
			{
				public DummyComponent1Target(ITimeSpan timeHorizon)
					: base(timeHorizon)
				{
				}
			}

			internal class DummyComponent1Source : DummyComponent1Base
			{
				IValueSetConverterTime _convertor;
                bool _isVector;

                public DummyComponent1Source(ITimeSpan timeHorizon, IValueSetConverterTime convertor, bool isVector)
					: base(timeHorizon)
				{
					_convertor = convertor;
                    _isVector = isVector;
				}

				public override IValueSet GetValues(ITime1 time, string linkID)
				{
					var time2 = Utilities.Standard1.ToTime2(time);

					var valueSet2 = _convertor.GetValueSetAt(time2);

                    return _isVector
                        ? new VectorSet(valueSet2) as IValueSet
                        : new ScalarSet(valueSet2);
				}
			}
		}
	}
}
