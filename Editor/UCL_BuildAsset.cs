
// RCG_AutoHeader
// to change the auto header please go to RCG_AutoHeader.cs
// Create time : 12/26 2024
using UnityEngine;
using UnityEditor;
using UCL.Core;
using UCL.Core.LocalizeLib;
using UCL.Core.Page;
using UnityEditor.AddressableAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using System.Linq;
using System;
using System.IO;

namespace UCL.BuildLib
{
    public static class AssetGroup
    {
        public const string Build = "Build";

        public enum EditConfigType : int
        {
            UCL_BuildAsset = 1,
        }
    }

    /// <summary>
    /// Steam相關設定
    /// </summary>
    [UCL.Core.ATTR.UCL_GroupIDAttribute(AssetGroup.Build)]
    [UCL.Core.ATTR.UCL_Sort((int)AssetGroup.EditConfigType.UCL_BuildAsset)]


    public class UCL_BuildAsset : UCL_Asset<UCL_BuildAsset>
    {
        /// <summary>
        /// 要套用的BuildProfile
        /// </summary>
        [UCL.Core.PA.UCL_List(nameof(AllBuildProfilesName))]
        public string m_BuildProfile;

        public BuildTargetGroup m_BuildTargetGroup = BuildTargetGroup.Standalone;
        public BuildTarget m_BuildTarget = BuildTarget.StandaloneWindows64;
        [UCL.Core.PA.UCL_EnumMask(m_DrawProperty = true)] public BuildOptions m_BuildOption = BuildOptions.None;

        [Header("OutputFolder etc. Build/PC/")]
        [UCL.Core.PA.UCL_FolderExplorer(UCL.Core.PA.ExplorerType.AssetsRoot)]
        public string m_OutputPath = "";

        public string m_OutputName = "";
        //[Header("Use Editor Setting if m_ScenesInBuild is Empty.")]
        //public SceneAsset[] m_ScenesInBuild;

        /// <summary>
        /// Output build log
        /// </summary>
        public bool m_OutputBuildLog = false;

        /// <summary>
        /// PreBuildProcess
        /// </summary>
        public List<UCL_PreBuildSetting> m_PreBuildProcess = new ();
        /// <summary>
        /// PostBuildProcess
        /// </summary>
        public List<UCL_PreBuildSetting> m_PostBuildProcess = new();


        protected System.Text.StringBuilder m_LogStringBuilder = null;

