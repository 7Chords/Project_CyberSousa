using System.Collections;
using DG.Tweening;
using SCFrame;
using UnityEngine;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {
        private const string DialogueOptionPresentationCoroutineName = "dialogue_option_presentation";
        private const float CustomerDialogueScaleDuration = 0.28f;
        private const float CustomerDialogueStartScale = 0.72f;
        private const float PlayerDialogueMoveDuration = 0.5f;
        private const float PlayerDialogueEnterOffsetY = -48f;
        private const float DialogueOptionEnterDuration = 0.24f;
        private const float DialogueOptionEnterStagger = 0.08f;
        private const float DialogueOptionEnterOffsetX = 72f;

        private readonly TweenContainer _dialogueAnimTweenContainer = new TweenContainer();
        private bool _hasCustomerDialogueStripShown;
        private long _lastPlayerDialogueAnimLineId;
        private long _lastOptionEnterAnimSourceLineId;
        private bool _dialogueStripOriginCached;
        private Vector3 _customerDialogueOriginScale = Vector3.one;
        private Vector2 _playerDialogueOriginPos;

        private void PlayCustomerDialogueEnterAnimation()
        {
            if (_hasCustomerDialogueStripShown || mono.dialogueLeftArea == null)
                return;

            _hasCustomerDialogueStripShown = true;
            CacheDialogueStripOrigins();

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
            if (lineId <= 0 || lineId == _lastPlayerDialogueAnimLineId || mono.dialogueRightArea == null)
                return;

            _lastPlayerDialogueAnimLineId = lineId;
            CacheDialogueStripOrigins();

            RectTransform rectTransform = mono.dialogueRightArea.rectTransform;
            rectTransform.DOKill();
            mono.dialogueRightArea.enabled = true;
            rectTransform.anchoredPosition = _playerDialogueOriginPos + new Vector2(0f, PlayerDialogueEnterOffsetY);
            Tween tween = rectTransform
                .DOAnchorPos(_playerDialogueOriginPos, PlayerDialogueMoveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
            _dialogueAnimTweenContainer.RegDoTween(tween);
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

        private void CacheDialogueStripOrigins()
        {
            if (_dialogueStripOriginCached)
                return;

            if (mono.dialogueLeftArea != null)
                _customerDialogueOriginScale = mono.dialogueLeftArea.rectTransform.localScale;

            if (mono.dialogueRightArea != null)
                _playerDialogueOriginPos = mono.dialogueRightArea.rectTransform.anchoredPosition;

            _dialogueStripOriginCached = true;
        }

        private void ResetDialogueStripTransforms()
        {
            CacheDialogueStripOrigins();
            if (mono.dialogueLeftArea != null)
            {
                RectTransform rectTransform = mono.dialogueLeftArea.rectTransform;
                rectTransform.DOKill();
                rectTransform.localScale = _customerDialogueOriginScale;
            }

            if (mono.dialogueRightArea != null)
            {
                RectTransform rectTransform = mono.dialogueRightArea.rectTransform;
                rectTransform.DOKill();
                rectTransform.anchoredPosition = _playerDialogueOriginPos;
            }
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
            _lastPlayerDialogueAnimLineId = 0;
            _lastOptionEnterAnimSourceLineId = 0;
            ResetDialogueStripTransforms();
            ResetDialogueOptionItemPresentStates();
        }
    }
}
