using System;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace UzTypist.Context
{
    internal static class CaretContextReader
    {
        public static bool TryGetContext(
            out char? charBeforeCaret,
            out bool doubleQuoteOpen,
            out bool singleQuoteOpen)
        {
            charBeforeCaret = null;
            doubleQuoteOpen = false;
            singleQuoteOpen = false;

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

                TextPatternRange beforeRange = caretRange.Clone();
                beforeRange.ExpandToEnclosingUnit(TextUnit.Paragraph);
                beforeRange.MoveEndpointByRange(
                    TextPatternRangeEndpoint.End, caretRange, TextPatternRangeEndpoint.Start);

                string before = beforeRange.GetText(-1) ?? string.Empty;

                charBeforeCaret = before.Length > 0 ? before[before.Length - 1] : (char?)null;

                foreach (char c in before)
                {
                    switch (c)
                    {
                        case '“':
                            doubleQuoteOpen = true;
                            break;
                        case '”':
                            doubleQuoteOpen = false;
                            singleQuoteOpen = false;
                            break;
                        case '‘':
                            singleQuoteOpen = true;
                            break;
                        case '’':
                            singleQuoteOpen = false;
                            break;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