        public static BuildProfile[] LoadAllBuildProfiles()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BuildProfile)}");
            BuildProfile[] profiles = new BuildProfile[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                profiles[i] = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            }
            return profiles;
        }
        public static List<string> LoadAllBuildProfilesPath()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BuildProfile)}");
            List<string> paths = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                paths.Add(path);
                //profiles[i] = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            }
            return paths;
        }
        private List<string> m_AllBuildProfiles = null;

        public IList<string> AllBuildProfilesName()
        {

            //var settings = AddressableAssetSettingsDefaultObject.Settings;
            //return settings.profileSettings.GetAllProfileNames();
            if (m_AllBuildProfiles == null)
            {
                m_AllBuildProfiles = new List<string>();
                var profiles = LoadAllBuildProfilesPath();
                foreach (var profilePath in profiles)
                {
                    m_AllBuildProfiles.Add(profilePath);
                }
                //var settings = AddressableAssetSettingsDefaultObject.Settings;
                //m_AllBuildProfiles = settings.profileSettings.GetAllProfileNames();
                //// 列出所有的BuildProfile
                //foreach (var profile in settings.profileSettings.GetAllProfileNames())
                //{
                //    Debug.Log("Profile Name: " + profile.name);
                //}
            }
            return m_AllBuildProfiles;
        }
        /// <summary>
        /// Preview(OnGUI)
        /// </summary>
        /// <param name="iIsShowEditButton">Show edit button in preview window?</param>
        override public void Preview(UCL.Core.UCL_ObjectDictionary iDataDic, bool iIsShowEditButton = false)
        {
            //GUILayout.BeginHorizontal();
            using (var aScope = new GUILayout.VerticalScope("box", GUILayout.ExpandWidth(false)))
            {
                GUILayout.Label($"{UCL_LocalizeManager.Get("Preview")}({ID})[BuildProfile:{m_BuildProfile}]", UCL.Core.UI.UCL_GUIStyle.LabelStyle);
                if (iIsShowEditButton)
                {
                    if (GUILayout.Button(UCL_LocalizeManager.Get("Edit"), UCL.Core.UI.UCL_GUIStyle.ButtonStyle))
                    {
                        UCL_CommonEditPage.Create(this);
                    }
                }
            }
            //GUILayout.EndHorizontal();
        }

        public override void OnGUI(UCL_ObjectDictionary iDataDic)
        {
            using (var scope = new GUILayout.VerticalScope("box"))//, GUILayout.Width(500)
            {
                UCL.Core.UI.UCL_GUILayout.DrawObjectData(this, iDataDic, string.Empty, true, LocalizeFieldName);
                var settings = AddressableAssetSettingsDefaultObject.Settings;


#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                if (GUILayout.Button(UCL_LocalizeManager.Get("Open Output folder"), UCL.Core.UI.UCL_GUIStyle.ButtonStyle))
                {
                    var path = GetBuildPath(m_OutputPath);
                    Directory.CreateDirectory(path);
                    UCL.Core.FileLib.WindowsLib.OpenExplorer(path);
                }
#endif

                if (GUILayout.Button(UCL_LocalizeManager.Get("Build"), UCL.Core.UI.UCL_GUIStyle.ButtonStyle))
                {
                    Cysharp.Threading.Tasks.UniTaskExtensions.Forget(BuildAsync(m_OutputPath));
                }

                //if (!Application.isPlaying)
                //{
                //    GUILayout.Label("!Application.isPlaying", UCL_GUIStyle.LabelStyle);
                //    return;
                //}
            }
        }
        /// <summary>
        /// 套用BuildProfile
        /// </summary>
        /// <returns></returns>
        virtual protected BuildProfile SetBuildProfile()
        {
            if (string.IsNullOrEmpty(m_BuildProfile))
            {
                Debug.LogError($"{GetType().Name}.SetBuildProfile, string.IsNullOrEmpty(m_BuildProfile)");
                return BuildProfile.GetActiveBuildProfile();
            }
            try
            {
                var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(m_BuildProfile);
                if (profile == null)
                {
                    Debug.LogError($"{GetType().Name}.SetBuildProfile,m_BuildProfile:{m_BuildProfile}, profile == null");
                    return BuildProfile.GetActiveBuildProfile();
                }
                Debug.Log($"{GetType().Name}.SetBuildProfile,m_BuildProfile:{m_BuildProfile}, profile:{profile.name}");
                BuildProfile.SetActiveBuildProfile(profile);//套用
                return profile;
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            return BuildProfile.GetActiveBuildProfile();
        }
        protected static string GetBuildPath(string path) => Application.dataPath.Replace("Assets", path);
        virtual protected async Cysharp.Threading.Tasks.UniTask BuildAsync(string path)
        {
            try
            {
                var profile = SetBuildProfile();
                if (profile == null)
                {
                    Debug.LogError($"{GetType().Name}.BuildAsync,m_BuildProfile:{m_BuildProfile}, profile == null");
                    return;
                }
                Debug.Log($"{GetType().Name}.BuildAsync,m_BuildProfile:{m_BuildProfile}, profile:{profile.name}");
                if (m_OutputBuildLog)
                {
                    m_LogStringBuilder = new System.Text.StringBuilder();
                    Application.logMessageReceivedThreaded += ThreadedLog;
                }




                string buildPath = GetBuildPath(path);
                if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
                {
                    Debug.LogWarning("UnityEditorInternal.InternalEditorUtility.inBatchMode == true");
                    string str = GetArg("-output");
                    Debug.LogWarning("-output:" + str);
                    if (!string.IsNullOrEmpty(str))
                    {
                        buildPath = str;
                        Debug.LogWarning("Set build_path:" + str);
                    }
                    else
                    {
                        Debug.LogWarning("-output string.IsNullOrEmpty!!");
                    }
                }
                if (buildPath.Length > 0)
                {
                    char aLastChar = buildPath[buildPath.Length - 1];
                    if (aLastChar != '\\' && aLastChar != '/')
                    {
                        buildPath += '/';
                    }
                }
                string outputPath = buildPath + m_OutputName;
                Debug.LogWarning("PerformBuild output_path:" + outputPath);
                Core.FileLib.Lib.CreateDirectory(buildPath);
                BuildData buildData = new BuildData(buildPath);
                if (!m_PreBuildProcess.IsNullOrEmpty())
                {
                    foreach (var preBuildProcess in m_PreBuildProcess)
                    {
                        try
                        {
                            await preBuildProcess.OnBuild(buildData);
                        }
                        catch(System.Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }

                var scenes = profile.scenes;
                string[] scenePaths = null;
                if (scenes.IsNullOrEmpty())
                {
                    scenePaths = UCL.SceneLib.Lib.GetAcitveScenesPath();
                }
                else
                {
                    scenePaths = scenes.Select(scene => scene.path).ToArray();
                }
                var aResult = BuildPipeline.BuildPlayer(scenePaths, outputPath, m_BuildTarget, m_BuildOption);

                if (!m_PostBuildProcess.IsNullOrEmpty())
                {
                    foreach (var buildProcess in m_PostBuildProcess)
                    {
                        try
                        {
                            await buildProcess.OnBuild(buildData);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }

                if (m_OutputBuildLog)
                {
                    System.IO.File.WriteAllText(buildPath + "BuildReport.txt", aResult.summary.UCL_ToString());
                    Application.logMessageReceivedThreaded -= ThreadedLog;
                    if (m_LogStringBuilder != null)
                    {
                        System.IO.File.WriteAllText(buildPath + "BuildLog.txt", m_LogStringBuilder.ToString());
                    }
                }
#if UNITY_EDITOR_WIN
                Core.FileLib.WindowsLib.OpenExplorer(buildPath.RemoveLast());
#endif

                //BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
                //buildPlayerOptions.scenes = profile.scenes.Select(scene => scene.path).ToArray();
                //buildPlayerOptions.target = m_BuildTarget;
                //buildPlayerOptions.targetGroup = m_BuildTargetGroup;


                //BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

                //if (report.summary.result == BuildResult.Succeeded)
                //{
                //    Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
                //}
                //else if (report.summary.result == BuildResult.Failed)
                //{
                //    Debug.Log("Build failed");
                //}

                //BuildPipeline.BuildPlayer(profile);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
            
        }

        protected void ThreadedLog(string iMessage, string iStackTrace = "", LogType iType = LogType.Log)
        {
            if (m_LogStringBuilder == null)
            {
                return;
            }
            lock (m_LogStringBuilder)
            {
                m_LogStringBuilder.AppendLine(iMessage);
            }
        }
        public static string GetArg(string arg = "-output")
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == arg)
                {
                    if (i + 1 < args.Length)
                    {
                        return args[i + 1];
                    }
                }
            }
            return null;
        }
    }
    public class BuildWithProfile
    {
        //public static BuildReport Build(BuildProfile profile)
        //{
        //    BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        //    {
        //        scenes = profile.scenes.Select(scene => scene.path).ToArray(),
        //        locationPathName = profile.outputPath,
        //        target = profile.,
        //        options = profile.buildOptions
        //    };

        //    BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

        //    if (report.summary.result == BuildResult.Succeeded)
        //    {
        //        Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
        //    }
        //    else if (report.summary.result == BuildResult.Failed)
        //    {
        //        Debug.Log("Build failed");
        //    }
        //    return report;
        //}
    }

    public class UCL_BuildEntry : UCL_AssetEntryDefault<UCL_BuildAsset>
    {
        public const string DefaultID = "Default";

        public UCL_BuildEntry() { m_ID = DefaultID; }
        public UCL_BuildEntry(string iID) { m_ID = iID; }

    }
}
