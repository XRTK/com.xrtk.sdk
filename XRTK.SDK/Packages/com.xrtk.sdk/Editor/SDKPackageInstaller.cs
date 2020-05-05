// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using UnityEditor;
using XRTK.Editor;
using XRTK.Extensions;
using XRTK.Utilities.Editor;

namespace XRTK.SDK.Editor
{
    [InitializeOnLoad]
    internal static class SDKPackageInstaller
    {
        private static readonly string DefaultPath = $"{MixedRealityPreferences.ProfileGenerationPath}SDK";
        private static readonly string HiddenProfilePath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(SdkPathFinder)).ToForwardSlashes()}\\{MixedRealityPreferences.HIDDEN_PROFILES_PATH}");
        private static readonly string HiddenPrefabPath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(SdkPathFinder)).ToForwardSlashes()}\\{MixedRealityPreferences.HIDDEN_PREFABS_PATH}");

        static SDKPackageInstaller()
        {
            EditorApplication.delayCall += CheckPackage;
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install XRTK.SDK Package Assets...", true, -1)]
        private static bool ImportPackageAssetsValidation()
        {
            return !Directory.Exists($"{DefaultPath}\\Profiles");
        }

        [MenuItem("Mixed Reality Toolkit/Packages/Install XRTK.SDK Package Assets...", false, -1)]
        private static void ImportPackageAssets()
        {
            EditorPreferences.Set($"{nameof(SDKPackageInstaller)}.Profiles", false);
            EditorApplication.delayCall += CheckPackage;
        }

        private static void CheckPackage()
        {

            if (!EditorPreferences.Get($"{nameof(SDKPackageInstaller)}.Profiles", false))
            {
                EditorPreferences.Set($"{nameof(SDKPackageInstaller)}.Profiles", PackageInstaller.TryInstallAssets(HiddenProfilePath, $"{DefaultPath}\\Profiles"));
            }

            if (!EditorPreferences.Get($"{nameof(SDKPackageInstaller)}.Prefabs", false))
            {
                EditorPreferences.Set($"{nameof(SDKPackageInstaller)}.Prefabs", PackageInstaller.TryInstallAssets(HiddenPrefabPath, $"{DefaultPath}\\Prefabs"));
            }
        }
    }
}
