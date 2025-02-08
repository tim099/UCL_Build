
// RCG_AutoHeader
// to change the auto header please go to RCG_AutoHeader.cs
// Create time : 02/08 2025
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UCL.Core;
using UnityEngine;


namespace UCL.BuildLib
{
    [UCL.Core.ATTR.EnableUCLEditor]
    public class UCL_BashBuildSetting : UCL_PreBuildSetting
    {
        public string m_FileName = "cmd.exe";//"/bin/bash";
        public string m_Arguments = "/c echo Hello, World!";//"-c echo Hello, World!";
        public bool m_CreateNoWindow = true;
        public bool m_UseShellExecute = false;

        [UCL.Core.PA.Conditional(nameof(m_UseShellExecute), false, false)] public bool m_RedirectStandardOutput = true;
        [UCL.Core.PA.Conditional(nameof(m_UseShellExecute), false, false)] public bool m_RedirectStandardError = true;
        override public async UniTask OnBuild(BuildData iBuildData)
        {
            await RunCommand();
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
            Debug.LogError($"RunCommand m_FileName:{m_FileName}, m_Arguments:{m_Arguments}");
            await UniTask.SwitchToThreadPool();
            //var tcs = new UniTaskCompletionSource<(object sender, EventArgs args)>();
            System.Diagnostics.Process process = new();
            process.StartInfo.FileName = m_FileName;
            process.StartInfo.Arguments = m_Arguments;
            process.StartInfo.CreateNoWindow = m_CreateNoWindow;
            process.StartInfo.UseShellExecute = m_UseShellExecute;
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
            Debug.LogError($"process.Start()");
            process.WaitForExit();
            Debug.LogError($"process.WaitForExit");
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
