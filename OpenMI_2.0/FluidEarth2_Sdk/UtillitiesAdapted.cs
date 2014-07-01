using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using FluidEarth2.Sdk.Interfaces;
using OpenMI.Standard2;
using OpenMI.Standard2.TimeSpace;
using FluidEarth2.Sdk.CoreStandard2;

namespace FluidEarth2.Sdk
{
    public static partial class Utilities
    {
        public static class Adapted
        {
#if DEPRECATED
            public static bool IsValid_ConstElementCount<TType>(IBaseOutput adaptee, IBaseInput target, out string whyNot)
            {
                return IsValid_ConstElementCount<TType>(adaptee, target, out whyNot, -1, -1);
            }

            public static bool IsValid_ConstElementCount<TType>(IBaseOutput adaptee, IBaseInput target, out string whyNot, int constElementCount, int vectorLength)
            {
                Packing<TType> packingAdaptee;

                if (!GetPacking(adaptee, out packingAdaptee, out whyNot))
                    return false;

                if (!packingAdaptee.ElementValueCountConstant)
                    return Failed("Adaptee valueset allows variable element counts", out whyNot);

                if (constElementCount > -1 && packingAdaptee.ElementValueCount != constElementCount)
                    return Failed(string.Format(
                        "Adaptee valueset const element count missmatch; {0} != {1}",
                        packingAdaptee.ElementValueCount, constElementCount), out whyNot);

                if (vectorLength > -1 && packingAdaptee.VectorLength != vectorLength)
                    return Failed(string.Format(
                        "Adaptee valueset vector length missmatch; {0} != {1}",
                        packingAdaptee.VectorLength, vectorLength), out whyNot);

                if (target != null)
                {
                    Packing<TType> packingAdapted;

                    if (!GetPacking(target, out packingAdapted, out whyNot))
                        return false;

                    if (!packingAdapted.ElementValueCountConstant)
                        return Failed("Target valueset allows variable element counts", out whyNot);

                    if (constElementCount > -1 && packingAdapted.ElementValueCount != constElementCount)
                        return Failed(string.Format(
                            "Target valueset invalid const element count match; {0} != {1}",
                            packingAdapted.ElementValueCount, constElementCount), out whyNot);

                    if (vectorLength > -1 && packingAdapted.VectorLength != vectorLength)
                        return Failed(string.Format(
                            "Target valueset vector length missmatch; {0} != {1}",
                            packingAdapted.VectorLength, vectorLength), out whyNot);
                }

                whyNot = "OK";
                return true;
            }

            public static bool IsValidValueDefinition<TType>(IBaseOutput adaptee, out string whyNot)
            {
                if (IsValidValueDefinitionExchangeItem<TType>(adaptee, out whyNot))
                    return true;

                whyNot = "Adaptee: " + whyNot;
                return false;
            }
#endif
            public static bool IsValidValueDefinition<TType>(IBaseInput target, out string whyNot)
            {
                if (IsValidValueDefinitionExchangeItem<TType>(target, out whyNot))
                    return true;

                whyNot = "Target: " + whyNot;
                return false;
            }

#if DEPRECATED
            public static bool GetPacking<TType>(IBaseOutput adaptee, out Packing<TType> packing, out string whyNot)
            {
                packing = null;

                if (!IsValidValueDefinition<TType>(adaptee, out whyNot))
                    return false;

                try
                {
                    IBaseValueSet valueSet = adaptee.Values;

                    if (valueSet == null)
                    {
                        whyNot = "Adaptee: Does not support Values calls before runtime so cannot determine packing information";
                        return false;
                    }

                    packing = new Packing<TType>(valueSet);
                }
                catch (System.Exception e)
                {
                    whyNot = "Adaptee: Does not support Values calls before runtime so cannot determine packing information, "
                        + e.Message;
                    return false;
                }

                whyNot = "OK";
                return true;
            }

