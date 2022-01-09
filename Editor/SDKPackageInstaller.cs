// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using XRTK.Editor;
using XRTK.Editor.Utilities;

namespace XRTK.SDK.Editor
{
    [InitializeOnLoad]
    internal static class SDKPackageInstaller
    {
        private static readonly string DefaultPath = $"{MixedRealityPreferences.ProfileGenerationPath}SDK";
        private static readonly string HiddenProfilePath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(SdkPathFinder))}{Path.DirectorySeparatorChar}{MixedRealityPreferences.HIDDEN_PROFILES_PATH}");
        private static readonly string HiddenPrefabPath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(SdkPathFinder))}{Path.DirectorySeparatorChar}{MixedRealityPreferences.HIDDEN_PREFABS_PATH}");
        private static readonly Dictionary<string, string> DefaultSdkAssets = new Dictionary<string, string>
        {
            {HiddenProfilePath,  $"{DefaultPath}{Path.DirectorySeparatorChar}Profiles"},
            {HiddenPrefabPath, $"{DefaultPath}{Path.DirectorySeparatorChar}Prefabs"}
        };

        static SDKPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install SDK Package Assets...", true, -1)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{DefaultPath}{Path.DirectorySeparatorChar}Profiles") ||
                   !Directory.Exists($"{DefaultPath}{Path.DirectorySeparatorChar}Prefabs");
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install SDK Package Assets...", false, -1)]
        private static void ImportPackageAssets()
        {
            EditorPreferences.Set($"{nameof(SDKPackageInstaller)}.Assets", false);
            EditorApplication.delayCall += CheckPackage;
        }

        private static void CheckPackage()
        {
            if (!EditorPreferences.Get($"{nameof(SDKPackageInstaller)}.Assets", false))
            {
                EditorPreferences.Set($"{nameof(SDKPackageInstaller)}.Assets", PackageInstaller.TryInstallAssets(DefaultSdkAssets));
            }
        }
    }
}
