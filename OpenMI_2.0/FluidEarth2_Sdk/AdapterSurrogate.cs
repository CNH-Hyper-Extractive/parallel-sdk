using System;
using System.Collections.Generic;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    public class AdapterSurrogate
        : AdaptedOutputTimeBase<string, string> // Dosnt matter what types, always invalid
    {
        public AdapterSurrogate()
        { }

        public AdapterSurrogate(IBaseOutput adaptee, IBaseInput target)
            : base(IdentityStatic(), adaptee, target, null, null)
        { }

        public static Identity IdentityStatic()
        {
            return new Identity(new Describes(
                "?",
                "Surrogate Adapter, i.e. must be replaced with a valid Adapter before runtime.")); 
        }

        public new const string XName = "AdapterSurrogate";

        public override void Initialise(XElement xElement, IDocumentAccessor accessor)
        {
            xElement = Persistence.ThisOrSingleChild(XName, xElement);
        }

        public override XElement Persist(IDocumentAccessor accessor)
        {
            return new XElement(XName, base.Persist(accessor));
        }

        public override bool IsValid(out string whyNot)
        {
            whyNot = "Is a surrogate adapter, always invalid, user must replace with valid adaper before runtime.";
            return false;
        }

        public override bool CanConnect(IBaseExchangeItem proposed, out string whyNot)
        {
            whyNot = "Can connect to anything, but cannot be used at runtime.";
            return true;
        }

        protected override void PrepareIt()
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }
        public override IEnumerable<TimeRecord<string>> AdaptRecords(List<TimeRecord<string>> toAdapt, IBaseExchangeItem querySpecifier)
        {
            throw new NotImplementedException("Not meant to be used at runtime, composition building tool only");
        }
    }
}
