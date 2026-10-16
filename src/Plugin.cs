// Copyright (c) 2026 KCFunica. Licensed under the MIT License.
using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using BepInEx.Bootstrap;

[assembly: AssemblyTitle("Dynamic Bone Collider Assignment Fix")]
[assembly: AssemblyCompany("KCFunica")]
[assembly: AssemblyCopyright("Copyright (c) 2026 KCFunica")]
[assembly: AssemblyVersion("0.2.1.0")]
[assembly: AssemblyFileVersion("0.2.1.0")]

namespace DynamicBoneColliderAssignmentFix
{
    [BepInPlugin(Guid, "Dynamic Bone Collider Assignment Fix", "0.2.1")]
    [BepInProcess("CharaStudio")]
    [BepInDependency("com.joan6694.kkplugins.kkpe", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
#if KKS
        public const string Guid = "io.github.KCFunica.kks.dynamicbonecolliderassignmentfix";
#else
        public const string Guid = "io.github.KCFunica.kk.dynamicbonecolliderassignmentfix";
#endif
        private static Plugin instance;
        private KkpeAccess access;
        private AssignmentPolicy policy;
        private HarmonyBridge harmony;
        private ConfigEntry<bool> preserve, debug;
        private bool failed;

        private void Awake()
        {
            instance = this;
            // The public identifier changed. Preserve a single previous config
            // without overwriting the new config or deleting the previous file.
            try
            {
#if KKS
                string previousPattern = "io.github.*.kks.dynamicbonecolliderassignmentfix.cfg";
#else
                string previousPattern = "io.github.*.kk.dynamicbonecolliderassignmentfix.cfg";
#endif
                if (ConfigMigration.CopyIfNeeded(Config.ConfigFilePath, previousPattern))
                {
                    Config.Reload();
                    Logger.LogInfo("Previous configuration imported for the new public identifier.");
                }
            }
            catch (Exception ex) { Logger.LogWarning("Previous configuration could not be imported. " + ex.Message); }
            preserve = Config.Bind<bool>("General", "PreserveColliderAssignments", true,
                "Protect configured colliders from new character Dynamic Bones during KKPE refresh. Existing assignments stay intact. Disable to use KKPE defaults.");
            debug = Config.Bind<bool>("General", "DebugLogging", false, "Log protected KKPE refresh events for troubleshooting.");
            try
            {
                PluginInfo info;
                if (!Chainloader.PluginInfos.TryGetValue("com.joan6694.kkplugins.kkpe", out info) || info.Instance == null)
                    throw new InvalidOperationException("PoseEditor is not loaded. Protection is disabled.");
                Assembly kkpe = info.Instance.GetType().Assembly;
                access = new KkpeAccess(kkpe);
                policy = new AssignmentPolicy(access);
                harmony = new HarmonyBridge(Guid);
                InstallPatch();
                Logger.LogInfo("Plugin loaded; " + kkpe.GetName().Name + " " + kkpe.GetName().Version + "; assignment protection=" + preserve.Value + "; " + access.Description + "; " + harmony.Description);
            }
            catch (Exception ex)
            {
                Fail(ex);
                if (harmony != null)
                    try { harmony.Remove(); } catch (Exception cleanup) { Logger.LogWarning(cleanup); }
            }
        }

        // Keep version-specific Harmony calls behind this call boundary so an
        // unsupported API/JIT failure is caught by Awake's compatibility handler.
        private void InstallPatch()
        {
            harmony.Install(access.RefreshMethod,
                typeof(Plugin).GetMethod("BeforeRefresh", BindingFlags.Static | BindingFlags.NonPublic),
                typeof(Plugin).GetMethod("AfterRefresh", BindingFlags.Static | BindingFlags.NonPublic),
                typeof(Plugin).GetMethod("FinishRefresh", BindingFlags.Static | BindingFlags.NonPublic));
        }

        private static void BeforeRefresh(object __instance, out ProtectionScope __state)
        {
            __state = null;
            Plugin plugin = instance;
            if (plugin == null || plugin.failed || !plugin.preserve.Value) return;
            try
            {
                object parent = plugin.access.EstablishedCharacter(__instance);
                if (parent != null) __state = plugin.policy.Begin(parent);
            }
            catch (Exception ex) { plugin.Fail(ex); }
        }

        private static void AfterRefresh(ProtectionScope __state)
        {
            if (__state == null) return;
            try
            {
                int secondary = __state.ProtectSecondaryBones();
                if (instance.debug.Value && __state.ColliderCount != 0)
                    instance.Logger.LogInfo("Protected KKPE refresh: colliders=" + __state.ColliderCount + ", new Ver02 assignments disabled=" + secondary);
            }
            catch (Exception ex) { instance.Fail(ex); }
        }

        // Harmony calls the finalizer on both success and exception. A void finalizer
        // leaves any original KKPE exception untouched; our errors cannot replace it.
        private static void FinishRefresh(ProtectionScope __state)
        {
            if (__state == null) return;
            try { __state.Dispose(); }
            catch (Exception ex) { if (instance != null) instance.Fail(ex); }
        }

        private void Fail(Exception ex)
        {
            if (!failed) Logger.LogWarning("Assignment protection stopped; KKPE original behavior will be used. " + ex);
            failed = true;
        }

        private void OnDestroy()
        {
            failed = true;
            if (harmony != null)
                try { harmony.Remove(); } catch (Exception ex) { Logger.LogWarning(ex); }
            if (instance == this) instance = null;
        }
    }
}
