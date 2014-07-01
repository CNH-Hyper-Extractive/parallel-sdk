using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Treat as immutable, i.e. cannot copy: create new and Detach() this
    /// </summary>
    public class Linkage : Identity, ILinkage
    {
        IBaseOutput _outputFirst;
        IBaseInput _input;
        List<IBaseExchangeItem> _chain 
            = new List<IBaseExchangeItem>();

        public Linkage(IBaseInput input)
        {
            Contract.Requires(input != null, "input != null");
            Contract.Requires(input.Provider != null, "input.Provider != null");

            _input = input;

            _chain.Add(input);

            var output = input.Provider;

            while (output is IBaseAdaptedOutput)
            {
                _chain.Add(output);
                output = ((IBaseAdaptedOutput)output).Adaptee;
                Contract.Requires(output != null, "((IBaseAdaptedOutput)next).Adaptee != null");
            }

            Contract.Requires(output is IBaseOutput, "next is IBaseOutput");
            Contract.Requires(object.ReferenceEquals(output, output),
                "Provided output is at end of provided input linkage chain");

            _chain.Add(output);

            _outputFirst = output;

            _chain.Reverse();
        }

        /// <summary>
        /// In order IBaseOutput, IBaseAdaptedOutput, ..., IBaseInput
        /// </summary>
        /// <returns></returns>
        public List<IBaseExchangeItem> Chainage
        {
            get { return _chain; }
        }

        public IBaseLinkableComponent Source { get { return _outputFirst.Component; } }
        public IBaseLinkableComponent Target { get { return _input.Component; } }
        public IBaseOutput OutputFirst { get { return _outputFirst; } }
        public IBaseInput Input { get { return _input; } }

        public new string Caption
        {
            set { base.Caption = value; }
            get
            {
                return string.Format("'{0}'.'{1}' -> '{2}'.'{3}'",
                    Source != null ? Source.Caption : "?",
                    _outputFirst != null ? _outputFirst.Caption : "?",
                    Target != null ? Target.Caption : "?",
                    _input != null ? _input.Caption : "?");
            }
        }

        /// <summary>
        /// In order Target ... Source
        /// </summary>
        public IEnumerable<IBaseAdaptedOutput> Adapters
        {
            get 
            { 
                return _chain
                    .GetRange(1, _chain.Count - 2)
                    .Cast<IBaseAdaptedOutput>()
                    .Reverse();  
            }
        }

        public IBaseOutput AdapteeLastValid
        {
            get 
            {
                if (_chain.Count == 2)
                    return _outputFirst;

                var adaptee = OutputLast;

                if (adaptee.Id == AdapterSurrogate.IdentityStatic().Id)
                    return ((IBaseAdaptedOutput)adaptee).Adaptee;

                return adaptee;
            }
        }

        public IBaseOutput OutputLast
        {
            get
            {
                if (_chain.Count == 2)
                    return _outputFirst;

                return Adapters.First();
            }
        }

        public void RemoveOrphanedAdapters(IComposition composition)
        {
            Contract.Requires(composition != null, "composition != null");

            var output = OutputLast;
            IBaseAdaptedOutput orphanedAdapter;

            while (output != null)
            {
                orphanedAdapter = FindOrphanedAdapter(output);

                while (orphanedAdapter != null)
                {
                    orphanedAdapter.Adaptee.RemoveAdaptedOutput(orphanedAdapter);
                    orphanedAdapter = FindOrphanedAdapter(output);

                    composition.Remove(composition.GetItem(orphanedAdapter));                  
                }

                output = output is IBaseAdaptedOutput
                    ? ((IBaseAdaptedOutput)output).Adaptee
                    : null;
            }
        }

        IBaseAdaptedOutput FindOrphanedAdapter(IBaseOutput output)
        {
            return output
                .AdaptedOutputs
                .Where(a => a.AdaptedOutputs.Count == 0 && a.Consumers.Count == 0)
                .FirstOrDefault();
        }

        /// <summary>
        // Remove connection with composition input/output
        /// </summary>
        public void Detach(IComposition composition)
        {
            if (_input == null || _input.Provider == null)
                return;

            _input.Provider.RemoveConsumer(_input);

            RemoveOrphanedAdapters(composition);
        }

        public override string ToString()
        {
            return Caption;
        }

        public bool IsValid(out string whyNot)
        {
            foreach (var item in _chain)
                if (item is IBaseExchangeItemProposed
                    && !((IBaseExchangeItemProposed)item).IsValid(out whyNot))
                    return false;

            whyNot = string.Empty;
            return true;
        }

        public string DetailsAsWikiText()
        {
            var errors = new List<string>();

            string whyNot;

            foreach (var item in _chain)
                if (item is IBaseExchangeItemProposed
                    && !((IBaseExchangeItemProposed)item).IsValid(out whyNot))
                    errors.Add(whyNot);

            var sb = new StringBuilder();

            sb.AppendLine(Utilities.DetailsAsWikiText(this));

            if (errors.Count > 0)
            {
                sb.AppendLine("== ERRORS");

                foreach (var error in errors)
                    sb.AppendLine("* " + error);
            }

            foreach (var item in _chain)
                sb.AppendLine(Utilities.DetailsAsWikiText(item));

            return sb.ToString();
        }

        /// <summary>
        /// If input is not already connected to output will make that connection
        /// </summary>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Linkage Create(IBaseOutput output, IBaseInput input)
        {
            Contract.Requires(input != null, "input != null");
            Contract.Requires(input.Provider != null || output != null, 
                "input.Provider != null || output != null");

            if (output != null)
                output.AddConsumer(input);

            return new Linkage(input);
        }

        public class CompareConnection : IEqualityComparer<ILinkage>
        {
            public bool Equals(ILinkage x, ILinkage y)
            {
                return object.ReferenceEquals(x.Input, y.Input)
                    && object.ReferenceEquals(x.OutputFirst, y.OutputFirst);
            }

            public int GetHashCode(ILinkage obj)
            {
                if (Object.ReferenceEquals(obj, null))
                    return 0;

                return obj.GetHashCode();
            }
        }

        public class CompareLinkage : IEqualityComparer<ILinkage>
        {
            public enum Equality { OutputsOnly = 0, OutputsInputs, }

            Equality _equality = Equality.OutputsInputs;

            public CompareLinkage(Equality equality)
            {
                _equality = equality;
            }

            public bool Equals(ILinkage x, ILinkage y)
            {
                if (!object.ReferenceEquals(x.OutputLast, y.OutputLast))
                    return false;

                if (_equality == Equality.OutputsOnly)
                    return true;

                return object.ReferenceEquals(x.Input, y.Input);
            }

            public int GetHashCode(ILinkage obj)
            {
                if (Object.ReferenceEquals(obj, null))
                    return 0;

                return obj.GetHashCode();
            }
        }
    }
}

