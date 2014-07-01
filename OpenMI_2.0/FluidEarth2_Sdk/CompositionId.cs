using System;
using System.Text;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using System.Diagnostics;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <inheritdoc />
    public abstract class IdentifiableItemBase : IIdentifiableItem
    {
        public abstract IIdentifiable Construct(XElement xConstructor);
        public abstract bool Connect(XElement xConnectivity);

        protected string _guid;

        string _cachedInstanceId;
        IDescribable _cachedInstanceDescribes;
        bool _cachedCaptionEdited;
        bool _cachedDescriptionEdited;

        protected IComposition _composition;
        IdentifiableItemType _interface = IdentifiableItemType.Unknown;
        protected IIdentifiable _itemInstance;
        protected XElement _xConstructor, _xConnectivity;
        SupportedPlatforms _platforms;

        public IdentifiableItemBase()
        {
            _guid = Guid.NewGuid().ToString();
        }

        public IdentifiableItemBase(IIdentifiable itemInstance, IComposition composition)
        {
            Contract.Requires(itemInstance != null, "itemInstance != null");
            Contract.Requires(composition != null, "composition != null");

            _composition = composition;

            _guid = Guid.NewGuid().ToString();

            _cachedInstanceId = itemInstance.Id;
            _cachedInstanceDescribes = new Describes(itemInstance);

            _cachedCaptionEdited = false;
            _cachedDescriptionEdited = false;

            _itemInstance = itemInstance;

            _xConstructor = Constructor(itemInstance);
            _xConnectivity = Connectivity(itemInstance);

            if (itemInstance is IBaseLinkableComponentProposed)
                _platforms = ((IBaseLinkableComponentProposed)itemInstance).SupportedPlatforms;
            else if (itemInstance is IBaseExchangeItemProposed)
                _platforms = ((IBaseExchangeItemProposed)itemInstance).SupportedPlatforms;
            else
                _platforms = composition.Platforms;

            if (itemInstance is IBaseLinkableComponent)
                _interface = IdentifiableItemType.IBaseLinkableComponent;
            else if (itemInstance is IBaseAdaptedOutput)
                _interface = IdentifiableItemType.IBaseAdaptedOutput;
            else if (itemInstance is IBaseOutput)
                _interface = IdentifiableItemType.IBaseOutput;
            else if (itemInstance is IBaseInput)
                _interface = IdentifiableItemType.IBaseInput;
            else if (itemInstance is IAdaptedOutputFactory)
                _interface = IdentifiableItemType.IAdaptedOutputFactory;
            else
                _interface = IdentifiableItemType.Unknown;
        }

        #region Persistence

        public const string XName = "IdentifiableItem";
        public const string XItemIdentity = "ItemIdentity";
        public const string XConstructor = "Constructor";
        public const string XConnectivity = "Connectivity";
        public const string XPlatforms = "Platforms";

        public virtual void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);

            _guid = Utilities.Xml.GetAttribute(xElement, "id");
            _cachedInstanceId = Utilities.Xml.GetAttribute(xElement, "instanceId");

            var interfaceType = Utilities.Xml.GetAttribute(xElement, "interface");

            _cachedCaptionEdited = Utilities.Xml.GetAttribute(xElement, "captionEdited", false);
            _cachedDescriptionEdited = Utilities.Xml.GetAttribute(xElement, "descriptionEdited", false);

            var caption = Utilities.Xml.GetAttribute(xElement, "caption");

            var description = _cachedDescriptionEdited
                ? xElement.Element("Description").Value
                : "Description unavailable until ItemInstance(composition) accessed";

            _cachedInstanceDescribes = new Describes(caption, description);
            _cachedCaptionEdited = true;

            _xConstructor = Persistence.ThisOrSingleChild(XConstructor, xElement);
            _xConnectivity = Persistence.ThisOrSingleChild(XConnectivity, xElement);

            _platforms = Persistence.PlatformsChi.Parse(xElement, accessor);

            _interface = (IdentifiableItemType)Enum.Parse(typeof(IdentifiableItemType), interfaceType);
        }

        public virtual XElement Persist(IDocumentAccessor accessor)
        {
            if (_itemInstance != null)
            {
                _xConstructor = Constructor(_itemInstance);
                _xConnectivity = Connectivity(_itemInstance);
            }

            Contract.Requires(_xConstructor != null, "_xConstructor != null");
            Contract.Requires(_xConnectivity != null, "_xConnectivity != null");

            var xml = new XElement(XName,
                new XAttribute("caption", _cachedInstanceDescribes.Caption), 
                new XAttribute("instanceId", _cachedInstanceId),
                new XAttribute("interface", _interface.ToString()), 
                new XAttribute("id", _guid),
                new XAttribute("captionEdited", _cachedCaptionEdited.ToString()),
                new XAttribute("descriptionEdited", _cachedDescriptionEdited.ToString()),
                Persistence.PlatformsChi.Persist(_platforms, accessor),
                _xConstructor,
                _xConnectivity);

            if (_cachedDescriptionEdited)
                xml.Add(new XElement("Description", _cachedInstanceDescribes.Description));

            return xml;
        }

        #endregion

        public virtual XElement Constructor(IIdentifiable itemInstance)
        {
            Contract.Requires(itemInstance != null, "itemInstance != null");
            Contract.Requires(_composition != null, "composition != null");

            return new XElement(XConstructor);
        }

        public virtual XElement Connectivity(IIdentifiable itemInstance)
        {
            Contract.Requires(itemInstance != null, "itemInstance != null");
            Contract.Requires(_composition != null, "composition != null");

            return new XElement(XConnectivity);
        }

        public IComposition Composition
        {
            get { return _composition; }
        }

        public bool IsInstantiated
        {
            get { return _itemInstance != null; }
        }

        public string UniqueId 
        {
            get { return _guid; }
        }

        public string InstanceId
        {
            get { return _cachedInstanceId; }
        }

        public string Caption
        {
            get 
            {
                if (IsInstantiated)
                    _cachedInstanceDescribes.Caption = _itemInstance.Caption;

                return _cachedInstanceDescribes.Caption; 
            }

            set
            {
                _cachedInstanceDescribes.Caption = value;

                if (IsInstantiated)
                {
                    _itemInstance.Caption = _cachedInstanceDescribes.Caption;
                    _cachedCaptionEdited = false;
                }
                else
                    _cachedCaptionEdited = true;
            }
        }

        public string Description
        {
            get 
            {
                if (IsInstantiated)
                    _cachedInstanceDescribes.Description = _itemInstance.Description;

                return _cachedInstanceDescribes.Description; 
            }

            set
            {
                _cachedInstanceDescribes.Description = value;

                if (IsInstantiated)
                {
                    _itemInstance.Description = _cachedInstanceDescribes.Description;
                    _cachedDescriptionEdited = false;
                }
                else
                    _cachedDescriptionEdited = true;
            }
        }

        public SupportedPlatforms Platforms
        {
            get { return _platforms; }
            set { _platforms = value; }
        }

        public IdentifiableItemType Interface
        {
            get { return _interface; }
        }

        public virtual IIdentifiable ItemInstance
        {
            get
            {
                if (_itemInstance == null)
                {
                    try
                    {
                        _itemInstance = Construct(_xConstructor);
                    }
                    catch (System.Exception e)
                    {
                        _itemInstance = null;

                        Trace.TraceError(string.Format(
                            "\"{0}\" Failed to create instance using compositionId.Construct()", Caption));
                        Trace.TraceError(e.Message);

                        while (e.InnerException != null)
                        {
                            e = e.InnerException;
                            Trace.TraceError("  " + e.Message);
                        }

                        throw;
                    }

                    if (_itemInstance != null)
                    {
                        var connected = Connect(_xConnectivity);

                        Contract.Requires(connected, "connected {0}.{1}",
                            _itemInstance.Id, _itemInstance.Caption);
                    }
                }

                if (_itemInstance != null)
                {
                    if (_cachedCaptionEdited)
                    {
                        _itemInstance.Caption = _cachedInstanceDescribes.Caption;
                        _cachedCaptionEdited = false;
                    }

                    if (_cachedDescriptionEdited)
                    {
                        _itemInstance.Description = _cachedInstanceDescribes.Description;
                        _cachedDescriptionEdited = false;
                    }
                }

                return _itemInstance;
            }
        }

        public virtual void AddDetailsAsWikiText(StringBuilder sb)
        {
            sb.AppendLine("= Identifiable Type");
            sb.AppendLine(string.Format("* Unique Id: \"{0}\"", _guid));
            sb.AppendLine(string.Format("* Interface: \"{0}\"", _interface));
            sb.AppendLine("== Identity ");
            sb.AppendLine(string.Format("* Caption: \"{0}\"", _cachedInstanceDescribes.Caption));
            sb.AppendLine(string.Format("* Id: \"{0}\"", _cachedInstanceId));
            sb.AppendLine(string.Format("=== Description"));

            if (Sdk.Utilities.IsProbablyWikiText(_cachedInstanceDescribes.Description))
                sb.AppendLine(_cachedInstanceDescribes.Description);
            else
            {
                sb.AppendLine("{{{");
                sb.AppendLine(_cachedInstanceDescribes.Description);
                sb.AppendLine("}}}");
            }

            sb.AppendLine("== Instance");

            if (_itemInstance != null)
            {
                var externalType = new ExternalType(_itemInstance.GetType());

                sb.AppendLine(string.Format("* Type: \"{0}\"", externalType.TypeName));
                sb.AppendLine(string.Format("* Assembly: \"{0}\"", externalType.AssemblyName));
                sb.AppendLine(string.Format("* Url: \"{0}\"", externalType.Url.AbsoluteUri));
            }

            if (_xConstructor != null)
            {
                sb.AppendLine("=== XML Persistence");
                sb.AppendLine("{{{");
                sb.AppendLine(_xConstructor.ToString());
                sb.AppendLine("}}}");
            }
        }
    }
}
