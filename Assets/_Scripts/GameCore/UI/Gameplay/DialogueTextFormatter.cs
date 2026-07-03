using System.Text;

namespace GameCore.UI
{
    public static class DialogueTextFormatter
    {
        private const char WordJoiner = '\u2060';

        public static string PreventLineStartPunctuation(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            StringBuilder builder = null;
            for (int index = 0; index < text.Length; index++)
            {
                char currentChar = text[index];
                if (index > 0
                    && IsLineStartForbiddenPunctuation(currentChar)
                    && text[index - 1] != WordJoiner
                    && !char.IsWhiteSpace(text[index - 1]))
                {
                    if (builder == null)
                        builder = new StringBuilder(text.Length + 4).Append(text, 0, index);

                    builder.Append(WordJoiner);
                }

                builder?.Append(currentChar);
            }

            return builder == null ? text : builder.ToString();
        }

        private static bool IsLineStartForbiddenPunctuation(char value)
        {
            return value == '，'
                || value == '。'
                || value == '、'
                || value == '！'
                || value == '？'
                || value == '；'
                || value == '：'
                || value == '）'
                || value == '】'
                || value == '》'
                || value == '」'
                || value == '』'
                || value == '”'
                || value == '’'
                || value == ')'
                || value == ']'
                || value == '}'
                || value == ','
                || value == '.'
                || value == '!'
                || value == '?'
                || value == ';'
                || value == ':'
                || value == '…';
        }
    }
}
