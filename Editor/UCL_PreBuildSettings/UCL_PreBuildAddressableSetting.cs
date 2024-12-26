
// RCG_AutoHeader
// to change the auto header please go to RCG_AutoHeader.cs
// Create time : 12/26 2024
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UCL.Core;
using UnityEngine;


namespace UCL.BuildLib
{
    public class UCL_PreBuildAddressableSetting : UCL_PreBuildSetting
    {
        /// <summary>
        /// CleanPlayerContent OnBuild
        /// </summary>
        public bool m_CleanPlayerContent = true;
        override public async UniTask OnBuild(BuildData iBuildData)
        {
            if (m_CleanPlayerContent)
            {
                UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.CleanPlayerContent(
                    UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            }

            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();
        }
    }
}