
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
    public interface UCLI_PreBuild
    {
        UniTask OnBuild(BuildData iBuildData);
    }

    [UCL.Core.ATTR.UCL_IgnoreInTypeListable]
    public class UCL_PreBuildSetting : UCLI_PreBuild, UCLI_TypeListable
    {
        virtual public UniTask OnBuild(BuildData iBuildData)
        {
            //var aAllTypes = typeof(UCL_PreBuildSetting).GetAllITypesAssignableFrom();
            //UCLI_TypeList
            throw new NotImplementedException();
        }
    }
}
