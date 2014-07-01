using FluidEarth2.Sdk.CoreStandard2;
using OpenMI.Standard2;
using System;
using System.IO;
using System.Xml.Linq;

namespace FluidEarth2.Sdk
{
    public class ComponentStateTimeWithEngine : BaseComponentTimeWithEngine
    {
        /// <summary>
        /// Pond Example namespace. Unique string for prefixing key names with.
        /// </summary>
        public const string ns = "FluidEarth2.Sdk.ComponentStateTimeWithEngine";
        /// <summary>
        /// Pond Example Argument namespace. Unique string for prefixing key names with.
        /// </summary>
        public const string nsArg = ns + ".Arg.";

        /// <summary>
        /// Arguments this example supports
        /// </summary>
        public enum Args {
            /// <summary>
            /// Location of file from which to de-serialize this component from
            /// </summary>
            Uri = 0,
        }

        /// <summary>
        /// Create a unique IArgument IIdentifiable from identifying enumeration value. 
        /// </summary>
        /// <param name="key">Enumeration value</param>
        /// <returns>Unique argument id</returns>
        public static IIdentifiable GetArgumentIdentity(Args key)
        {
            switch (key)
            {
                case Args.Uri:
                    return new Identity(
                        nsArg + key.ToString(),
                        "Component State",
                        "URI to XML from which to de-serialize this component from");
                default:
                    throw new NotImplementedException(key.ToString());
            }
        }

        /// <summary>
        /// Access ParametersGridRegular argument value from Arguments
        /// </summary>
        public Uri ArgumentStateUri
        {
            get
            {
                var argValue = Argument(GetArgumentIdentity(Args.Uri)).Value as ArgumentValueUri;

                return argValue.Value as Uri;
            }

            set
            {
                var argValue = Argument(GetArgumentIdentity(Args.Uri)).Value as ArgumentValueUri;

                argValue.Value = value;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ComponentStateTimeWithEngine()
            : base(new Identity(), 
                derivedComponentType: new ExternalType(typeof(ComponentTimeWithEngine)), 
                engineType: null)
        {
            ArgumentsAddRange(new IArgument[] {
                new ArgumentUri(GetArgumentIdentity(Args.Uri), null),
            });
        }

        /// <summary>
        /// Initialisation specific to Pond
        /// </summary>
        /// <param name="reinitialising">True if previously been through here before</param>
        protected override void DoInitialise(bool reinitialising)
        {
            ComponentState state = null;

            if (!reinitialising)
            {
                var stateUri = ArgumentStateUri;

                if (stateUri != null && stateUri.IsFile && File.Exists(stateUri.LocalPath))
                {
                    var accessor = new DocumentExistingFile(stateUri);

                    var document = XDocument.Load(stateUri.LocalPath);

                    state = new ComponentState();

                    state.Initialise(document.Root, accessor);

                    SetIdentity(state);

                    Caption = state.Caption; // as modified via argument

                    EngineType = new ExternalType(state.EngineType);

                    UseNativeDll = state.UseNativeEngine;
                }
            }

            base.DoInitialise(reinitialising);

            if (reinitialising || state == null)
                return;

            foreach (var arg in state.Arguments)
                Arguments.Add(arg);

            foreach (var input in state.Inputs)
                Add(input as BaseInput);

            foreach (var putput in state.Outputs)
                Add(putput as BaseOutput);
        }
    }
}
