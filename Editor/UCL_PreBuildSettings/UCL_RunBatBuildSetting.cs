
// RCG_AutoHeader
// to change the auto header please go to RCG_AutoHeader.cs
// Create time : 02/15 2025
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UCL.Core;
using UnityEngine;


namespace UCL.BuildLib
{
    [UCL.Core.ATTR.EnableUCLEditor]
    public class UCL_RunBatBuildSetting : UCL_PreBuildSetting
    {
        public enum EPathConfig
        {
            /// <summary>
            /// System.IO.Directory.GetParent(Application.dataPath).FullName;
            /// </summary>
            ParentOfDataPath,
            /// <summary>
            /// System.IO.Directory.GetParent(Application.dataPath).Parent.FullName;
            /// </summary>
            ParentOfParentOfDataPath,
        }

        public EPathConfig m_PathConfig = EPathConfig.ParentOfDataPath;
        public string m_FolderPath = "";
        public string m_FileName = "FileName.bat";

        public bool m_CreateNoWindow = true;
        public bool m_UseShellExecute = false;

        [UCL.Core.PA.Conditional(nameof(m_UseShellExecute), false, false)] public bool m_RedirectStandardOutput = true;
        [UCL.Core.PA.Conditional(nameof(m_UseShellExecute), false, false)] public bool m_RedirectStandardError = true;
        override public async UniTask OnBuild(BuildData iBuildData)
        {
            await RunCommand();
        }
        public string GetPath()
        {
            switch (m_PathConfig)
            {
                case EPathConfig.ParentOfDataPath:
                    {
                        return System.IO.Directory.GetParent(Application.dataPath).FullName;
                    }
                case EPathConfig.ParentOfParentOfDataPath:
                    {
                        return System.IO.Directory.GetParent(Application.dataPath).Parent.FullName;
                    }
            }
            return string.Empty;
        }
        [UCL.Core.ATTR.UCL_FunctionButton]
        public void RunScript()
        {
            Debug.LogError($"RunScript");
            RunCommand().Forget();

            //Thread newThread = new Thread(new ThreadStart(RunCommand));
            //newThread.Start();
        }



        private async UniTask RunCommand()
        {
            await UniTask.SwitchToThreadPool();
            var dir = GetPath();
            var folderPath = Path.Combine(dir, m_FolderPath);
            var path = Path.Combine(folderPath, m_FileName);

            Debug.Log($"dir:{dir}, folderPath:{folderPath},path:{path}");

            System.Diagnostics.ProcessStartInfo processStartInfo = new(path)
            {
                CreateNoWindow = m_CreateNoWindow,
                UseShellExecute = m_UseShellExecute,
                WorkingDirectory = folderPath,
            };
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(processStartInfo);


            if (!m_UseShellExecute)
            {
                process.StartInfo.RedirectStandardOutput = m_RedirectStandardOutput;
                process.StartInfo.RedirectStandardError = m_RedirectStandardError;
            }

            //process.Exited += (sender, args) =>
            //{
            //    tcs.TrySetResult((sender, args));
            //};
            process.Start();
            Debug.Log($"process.Start()");
            process.WaitForExit();
            Debug.Log($"process.WaitForExit");
            await UniTask.SwitchToMainThread();

            //var result = await tcs.Task;
            //Debug.LogError($"await tcs.Task");
            if (!m_UseShellExecute)
            {
                if (m_RedirectStandardOutput)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    UnityEngine.Debug.Log("Output: " + output);
                }
                if (m_RedirectStandardError)
                {
                    string error = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        UnityEngine.Debug.LogError(error);
                    }
                }
            }


            process.Close();
            process.Dispose();


        }
    }
}
