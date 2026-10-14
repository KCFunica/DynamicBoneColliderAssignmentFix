// Copyright (c) 2026 KCFunica. Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using DynamicBoneColliderAssignmentFix;

internal static class Tests
{
    private static int passed;
    internal static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static int Main(string[] args)
    {
        string core = args[0];
        string game = args[1];
        string posePath = args[2], managed = args[3];
        string pluginPath = args[4];
        AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs e) {
            string filename = new AssemblyName(e.Name).Name + ".dll";
            foreach (string root in new string[] { core, managed, Path.Combine(game, "BepInEx\\plugins"), Path.GetDirectoryName(posePath) }) {
                if (!Directory.Exists(root)) continue;
                string[] paths = Directory.GetFiles(root, filename, SearchOption.AllDirectories);
                if (paths.Length != 0) return Assembly.LoadFrom(paths[0]);
            }
            return null;
        };
        try
        {
            Test("replacement: existing ON/OFF survives and unknown bone is OFF", delegate {
                Fake f = new Fake(); object on = f.Bone(true), off = f.Bone(false), fresh = new object();
                using (f.Policy.Begin(f.Parent)) {
                    f.Recognize(on); f.Recognize(off); f.Recognize(fresh);
                    Check(f.Value(on) && !f.Value(off) && !f.Value(fresh), "assignment changed");
                }
                Check(f.Default, "default not restored");
            });
            Test("registered ON is not overwritten by physical-list mismatch", delegate {
                Fake f = new Fake(); object bone = f.Bone(false); f.Assignments[bone] = true;
                using (f.Policy.Begin(f.Parent)) { f.Recognize(bone); Check(f.Applied.Contains(bone), "ON lost"); }
            });
            Test("registered OFF is not overwritten by physical-list mismatch", delegate {
                Fake f = new Fake(); object bone = f.Bone(true); f.Assignments[bone] = false;
                using (f.Policy.Begin(f.Parent)) { f.Recognize(bone); Check(!f.Applied.Contains(bone), "OFF lost"); }
            });
            Test("dead old bone is skipped; replacement is not matched by name", delegate {
                Fake f = new Fake(); object old = f.Bone(true); f.Dead.Add(old); object fresh = new object();
                using (f.Policy.Begin(f.Parent)) { f.Recognize(fresh); }
                Check(!f.Assignments.ContainsKey(old) && !f.Value(fresh), "dead/new handling");
            });
            Test("untouched new collider keeps default behavior", delegate {
                Fake f = new Fake(); f.Configured = false; object fresh = new object();
                using (f.Policy.Begin(f.Parent)) { f.Recognize(fresh); }
                Check(f.Value(fresh), "new collider suppressed");
            });
            Test("Ver02: known ON stays ON; new physical ON is disabled", delegate {
                Fake f = new Fake(); object old = f.Bone(true), fresh = new object();
                f.Secondary.Add(old); f.Secondary.Add(fresh); f.Applied.Add(fresh);
                using (ProtectionScope scope = f.Policy.Begin(f.Parent)) {
                    Check(scope.ProtectSecondaryBones() == 1, "unexpected secondary count");
                }
                Check(f.Value(old) && !f.Value(fresh) && !f.Applied.Contains(fresh), "Ver02 handling");
            });
            Test("manual ON of a recognized secondary bone remains ON", delegate {
                Fake f = new Fake(); object fresh = new object(); f.Secondary.Add(fresh); f.Assignments[fresh] = true;
                using (ProtectionScope scope = f.Policy.Begin(f.Parent)) { Check(scope.ProtectSecondaryBones() == 0, "manual ON changed"); }
                Check(f.Value(fresh), "manual ON lost");
            });
            Test("nested refresh restores the enclosing then original default", delegate {
                Fake f = new Fake();
                using (f.Policy.Begin(f.Parent)) {
                    using (f.Policy.Begin(f.Parent)) { Check(!f.Default, "inner policy"); }
                    Check(!f.Default, "outer policy lost");
                }
                Check(f.Default, "outer restore");
            });
            Test("original false default is preserved", delegate {
                Fake f = new Fake(); f.Default = false;
                using (f.Policy.Begin(f.Parent)) { }
                Check(!f.Default, "false changed to true");
            });
            Test("partial suppression failure rolls back default", delegate {
                Fake f = new Fake(); f.FailAfterWriteOnce = true;
                bool threw = false;
                try { f.Policy.Begin(f.Parent); } catch (InvalidOperationException) { threw = true; }
                Check(threw && f.Default, "failed write leaked false");
            });
            Test("preparation failure does not change default", delegate {
                Fake f = new Fake(); f.Bone(true); f.FailAssignment = true;
                bool threw = false;
                try { f.Policy.Begin(f.Parent); } catch (InvalidOperationException) { threw = true; }
                Check(threw && f.Default, "preparation failure changed default");
            });
            Test("multiple colliders have independent assignments and restored defaults", MultiCollider);
            Test("another item's ON assignment is unchanged by character refresh", delegate {
                Fake f = new Fake(); object itemParent = new object(), itemBone = new object();
                ColliderTarget t = new ColliderTarget(f.Collider, f);
                f.SetAssignment(t, itemParent, itemBone, true);
                object fresh = new object();
                using (f.Policy.Begin(f.Parent)) { f.Recognize(fresh); }
                Check(f.Map(itemParent)[itemBone] && f.Applied.Contains(itemBone), "item ON was changed");
                Check(!f.Value(fresh) && !f.Assignments.ContainsKey(itemBone), "parent maps mixed");
            });
            Test("scope is idempotent and does not overwrite a later UI change", delegate {
                Fake f = new Fake(); ProtectionScope scope = f.Policy.Begin(f.Parent);
                scope.Dispose(); f.Default = false; scope.Dispose();
                Check(!f.Default, "double dispose overwrote later setting");
            });
            Test("no configured colliders performs no secondary scan", delegate {
                Fake f = new Fake(); f.Configured = false; f.FailSecondaryScan = true;
                using (ProtectionScope scope = f.Policy.Begin(f.Parent)) { Check(scope.ProtectSecondaryBones() == 0, "empty scope"); }
            });
            Test("real Harmony: finalizer restores on success and original exception", HarmonyRegression.Run);
            Test("Harmony 5-argument API is accepted", delegate {
                Check(RuntimeCompatibility.PatchApi(typeof(FivePatch)).GetParameters().Length == 5, "5-argument API rejected");
            });
            Test("Harmony 6-argument API with optional extension is accepted", delegate {
                Check(RuntimeCompatibility.PatchApi(typeof(SixPatch)).GetParameters().Length == 6, "6-argument API rejected");
            });
            Test("optional reference parameter uses concrete null without binder substitution", delegate {
                ParameterInfo parameter = typeof(SixPatch).GetMethod("Patch").GetParameters()[5];
                Check(HarmonyBridge.OptionalDefault(parameter) == null, "Missing sentinel leaked to Mono binder");
            });
            Test("optional value parameters preserve declared defaults", delegate {
                ParameterInfo[] parameters = typeof(OptionalValues).GetMethod("Defaults").GetParameters();
                Check((int)HarmonyBridge.OptionalDefault(parameters[0]) == 7, "integer default lost");
                Check((bool)HarmonyBridge.OptionalDefault(parameters[1]), "boolean default lost");
                Check((DayOfWeek)HarmonyBridge.OptionalDefault(parameters[2]) == DayOfWeek.Friday, "enum default lost");
                Check((int)HarmonyBridge.OptionalDefault(parameters[3]) == 0, "metadata-only value default lost");
            });
            Test("Harmony without a finalizer is rejected", delegate {
                bool rejected = false;
                try { RuntimeCompatibility.PatchApi(typeof(NoFinalizer)); } catch (MissingMethodException) { rejected = true; }
                Check(rejected, "unsafe API accepted");
            });
            Test("renamed field uses a unique exact shape", delegate {
                Check(RuntimeCompatibility.Field(typeof(FutureRefresh), "oldFlag", typeof(bool), false, true).Name == "Default", "renamed field missed");
            });
            Test("renamed recognition method is found through flag and list reads", delegate {
                FieldInfo flag = typeof(FutureRefresh).GetField("Default"), list = typeof(FutureRefresh).GetField("Bones");
                Check(RuntimeCompatibility.Refresh(typeof(FutureRefresh), flag, list).Name == "RenamedRecognition", "renamed method missed");
            });
            Test("ambiguous fields fail safely", delegate {
                bool rejected = false;
                try { RuntimeCompatibility.Field(typeof(AmbiguousContract), "old", typeof(bool), false, true); } catch (MissingFieldException) { rejected = true; }
                Check(rejected, "ambiguous field selected");
            });
            Test("changed named field type is rejected even if an alternative exists", delegate {
                bool rejected = false;
                try { RuntimeCompatibility.Field(typeof(ChangedContract), "Default", typeof(bool), false, true); } catch (MissingFieldException) { rejected = true; }
                Check(rejected, "changed contract guessed");
            });
            Test("namespace move with a unique type name is accepted", delegate {
                Check(RuntimeCompatibility.TypeByName(Assembly.GetExecutingAssembly(), "Old.FutureRefresh") == typeof(FutureRefresh), "namespace move missed");
            });
            Test("ambiguous recognition methods are rejected", delegate {
                bool rejected = false;
                try { RuntimeCompatibility.Refresh(typeof(AmbiguousRefresh), typeof(AmbiguousRefresh).GetField("Default"), typeof(AmbiguousRefresh).GetField("Bones")); }
                catch (AmbiguousMatchException) { rejected = true; }
                Check(rejected, "ambiguous method selected");
            });
            Test("produced plugin resolves the target game's Unity base type", delegate {
                Assembly production = Assembly.LoadFrom(Path.GetFullPath(pluginPath));
                Type plugin = production.GetType("DynamicBoneColliderAssignmentFix.Plugin", true);
                Check(typeof(UnityEngine.Component).IsAssignableFrom(plugin), "Unity reference is incompatible");
                Check(production.GetName().Version.ToString() == "0.2.1.0", "stale artifact tested");
                AssemblyCopyrightAttribute copyright = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(production, typeof(AssemblyCopyrightAttribute));
                Check(copyright != null && copyright.Copyright == "Copyright (c) 2026 KCFunica", "copyright metadata mismatch");
                string publicGuid = (string)plugin.GetField("Guid").GetRawConstantValue();
                Check(publicGuid.StartsWith("io.github.KCFunica."), "public identifier mismatch");
            });
            Test("production reflection adapter binds to installed KKPE/game DLLs", delegate {
                Assembly kkpe = Assembly.LoadFrom(posePath);
                KkpeAccess adapter = new KkpeAccess(kkpe);
                Check(adapter.RefreshMethod.DeclaringType.FullName == "HSPE.AMModules.DynamicBonesEditor", "wrong patch target");
            });
            ConfigMigrationRegression();
            Console.WriteLine("PASS " + passed + " regression tests (CLR " + Environment.Version + ")");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private static void ConfigMigrationRegression()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ColliderFixMigration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, "io.github.KCFunica.kk.dynamicbonecolliderassignmentfix.cfg");
        string previous = Path.Combine(directory, "io.github.PreviousOwner.kk.dynamicbonecolliderassignmentfix.cfg");
        string extra = Path.Combine(directory, "io.github.AnotherOwner.kk.dynamicbonecolliderassignmentfix.cfg");
        const string pattern = "io.github.*.kk.dynamicbonecolliderassignmentfix.cfg";
        try
        {
            Test("config import: no prior file leaves defaults untouched", delegate {
                Check(!ConfigMigration.CopyIfNeeded(target, pattern) && !File.Exists(target), "unexpected config created");
            });
            Test("config import: preserves previous settings and original file", delegate {
                const string text = "[General]\r\nPreserveColliderAssignments = false\r\nDebugLogging = true\r\n";
                File.WriteAllText(previous, text);
                Check(ConfigMigration.CopyIfNeeded(target, pattern), "config not imported");
                Check(File.ReadAllText(target) == text && File.ReadAllText(previous) == text, "settings or original modified");
            });
            Test("config import: existing new config is never overwritten", delegate {
                File.WriteAllText(target, "existing new settings");
                Check(!ConfigMigration.CopyIfNeeded(target, pattern) && File.ReadAllText(target) == "existing new settings", "existing config overwritten");
            });
            Test("config import: ambiguous prior files are rejected without changes", delegate {
                File.Delete(target);
                File.WriteAllText(extra, "other settings");
                bool rejected = false;
                try { ConfigMigration.CopyIfNeeded(target, pattern); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected && !File.Exists(target) && File.Exists(previous) && File.Exists(extra), "ambiguous import changed files");
            });
        }
        finally
        {
            foreach (string path in new string[] { target, previous, extra }) if (File.Exists(path)) File.Delete(path);
            Directory.Delete(directory);
        }
    }

    private static void MultiCollider()
    {
        Multi m = new Multi(); m.B.Parent = m.A.Parent; object bone = new object(); m.A.Known.Add(bone); m.B.Known.Add(bone);
        m.A.Applied.Add(bone); m.B.Default = false;
        using (new AssignmentPolicy(m).Begin(m.A.Parent)) {
            Check(m.A.Value(bone) && !m.B.Value(bone), "mixed assignments");
            Check(!m.A.Default && !m.B.Default, "defaults not suppressed");
        }
        Check(m.A.Default && !m.B.Default, "mixed restored defaults");
    }

    internal sealed class Fake : IAssignmentAccess
    {
        internal object Parent = new object();
        internal readonly object Collider = new object();
        internal readonly List<object> Known = new List<object>(), Secondary = new List<object>(), Dead = new List<object>();
        internal readonly HashSet<object> Applied = new HashSet<object>();
        internal readonly Dictionary<object, bool> Assignments = new Dictionary<object, bool>();
        private readonly Dictionary<object, Dictionary<object, bool>> otherParents = new Dictionary<object, Dictionary<object, bool>>();
        internal bool Default = true, Configured = true, FailAfterWriteOnce, FailAssignment, FailSecondaryScan;
        internal AssignmentPolicy Policy { get { return new AssignmentPolicy(this); } }
        internal object Bone(bool on) { object b = new object(); Known.Add(b); if (on) Applied.Add(b); return b; }
        internal bool Value(object b) { return Assignments[b]; }
        internal void Recognize(object b) {
            if (!Assignments.ContainsKey(b)) Assignments[b] = Default;
            if (Assignments[b]) Applied.Add(b); else Applied.Remove(b);
        }
        public IList<ColliderTarget> ConfiguredColliders() {
            return Configured ? new List<ColliderTarget> { new ColliderTarget(Collider, this) } : new List<ColliderTarget>();
        }
        public IEnumerable<object> KnownBones(object parent) { return Known; }
        public IEnumerable<object> CurrentSecondaryBones(object parent) {
            if (FailSecondaryScan) throw new InvalidOperationException("unexpected secondary scan");
            return Secondary;
        }
        public bool IsAlive(object value) { return value != null && !Dead.Contains(value); }
        internal Dictionary<object, bool> Map(object parent) {
            if (Object.ReferenceEquals(parent, Parent)) return Assignments;
            Dictionary<object, bool> map;
            if (!otherParents.TryGetValue(parent, out map)) { map = new Dictionary<object, bool>(); otherParents.Add(parent, map); }
            return map;
        }
        public bool HasAssignment(ColliderTarget t, object p, object b) { return ((Fake)t.Editor).Map(p).ContainsKey(b); }
        public bool IsApplied(ColliderTarget t, object b) { return ((Fake)t.Editor).Applied.Contains(b); }
        public void SetAssignment(ColliderTarget t, object p, object b, bool enabled) {
            Fake f = (Fake)t.Editor;
            if (f.FailAssignment) throw new InvalidOperationException("injected assignment failure");
            f.Map(p)[b] = enabled;
            if (enabled) f.Applied.Add(b); else f.Applied.Remove(b);
        }
        public bool GetDefault(object editor) { return ((Fake)editor).Default; }
        public void SetDefault(object editor, bool value) {
            Fake f = (Fake)editor; f.Default = value;
            if (f.FailAfterWriteOnce) { f.FailAfterWriteOnce = false; throw new InvalidOperationException("injected flag failure"); }
        }
    }

    private sealed class Multi : IAssignmentAccess
    {
        internal readonly Fake A = new Fake(), B = new Fake();
        public IList<ColliderTarget> ConfiguredColliders() { return new List<ColliderTarget> { new ColliderTarget(A.Collider, A), new ColliderTarget(B.Collider, B) }; }
        public IEnumerable<object> KnownBones(object p) { return A.Known; }
        public IEnumerable<object> CurrentSecondaryBones(object p) { return A.Secondary; }
        public bool IsAlive(object v) { return A.IsAlive(v); }
        public bool HasAssignment(ColliderTarget t, object p, object b) { return A.HasAssignment(t, p, b); }
        public bool IsApplied(ColliderTarget t, object b) { return A.IsApplied(t, b); }
        public void SetAssignment(ColliderTarget t, object p, object b, bool e) { A.SetAssignment(t, p, b, e); }
        public bool GetDefault(object e) { return A.GetDefault(e); }
        public void SetDefault(object e, bool v) { A.SetDefault(e, v); }
    }
}

