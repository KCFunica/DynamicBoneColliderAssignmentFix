// Copyright (c) 2026 KCFunica. Licensed under the MIT License.
using System;
using System.IO;

namespace DynamicBoneColliderAssignmentFix
{
    internal static class ConfigMigration
    {
        internal static bool CopyIfNeeded(string destination, string previousPattern)
        {
            if (File.Exists(destination)) return false;
            string directory = Path.GetDirectoryName(destination);
            if (!Directory.Exists(directory)) return false;
            string[] candidates = Directory.GetFiles(directory, previousPattern, SearchOption.TopDirectoryOnly);
            if (candidates.Length == 0) return false;
            if (candidates.Length != 1)
                throw new InvalidOperationException("Multiple previous configuration files match; automatic import was skipped.");
            File.Copy(candidates[0], destination, false);
            return true;
        }
    }
}
