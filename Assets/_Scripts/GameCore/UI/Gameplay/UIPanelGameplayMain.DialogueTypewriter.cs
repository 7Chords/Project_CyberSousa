using System.Collections;
using SCFrame;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {
        private const string DialogueTypewriterCoroutineName = "dialogue_typewriter";
        private const float DialogueTypewriterCharInterval = 0.05f;

        private readonly CoroutineContainer _dialogueCoroutineContainer = new CoroutineContainer();
        private Text _activeDialogueTypewriterText;
        private string _activeDialogueTypewriterFullText;
        private long _dialogueTypewriterLineId;
        private bool _isDialogueTypewriterPlaying;
        private bool _awaitDialogueAdvanceAfterPlayerLine;
        private bool _pendingEndDialogueAfterPlayerLine;
        private long _pendingDialogueIdAfterPlayerLine;

        private void StartDialogueTypewriter(Text targetText, string fullText, long lineId)
        {
            StopDialogueTypewriterRoutine();

            _dialogueTypewriterLineId = lineId;
            if (targetText == null || string.IsNullOrEmpty(fullText))
            {
                if (targetText != null)
                    targetText.text = string.Empty;

                _isDialogueTypewriterPlaying = false;
                UpdateDialogueStripVisibility();
                RefreshDialogueAdvanceClickArea();
                RefreshDialogueOptionUi();
                return;
            }

            _activeDialogueTypewriterText = targetText;
            _activeDialogueTypewriterFullText = fullText;
            _isDialogueTypewriterPlaying = true;
            targetText.text = string.Empty;
            UpdateDialogueStripVisibility();
            RefreshDialogueAdvanceClickArea();
            RefreshDialogueOptionUi();
            _dialogueCoroutineContainer.Run(DialogueTypewriterRoutine(), DialogueTypewriterCoroutineName);
        }

        private IEnumerator DialogueTypewriterRoutine()
        {
            Text targetText = _activeDialogueTypewriterText;
            string fullText = _activeDialogueTypewriterFullText;
            if (targetText == null || string.IsNullOrEmpty(fullText))
            {
                FinishDialogueTypewriter();
                yield break;
            }

            for (int charCount = 1; charCount <= fullText.Length; charCount++)
            {
                if (targetText == null)
                    yield break;

                targetText.text = fullText.Substring(0, charCount);
                UpdateDialogueStripVisibility();
                yield return new WaitForSecondsRealtime(DialogueTypewriterCharInterval);
            }

            FinishDialogueTypewriter();
        }

        private bool TrySkipDialogueTypewriter()
        {
            if (!_isDialogueTypewriterPlaying)
                return false;

            CompleteDialogueTypewriterInstant();
            return true;
        }

        private void CompleteDialogueTypewriterInstant()
        {
            _dialogueCoroutineContainer.Kill(DialogueTypewriterCoroutineName);
            if (_activeDialogueTypewriterText != null)
                _activeDialogueTypewriterText.text = _activeDialogueTypewriterFullText ?? string.Empty;

            FinishDialogueTypewriter();
        }

        private void FinishDialogueTypewriter()
        {
            _isDialogueTypewriterPlaying = false;
            _activeDialogueTypewriterText = null;
            _activeDialogueTypewriterFullText = null;
            UpdateDialogueStripVisibility();
            RefreshDialogueAdvanceClickArea();
            RefreshDialogueOptionUi();
        }

        private void ClearPendingDialogueAdvanceAfterPlayerLine()
        {
            _awaitDialogueAdvanceAfterPlayerLine = false;
            _pendingEndDialogueAfterPlayerLine = false;
            _pendingDialogueIdAfterPlayerLine = 0;
        }

        private bool TryAdvanceDialogueAfterPlayerLine()
        {
            if (!_awaitDialogueAdvanceAfterPlayerLine || _isDialogueTypewriterPlaying)
                return false;

            bool endDialogue = _pendingEndDialogueAfterPlayerLine;
            long nextDialogueId = _pendingDialogueIdAfterPlayerLine;
            ClearPendingDialogueAdvanceAfterPlayerLine();
            if (endDialogue)
            {
                EndDialogue();
                return true;
            }

            SetCurrentDialogue(nextDialogueId);
            return true;
        }

        private void StopDialogueTypewriterRoutine()
        {
            _dialogueCoroutineContainer.Kill(DialogueTypewriterCoroutineName);
            _isDialogueTypewriterPlaying = false;
            _activeDialogueTypewriterText = null;
            _activeDialogueTypewriterFullText = null;
            _dialogueTypewriterLineId = 0;
        }

        private void StopDialogueTypewriter()
        {
            StopDialogueTypewriterRoutine();
            ClearPendingDialogueAdvanceAfterPlayerLine();
        }

        private void StopDialoguePresentation()
        {
            StopDialogueTypewriter();
            ResetDialogueUiAnimations();
        }

        private void UpdateDialogueStripVisibility()
        {
            if (mono.dialogueLeftArea != null)
            {
                bool hasLeftContent = !string.IsNullOrEmpty(mono.txtDialogueLeft?.text)
                    || (_isDialogueTypewriterPlaying && _activeDialogueTypewriterText == mono.txtDialogueLeft);
                mono.dialogueLeftArea.enabled = hasLeftContent;
                if (!hasLeftContent)
                    _hasCustomerDialogueStripShown = false;
            }

            if (mono.dialogueRightArea != null)
            {
                bool hasRightContent = !string.IsNullOrEmpty(mono.txtDialogueRight?.text)
                    || (_isDialogueTypewriterPlaying && _activeDialogueTypewriterText == mono.txtDialogueRight);
                mono.dialogueRightArea.enabled = hasRightContent;
                if (!hasRightContent)
                {
                    _hasPlayerDialogueStripShown = false;
                    ResetPlayerDialogueStripInstant();
                }
            }
        }

        private void PlayCurrentDialogueTypewriter(bool isPlayerDialogue, string currentContent)
        {
            Text targetText = isPlayerDialogue ? mono.txtDialogueRight : mono.txtDialogueLeft;
            Text otherText = isPlayerDialogue ? mono.txtDialogueLeft : mono.txtDialogueRight;
            if (otherText != null)
                otherText.text = string.Empty;

            long lineId = _currentDialogueRefData != null ? _currentDialogueRefData.id : 0;
            if (_dialogueTypewriterLineId == lineId && (_isDialogueTypewriterPlaying || IsDialogueTypewriterComplete(targetText, currentContent)))
                return;

            if (isPlayerDialogue)
                PlayPlayerDialogueEnterAnimation(lineId);
            else
                PlayCustomerDialogueEnterAnimation();

            StartDialogueTypewriter(targetText, currentContent, lineId);
        }

        private bool IsDialogueTypewriterComplete(Text targetText, string fullText)
        {
            return targetText != null && targetText.text == fullText;
        }
    }
}
