using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UCL.Core;
using UnityEngine;


namespace UCL.BuildLib
{
    public class BuildData
    {
        public BuildData() { }
        public BuildData(string iOutputPath)
        {
            m_OutputPath = iOutputPath;
        }
        public string m_OutputPath;
    }
    [Obsolete("Please use UCL_PreBuildSetting instead")]
    public class UCL_PreBuildProcess : MonoBehaviour
    {
        virtual public void OnBuild(BuildData iOutputPath, System.Action iEndAct)
        {

        }
    }
}