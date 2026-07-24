using System;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace UzTypist.Context
{
    internal static class CaretContextReader
    {
        public static bool TryGetCharBeforeCaret(out char? charBeforeCaret)
        {
            charBeforeCaret = null;

            try
            {
                AutomationElement focused = AutomationElement.FocusedElement;
                if (focused == null)
                {
                    return false;
                }

                if (!focused.TryGetCurrentPattern(TextPattern.Pattern, out object patternObj))
                {
                    return false;
                }

                var textPattern = (TextPattern)patternObj;
                TextPatternRange[] selection = textPattern.GetSelection();
                if (selection == null || selection.Length == 0)
                {
                    return false;
                }

                TextPatternRange caretRange = selection[0].Clone();

                TextPatternRange lineRange = caretRange.Clone();
                lineRange.ExpandToEnclosingUnit(TextUnit.Line);
                bool atLineStart = lineRange.CompareEndpoints(
                    TextPatternRangeEndpoint.Start, caretRange, TextPatternRangeEndpoint.Start) == 0;

                if (atLineStart)
                {
                    charBeforeCaret = null;
                    return true;
                }

                int moved = caretRange.MoveEndpointByUnit(
                    TextPatternRangeEndpoint.Start, TextUnit.Character, -1);

                if (moved == 0)
                {
                    charBeforeCaret = null;
                    return true;
                }

                string text = caretRange.GetText(1);
                charBeforeCaret = text.Length > 0 ? text[0] : (char?)null;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