internal static class HarmonyRegression
{
    private static Tests.Fake fake;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Original(bool fail) {
        Tests.Check(!fake.Default, "prefix not applied");
        if (fail) throw new ApplicationException("original failure");
    }
    private static void Prefix(out ProtectionScope __state) { __state = fake.Policy.Begin(fake.Parent); }
    private static void Finalizer(ProtectionScope __state) { if (__state != null) __state.Dispose(); }
    internal static void Run() {
        fake = new Tests.Fake();
        HarmonyBridge harmony = new HarmonyBridge("assignmentfix.regression");
        BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        try {
            harmony.Install(typeof(HarmonyRegression).GetMethod("Original", flags),
                typeof(HarmonyRegression).GetMethod("Prefix", flags), null,
                typeof(HarmonyRegression).GetMethod("Finalizer", flags));
            Original(false); Tests.Check(fake.Default, "normal finalizer failed");
            bool originalPropagated = false;
            try { Original(true); } catch (ApplicationException ex) { originalPropagated = ex.Message == "original failure"; }
            Tests.Check(originalPropagated && fake.Default, "original exception lost or flag not restored");
        } finally { harmony.Remove(); }
    }
}

internal sealed class FivePatch
{
    public void Patch(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix, HarmonyMethod transpiler, HarmonyMethod finalizer) { }
}
internal sealed class SixPatch
{
    public void Patch(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix, HarmonyMethod transpiler, HarmonyMethod finalizer,
        [System.Runtime.InteropServices.Optional] HarmonyMethod extension) { }
}
internal sealed class NoFinalizer
{
    public void Patch(MethodBase original, HarmonyMethod prefix, HarmonyMethod postfix, HarmonyMethod transpiler) { }
}
internal sealed class OptionalValues
{
    public void Defaults(
        [System.Runtime.InteropServices.Optional, System.Runtime.InteropServices.DefaultParameterValue(7)] int number,
        [System.Runtime.InteropServices.Optional, System.Runtime.InteropServices.DefaultParameterValue(true)] bool enabled,
        [System.Runtime.InteropServices.Optional, System.Runtime.InteropServices.DefaultParameterValue(DayOfWeek.Friday)] DayOfWeek day,
        [System.Runtime.InteropServices.Optional] int metadataOnly) { }
}
internal sealed class FutureRefresh
{
    public bool Default = true;
    public List<object> Bones = new List<object>();
    public void RenamedRecognition() { if (Default && Bones.Count != 0) GC.KeepAlive(Bones); }
    public void OtherMethod() { GC.KeepAlive(this); }
}
internal sealed class AmbiguousContract { public bool A = true; public bool B = false; }
internal sealed class ChangedContract { public int Default = 1; public bool Alternative = false; }
internal sealed class AmbiguousRefresh
{
    public bool Default = true;
    public List<object> Bones = new List<object>();
    public void A() { if (Default && Bones.Count != 0) GC.KeepAlive(Bones); }
    public void B() { if (Default && Bones.Count != 0) GC.KeepAlive(Bones); }
}
