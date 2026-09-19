using System.Text;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using UniGetUI.Avalonia.ViewModels.Pages.LogPages;

namespace UniGetUI.Avalonia.Views.Controls;

// Read-only colored log/console viewer shared by every log and command-output view: virtualized
// rendering (smooth scroll), free character-level selection across lines, per-line colors, and a
// theme-aware hyperlink color.
public class LogTextEditor : TextEditor
{
    // Per physical line color, indexed by 0-based document line number.
    private readonly List<IBrush> _lineColors = [];

    // Use AvaloniaEdit's TextEditor theme/template; a subclass key has no matching ControlTheme.
    protected override Type StyleKeyOverride => typeof(TextEditor);

    public LogTextEditor()
    {
        IsReadOnly = true;
        ShowLineNumbers = false;
        WordWrap = true;
        Background = Brushes.Transparent;
        FontFamily = new FontFamily("Cascadia Mono,Consolas,Menlo,monospace");
        FontSize = 12;
        Padding = new Thickness(8);
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        TextArea.TextView.LineTransformers.Add(new SeverityColorizer(_lineColors));
        UpdateLinkColor();
        ActualThemeVariantChanged += (_, _) => UpdateLinkColor();
    }

    public void SetLines(IEnumerable<LogLineItem> lines)
    {
        _lineColors.Clear();
        var sb = new StringBuilder();
        bool first = true;

        foreach (var item in lines)
        {
            foreach (string physicalLine in item.Text.Split('\n'))
            {
                if (!first) sb.Append('\n');
                sb.Append(physicalLine);
                _lineColors.Add(item.Foreground);
                first = false;
            }
        }

        Text = sb.ToString();
        TextArea.TextView.Redraw();
    }

    public void AppendLine(LogLineItem line)
    {
        var sb = new StringBuilder();
        foreach (string physicalLine in line.Text.Split('\n'))
        {
            if (Document.TextLength > 0 || sb.Length > 0)
                sb.Append('\n');
            sb.Append(physicalLine);
            _lineColors.Add(line.Foreground);
        }

        Document.Insert(Document.TextLength, sb.ToString());
    }

    // Overwrites the last physical line in place instead of appending, so carriage-return progress
    // redraws (installer spinners) repaint a single line rather than flooding the view.
    public void ReplaceLastLine(LogLineItem line)
    {
        if (Document.TextLength == 0 || _lineColors.Count == 0)
        {
            AppendLine(line);
            return;
        }

        DocumentLine last = Document.GetLineByNumber(Document.LineCount);
        Document.Replace(last.Offset, last.Length, line.Text);
        _lineColors[^1] = line.Foreground;
    }

    public void ClearLines()
    {
        _lineColors.Clear();
        Text = string.Empty;
    }

    public void ScrollToBottom() => ScrollToEnd();

    // True when the view is pinned to the last line; used to keep auto-scroll from fighting a manual scroll-up.
    public bool IsScrolledToBottom => ExtentHeight - ViewportHeight - VerticalOffset <= 1.0;

    // AvaloniaEdit auto-links URLs; use the app's theme-aware hyperlink brush instead of
    // AvaloniaEdit's fixed default so links remain legible in both themes.
    private void UpdateLinkColor()
    {
        if (Application.Current?.TryGetResource(
                "HyperlinkForeground",
                Infrastructure.ThemeHelper.Variant,
                out var resource) == true && resource is IBrush brush)
            TextArea.TextView.LinkTextForegroundBrush = brush;
    }

    private sealed class SeverityColorizer(List<IBrush> lineColors) : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.Length == 0) return;
            int index = line.LineNumber - 1;
            if (index < 0 || index >= lineColors.Count) return;

            IBrush brush = lineColors[index];
            ChangeLinePart(line.Offset, line.EndOffset, element => element.TextRunProperties.SetForegroundBrush(brush));
        }
    }
}
