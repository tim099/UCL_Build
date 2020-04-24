using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace UCL.BuildLib {
    [CreateAssetMenu(fileName = "New BuildSetting", menuName = "UCL/BuildSetting")]
    public class UCL_BuildSetting : ScriptableObject {
#if UNITY_EDITOR
        #region InspectorButton
        /// <summary>
        /// Create a Button invoke SetSetting()
        /// </summary>
        [Header("Apply BuildSetting to EditorBuildSetting")]
        [UCL.Core.PA.UCL_ButtonProperty("ApplySetting")] public bool m_ApplySetting;

        /// <summary>
        /// Create a Button invoke LoadCurrentSetting()
        /// </summary>
        [Header("Load current EditorBuildSetting into BuildSetting")]
        [UCL.Core.PA.UCL_ButtonProperty("LoadCurrentSetting")] public bool m_LoadCurrentSetting;
        /// <summary>
        /// Create a Button invoke Build()
        /// </summary>
        [Header("Apply BuildSetting to EditorBuildSetting and Build")]
        [UCL.Core.PA.UCL_ButtonProperty("Build")] public bool m_Build;

        [Space(10)]
        #endregion

        /*
        //[Flags]
        public enum MyEnum {
            Foo = 1<<1,
            Bar = 1<<2,

            Baz = 1<<4,
            QAQ = 1<<7
        }

        [UCL.Core.PA.UCL_EnumMaskProperty] public MyEnum m_Test;
        */

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


        public BuildTargetGroup m_BuildTargetGroup = BuildTargetGroup.Standalone;
        public BuildTarget m_BuildTarget = BuildTarget.StandaloneWindows64;
        [UCL.Core.PA.UCL_EnumMaskProperty] public BuildOptions m_BuildOption = BuildOptions.None;
        public Texture2D[] m_Icons;
        public bool m_BuildAppBundle = false;
        public string m_OutputPath = "";

        [Header("Use Editor Setting if m_ScenesInBuild is Empty.")]
        public UnityEngine.Object[] m_ScenesInBuild;

        #region KeyStore
        [Header("Key Store")]
        public string m_KeystoreName = "";
        public string m_KeystorePass = "";
        public string m_KeyaliasName = "";
        public string m_KeyaliasPass = "";
        #endregion
        [Header("Define Symbols not apply DefaultBuildSetting")]
        public string m_ScriptingDefineSymbols = "";

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


        void PerformBuild(string path) {
            Debug.LogWarning("PerformBuild path:" + path);
            Debug.LogWarning("PerformBuild target:" + m_BuildTarget.ToString());

            var res = BuildPipeline.BuildPlayer(GetScenesPath(), Application.dataPath.Replace("Assets", path), m_BuildTarget , m_BuildOption);
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
            }else {
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
            PlayerSettings.Android.keystoreName = Application.dataPath.Replace("Assets", m_KeystoreName);

            PlayerSettings.keystorePass = m_KeystorePass;
            PlayerSettings.Android.keyaliasName = m_KeyaliasName;
            PlayerSettings.keyaliasPass = m_KeyaliasPass;
            EditorUserBuildSettings.buildAppBundle = m_BuildAppBundle;
            /*
            if(PlayerSettings.GetScriptingDefineSymbolsForGroup(m_BuildTargetGroup) != m_ScriptingDefineSymbols) {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(m_BuildTargetGroup, m_ScriptingDefineSymbols);
            }
            */
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
        public void LoadCurrentSetting() {
            m_BuildTarget = EditorUserBuildSettings.activeBuildTarget;
            m_BuildTargetGroup = BuildPipeline.GetBuildTargetGroup(m_BuildTarget);

            m_ProductName = PlayerSettings.productName;

            m_KeystoreName = PlayerSettings.Android.keystoreName;
            m_KeystorePass = PlayerSettings.keystorePass;
            m_KeyaliasName = PlayerSettings.Android.keyaliasName;
            m_KeyaliasPass = PlayerSettings.keyaliasPass;

            m_BuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            
            m_ScriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(m_BuildTargetGroup);

            
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
                string path = Application.dataPath.Replace("Assets", "");
                Debug.LogWarning("Application.dataPath:" + path);
                m_OutputPath = player_options.locationPathName.Replace(path, "");
            } catch(Exception e) {
                Debug.LogError("LoadCurrentSetting() Exception:" + e);
            }
            
            m_BuildOption = player_options.options;
            //m_BuildOption = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(defaultOptions).options;
            m_Icons = PlayerSettings.GetIconsForTargetGroup(m_BuildTargetGroup);
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
        public void ApplySetting() {
            Debug.Log("SetBuildSetting:" + name +",Prev:"+ GetCurrentSettingPath());
            UCL_BuildSetting default_setting = m_DefaultSetting;
            if(default_setting == null) {
                default_setting = GetDefaultSetting();
            }
            if(default_setting!=null && default_setting != this) {//Load default setting first!!
                default_setting.ApplyDefaultSetting();
            }
            EditorUserBuildSettings.SwitchActiveBuildTarget(m_BuildTargetGroup, m_BuildTarget);
            
            if(!string.IsNullOrEmpty(m_ProductName)) {
                PlayerSettings.productName = m_ProductName;
            }
            if(!string.IsNullOrEmpty(m_KeystoreName)) {
                PlayerSettings.Android.keystoreName = Application.dataPath.Replace("Assets", m_KeystoreName);
            }
            if(!string.IsNullOrEmpty(m_KeystorePass)) {
                PlayerSettings.keystorePass = m_KeystorePass;
            }
            if(!string.IsNullOrEmpty(m_KeyaliasName)) {
                PlayerSettings.Android.keyaliasName = m_KeyaliasName;
            }
            if(!string.IsNullOrEmpty(m_KeyaliasPass)) {
                PlayerSettings.keyaliasPass = m_KeyaliasPass;
            }

            if(m_Icons != null && m_Icons.Length > 0) {
                SetIcons(m_Icons);
            } else if(default_setting != null) {
                SetIcons(default_setting.m_Icons);
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