using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
        [UCL.Core.PA.UCL_ButtonProperty("ApplySetting")] public bool m_ApplySetting;

        /// <summary>
        /// Create a Button invoke Build()
        /// </summary>
        [UCL.Core.PA.UCL_ButtonProperty("Build")] public bool m_Build;
        #endregion

        /// <summary>
        /// if(m_ProductName == "") PlayerSettings.productName will use setting in DefaultBuildSetting
        /// </summary>
        public string m_ProductName = "";

        public BuildTargetGroup m_BuildTargetGroup = BuildTargetGroup.Standalone;
        public BuildTarget m_BuildTarget = BuildTarget.StandaloneWindows64;
        public BuildOptions m_BuildOption = BuildOptions.None;
        public bool m_BuildAppBundle = false;
        public string m_OutputPath = "";
        #region KeyStore
        [Header("Key Store")]
        public string m_KeystoreName = "";
        public string m_KeystorePass = "";
        public string m_KeyaliasName = "";
        public string m_KeyaliasPass = "";
        #endregion
        [Header("Define Symbols not apply DefaultBuildSetting")]
        public string m_ScriptingDefineSymbols = "";
        //public string m_CurrentSetting = "";

        public static UCL_BuildSetting GetSetting(string path) {
            return Resources.Load<UCL_BuildSetting>(path);
        }
        public static UCL_BuildSetting GetDefaultSetting() {
            return GetSetting("DefaultBuildSetting");
        }
        public static string GetCurrentSettingName() {
            string str = PlayerPrefs.GetString("Current_UCL_BuildSetting");
            Debug.LogWarning("GetCurrentSettingName:" + str);
            return str;
        }
        protected static void SetCurrentSettingName(string name) {
            PlayerPrefs.SetString("Current_UCL_BuildSetting", name);
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
                string prev_setting = GetCurrentSettingName();
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
                    GetSetting(prev_setting)?.ApplySetting();
                }
            }catch(Exception e) {
                Debug.LogError("BuildBySetting() Exception:" + e);
            }

        }


        void PerformBuild(string path) {
            Debug.LogWarning("PerformBuild path:" + path);
            Debug.LogWarning("PerformBuild target:" + m_BuildTarget.ToString());

            var res = BuildPipeline.BuildPlayer(UCL.SceneLib.Lib.GetAcitveScenesPath(), Application.dataPath.Replace("Assets", path), m_BuildTarget , m_BuildOption);
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
        public void ApplySetting() {
            Debug.Log("SetBuildSetting:" + name);
            var default_setting = GetDefaultSetting();
            
            if(default_setting != this) {//Load default setting first!!
                default_setting.ApplyDefaultSetting();
            } else {//this is default setting!! Init build setting!!
                //PlayerSettings.productName = m_ProductName;
                //return;
            }
            SetCurrentSettingName(name);
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

            if(PlayerSettings.GetScriptingDefineSymbolsForGroup(m_BuildTargetGroup) != m_ScriptingDefineSymbols) {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(m_BuildTargetGroup, m_ScriptingDefineSymbols);
            }

            EditorUserBuildSettings.buildAppBundle = m_BuildAppBundle;


            AssetDatabase.SaveAssets();
        }
#endif
    }
}