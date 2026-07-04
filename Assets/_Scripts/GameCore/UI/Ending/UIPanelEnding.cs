using System.Collections;
using DG.Tweening;
using GameCore.Flow;
using SCFrame;
using SCFrame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public class UIPanelEnding : _ASCUIPanelBase<UIMonoEnding>
    {
        private const string EndingTypewriterCoroutineName = "ending_typewriter";
        private const float EndingTypewriterCharInterval = 0.05f;
        private const float EndingLineStayDuration = 1f;
        private const string TvMaskObjectName = "img_tv_front";

        private static readonly string[] BadEndingLines =
        {
            "End：不合格按键员",
            "你按错了太多次。",
            "管理员委婉地表示，",
            "你和住户之间可能存在沟通障碍。",
            "第二天，",
            "你被安排去看守安置区外侧的小门。",
            "工作内容简单很多：",
            "门开了就关。",
            "门关了就看着。",
            "直到有一天，",
            "你看到曾经那栋公寓。",
            "拔地而起。"
        };

        private static readonly string[] Ending1Lines =
        {
            "End：一切正常",
            "你按下了确认键。",
            "第二天，",
            "你照常上班。",
            "没有处分，没有奖励，",
            "也没有人提起昨晚。",
            "考考仍然带着面包。",
            "来福仍然制造噪音。",
            "电梯仍然上行下行。"
        };

        private static readonly string[] Ending2Lines =
        {
            "End：集体迁徙",
            "你没有按下确认键。",
            "系统没有进行最后一次的检查。",
            "管理员没有发现夹层。",
            "而那栋楼，终于不再扮演一栋楼。",
            "你听到一阵轰鸣。",
            "公寓拔地而起。",
            "灯光、房间、住户、沉默，",
            "地球的眼睛、胃、羽毛、根须，",
            "罪恶、无法辩白的使命，",
            "全都被火焰托举到夜空里。",
            "只有上行。"
        };

        public static EGameEndingType pendingEndingType = EGameEndingType.BAD;

        private readonly CoroutineContainer _endingCoroutineContainer = new CoroutineContainer();
        private TweenContainer _tvMaskFadeContainer;

        public UIPanelEnding(UIMonoEnding _mono, SCUIShowType _showType) : base(_mono, _showType)
        {
        }

        public override void AfterInitialize()
        {
            _tvMaskFadeContainer = new TweenContainer();
            EnsureTvMaskReference();
        }

        public override void BeforeDiscard()
        {
            StopEndingTextPlayback();
            StopTvMaskFade();
            _tvMaskFadeContainer?.KillAllDoTween();
            _tvMaskFadeContainer = null;
            UnbindEvents();
        }

        public override void OnHidePanel()
        {
            StopEndingTextPlayback();
            StopTvMaskFade();
            UnbindEvents();
        }

        public override void OnShowPanel()
        {
            EnsureTvMaskReference();
            PlayTvMaskFadeOut();
            ApplyEndingContent();

            if (mono.btnReturnMain != null)
                mono.btnReturnMain.AddMouseLeftClickDown(OnBtnReturnMainClicked);
        }

        private void ApplyEndingContent()
        {
            switch (pendingEndingType)
            {
                case EGameEndingType.BAD:
                    SetEndingSprite(mono.sprBadEnding);
                    PlayEndingLines(BadEndingLines);
                    break;
                case EGameEndingType.ENDING_1:
                    SetEndingSprite(mono.sprEnding1);
                    PlayEndingLines(Ending1Lines);
                    break;
                case EGameEndingType.ENDING_2:
                    SetEndingSprite(mono.sprEnding2);
                    PlayEndingLines(Ending2Lines);
                    break;
                default:
                    SetEndingSprite(null);
                    PlayEndingLines(new[] { "本次值班已经结束。" });
                    break;
            }
        }

        private void SetEndingSprite(Sprite endingSprite)
        {
            if (mono.imgEnding == null)
                return;

            mono.imgEnding.sprite = endingSprite;
            SCCommon.SetGameObjectEnable(mono.imgEnding.gameObject, endingSprite != null);
        }

        private void PlayEndingLines(string[] lines)
        {
            StopEndingTextPlayback();
            SetTextInstant(string.Empty);
            if (lines == null || lines.Length == 0)
                return;

            _endingCoroutineContainer.Run(EndingTextPlaybackRoutine(lines), EndingTypewriterCoroutineName);
        }

        private IEnumerator EndingTextPlaybackRoutine(string[] lines)
        {
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                yield return PlayEndingLineRoutine(lines[lineIndex]);
                if (lineIndex < lines.Length - 1)
                    yield return new WaitForSecondsRealtime(EndingLineStayDuration);
            }
        }

        private IEnumerator PlayEndingLineRoutine(string line)
        {
            if (mono.txtSummary == null)
                yield break;

            string rawText = string.IsNullOrEmpty(line) ? string.Empty : line;
            if (rawText.Length <= 0)
            {
                mono.txtSummary.text = string.Empty;
                yield break;
            }

            yield return UITextTypewriterUtility.Play(mono.txtSummary, rawText, EndingTypewriterCharInterval);
            mono.txtSummary.text = UITextTypewriterUtility.FormatFullText(rawText);
        }

        private void StopEndingTextPlayback()
        {
            _endingCoroutineContainer.Kill(EndingTypewriterCoroutineName);
        }

        private void SetTextInstant(string summary)
        {
            if (mono.txtSummary != null)
                mono.txtSummary.text = summary;
        }

        private void UnbindEvents()
        {
            if (mono.btnReturnMain != null)
                mono.btnReturnMain.RemoveMouseLeftClickDown(OnBtnReturnMainClicked);
        }

        private void OnBtnReturnMainClicked(PointerEventData eventData, object[] args)
        {
            AudioMgr.instance.PlaySfx(AudioKeys.ButtonClick);
            GamePlayerDataMgr.instance.ResetRuntimeData();
            GameFlowController.instance.EnterMainMenu();
        }

        private void EnsureTvMaskReference()
        {
            if (mono.imgTvMask != null)
                return;

            mono.imgTvMask = FindChildImage(TvMaskObjectName);
            if (mono.imgTvMask == null)
                mono.imgTvMask = FindChildImage("TvMask");
        }

        private void PlayTvMaskFadeOut()
        {
            if (mono.imgTvMask == null)
                return;

            StopTvMaskFade();
            mono.imgTvMask.gameObject.SetActive(true);
            mono.imgTvMask.raycastTarget = false;

            Color maskColor = mono.imgTvMask.color;
            maskColor.a = 1f;
            mono.imgTvMask.color = maskColor;

            float duration = mono.tvMaskFadeDuration > 0f ? mono.tvMaskFadeDuration : 1.2f;
            _tvMaskFadeContainer.RegDoTween(
                mono.imgTvMask
                    .DOFade(0f, duration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (mono.imgTvMask != null)
                            mono.imgTvMask.gameObject.SetActive(false);
                    }));
        }

        private void StopTvMaskFade()
        {
            if (mono.imgTvMask == null)
                return;

            mono.imgTvMask.DOKill();
            Color maskColor = mono.imgTvMask.color;
            maskColor.a = 1f;
            mono.imgTvMask.color = maskColor;
            _tvMaskFadeContainer?.KillAllDoTween();
        }

        private Image FindChildImage(string objectName)
        {
            Transform target = FindChildTransformRecursive(mono.transform, objectName);
            return target != null ? target.GetComponent<Image>() : null;
        }

        private Transform FindChildTransformRecursive(Transform parent, string objectName)
        {
            if (parent == null)
                return null;

            if (parent.name == objectName)
                return parent;

            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                Transform result = FindChildTransformRecursive(child, objectName);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
