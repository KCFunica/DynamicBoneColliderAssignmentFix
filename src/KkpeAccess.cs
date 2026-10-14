// Copyright (c) 2026 KCFunica. Licensed under the MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DynamicBoneColliderAssignmentFix
{
    internal sealed class KkpeAccess : IAssignmentAccess
    {
        private readonly Type poseType, charaType, boneType, secondaryType;
        private readonly FieldInfo parentField, poseEditor, bones, boobsEditor, secondaryBones;
        private readonly FieldInfo loneColliders, dirtyColliders, assignments, defaultFlag, colliderList, secondaryColliderList;
        private readonly MethodInfo setIgnore;
        internal readonly MethodInfo RefreshMethod;
        internal string Description { get { return "assignment-map/default-flag; refresh=" + RefreshMethod.Name; } }

        internal KkpeAccess(Assembly kkpe)
        {
            poseType = RuntimeCompatibility.TypeByName(kkpe, "HSPE.PoseController");
            charaType = RuntimeCompatibility.TypeByName(kkpe, "HSPE.CharaPoseController");
            Type editorType = RuntimeCompatibility.TypeByName(kkpe, "HSPE.AMModules.DynamicBonesEditor");
            Type colliderEditorType = RuntimeCompatibility.TypeByName(kkpe, "HSPE.AMModules.CollidersEditor");
            Type baseModule = RuntimeCompatibility.TypeByName(kkpe, "HSPE.AMModules.AdvancedModeModule");
            Type dataType = colliderEditorType.GetNestedType("ColliderDataBase", BindingFlags.Public | BindingFlags.NonPublic);
            if (dataType == null) throw new TypeLoadException("ColliderDataBase is missing.");
            poseEditor = RuntimeCompatibility.Field(poseType, "_dynamicBonesEditor", editorType, false, false);
            parentField = RuntimeCompatibility.Field(baseModule, "_parent", poseType, false, false);
            FieldInfo registry = null;
            foreach (FieldInfo field in colliderEditorType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != typeof(Dictionary<,>)) continue;
                Type[] arguments = field.FieldType.GetGenericArguments();
                if (arguments[0].Name != "DynamicBoneCollider" || arguments[1] != colliderEditorType) continue;
                if (registry != null) throw new AmbiguousMatchException("Multiple lone collider registries.");
                registry = field;
            }
            if (registry == null) throw new MissingFieldException(colliderEditorType.FullName, "lone collider registry");
            loneColliders = registry;
            Type colliderType = loneColliders.FieldType.GetGenericArguments()[0];
            Assembly game = colliderType.Assembly;
            boneType = game.GetType("DynamicBone", true);
            Type perBone = typeof(Dictionary<,>).MakeGenericType(typeof(object), typeof(bool));
            Type map = typeof(Dictionary<,>).MakeGenericType(poseType, perBone);
            dirtyColliders = RuntimeCompatibility.Field(colliderEditorType, "_dirtyColliders", typeof(Dictionary<,>).MakeGenericType(colliderType, dataType), false, false);
            assignments = RuntimeCompatibility.Field(dataType, "allDynamicBones", map, false, false);
            defaultFlag = RuntimeCompatibility.Field(colliderEditorType, "_addNewDynamicBonesAsDefault", typeof(bool), false, true);
            bones = RuntimeCompatibility.Field(editorType, "_dynamicBones", typeof(List<>).MakeGenericType(boneType), false, false);
            colliderList = RuntimeCompatibility.Field(boneType, "m_Colliders", typeof(List<>).MakeGenericType(colliderType), false, false);
            secondaryType = game.GetType("DynamicBone_Ver02", false);
            if (secondaryType != null)
            {
                secondaryColliderList = RuntimeCompatibility.Field(secondaryType, "Colliders", typeof(List<>).MakeGenericType(colliderType), false, false);
                Type boobsType = RuntimeCompatibility.TypeByName(kkpe, "HSPE.AMModules.BoobsEditor");
                boobsEditor = RuntimeCompatibility.Field(charaType, "_boobsEditor", boobsType, false, false);
                secondaryBones = RuntimeCompatibility.Field(boobsType, "_dynamicBones", secondaryType.MakeArrayType(), false, false);
            }
            setIgnore = RuntimeCompatibility.Method(colliderEditorType, "SetIgnoreDynamicBone",
                new Type[] { colliderType, poseType, typeof(object), typeof(bool) });
            RefreshMethod = RuntimeCompatibility.Refresh(editorType, defaultFlag, bones);
            if (setIgnore == null || setIgnore.ReturnType != typeof(void) || RefreshMethod == null || RefreshMethod.ReturnType != typeof(void))
                throw new MissingMethodException("Required KKPE assignment/refresh method is missing or changed.");
        }

        internal object EstablishedCharacter(object editor)
        {
            object parent = parentField.GetValue(editor);
            // Skip initial construction and item controllers. Scene construction and
            // new collider initialization retain KKPE's original behavior.
            if (!IsAlive(parent) || !charaType.IsInstanceOfType(parent)) return null;
            return System.Object.ReferenceEquals(poseEditor.GetValue(parent), editor) ? parent : null;
        }

        public IList<ColliderTarget> ConfiguredColliders()
        {
            List<ColliderTarget> result = new List<ColliderTarget>();
            IDictionary registry = (IDictionary)loneColliders.GetValue(null);
            if (registry == null) throw new InvalidOperationException("KKPE collider registry is not initialized.");
            foreach (DictionaryEntry pair in registry)
            {
                if (!IsAlive(pair.Key) || pair.Value == null) continue;
                IDictionary dirty = (IDictionary)dirtyColliders.GetValue(pair.Value);
                // Untouched new colliders retain their normal initial behavior.
                if (dirty.Contains(pair.Key)) result.Add(new ColliderTarget(pair.Key, pair.Value));
            }
            return result;
        }

        public IEnumerable<object> KnownBones(object parent)
        {
            object editor = poseEditor.GetValue(parent);
            if (editor != null)
                foreach (object bone in (IList)bones.GetValue(editor)) yield return bone;
            object secondaryEditor = boobsEditor == null ? null : boobsEditor.GetValue(parent);
            if (secondaryEditor != null)
                foreach (object bone in (IList)secondaryBones.GetValue(secondaryEditor)) yield return bone;
        }

        public IEnumerable<object> CurrentSecondaryBones(object parent)
        {
            if (secondaryType == null || boobsEditor.GetValue(parent) == null) yield break;
            Component component = (Component)parent;
            // Only this character, only during KKPE refresh. Attached child items
            // with their own PoseController must not be treated as replacement bones.
            foreach (Component bone in component.GetComponentsInChildren(secondaryType, true))
                if (bone != null && bone.GetComponentInParent(poseType) == component) yield return bone;
        }

        public bool IsAlive(object value)
        {
            UnityEngine.Object unity = value as UnityEngine.Object;
            return !System.Object.ReferenceEquals(unity, null) && unity != null;
        }

        public bool HasAssignment(ColliderTarget target, object parent, object bone)
        {
            IDictionary dirty = (IDictionary)dirtyColliders.GetValue(target.Editor);
            if (!dirty.Contains(target.Collider)) return false;
            IDictionary all = (IDictionary)assignments.GetValue(dirty[target.Collider]);
            IDictionary perParent = all[parent] as IDictionary;
            return perParent != null && perParent.Contains(bone);
        }

        public bool IsApplied(ColliderTarget target, object bone)
        {
            FieldInfo list = boneType.IsInstanceOfType(bone) ? colliderList : secondaryColliderList;
            if (list == null || !list.DeclaringType.IsInstanceOfType(bone)) throw new InvalidOperationException("Unknown Dynamic Bone type.");
            return ((IList)list.GetValue(bone)).Contains(target.Collider);
        }

        public void SetAssignment(ColliderTarget target, object parent, object bone, bool enabled)
        { setIgnore.Invoke(target.Editor, new object[] { target.Collider, parent, bone, !enabled }); }
        public bool GetDefault(object editor) { return (bool)defaultFlag.GetValue(editor); }
        public void SetDefault(object editor, bool enabled) { defaultFlag.SetValue(editor, enabled); }
    }
}
