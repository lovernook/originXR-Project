using UnityEngine;
using TMPro;
using OriginXR.Data;

namespace OriginXR.Battle
{
    /// <summary>
    /// 战斗结算面板控制器
    /// 挂载到 SettlePanel 上，BattleManager 战斗结束时调用 Show
    /// </summary>
    public class SettlePanelController : MonoBehaviour
    {
        [Header("UI 引用")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI accuracyText;
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI starsText;
        public StarAnimator starAnimator;

        /// <summary>显示结算数据</summary>
        public void Show(StageResultData result)
        {
            gameObject.SetActive(true);

            if (scoreText != null)
                scoreText.text = $"得分: {result.score}";

            if (accuracyText != null)
                accuracyText.text = $"正确率: {result.correctCount}/{result.totalCount} ({result.GetAccuracyText()})";

            if (comboText != null)
                comboText.text = $"最高连击: {result.maxCombo}";

            if (starsText != null)
                starsText.text = result.GetStarText();

            Debug.Log($"[SettlePanel] 结算显示: 得分={result.score} 正确率={result.GetAccuracyText()} 连击={result.maxCombo} ★={result.starsEarned}");

            // 播放星星弹出动画
            if (starAnimator != null)
                starAnimator.Play(result.starsEarned);
        }
    }
}
