// Copyright (c) 2026 KCFunica. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicBoneColliderAssignmentFix
{
    internal static class RuntimeCompatibility
    {
        internal static Type TypeByName(Assembly assembly, string fullName)
        {
            Type exact = assembly.GetType(fullName, false);
            if (exact != null) return exact;
            string name = fullName.Substring(fullName.LastIndexOf('.') + 1);
            Type found = null;
            foreach (Type type in assembly.GetTypes())
                if (type.Name == name)
                {
                    if (found != null) throw new AmbiguousMatchException("Multiple types match " + name);
                    found = type;
                }
            if (found == null) throw new TypeLoadException("Missing PoseEditor type " + fullName);
            return found;
        }

        internal static FieldInfo Field(Type type, string name, Type shape, bool isStatic, bool writable)
        {
            FieldInfo exact = null, candidate = null;
            for (Type current = type; current != null; current = current.BaseType)
                foreach (FieldInfo field in current.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (field.Name == name && (field.IsStatic != isStatic || field.FieldType != shape || (writable && (field.IsInitOnly || field.IsLiteral))))
                        throw new MissingFieldException(type.FullName, name + " (named field has an incompatible contract)");
                    if (field.IsStatic != isStatic || field.FieldType != shape || (writable && (field.IsInitOnly || field.IsLiteral))) continue;
                    if (field.Name == name) exact = field;
                    if (candidate == null) candidate = field;
                    else if (candidate != field) candidate = AmbiguousField;
                }
            if (exact != null) return exact;
            if (candidate == null || candidate == AmbiguousField)
                throw new MissingFieldException(type.FullName, name + " (no unique field with the required structure)");
            return candidate;
        }
        private static readonly FieldInfo AmbiguousField = typeof(RuntimeCompatibility).GetField("AmbiguousField", BindingFlags.Static | BindingFlags.NonPublic);

        internal static MethodInfo Method(Type type, string name, Type[] parameters)
        {
            MethodInfo exact = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null);
            if (exact != null && exact.ReturnType == typeof(void) && !exact.IsGenericMethod && !exact.IsAbstract) return exact;
            MethodInfo candidate = null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.ReturnType != typeof(void) || method.IsGenericMethod || method.IsAbstract) continue;
                ParameterInfo[] args = method.GetParameters();
                if (args.Length != parameters.Length) continue;
                bool matches = true;
                for (int i = 0; i < args.Length; i++) if (args[i].ParameterType != parameters[i]) matches = false;
                if (!matches) continue;
                if (method.Name == name) return method;
                if (candidate != null) throw new AmbiguousMatchException("Multiple API methods match " + name);
                candidate = method;
            }
            if (candidate == null) throw new MissingMethodException(type.FullName, name);
            return candidate;
        }

        private static readonly Dictionary<short, OpCode> codes = BuildCodes();
        private static Dictionary<short, OpCode> BuildCodes()
        {
            Dictionary<short, OpCode> result = new Dictionary<short, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (field.FieldType == typeof(OpCode)) { OpCode code = (OpCode)field.GetValue(null); result[code.Value] = code; }
            return result;
        }
        internal static bool ReadsField(MethodInfo method, FieldInfo field)
        {
            MethodBody body = method.GetMethodBody();
            if (body == null) return false;
            byte[] il = body.GetILAsByteArray();
            for (int i = 0; i < il.Length; )
            {
                short value = il[i++];
                if (value == 0xfe) value = (short)(0xfe00 | il[i++]);
                OpCode code = codes[value];
                if (code == OpCodes.Ldfld || code == OpCodes.Ldsfld)
                    if (method.Module.ResolveField(BitConverter.ToInt32(il, i)).Equals(field)) return true;
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: i++; break;
                    case OperandType.InlineVar: i += 2; break;
                    case OperandType.InlineI8: case OperandType.InlineR: i += 8; break;
                    case OperandType.InlineSwitch: int count = BitConverter.ToInt32(il, i); i += 4 + count * 4; break;
                    default: i += 4; break;
                }
            }
            return false;
        }

        internal static MethodInfo Refresh(Type type, FieldInfo defaultFlag, FieldInfo bones)
        {
            MethodInfo found = null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                if (method.ReturnType == typeof(void) && !method.IsGenericMethod && method.GetParameters().Length == 0 &&
                    ReadsField(method, defaultFlag) && ReadsField(method, bones))
                {
                    if (found != null) throw new AmbiguousMatchException("Multiple recognition methods use the expected assignment contract.");
                    found = method;
                }
            if (found == null) throw new MissingMethodException("PoseEditor recognition no longer reads the expected default flag and bone list.");
            return found;
        }

        internal static MethodInfo PatchApi(Type type)
        {
            MethodInfo selected = null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != "Patch") continue;
                ObsoleteAttribute obsolete = Attribute.GetCustomAttribute(method, typeof(ObsoleteAttribute)) as ObsoleteAttribute;
                if (obsolete != null && obsolete.IsError) continue;
                ParameterInfo[] args = method.GetParameters();
                if (args.Length < 5 || args[0].ParameterType != typeof(MethodBase)) continue;
                bool prefix = false, postfix = false, finalizer = false, valid = true;
                for (int i = 1; i < args.Length; i++)
                {
                    string role = args[i].Name;
                    bool hook = role == "prefix" || role == "postfix" || role == "finalizer" || role == "transpiler";
                    if (hook && args[i].ParameterType.FullName != "HarmonyLib.HarmonyMethod") { valid = false; break; }
                    if (role == "prefix") prefix = true;
                    else if (role == "postfix") postfix = true;
                    else if (role == "finalizer") finalizer = true;
                    else if (role != "transpiler" && !args[i].IsOptional) valid = false;
                }
                if (valid && prefix && postfix && finalizer && (selected == null || args.Length < selected.GetParameters().Length)) selected = method;
            }
            if (selected == null) throw new MissingMethodException("Harmony has no compatible Patch API with a finalizer.");
            return selected;
        }
    }
}
