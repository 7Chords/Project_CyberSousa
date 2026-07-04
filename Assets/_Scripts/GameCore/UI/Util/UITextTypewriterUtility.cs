using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameCore.UI
{
    public static class UITextTypewriterUtility
    {
        public static IEnumerator Play(Text targetText, string rawText, float charInterval, Action<Text, char> onCharShown = null)
        {
            if (targetText == null || string.IsNullOrEmpty(rawText))
                yield break;

            for (int charCount = 1; charCount <= rawText.Length; charCount++)
            {
                targetText.text = DialogueTextFormatter.PreventLineStartPunctuation(rawText.Substring(0, charCount));
                onCharShown?.Invoke(targetText, rawText[charCount - 1]);
                yield return new WaitForSecondsRealtime(charInterval);
            }
        }

        public static string FormatFullText(string rawText)
        {
            return DialogueTextFormatter.PreventLineStartPunctuation(rawText);
        }
    }
}
