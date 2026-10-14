// Copyright (c) 2026 unica. Licensed under the MIT License.
using System;
using System.Collections.Generic;

namespace DynamicBoneColliderAssignmentFix
{
    internal sealed class ColliderTarget
    {
        internal readonly object Collider;
        internal readonly object Editor;
        internal ColliderTarget(object collider, object editor) { Collider = collider; Editor = editor; }
    }

    // No Unity or KKPE dependencies: exercised with assignment/lifecycle regression tests.
    internal interface IAssignmentAccess
    {
        IList<ColliderTarget> ConfiguredColliders();
        IEnumerable<object> KnownBones(object parent);
        IEnumerable<object> CurrentSecondaryBones(object parent);
        bool IsAlive(object value);
        bool HasAssignment(ColliderTarget target, object parent, object bone);
        bool IsApplied(ColliderTarget target, object bone);
        void SetAssignment(ColliderTarget target, object parent, object bone, bool enabled);
        bool GetDefault(object editor);
        void SetDefault(object editor, bool enabled);
    }

    internal sealed class AssignmentPolicy
    {
        private readonly IAssignmentAccess access;
        internal AssignmentPolicy(IAssignmentAccess access) { this.access = access; }

        internal ProtectionScope Begin(object parent)
        {
            IList<ColliderTarget> targets = access.ConfiguredColliders();
            ProtectionScope scope = new ProtectionScope(access, parent, targets);
            // Seed only missing entries from previously recognized, still living bones.
            // Existing dictionary values remain authoritative. Never derive an old
            // assignment from a newly enumerated replacement bone.
            List<object> known = new List<object>(access.KnownBones(parent));
            foreach (ColliderTarget target in targets)
                foreach (object bone in known)
                    if (access.IsAlive(bone) && !access.HasAssignment(target, parent, bone))
                        access.SetAssignment(target, parent, bone, access.IsApplied(target, bone));
            try
            {
                scope.SuppressDefaults();
                return scope;
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }
    }

    internal sealed class ProtectionScope : IDisposable
    {
        private readonly IAssignmentAccess access;
        private readonly object parent;
        private readonly IList<ColliderTarget> targets;
        private readonly List<KeyValuePair<object, bool>> defaults = new List<KeyValuePair<object, bool>>();
        private bool disposed;
        internal int ColliderCount { get { return targets.Count; } }

        internal ProtectionScope(IAssignmentAccess access, object parent, IList<ColliderTarget> targets)
        { this.access = access; this.parent = parent; this.targets = targets; }

        internal void SuppressDefaults()
        {
            foreach (ColliderTarget target in targets)
            {
                bool seen = false;
                foreach (KeyValuePair<object, bool> entry in defaults)
                    if (Object.ReferenceEquals(entry.Key, target.Editor)) { seen = true; break; }
                if (seen) continue;
                bool original = access.GetDefault(target.Editor);
                // Record before writing so even a partially failed write can be restored.
                defaults.Add(new KeyValuePair<object, bool>(target.Editor, original));
                access.SetDefault(target.Editor, false);
            }
        }

        internal int ProtectSecondaryBones()
        {
            int count = 0;
            if (targets.Count == 0 || !access.IsAlive(parent)) return count;
            foreach (object bone in access.CurrentSecondaryBones(parent))
            {
                if (!access.IsAlive(bone)) continue;
                foreach (ColliderTarget target in targets)
                    if (access.IsAlive(target.Collider) && !access.HasAssignment(target, parent, bone))
                    {
                        access.SetAssignment(target, parent, bone, false);
                        count++;
                    }
            }
            return count;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Exception failure = null;
            // Reverse order also makes nested refresh scopes restore correctly.
            for (int i = defaults.Count - 1; i >= 0; i--)
                try { access.SetDefault(defaults[i].Key, defaults[i].Value); }
                catch (Exception ex) { if (failure == null) failure = ex; }
            defaults.Clear();
            if (failure != null) throw new InvalidOperationException("Could not restore a KKPE default flag.", failure);
        }
    }
}
