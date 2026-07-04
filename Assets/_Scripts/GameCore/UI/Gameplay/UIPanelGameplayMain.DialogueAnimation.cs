using System.Collections;
using DG.Tweening;
using GameCore;
using SCFrame;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {
        private const string DialogueOptionPresentationCoroutineName = "dialogue_option_presentation";
        private const float CustomerDialogueScaleDuration = 0.28f;
        private const float CustomerDialogueStartScale = 0.72f;
        private const float PlayerDialogueMoveDuration = 0.30f;
        private const float PlayerDialogueEnterOffsetY = -48f;
        private const float DialogueOptionEnterDuration = 0.24f;
        private const float DialogueOptionEnterStagger = 0.08f;
        private const float DialogueOptionEnterOffsetX = 72f;
        private const float DialogueTextTickDuration = 0.10f;
        private const float DialogueTextTickOffsetY = 4f;
        private const float DialogueTextPunctuationPunch = 1.035f;
        private const float DialogueTextCompleteDuration = 0.16f;

        private readonly TweenContainer _dialogueAnimTweenContainer = new TweenContainer();
        private bool _hasCustomerDialogueStripShown;
        private bool _hasPlayerDialogueStripShown;
        private long _lastOptionEnterAnimSourceLineId;
        private bool _customerDialogueOriginCached;
        private Vector3 _customerDialogueOriginScale = Vector3.one;
        private Vector2 _playerDialogueOriginPos;
        private bool _playerDialogueOriginReady;
        private CanvasGroup _playerDialogueCanvasGroup;
        private bool _leftDialogueTextOriginReady;
        private bool _rightDialogueTextOriginReady;
        private Vector2 _leftDialogueTextOriginPos;
        private Vector2 _rightDialogueTextOriginPos;
        private Vector3 _leftDialogueTextOriginScale = Vector3.one;
        private Vector3 _rightDialogueTextOriginScale = Vector3.one;

        private void PlayCustomerDialogueEnterAnimation()
        {
            if (_hasCustomerDialogueStripShown || mono.dialogueLeftArea == null)
                return;

            _hasCustomerDialogueStripShown = true;
            CacheCustomerDialogueOrigin();
            AudioMgr.instance.PlaySfx(AudioKeys.CustomerDialogueBubble);

            RectTransform rectTransform = mono.dialogueLeftArea.rectTransform;
            rectTransform.DOKill();
            mono.dialogueLeftArea.enabled = true;
            rectTransform.localScale = _customerDialogueOriginScale * CustomerDialogueStartScale;
            Tween tween = rectTransform
                .DOScale(_customerDialogueOriginScale, CustomerDialogueScaleDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            _dialogueAnimTweenContainer.RegDoTween(tween);
        }

        private void PlayPlayerDialogueEnterAnimation(long lineId)
        {
            if (lineId <= 0 || mono.dialogueRightArea == null || _hasPlayerDialogueStripShown)
                return;

            _hasPlayerDialogueStripShown = true;
            AudioMgr.instance.PlaySfx(AudioKeys.PlayerDialoguePopup);

            RectTransform rectTransform = mono.dialogueRightArea.rectTransform;
            rectTransform.DOKill();
            mono.dialogueRightArea.enabled = true;
            RefreshPlayerDialogueRestPosition();

            CanvasGroup canvasGroup = EnsurePlayerDialogueCanvasGroup();
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            rectTransform.anchoredPosition = _playerDialogueOriginPos + new Vector2(0f, PlayerDialogueEnterOffsetY);

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Join(rectTransform
                .DOAnchorPos(_playerDialogueOriginPos, PlayerDialogueMoveDuration)
                .SetEase(Ease.OutCubic));
            sequence.Join(canvasGroup
                .DOFade(1f, PlayerDialogueMoveDuration)
                .SetEase(Ease.OutCubic));
            _dialogueAnimTweenContainer.RegDoTween(sequence);
        }

        private CanvasGroup EnsurePlayerDialogueCanvasGroup()
        {
            if (_playerDialogueCanvasGroup != null)
                return _playerDialogueCanvasGroup;

            if (mono.dialogueRightArea == null)
                return null;

            _playerDialogueCanvasGroup = mono.dialogueRightArea.GetComponent<CanvasGroup>();
            if (_playerDialogueCanvasGroup == null)
                _playerDialogueCanvasGroup = mono.dialogueRightArea.gameObject.AddComponent<CanvasGroup>();

            return _playerDialogueCanvasGroup;
        }

        private void RefreshPlayerDialogueRestPosition()
        {
            if (mono.dialogueRightArea == null)
                return;

            RectTransform rectTransform = mono.dialogueRightArea.rectTransform;
            Canvas.ForceUpdateCanvases();
            if (mono.dialogueSection != null)
            {
                RectTransform dialogueSectionRect = mono.dialogueSection.transform as RectTransform;
                if (dialogueSectionRect != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueSectionRect);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            _playerDialogueOriginPos = rectTransform.anchoredPosition;
            _playerDialogueOriginReady = true;
        }

        private void ScheduleDialogueOptionPresentation(long sourceLineId)
        {
            _dialogueCoroutineContainer.RunExclusive(
                DialogueOptionPresentationRoutine(sourceLineId),
                DialogueOptionPresentationCoroutineName);
        }

        private IEnumerator DialogueOptionPresentationRoutine(long sourceLineId)
        {
            yield return null;
            RebuildDialogueOptionLayoutsImmediate();
            PlayDialogueOptionsEnterAnimation(sourceLineId);
        }

        private void PlayDialogueOptionsEnterAnimation(long sourceLineId)
        {
            if (sourceLineId <= 0 || sourceLineId == _lastOptionEnterAnimSourceLineId)
                return;

            _lastOptionEnterAnimSourceLineId = sourceLineId;
            int optionCount = _currentOptionDialogueList.Count;
            if (optionCount <= 0)
                return;

            for (int index = 0; index < optionCount; index++)
            {
                UIMonoDialogueOption optionItem = GetOrCreateDialogueOptionItem(index);
                if (optionItem == null || !optionItem.gameObject.activeSelf)
                    continue;

                RectTransform rectTransform = optionItem.AnimRoot;
                if (rectTransform == null)
                    continue;

                CanvasGroup canvasGroup = optionItem.EnsureCanvasGroup();
                Vector2 targetPos = rectTransform.anchoredPosition;
                rectTransform.DOKill();
                canvasGroup.DOKill();
                rectTransform.anchoredPosition = targetPos + new Vector2(DialogueOptionEnterOffsetX, 0f);
                canvasGroup.alpha = 0f;

                float delay = index * DialogueOptionEnterStagger;
                Sequence sequence = DOTween.Sequence().SetUpdate(true);
                if (delay > 0f)
                    sequence.AppendInterval(delay);
                sequence.Append(rectTransform
                    .DOAnchorPos(targetPos, DialogueOptionEnterDuration)
                    .SetEase(Ease.OutCubic));
                sequence.Join(canvasGroup
                    .DOFade(1f, DialogueOptionEnterDuration)
                    .SetEase(Ease.OutCubic));
                _dialogueAnimTweenContainer.RegDoTween(sequence);
            }
        }

        private void PlayDialogueTextTickAnimation(Text targetText, char typedChar)
        {
            if (targetText == null || char.IsWhiteSpace(typedChar))
                return;

            RectTransform rectTransform = targetText.rectTransform;
            if (rectTransform == null)
                return;

            CacheDialogueTextOrigin(targetText);
            ResetDialogueTextTransform(targetText);

            bool isPunctuation = IsDialoguePunctuation(typedChar);
            rectTransform.DOKill();
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(rectTransform
                .DOAnchorPos(GetDialogueTextOriginPos(targetText) + Vector2.up * DialogueTextTickOffsetY, DialogueTextTickDuration * 0.45f)
                .SetEase(Ease.OutQuad));
            sequence.Append(rectTransform
                .DOAnchorPos(GetDialogueTextOriginPos(targetText), DialogueTextTickDuration * 0.55f)
                .SetEase(Ease.OutCubic));
            if (isPunctuation)
            {
                sequence.Join(rectTransform
                    .DOPunchScale(Vector3.one * (DialogueTextPunctuationPunch - 1f), DialogueTextTickDuration * 1.35f, 4, 0.45f)
                    .SetEase(Ease.OutQuad));
            }

            sequence.OnKill(() => ResetDialogueTextTransform(targetText));
            _dialogueAnimTweenContainer.RegDoTween(sequence);
        }

        private void PlayDialogueTextCompleteAnimation(Text targetText)
        {
            if (targetText == null || string.IsNullOrEmpty(targetText.text))
                return;

            RectTransform rectTransform = targetText.rectTransform;
            if (rectTransform == null)
                return;

            CacheDialogueTextOrigin(targetText);
            ResetDialogueTextTransform(targetText);

            rectTransform.DOKill();
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(rectTransform
                .DOPunchScale(Vector3.one * 0.025f, DialogueTextCompleteDuration, 5, 0.35f)
                .SetEase(Ease.OutQuad));
            sequence.OnKill(() => ResetDialogueTextTransform(targetText));
            _dialogueAnimTweenContainer.RegDoTween(sequence);
        }

        private void KillDialogueTextAnimation(Text targetText)
        {
            if (targetText == null)
                return;

            RectTransform rectTransform = targetText.rectTransform;
            if (rectTransform == null)
                return;

            rectTransform.DOKill();
            ResetDialogueTextTransform(targetText);
        }

        private void CacheDialogueTextOrigin(Text targetText)
        {
            if (targetText == null)
                return;

            RectTransform rectTransform = targetText.rectTransform;
            if (rectTransform == null)
                return;

            if (targetText == mono.txtDialogueLeft)
            {
                if (_leftDialogueTextOriginReady)
                    return;

                _leftDialogueTextOriginPos = rectTransform.anchoredPosition;
                _leftDialogueTextOriginScale = rectTransform.localScale;
                _leftDialogueTextOriginReady = true;
                return;
            }

            if (targetText == mono.txtDialogueRight)
            {
                if (_rightDialogueTextOriginReady)
                    return;

                _rightDialogueTextOriginPos = rectTransform.anchoredPosition;
                _rightDialogueTextOriginScale = rectTransform.localScale;
                _rightDialogueTextOriginReady = true;
            }
        }

        private Vector2 GetDialogueTextOriginPos(Text targetText)
        {
            if (targetText == mono.txtDialogueLeft && _leftDialogueTextOriginReady)
                return _leftDialogueTextOriginPos;
            if (targetText == mono.txtDialogueRight && _rightDialogueTextOriginReady)
                return _rightDialogueTextOriginPos;
            return targetText.rectTransform.anchoredPosition;
        }

        private Vector3 GetDialogueTextOriginScale(Text targetText)
        {
            if (targetText == mono.txtDialogueLeft && _leftDialogueTextOriginReady)
                return _leftDialogueTextOriginScale;
            if (targetText == mono.txtDialogueRight && _rightDialogueTextOriginReady)
                return _rightDialogueTextOriginScale;
            return targetText.rectTransform.localScale;
        }

        private void ResetDialogueTextTransform(Text targetText)
        {
            if (targetText == null)
                return;

            RectTransform rectTransform = targetText.rectTransform;
            if (rectTransform == null)
                return;

            rectTransform.anchoredPosition = GetDialogueTextOriginPos(targetText);
            rectTransform.localScale = GetDialogueTextOriginScale(targetText);
        }

        private static bool IsDialoguePunctuation(char typedChar)
        {
            return typedChar == '，'
                || typedChar == '。'
                || typedChar == '！'
                || typedChar == '？'
                || typedChar == ','
                || typedChar == '.'
                || typedChar == '!'
                || typedChar == '?'
                || typedChar == '…';
        }

        private void CacheCustomerDialogueOrigin()
        {
            if (_customerDialogueOriginCached || mono.dialogueLeftArea == null)
                return;

            _customerDialogueOriginScale = mono.dialogueLeftArea.rectTransform.localScale;
            _customerDialogueOriginCached = true;
        }

        private void ResetPlayerDialogueStripInstant()
        {
            if (mono.dialogueRightArea == null)
                return;

            RectTransform rectTransform = mono.dialogueRightArea.rectTransform;
            rectTransform.DOKill();
            if (_playerDialogueOriginReady)
                rectTransform.anchoredPosition = _playerDialogueOriginPos;

            CanvasGroup canvasGroup = _playerDialogueCanvasGroup;
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
            }
        }

        private void ResetDialogueStripTransforms()
        {
            CacheCustomerDialogueOrigin();
            if (mono.dialogueLeftArea != null)
            {
                RectTransform rectTransform = mono.dialogueLeftArea.rectTransform;
                rectTransform.DOKill();
                rectTransform.localScale = _customerDialogueOriginScale;
            }

            ResetPlayerDialogueStripInstant();
        }

        private void ResetDialogueOptionItemPresentStates()
        {
            for (int index = 0; index < _dialogueOptionItemPool.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                if (optionItem == null)
                    continue;

                RectTransform rectTransform = optionItem.AnimRoot;
                if (rectTransform != null)
                    rectTransform.DOKill();

                CanvasGroup canvasGroup = optionItem.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOKill();
                    canvasGroup.alpha = 1f;
                }
            }
        }

        private void ResetDialogueUiAnimations()
        {
            _dialogueAnimTweenContainer.KillAllDoTween();
            _dialogueCoroutineContainer.Kill(DialogueOptionPresentationCoroutineName);
            _hasCustomerDialogueStripShown = false;
            _hasPlayerDialogueStripShown = false;
            _playerDialogueOriginReady = false;
            _lastOptionEnterAnimSourceLineId = 0;
            ResetDialogueStripTransforms();
            if (mono.txtDialogueLeft != null)
                ResetDialogueTextTransform(mono.txtDialogueLeft);
            if (mono.txtDialogueRight != null)
                ResetDialogueTextTransform(mono.txtDialogueRight);
            _leftDialogueTextOriginReady = false;
            _rightDialogueTextOriginReady = false;
            ResetDialogueOptionItemPresentStates();
        }
    }
}
