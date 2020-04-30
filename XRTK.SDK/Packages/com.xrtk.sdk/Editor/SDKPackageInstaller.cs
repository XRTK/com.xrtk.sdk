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
        private static readonly string HiddenPath = Path.GetFullPath($"{PathFinderUtility.ResolvePath<IPathFinder>(typeof(SdkPathFinder)).ToForwardSlashes()}\\{MixedRealityPreferences.HIDDEN_PROFILES_PATH}");

        static SDKPackageInstaller()
        {
            if (!EditorPreferences.Get($"{nameof(SDKPackageInstaller)}", false))
            {
                EditorPreferences.Set($"{nameof(SDKPackageInstaller)}", PackageInstaller.TryInstallProfiles(HiddenPath, DefaultPath));
            }
        }
    }
}
