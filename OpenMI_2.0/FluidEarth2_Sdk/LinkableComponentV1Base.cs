using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FluidEarth2.Sdk.CoreStandard2;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;

namespace FluidEarth2.Sdk
{
    /// <summary>
    /// Common implementation for components that upgrade OpenMI 1.4 components so can be used
    /// in OpenMI 2.0 compositions.
    /// </summary>
    public abstract class LinkableComponentV1Base : BaseComponentTimeWithEngine
    {
        /// <summary>
        /// Convert a OpenMI 1.4 input exchange item into corresponding OpenMI 2.0 base input.
        /// </summary>
        /// <param name="item1">OpenMI version 1.4 input exchange item</param>
        /// <returns>OpenMI 2.0 base input</returns>
        protected abstract IBaseInput Convert(OpenMI.Standard.IInputExchangeItem item1);

        /// <summary>
        /// Convert a OpenMI 1.4 output exchange item into corresponding OpenMI 2.0 base output.
        /// </summary>
        /// <param name="item1">OpenMI version 1.4 output exchange item</param>
        /// <returns>OpenMI 2.0 base output</returns>
        protected abstract IBaseOutput Convert(OpenMI.Standard.IOutputExchangeItem item1);

        /// <summary>
        /// Only constructor
        /// </summary>
        /// <param name="identity">Derived class identity</param>
        /// <param name="derivedComponentType">Derived class Type</param>
        /// <param name="engineType">Runtime engine Type</param>
        /// <param name="useNativeDllArgument">Runtime engine requires location details of native engine DLL</param>
        public LinkableComponentV1Base(IIdentifiable identity, ExternalType derivedComponentType, ExternalType engineType, bool useNativeDllArgument)
            : base(identity, derivedComponentType, engineType, useNativeDllArgument)
        {
            // Null URI's cause problems so initialise to something wrong
            var uri = new Uri(@"http://sourceforge.net/projects/fluidearth/");
            var omiUri = new ArgumentUri(GetArgumentIdentity(Args.V1Omi), uri);

            // There is a design error should not have to add valueChanged in two ways here!

            omiUri.ValueChanged += new EventHandler<ArgumentBase.ValueChangedEventArgs>(omiUri_ValueChanged);

            Arguments.Add(omiUri);

            var argValue = Argument(GetArgumentIdentity(Args.V1Omi)).Value as ArgumentValueUri;

            argValue.ValueChanged += new EventHandler<ArgumentValueBase<Uri>.ValueChangedEventArgs>(OnArgOmiUriValueChanged);
        }

        /// <summary>
        /// when OMI Argument Uri value changes update Arguments to include arguments defined by that OMI.
        /// </summary>
        /// <param name="sender">Event host</param>
        /// <param name="e">Value changing details</param>
        void omiUri_ValueChanged(object sender, ArgumentBase.ValueChangedEventArgs e)
        {
            var reports = new List<IReport>();

            UpdateArgumentsFromOmiV1(reports);
        }

        /// <summary>
        /// when OMI Argument Uri value changes update Arguments to include arguments defined by that OMI.
        /// </summary>
        /// <param name="sender">Event host</param>
        /// <param name="e">Value changing details</param>
        void OnArgOmiUriValueChanged(object sender, ArgumentValueBase<Uri>.ValueChangedEventArgs e)
        {
            var reports = new List<IReport>();

            UpdateArgumentsFromOmiV1(reports);
        }

        /// <summary>
        /// Arguments implemented by this class
        /// </summary>
        public enum Args
        {
            /// <summary>
            /// Uri of OpenMI v1 OMI XML
            /// </summary>
            V1Omi = 0,
        }