            public static bool GetPacking<TType>(IBaseInput target, out Packing<TType> packing, out string whyNot)
            {
                packing = null;

                if (!IsValidValueDefinition<TType>(target, out whyNot))
                    return false;

                try
                {
                    IBaseValueSet valueSet = target.Values;

                    if (valueSet == null)
                    {
                        whyNot = "Adaptee: Does not support Values calls before runtime so cannot determine packing information";
                        return false;
                    }

                    packing = new Packing<TType>(valueSet);
                }
                catch (System.Exception e)
                {
                    whyNot = "Adaptee: Does not support Values calls before runtime so cannot determine packing information, "
                        + e.Message;
                    return false;
                }

                whyNot = "OK";
                return true;
            }
#endif
            public static bool IsValidValueDefinitionExchangeItem<TType>(IBaseExchangeItem item, out string whyNot)
            {
                if (item == null)
                    return Failed("IBaseExchangeItem == null", out whyNot);
                if (item.ValueDefinition == null)
                    return Failed("IBaseExchangeItem.ValueDefinition == null", out whyNot);
                if (item.ValueDefinition.ValueType != typeof(TType))
                    return Failed("IBaseExchangeItem.ValueDefinition.ValueType != " 
                        + string.Format("typeof({0})", typeof(TType).ToString()), out whyNot);

                whyNot = "OK";
                return true;
            }

            public static bool Failed(string message, out string whyNot)
            {
                whyNot = message;
                return false;
            }

            public static bool WouldBeValid_Type<TType>(IBaseExchangeItem item, StringBuilder details)
            {
                Contract.Requires(details != null, "details != null");

                if (item == null)
                {
                    details.AppendLine("* IBaseExchangeItem == null, INVALID");
                    return false;
                }

                if (item.ValueDefinition == null)
                {
                    details.AppendLine("* IBaseExchangeItem.ValueDefinition == null, INVALID");
                    return false;
                }

                if (item.ValueDefinition.ValueType != typeof(ValueTypeNull))
                {
                    if (item.ValueDefinition.ValueType != typeof(TType))
                    {
                        details.AppendLine(string.Format("* ValueType is \"{0}\" required to be \"{1}\", INVALID",
                            item.ValueDefinition.ValueType.ToString(), typeof(TType).ToString()));
                        return false;
                    }
                }

                details.AppendLine(string.Format("* ValueType is \"{0}\", OK",
                    item.ValueDefinition.ValueType.ToString()));

                return true;
            }

            public enum ElementSetOptions { NotEmpty = 1, NoZ, NoM, }

            public static bool WouldBeValid_ElementSet(IBaseExchangeItem item, ElementType type, ElementSetOptions options,  StringBuilder details)
            {
                IElementSet elementSet = Utilities.AsElementSet(item);

                if (elementSet == null)
                {
                    details.AppendLine("* Geometry not implemented as an element set, INVALID");
                    return false;
                }

                bool ok = true;

                if (elementSet.ElementType != type)
                {
                    details.AppendLine(string.Format("* Element set type is \"{0}\" required to be \"{1}\", INVALID",
                        elementSet.ElementType.ToString(), type.ToString()));
                    ok = false;
                }
                else
                    details.AppendLine(string.Format("* Element set type is \"{0}\", OK",
                        elementSet.ElementType.ToString()));

                if ((options & ElementSetOptions.NotEmpty) != 0
                    && elementSet.ElementCount < 1)
                {
                    details.AppendLine("* Element set has no elements defined, INVALID");
                    ok = false;
                }
                else
                    details.AppendLine("* Element set has elements defined, OK");

                if (elementSet.HasZ)
                {
                    details.Append("* Element set has Z");

                    if ((options & ElementSetOptions.NoZ) != 0)
                    {
                        details.AppendLine(", INVALID");
                        ok = false;
                    }
                    else
                        details.AppendLine(", OK");
                }

                if (elementSet.HasM)
                {
                    details.Append("* Element set has M");

                    if ((options & ElementSetOptions.NoM) != 0)
                    {
                        details.AppendLine(", INVALID");
                        ok = false;
                    }
                    else
                        details.AppendLine(", OK");
                }

                return ok;
            }
        }
    }
}
