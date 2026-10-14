// Copyright (c) 2026 unica. Licensed under the MIT License.
using System;
using System.Reflection;
using HarmonyLib;

namespace DynamicBoneColliderAssignmentFix
{
    internal sealed class HarmonyBridge
    {
        private readonly Harmony harmony;
        private readonly string id;
        private readonly MethodInfo patch, cleanup;
        internal string Description { get { return "Harmony " + typeof(Harmony).Assembly.GetName().Version + "/Patch(" + patch.GetParameters().Length + ")"; } }
        internal HarmonyBridge(string id)
        {
            this.id = id;
            patch = RuntimeCompatibility.PatchApi(typeof(Harmony));
            cleanup = typeof(Harmony).GetMethod("UnpatchSelf", BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null);
            if (cleanup == null) cleanup = typeof(Harmony).GetMethod("UnpatchAll", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
            if (cleanup == null) throw new MissingMethodException("Harmony has no owner-scoped unpatch API.");
            harmony = new Harmony(id);
        }
        internal void Install(MethodInfo original, MethodInfo prefix, MethodInfo postfix, MethodInfo finalizer)
        {
            ParameterInfo[] parameters = patch.GetParameters();
            object[] args = new object[parameters.Length];
            args[0] = original;
            for (int i = 1; i < args.Length; i++)
                switch (parameters[i].Name)
                {
                    case "prefix": args[i] = new HarmonyMethod(prefix); break;
                    case "postfix": args[i] = postfix == null ? null : new HarmonyMethod(postfix); break;
                    case "finalizer": args[i] = new HarmonyMethod(finalizer); break;
                    case "transpiler": args[i] = null; break;
                    default: args[i] = OptionalDefault(parameters[i]); break;
                }
            patch.Invoke(harmony, args);
        }
        // Unity's legacy Mono binder cannot reliably substitute Type.Missing.
        // Supply a concrete value even for metadata-only [Optional] parameters.
        internal static object OptionalDefault(ParameterInfo parameter)
        {
            object value = parameter.DefaultValue;
            if (value == Missing.Value || value == DBNull.Value)
                return parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            if (value != null && parameter.ParameterType.IsEnum && value.GetType() != parameter.ParameterType)
                return Enum.ToObject(parameter.ParameterType, value);
            return value;
        }
        internal void Remove() { cleanup.Invoke(cleanup.IsStatic ? null : harmony, cleanup.GetParameters().Length == 0 ? null : new object[] { id }); }
    }
}