        /// <summary>
        /// Identities of arguments implemented by this class
        /// </summary>
        /// <param name="key">Argument</param>
        /// <returns>Argument identity</returns>
        public static IIdentifiable GetArgumentIdentity(Args key)
        {
            switch (key)
            {
                case Args.V1Omi:
                    return new Identity(
                        "FluidEarth2.Sdk.LinkableComponentV1WrapperBase." + key.ToString(),
                        "V1 OMI",
                        "The OpenMI version 1 OMI XML");
                default:
                    break;
            }

            throw new NotImplementedException(key.ToString());
        }

        /// <summary>
        /// Validate arguments implemented by this class
        /// </summary>
        /// <param name="arg">Argument to validate</param>
        /// <param name="message">Validation message</param>
        /// <returns>Validation status</returns>
        protected override ValidArgumentMessage ValidArgumentValue(IArgument arg, out string message)
        {
            try
            {
                message = string.Empty;

                if (arg.Id == GetArgumentIdentity(Args.V1Omi).Id)
                {
                    var uriV1 = ArgumentV1Omi;

                    // ToDo: Validate against ArgsV1Wrapper schema

                    return ValidArgumentMessage.OK;
                }
            }
            catch (Exception e)
            {
                message = string.Format(
                    "# Argument \"{0}\" = \"{1}\" not set to a valid value.\r\n* {2}",
                    arg.Caption, arg.ValueAsString, e.Message);

                return ValidArgumentMessage.Error;
            }

            return base.ValidArgumentValue(arg, out message);
        }

        /// <summary>
        /// Setting to a valid URI will cause validation of the containing XML against the OpenMI OMI V1 Schema.
        /// If validates OK, the V1 arguments will be converted to V2 arguments and added to the components
        /// Arguments attribute.
        /// </summary>
        public Uri ArgumentV1Omi
        {
            get
            {
                var argValue = Argument(GetArgumentIdentity(Args.V1Omi))
                    .Value as ArgumentValueUri;

                return argValue.Value;
            }
            
            set
            {
                var argValue = Argument(GetArgumentIdentity(Args.V1Omi))
                    .Value as ArgumentValueUri; 
                
                argValue.Value = value;
            }
        }

        /// <summary>
        /// Update Arguments to include OpenMI V1 arguments defined by Args.V1Omi value 
        /// </summary>
        /// <param name="reports">Details about method success</param>
        /// <returns>True if successful</returns>
        bool UpdateArgumentsFromOmiV1(List<IReport> reports)
        {
            Contract.Requires(reports != null, "reports != null");

            var argsV1 = Arguments
                .Where(a => a is ArgumentStandard1)
                .ToArray();

            foreach (var a in argsV1)
                Arguments.Remove(a);

            XElement xLinkableComponent;
            List<Utilities.Standard1.Argument1> args1;

            if (!UpdateArgumentsFromOmiV1(ArgumentV1Omi, out xLinkableComponent, out args1, reports))
                return false;

            foreach (var a in args1.Select(a => new ArgumentStandard1(a)))
                Arguments.Add(a);

            return true;
        }

        /// <summary>
        /// Update Arguments to include OpenMI V1 arguments defined by OMI Uri 
        /// </summary>
        /// <param name="uriOmiV1">Uri of OpenMI v1 OMI XML</param>
        /// <param name="xLinkableComponent">XML Contents of OMI Uri</param>
        /// <param name="args1">OpenMI V1 arguments constructed from xLinkableComponent</param>
        /// <param name="reports">Details about method success</param>
        /// <returns>True if successful</returns>
        public static bool UpdateArgumentsFromOmiV1(Uri uriOmiV1, out XElement xLinkableComponent, out List<Utilities.Standard1.Argument1> args1, List<IReport> reports)
        {
            Contract.Requires(reports != null, "reports != null");

            xLinkableComponent = null;
            args1 = null;

            if (uriOmiV1 == null)
            {
                var id = GetArgumentIdentity(Args.V1Omi);
                reports.Add(Report.Error(Report.ResourceIds.InvalidUri, id.Caption,
                    "URI argument for OMI V1 not specified"));
                return false;
            }

            if (!File.Exists(uriOmiV1.LocalPath))
            {
                var id = GetArgumentIdentity(Args.V1Omi);
                reports.Add(Report.Error(Report.ResourceIds.FileMissing, id.Caption,
                    uriOmiV1.LocalPath));
                return false;
            }

            var omiFile = new DocumentExistingFile(uriOmiV1);

            var xOmi = omiFile.Open();

            xLinkableComponent = xOmi.Root;

            if (!Omi.Component.Validates(xLinkableComponent, Xsd.ComponentV1, reports))
                return false;

            var ns1 = Omi.Component.NamespaceOpenMIv1;

            var xArguments = xOmi
                .Element(ns1.GetName("LinkableComponent"))
                .Elements(ns1.GetName("Arguments"))
                .SingleOrDefault();

            if (xArguments != null)
                args1 = Utilities.Standard1.Arguments1(
                    xArguments,
                    ns1,
                    omiFile.Uri);

            return true;
        }

