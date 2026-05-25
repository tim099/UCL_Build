
// RCG_AutoHeader
// to change the auto header please go to RCG_AutoHeader.cs
// Create time : 05/25 2026
// 區塊職責：本檔提供「從 Agent Command 觸發完整 Player Build」的 Cmd，讓 agent (跑 run_cmd.py) 也能跑真正的 player build，
//          不必每次都靠 Tim 手動點 UCL_BuildAsset 的 Build 按鈕。
// 物理意義：載入指定的 UCL_BuildAsset (專案 build 設定/pipeline)，呼叫其 BuildAsync — 走 profile 切換 + Pre/PostBuildProcess
//          (含 addressable / module build 等 IPreBuildSetting) + BuildPipeline.BuildPlayer，產出實際可執行檔。
// 數值影響：實際打 player build 寫入磁碟 (耗時數十秒~分鐘)；本 Cmd 另把 BuildReport.summary 摘要寫一份 md 報告 + Debug.Log，
//          build 失敗 (result != Succeeded 或 report == null) 則 throw 讓 run_cmd 顯示失敗。
// 設計取捨：
//   - 參考 Cmd_BuildAddressable / Cmd_DiagnoseAssetReflection 的 handler 樣板 (per Tim task)。
//   - 重用既有 UCL_BuildAsset.BuildAsync 而非自己裸呼叫 BuildPipeline — 才能套到專案的 profile + pre/post 流程 (含 addressables)。
//   - 為此把 UCL_BuildAsset.BuildAsync 從 protected void 改為 public 回傳 BuildReport，讓 Cmd 取得成功/失敗 (行為不變)。
//   - 放置於 UCL_Build/Editor (UCL_BuildEditor.asmdef)：該 asmdef 已引用 UCL_Core (handler base) + Editor-only，
//     且 UCL_BuildAsset 同 assembly。Cmd 自動發現走 GetAllSubclass→GetAllTypes (掃全 assembly) 故會被註冊。
// ship 2026-05-25 gura (Tim task: 搜尋有無 Build CMD，沒有則加一個並測 build)
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UCL.Core;
using UCL.Core.EditorLib;
using UCL.Core.EditorLib.AgentCommands;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UCL.BuildLib
{
    /// <summary>
    /// Agent Command：觸發完整 Player Build (透過 UCL_BuildAsset.BuildAsync)。
    ///
    /// 讓 agent 透過 run_cmd.py 跑真正的 player build (重現 Tim 手動按 Build 的同一條 pipeline)，
    /// 含 profile 切換 + Pre/PostBuildProcess (addressable / module 等) + BuildPipeline.BuildPlayer。
    ///
    /// 參數：
    /// - <c>buildAsset</c>（選填，預設 "Default"）：要用哪個 UCL_BuildAsset 設定。
    /// - <c>output</c>（選填）：輸出資料夾 (相對專案根)；缺則用該 build asset 的 m_OutputPath。
    /// </summary>
    public class Cmd_Build : UCL_AgentCommandHandlerBase
    {
        public override string CommandType => "Build";

        public override string ShortDescription =>
            "Run a full Player build via UCL_BuildAsset (profile + Pre/PostBuildProcess + BuildPipeline.BuildPlayer).";

        public override string ArgsSchema =>
            "buildAsset=UCL_BuildAsset id (default Default)\n" +
            "output=輸出資料夾 (相對專案根；缺則用該 build asset 的 m_OutputPath)\n" +
            "runPostBuild=true|false (default false — 測試 build 預設跳過 PostBuildProcess，例如 Steam VDF 上傳；要連上傳一起跑才設 true)";

        /// <summary>Page「Fill Example」一鍵填入用。</summary>
        public override string ExampleArgs => "buildAsset=Default";

        public override string HelpURL =>
            "ucl_core:Docs~/{lang}/API/UCL_AgentCommand/Cmd_Build.md";

        // 專案根路徑 (與其他 Cmd 一致，用於解析相對輸出路徑)
        private static string ProjectRoot => UCL_RepoPath.UnityProjectRoot;

        public override async UniTask ExecuteAsync(Dictionary<string, string> args, CancellationToken token)
        {
            // 讓 Cmd 切到下一影格再跑
            await UniTask.Yield();

            // 區塊職責：解析參數
            // 物理意義：buildAsset 決定用哪份 build 設定；output 覆寫輸出資料夾
            // 數值影響：影響實際 build 行為與輸出位置
            string buildAssetId = GetArg(args, "buildAsset", UCL_BuildEntry.DefaultID);
            string output = GetArg(args, "output", null);
            // 測試 build 預設跳過 PostBuildProcess (Steam VDF 上傳等)；要連上傳才設 runPostBuild=true
            bool runPostBuild = string.Equals(GetArg(args, "runPostBuild", "false"), "true", StringComparison.OrdinalIgnoreCase);

            // 區塊職責：確保 ModuleService 初始化完成 (build asset 是 UCL_Asset，需 module 載入後才讀得到)
            // 物理意義：對齊 BuildInBatchMode 的前置等待
            await UCL_ModuleService.WaitUntilInitialized(token);

            // 區塊職責：載入 build asset
            // 物理意義：UCL_BuildAsset.Util.GetData(id) 取得指定設定；null → fail-loud
            // 數值影響：缺設定直接 throw，避免後續 NRE
            var asset = UCL_BuildAsset.Util.GetData(buildAssetId);
            if (asset == null)
            {
                throw new Exception($"[Cmd:Build] UCL_BuildAsset id '{buildAssetId}' 不存在 — 確認 Core module 內有此 build 設定 (e.g. Default / Development / Demo)。");
            }

            string usedOutput = string.IsNullOrEmpty(output) ? asset.m_OutputPath : output;
            Debug.Log($"[Cmd:Build] 開始 build, buildAsset:{buildAssetId}, output:{usedOutput}, runPostBuild:{runPostBuild}");

            // 區塊職責：實際 build
            // 物理意義：BuildAsync 走完整 pipeline；回傳 BuildReport (null = 流程中途拋例外被吞 / profile 缺)
            // 數值影響：report.summary.result 決定成功失敗
            BuildReport aReport = null;
            string aExceptionDump = null;
            System.DateTime aStart = System.DateTime.Now;
            try
            {
                aReport = await asset.BuildAsync(usedOutput, runPostBuild);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                aExceptionDump = $"{ex.GetType().Name}: {ex.Message}\n{HeadStack(ex)}";
                Debug.LogException(ex);
            }
            double aElapsed = (System.DateTime.Now - aStart).TotalSeconds;

            // 區塊職責：判定成功與否
            // 物理意義：report 為 null (中途拋例外/profile 缺) 或 result != Succeeded → 失敗
            // 數值影響：決定報告內容 + 是否 throw
            bool aSucceeded = aReport != null && aReport.summary.result == BuildResult.Succeeded;

            // 區塊職責：寫報告
            string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputPath = $"AgentCommands/build_{ts}.md";
            string absOut = Path.IsPathRooted(outputPath) ? outputPath : Path.Combine(ProjectRoot, outputPath);
            string outDir = Path.GetDirectoryName(absOut);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }
            string content = RenderMarkdown(aSucceeded, buildAssetId, usedOutput, aReport, aExceptionDump, aElapsed);
            File.WriteAllText(absOut, content, new UTF8Encoding(false));

            // 區塊職責：終局 log + (失敗時) throw
            // 物理意義：成功印 summary；失敗 throw 讓 run_cmd 顯示失敗 (對齊跨層次驗證：不只信 stdout)
            // 數值影響：throw 會被 runner 標記此 Cmd 失敗
            if (!aSucceeded)
            {
                string aReason = aExceptionDump
                    ?? (aReport != null ? $"result={aReport.summary.result}, errors={aReport.summary.totalErrors}" : "BuildReport == null (流程中途中斷)");
                Debug.LogError($"[Cmd:Build] FAILED ({aElapsed:0.0}s) → {outputPath}\n{aReason}");
                throw new Exception($"[Cmd:Build] Player build FAILED: {aReason}");
            }

            Debug.Log($"[Cmd:Build] OK ({aElapsed:0.0}s) → {outputPath}");
            await UniTask.CompletedTask;
        }

        // 區塊職責：把 build 結果渲染成 markdown 報告
        // 物理意義：成功/失敗 + 關鍵數據 (result/errors/warnings/size/time/output)
        // 數值影響：純輸出
        private static string RenderMarkdown(bool iSucceeded, string iBuildAssetId, string iOutput,
                                             BuildReport iReport, string iExceptionDump, double iElapsed)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Player Build Result");
            sb.AppendLine();
            sb.AppendLine($"- **Generated**: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- **Result**: {(iSucceeded ? "✅ SUCCESS" : "❌ FAILED")}");
            sb.AppendLine($"- **BuildAsset**: `{iBuildAssetId}`");
            sb.AppendLine($"- **Output**: `{iOutput}`");
            sb.AppendLine($"- **Elapsed (Cmd 量測)**: {iElapsed:0.0}s");
            if (iReport != null)
            {
                var s = iReport.summary;
                sb.AppendLine($"- **BuildResult**: {s.result}");
                sb.AppendLine($"- **Platform**: {s.platform}");
                sb.AppendLine($"- **Total errors**: {s.totalErrors}");
                sb.AppendLine($"- **Total warnings**: {s.totalWarnings}");
                sb.AppendLine($"- **Total time**: {s.totalTime}");
                sb.AppendLine($"- **Total size**: {s.totalSize} bytes");
                sb.AppendLine($"- **Output path**: `{s.outputPath}`");
            }
            else
            {
                sb.AppendLine("- **BuildReport**: null (流程中途中斷 — 見下方 exception 或 profile 設定)");
            }
            sb.AppendLine();

            if (!iSucceeded && !string.IsNullOrEmpty(iExceptionDump))
            {
                sb.AppendLine("## Exception");
                sb.AppendLine();
                sb.AppendLine("```");
                sb.AppendLine(iExceptionDump);
                sb.AppendLine("```");
            }
            else if (iSucceeded)
            {
                sb.AppendLine("> Player build succeeded. ✅");
            }
            return sb.ToString();
        }

        // 取例外 stack 前 8 行
        private static string HeadStack(Exception ex)
        {
            string s = ex.StackTrace ?? "";
            var lines = s.Split('\n');
            int n = Math.Min(8, lines.Length);
            var sb = new StringBuilder();
            for (int i = 0; i < n; i++) sb.AppendLine(lines[i]);
            return sb.ToString();
        }
    }
}
#endif
