using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace UCL.BuildLib {
#if UNITY_EDITOR
    [Core.ATTR.EnableUCLEditor]
#endif
    [CreateAssetMenu(fileName = "New BuildSetting", menuName = "UCL/BuildSetting")]
    public class UCL_BuildSetting : ScriptableObject {
        [System.Serializable]
        public struct KeyStoreSetting {
            //[Header("Key Store")]
            public string m_KeystoreName;
            public string m_KeystorePass;
            public string m_KeyaliasName;
            public string m_KeyaliasPass;
        }
        [System.Serializable]
        public class VersionSetting {
            /// <summary>
            /// Bundle Version
            /// </summary>
            public string m_BundleVersion = "1.0.0";

            /// <summary>
            /// Android bundleVersionCode
            /// </summary>
            public int m_BundleVersionCode = 1;

            /// <summary>
            /// IOS buildNumber
            /// </summary>
            public string m_BuildNumber = "1";

            public void UpdateVersion() {
                string ver = m_BundleVersion;
                var strs = ver.Split('.');
                if(strs.Length > 0) {
                    int val = int.Parse(strs[strs.Length - 1]);
                    val++;
                    ver = "";
                    for(int i = 0; i < strs.Length - 1; i++) {
                        ver += strs[i] + ".";
                    }
                    ver += val.ToString();
                    m_BundleVersion = ver;
                }
                UpdateBundleVersion();
            }
            public void UpdateBundleVersion() {
                m_BundleVersionCode++;
                int val = 0;
                if(int.TryParse(m_BuildNumber, out val)) {
                    m_BuildNumber = (val + 1).ToString();
                } else {
                    m_BuildNumber = "1";
                }
            }
            public void ApplySetting() {
                PlayerSettings.bundleVersion = m_BundleVersion;
                PlayerSettings.Android.bundleVersionCode = m_BundleVersionCode;
                PlayerSettings.iOS.buildNumber = m_BuildNumber;
            }
            public void LoadCurrentSetting() {
                m_BundleVersion = PlayerSettings.bundleVersion;
                m_BundleVersionCode = PlayerSettings.Android.bundleVersionCode;
                m_BuildNumber = PlayerSettings.iOS.buildNumber;
            }
        }
#if UNITY_EDITOR
        #region InspectorButton
        /// <summary>
        /// Create a Button invoke SetSetting()
        /// </summary>
        [Header("Apply BuildSetting to EditorBuildSetting")]
        [UCL.Core.PA.UCL_ButtonAttribute("ApplySetting")] public bool m_ApplySetting;

        /// <summary>
        /// Create a Button invoke LoadCurrentSetting()
        /// </summary>
        [Header("Load current EditorBuildSetting into BuildSetting")]
        [UCL.Core.PA.UCL_ButtonAttribute("LoadCurrentSetting")] public bool m_LoadCurrentSetting;
        /// <summary>
        /// Create a Button invoke Build()
        /// </summary>
        [Header("Apply BuildSetting to EditorBuildSetting and Build")]
        [UCL.Core.PA.UCL_ButtonAttribute("Build")] public bool m_Build;

        /// <summary>
        /// Create a Button invoke OpenOutputFolder()
        /// </summary>
        [Header("Open OutputFolder")]
        [UCL.Core.PA.UCL_ButtonAttribute("OpenOutputFolder")] public bool m_OpenOutputFolder;

        /// <summary>
        /// Output build log
        /// </summary>
        public bool m_OutputBuildLog = false;
        public void OpenOutputFolder() {
            string path = Core.FileLib.EditorLib.OpenAssetsFolderExplorer(m_OutputPath);
#if UNITY_EDITOR_WIN
            Core.FileLib.WindowsLib.OpenAssetExplorer(path);
#endif
            Debug.LogWarning("path:" + path);
            if(!string.IsNullOrEmpty(path)) {
                m_OutputPath = path;// + "/";
                Application.OpenURL(Application.dataPath.Replace("Assets", path));
            }
        }

        [Space(10)]
#endregion

        /// <summary>
        /// DefaultSettingg this BuildSetting Base On
        /// Most of the unset setting will use setting in DefaultSetting
        /// </summary>
        [Header("DefaultSetting this BuildSetting Base On")]
        public UCL_BuildSetting m_DefaultSetting;

        /// <summary>
        /// if(m_ProductName == "") PlayerSettings.productName will use setting in DefaultBuildSetting
        /// </summary>
        public string m_ProductName = "";
        public string m_ApplicationIdentifier="";

        public BuildTargetGroup m_BuildTargetGroup = BuildTargetGroup.Standalone;
        public BuildTarget m_BuildTarget = BuildTarget.StandaloneWindows64;
        [UCL.Core.PA.UCL_EnumMask(m_DrawProperty = true)] public BuildOptions m_BuildOption = BuildOptions.None;
        public Texture2D[] m_Icons;
        public Texture2D m_DefaultIcon;
        public bool m_BuildAppBundle = false;
        [Header("OutputFolder etc. Build/PC/")]public string m_OutputPath = "";
        public string m_OutputName = "";
        [Header("Use Editor Setting if m_ScenesInBuild is Empty.")]
        public SceneAsset[] m_ScenesInBuild;
        #region KeyStore
        [Header("Key Store")]
        public KeyStoreSetting m_KeyStoreSetting = new KeyStoreSetting();
        #endregion

        #region VersionSetting
        [Header("VersionSetting")]
        public VersionSetting m_VersionSetting = new VersionSetting();
        #endregion
        [Header("Define Symbols not apply DefaultBuildSetting")]
        public string m_ScriptingDefineSymbols = "";

        public System.Text.StringBuilder m_LogStringBuilder = null;
        public static UCL_BuildSetting GetSetting(string path) {
            return Resources.Load<UCL_BuildSetting>(path);
            //AssetDatabase.LoadMainAssetAtPath
        }
        public static UCL_BuildSetting GetSettingByPath(string path) {
            return AssetDatabase.LoadMainAssetAtPath(path) as UCL_BuildSetting;
        }
        public static UCL_BuildSetting GetDefaultSetting() {
            return GetSetting("DefaultBuildSetting");
        }
        public static string GetCurrentSettingPath() {
            string str = PlayerPrefs.GetString("Current_UCL_BuildSetting");
            Debug.LogWarning("GetCurrentSettingName:" + str);
            return str;
        }
        protected static void SetCurrentSettingPath(string name) {
            PlayerPrefs.SetString("Current_UCL_BuildSetting", name);
            PlayerPrefs.Save();
            Debug.LogWarning("SetCurrentSettingName:" + name);
        }
        [UnityEditor.MenuItem("UCL/BuildLib/DefaultBuildSetting")]
        public static void SelectDefaultSetting() {
            UnityEditor.Selection.activeObject = GetSetting("DefaultBuildSetting");
        }
        static string GetArg(string arg = "-output") {
            var args = Environment.GetCommandLineArgs();
            for(int i = 0; i < args.Length; i++) {
                if(args[i].ToLower() == arg) {
                    if(i + 1 < args.Length) {
                        return args[i + 1];
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// BuildBySetting() provide for batch mode build!!
        /// </summary>
        public static void BuildBySetting() {
            try {
                string prev_setting = GetCurrentSettingPath();
                UCL_BuildSetting setting = null;
                const string BuildSettingKey = "-buildsetting";
                string setting_name = GetArg(BuildSettingKey);
                if(!string.IsNullOrEmpty(setting_name)) {
                    setting = GetSetting(setting_name);
                    Debug.LogWarning("BuildBySetting():" + setting_name);
                    if(setting == null) {
                        Debug.LogError("BuildBySetting GetSetting Fail:" + setting_name);
                    }
                }
                if(setting == null) {
                    setting = GetDefaultSetting();
                }
                if(setting == null) return;//No Default Setting!!

                setting.Build();

                if(!string.IsNullOrEmpty(prev_setting)) {//reset to prev setting
                    GetSettingByPath(prev_setting)?.ApplySetting();
                }
            }catch(Exception e) {
                Debug.LogError("BuildBySetting() Exception:" + e);
            }

        }
        protected void ThreadedLog(string iMessage, string iStackTrace = "", LogType iType = LogType.Log)
        {
            if(m_LogStringBuilder == null)
            {
                return;
            }
            lock (m_LogStringBuilder)
            {
                m_LogStringBuilder.AppendLine(iMessage);
            }
        }
        void PerformBuild(string path) {
            if (m_OutputBuildLog)
            {
                m_LogStringBuilder = new System.Text.StringBuilder();
                Application.logMessageReceivedThreaded += ThreadedLog;
            }
            Debug.LogWarning("PerformBuild path:" + path);
            Debug.LogWarning("PerformBuild target:" + m_BuildTarget.ToString());

            string build_path = Application.dataPath.Replace("Assets", path);
            if(UnityEditorInternal.InternalEditorUtility.inBatchMode) {
                Debug.LogWarning("UnityEditorInternal.InternalEditorUtility.inBatchMode == true");
                string str = GetArg("-output");
                Debug.LogWarning("-output:"+ str);
                if(!string.IsNullOrEmpty(str)) {
                    build_path = str;
                    Debug.LogWarning("Set build_path:"+ str);
                } else {
                    Debug.LogWarning("-output string.IsNullOrEmpty!!");
                }
            }
            if(build_path.Length > 0) {
                char aLastChar = build_path[build_path.Length - 1];
                if(aLastChar != '\\' && aLastChar != '/') {
                    build_path += '/';
                }
            }
            string output_path = build_path + m_OutputName;
            Debug.LogWarning("PerformBuild output_path:" + output_path);
            Core.FileLib.Lib.CreateDirectory(build_path);
            var aResult = BuildPipeline.BuildPlayer(GetScenesPath(), output_path, m_BuildTarget , m_BuildOption);
            if (m_OutputBuildLog)
            {
                System.IO.File.WriteAllText(build_path + "BuildReport.txt", aResult.summary.UCL_ToString());
                Application.logMessageReceivedThreaded -= ThreadedLog;
                if (m_LogStringBuilder != null)
                {
                    System.IO.File.WriteAllText(build_path + "BuildLog.txt", m_LogStringBuilder.ToString());
                }
            }
#if UNITY_EDITOR_WIN
            Core.FileLib.WindowsLib.OpenExplorer(build_path.RemoveLast());
#endif
        }
        public string GetScenePath(string scene_name) {
            string scene_path = "";
#if UNITY_EDITOR
            for(int i = 0; i < m_ScenesInBuild.Length; i++) {
                var scene = m_ScenesInBuild[i];
                if(scene.name == scene_name) {
                    scene_path = AssetDatabase.GetAssetPath(scene.GetInstanceID());
                }
            }
#endif
            if(string.IsNullOrEmpty(scene_path)) {//m_ScenesInBuild.Length == 0
                scene_path = UCL.SceneLib.Lib.GetScenePath(scene_name);
            }
            return scene_path;
        }
        public string[] GetScenesName() {
            string[] ScenesName = null;

            UnityEngine.Object[] ScenesInBuild = m_ScenesInBuild;
            if((ScenesInBuild == null || ScenesInBuild.Length == 0) && m_DefaultSetting != null) ScenesInBuild = m_DefaultSetting.m_ScenesInBuild;

            if(ScenesInBuild != null && ScenesInBuild.Length > 0) {
                ScenesName = new string[ScenesInBuild.Length];
                List<string> ScenesNameList = new List<string>();
                for(int i = 0; i < ScenesInBuild.Length; i++) {
                    var scene = ScenesInBuild[i];
                    ScenesName[i] = scene.name;
                }
            } else {
                ScenesName = UCL.SceneLib.Lib.GetScenesName();
            }

            return ScenesName;
        }
        public string[] GetScenesPath() {
            string[] ScenesPath = null;
            UnityEngine.Object[] ScenesInBuild = m_ScenesInBuild;
            if((ScenesInBuild == null ||ScenesInBuild.Length == 0) && m_DefaultSetting != null) ScenesInBuild = m_DefaultSetting.m_ScenesInBuild;
            if(ScenesInBuild != null && ScenesInBuild.Length > 0) {
                List<string> ScenesPathList = new List<string>();
                for(int i = 0; i < ScenesInBuild.Length; i++) {
                    var scene = ScenesInBuild[i];
                    ScenesPathList.Add(AssetDatabase.GetAssetPath(scene.GetInstanceID()));
                }
                ScenesPath = ScenesPathList.ToArray();
            } else {
                ScenesPath = UCL.SceneLib.Lib.GetAcitveScenesPath();
            }
            for(int i = 0; i < ScenesPath.Length; i++) {
                var scene = ScenesPath[i];
                //Debug.LogWarning("Path:" + scene);
            }
            return ScenesPath;
        }
        public void Build() {
            ApplySetting();
            PerformBuild(m_OutputPath);
        }
        void ApplyDefaultSetting() {
            PlayerSettings.productName = m_ProductName;
            PlayerSettings.Android.keystoreName = Application.dataPath.Replace("Assets",m_KeyStoreSetting.m_KeystoreName);

            PlayerSettings.keystorePass = m_KeyStoreSetting.m_KeystorePass;
            PlayerSettings.Android.keyaliasName = m_KeyStoreSetting.m_KeyaliasName;
            PlayerSettings.keyaliasPass = m_KeyStoreSetting.m_KeyaliasPass;
            EditorUserBuildSettings.buildAppBundle = m_BuildAppBundle;

            if(!string.IsNullOrEmpty(m_ApplicationIdentifier)) {
                PlayerSettings.SetApplicationIdentifier(m_BuildTargetGroup, m_ApplicationIdentifier);
            }
            /*
            if(PlayerSettings.GetScriptingDefineSymbolsForGroup(m_BuildTargetGroup) != m_ScriptingDefineSymbols) {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(m_BuildTargetGroup, m_ScriptingDefineSymbols);
            }
            */
        }
        protected void SetIcon(Texture2D icon) {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown , new Texture2D[] { icon });
        }
        protected void SetIcons(Texture2D[] icons) {
            if(icons == null || icons.Length == 0) return;
            int[] iconSizes = PlayerSettings.GetIconSizesForTargetGroup(m_BuildTargetGroup);
            Texture2D[] tmp_icon = new Texture2D[iconSizes.Length];
            for(int i = 0; i < tmp_icon.Length; i++) {
                if(i < icons.Length) {
                    tmp_icon[i] = icons[i];
                } else {
                    tmp_icon[i] = icons[icons.Length - 1];
                }
            }
            PlayerSettings.SetIconsForTargetGroup(m_BuildTargetGroup, tmp_icon);
            //PlayerSettings.SetPlatformIcons(m_BuildTargetGroup, m_Icons);
        }
        [Core.ATTR.UCL_FunctionButton]
        public void LoadCurrentKeystore() {
            m_KeyStoreSetting.m_KeystoreName = PlayerSettings.Android.keystoreName;
            m_KeyStoreSetting.m_KeystorePass = PlayerSettings.keystorePass;
            m_KeyStoreSetting.m_KeyaliasName = PlayerSettings.Android.keyaliasName;
            m_KeyStoreSetting.m_KeyaliasPass = PlayerSettings.keyaliasPass;
            EditorUtility.SetDirty(this);
        }
        [Core.ATTR.UCL_FunctionButton]
        public void UpdateVersion() {
            m_VersionSetting.UpdateVersion();
            m_VersionSetting.ApplySetting();
        }
        [Core.ATTR.UCL_FunctionButton]
        public void LoadVersionSetting() {
            m_VersionSetting.LoadCurrentSetting();
        }
        [Core.ATTR.UCL_FunctionButton]
        public void ApplyVersionSetting() {
            m_VersionSetting.ApplySetting();
        }
        public void LoadCurrentSetting() {
            //EditorBuildSettings.AddConfigObject
            //https://docs.unity3d.com/ScriptReference/EditorBuildSettings.AddConfigObject.html

            m_BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            m_BuildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_BuildTarget);

            m_ProductName = PlayerSettings.productName;

            LoadCurrentKeystore();

            m_BuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            
            m_ScriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(m_BuildTargetGroup);
            m_ApplicationIdentifier = PlayerSettings.applicationIdentifier;

            /*
                        m_BuildOption = 0;
            if(EditorUserBuildSettings.allowDebugging) m_BuildOption |= BuildOptions.AllowDebugging;

            if(EditorUserBuildSettings.development) m_BuildOption |= BuildOptions.Development;
            if(EditorUserBuildSettings.waitForPlayerConnection) m_BuildOption |= BuildOptions.WaitForPlayerConnection;
            if(EditorUserBuildSettings.buildScriptsOnly) m_BuildOption |= BuildOptions.BuildScriptsOnly;
            */
            var player_options = GetBuildPlayerOptions();
            //m_BuildTarget = player_options.target;

            try {
                string path = Application.dataPath.Replace("Assets", string.Empty);
                Debug.LogWarning("Application.dataPath:" + path);
                m_OutputPath = Core.FileLib.Lib.GetFolderPath(player_options.locationPathName.Replace(path, ""));
            } catch(Exception e) {
                Debug.LogError("LoadCurrentSetting() Exception:" + e);
            }
            
            m_BuildOption = player_options.options;
            //m_BuildOption = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(defaultOptions).options;
            m_Icons = PlayerSettings.GetIconsForTargetGroup(m_BuildTargetGroup);
            var default_Icon = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            if(default_Icon != null && default_Icon.Length > 0) {
                m_DefaultIcon = default_Icon[0];
            } else {
                m_DefaultIcon = null;
            }
            EditorUtility.SetDirty(this);
        }
        static public BuildPlayerOptions GetBuildPlayerOptions() {
            try {
                MethodInfo method = typeof(BuildPlayerWindow.DefaultBuildMethods).GetMethod("GetBuildPlayerOptionsInternal",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if(method != null) {
                    BuildPlayerOptions defaultOptions = new BuildPlayerOptions();
                    bool askForLocation = false;
                    var option = (BuildPlayerOptions)(method.Invoke(null, new object[] { askForLocation, defaultOptions }));
                    return option;
                } else {
                    Debug.LogError("UCL_BuildSetting method GetBuildPlayerOptionsInternal not Exist!!");
                }
            } catch(Exception e) {
                Debug.LogError("UCL_BuildSetting GetBuildPlayerOptions() Exception:" + e);
            }
            return default;
        }
        public void ApplyScenesInBuildSetting() {
            UCL_BuildSetting default_setting = m_DefaultSetting;
            if(default_setting == null) {
                default_setting = GetDefaultSetting();
            }
            if(default_setting != null && default_setting != this) {//Load default setting first!!
                default_setting.ApplyScenesInBuildSetting();
            }
            if(m_ScenesInBuild != null && m_ScenesInBuild.Length > 0) {
                EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[m_ScenesInBuild.Length];
                for(int i = 0; i < scenes.Length; i++) {
                    var scene = new EditorBuildSettingsScene(AssetDatabase.GetAssetPath(m_ScenesInBuild[i].GetInstanceID()), true);
                    scenes[i] = scene;
                }
                EditorBuildSettings.scenes = scenes;
            }
        }
        public void ApplySetting() {
            Debug.Log("SetBuildSetting:" + name +",Prev:"+ GetCurrentSettingPath());
            UCL_BuildSetting default_setting = m_DefaultSetting;
            if(default_setting == null) {
                default_setting = GetDefaultSetting();
            }
            if(default_setting != null && default_setting != this) {//Load default setting first!!
                default_setting.ApplyDefaultSetting();
            }
            ApplyScenesInBuildSetting();
            EditorUserBuildSettings.SwitchActiveBuildTarget(m_BuildTargetGroup, m_BuildTarget);
            if(!string.IsNullOrEmpty(m_ApplicationIdentifier)) {
                PlayerSettings.SetApplicationIdentifier(m_BuildTargetGroup, m_ApplicationIdentifier);
            }
            
            if(!string.IsNullOrEmpty(m_ProductName)) {
                PlayerSettings.productName = m_ProductName;
            }
            if(!string.IsNullOrEmpty(m_KeyStoreSetting.m_KeystoreName)) {
                PlayerSettings.Android.keystoreName = Application.dataPath.Replace("Assets", m_KeyStoreSetting.m_KeystoreName);
            }
            if(!string.IsNullOrEmpty(m_KeyStoreSetting.m_KeystorePass)) {
                PlayerSettings.keystorePass = m_KeyStoreSetting.m_KeystorePass;
            }
            if(!string.IsNullOrEmpty(m_KeyStoreSetting.m_KeyaliasName)) {
                PlayerSettings.Android.keyaliasName = m_KeyStoreSetting.m_KeyaliasName;
            }
            if(!string.IsNullOrEmpty(m_KeyStoreSetting.m_KeyaliasPass)) {
                PlayerSettings.keyaliasPass = m_KeyStoreSetting.m_KeyaliasPass;
            }

            if(m_Icons != null && m_Icons.Length > 0) {
                SetIcons(m_Icons);
            } else if(default_setting != null) {
                SetIcons(default_setting.m_Icons);
            }
            if(m_DefaultIcon != null) {
                SetIcon(m_DefaultIcon);
            } else if(default_setting != null) {
                SetIcon(default_setting.m_DefaultIcon);
            }
            if(PlayerSettings.GetScriptingDefineSymbolsForGroup(m_BuildTargetGroup) != m_ScriptingDefineSymbols) {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(m_BuildTargetGroup, m_ScriptingDefineSymbols);
            }

            EditorUserBuildSettings.buildAppBundle = m_BuildAppBundle;
            SetCurrentSettingPath(AssetDatabase.GetAssetPath(this));
            AssetDatabase.SaveAssets();
            
        }
#endif
        }
    }