        /// <summary>
        /// Create an instance of OpenMI.Standard.ILinkableComponent
        /// </summary>
        /// <param name="uriOmiV1">Uri of OMI XML</param>
        /// <param name="component1">Instance of OpenMI.Standard.ILinkableComponent</param>
        /// <param name="reports">Details about method success</param>
        /// <returns>True if successful</returns>
        public static bool InstantiateComponent1(Uri uriOmiV1, out OpenMI.Standard.ILinkableComponent component1, List<IReport> reports)
        {
            component1 = null;
            XElement xLinkableComponent;
            List<Utilities.Standard1.Argument1> args1;

            if (!UpdateArgumentsFromOmiV1(uriOmiV1, out xLinkableComponent, out args1, reports))
                return false;

            var assembly = Utilities.Xml.GetAttribute(xLinkableComponent, "Assembly");
            var type = Utilities.Xml.GetAttribute(xLinkableComponent, "Type");

            var omiFile = new DocumentExistingFile(uriOmiV1);
            var assemblyUri = new Uri(uriOmiV1, assembly);

            var typeV1 = new ExternalType(omiFile);
            typeV1.Initialise(assemblyUri.LocalPath, type);

            Type tComponentV1;
            component1 = typeV1.CreateInstance(out tComponentV1) as OpenMI.Standard.ILinkableComponent;

            if (component1 == null)
            {
                reports.Add(Report.Error(Report.ResourceIds.Instantiation, tComponentV1.ToString(), string.Empty));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Create the V2 component from the V1 component
        /// </summary>
        /// <param name="reports">Details about method success</param>
        /// <returns>True if successful</returns>
        protected virtual bool CreateComponent(List<IReport> reports)
        {
            OpenMI.Standard.ILinkableComponent component1;

            if (!InstantiateComponent1(ArgumentV1Omi, out component1, reports))
                return false;

            var args = Arguments
                .Select(a => new Utilities.Standard1.Argument1(a));

            component1.Initialize(args.ToArray());

            ArgumentTimeHorizon = new Time(
                component1.TimeHorizon.Start.ModifiedJulianDay,
                component1.TimeHorizon.End.ModifiedJulianDay);

            Inputs.Clear();

            for (int n = 0; n < component1.InputExchangeItemCount; ++n)
                Inputs.Add(Convert(component1.GetInputExchangeItem(n)));

            Outputs.Clear();

            for (int n = 0; n < component1.OutputExchangeItemCount; ++n)
                Outputs.Add(Convert(component1.GetOutputExchangeItem(n)));

            component1.Dispose();

            return true;
        }

        /// <summary>
        /// Intercept initialise call to build V2 component from V1 component
        /// </summary>
        /// <param name="reinitialising">True if component has been initialised before</param>
        protected override void DoInitialise(bool reinitialising)
        {
            base.DoInitialise(reinitialising);

            var reports = new List<IReport>();

            if (!CreateComponent(reports))
                throw new Exception("Creating V2 component from V1 OMI XML", null, reports);
        }
    }
}

