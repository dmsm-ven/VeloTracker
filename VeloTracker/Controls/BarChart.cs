using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace VeloTracker.Controls;

/// <summary>
/// Лёгкая столбчатая диаграмма на чистом WPF (DrawingContext) без сторонних зависимостей.
/// Цвета задаются через свойства, поэтому диаграмма следует теме приложения (DynamicResource).
/// </summary>
public sealed class BarChart : FrameworkElement
{
    private const double AxisFontSize = 12d;
    private const double LabelFontSize = 11d;
    private const double LabelAngle = -55d;
    private const double TickCount = 5d;

    private static readonly double LabelSin = Math.Sin(55d * Math.PI / 180d);
    private static readonly double LabelCos = Math.Cos(55d * Math.PI / 180d);

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<double>), typeof(BarChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
        nameof(Labels), typeof(IReadOnlyList<string>), typeof(BarChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ItemToolTipsProperty = DependencyProperty.Register(
        nameof(ItemToolTips), typeof(IReadOnlyList<string>), typeof(BarChart),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty YAxisTitleProperty = DependencyProperty.Register(
        nameof(YAxisTitle), typeof(string), typeof(BarChart),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
        nameof(BarBrush), typeof(Brush), typeof(BarChart),
        new FrameworkPropertyMetadata(Brushes.SeaGreen, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextBrushProperty = DependencyProperty.Register(
        nameof(TextBrush), typeof(Brush), typeof(BarChart),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GridBrushProperty = DependencyProperty.Register(
        nameof(GridBrush), typeof(Brush), typeof(BarChart),
        new FrameworkPropertyMetadata(Brushes.DimGray, FrameworkPropertyMetadataOptions.AffectsRender));

    private double _plotLeft;
    private double _plotTop;
    private double _plotWidth;
    private double _plotHeight;

    public IReadOnlyList<double>? Values
    {
        get => (IReadOnlyList<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IReadOnlyList<string>? Labels
    {
        get => (IReadOnlyList<string>?)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    /// <summary>Подсказки для каждого столбца (в том же порядке, что и Values).</summary>
    public IReadOnlyList<string>? ItemToolTips
    {
        get => (IReadOnlyList<string>?)GetValue(ItemToolTipsProperty);
        set => SetValue(ItemToolTipsProperty, value);
    }

    public string YAxisTitle
    {
        get => (string)GetValue(YAxisTitleProperty);
        set => SetValue(YAxisTitleProperty, value);
    }

    public Brush BarBrush
    {
        get => (Brush)GetValue(BarBrushProperty);
        set => SetValue(BarBrushProperty, value);
    }

    public Brush TextBrush
    {
        get => (Brush)GetValue(TextBrushProperty);
        set => SetValue(TextBrushProperty, value);
    }

    public Brush GridBrush
    {
        get => (Brush)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;

        // Прозрачный фон нужен, чтобы элемент получал события мыши по всей области.
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

        var values = Values;
        if (values is null || values.Count == 0 || width <= 0 || height <= 0)
        {
            _plotWidth = _plotHeight = 0;
            return;
        }

        var labels = Labels;
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(TextElement.GetFontFamily(this), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var textBrush = TextBrush;

        FormattedText Measure(string text, double size) =>
            new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, size, textBrush, pixelsPerDip);

        // Шкала по оси Y.
        var (axisMax, step) = NiceScale(Math.Max(values.Max(), 1d));
        var tickCount = (int)Math.Round(axisMax / step);
        var yFormat = step % 1d == 0d ? "N0" : "N1";
        var yTexts = new FormattedText[tickCount + 1];
        for (var i = 0; i <= tickCount; i++)
            yTexts[i] = Measure((i * step).ToString(yFormat, CultureInfo.CurrentCulture), LabelFontSize);

        var titleText = string.IsNullOrWhiteSpace(YAxisTitle) ? null : Measure(YAxisTitle, AxisFontSize);
        var titleWidth = titleText is null ? 0d : titleText.Height + 6d;
        var yLabelsWidth = yTexts.Max(t => t.Width);

        // Подписи по оси X (повёрнутые).
        var xTexts = new FormattedText?[values.Count];
        var labelHeight = 0d;
        var maxLabelWidth = 0d;
        for (var i = 0; i < values.Count; i++)
        {
            if (labels is null || i >= labels.Count) continue;
            var text = Measure(labels[i], LabelFontSize);
            xTexts[i] = text;
            labelHeight = Math.Max(labelHeight, text.Height);
            maxLabelWidth = Math.Max(maxLabelWidth, text.Width);
        }

        var bottomMargin = 10d + (maxLabelWidth > 0 ? maxLabelWidth * LabelSin + labelHeight * LabelCos + 8d : 0d);
        _plotLeft = titleWidth + yLabelsWidth + 14d;
        _plotTop = 12d;
        _plotWidth = Math.Max(1d, width - _plotLeft - 12d);
        _plotHeight = Math.Max(1d, height - _plotTop - bottomMargin);
        var plotBottom = _plotTop + _plotHeight;

        // Сетка и подписи оси Y.
        var gridPen = new Pen(GridBrush, 1d);
        for (var i = 0; i <= tickCount; i++)
        {
            var y = plotBottom - _plotHeight * (i * step / axisMax);
            var snappedY = Math.Round(y) + 0.5d;
            dc.DrawLine(gridPen, new Point(_plotLeft, snappedY), new Point(_plotLeft + _plotWidth, snappedY));
            dc.DrawText(yTexts[i], new Point(_plotLeft - 8d - yTexts[i].Width, y - yTexts[i].Height / 2d));
        }

        // Заголовок оси Y.
        if (titleText is not null)
        {
            var cx = titleText.Height / 2d;
            var cy = _plotTop + _plotHeight / 2d;
            dc.PushTransform(new RotateTransform(-90d, cx, cy));
            dc.DrawText(titleText, new Point(cx - titleText.Width / 2d, cy - titleText.Height / 2d));
            dc.Pop();
        }

        // Столбцы.
        var slot = _plotWidth / values.Count;
        var barWidth = Math.Max(1d, slot * 0.72d);
        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            if (value <= 0d) continue;

            var barHeight = Math.Max(1d, _plotHeight * (value / axisMax));
            var x = _plotLeft + i * slot + (slot - barWidth) / 2d;
            dc.DrawRoundedRectangle(BarBrush, null, new Rect(x, plotBottom - barHeight, barWidth, barHeight), 2d, 2d);
        }

        // Подписи оси X: пропускаем часть, если столбцы слишком узкие.
        var labelStep = labelHeight > 0 ? Math.Max(1, (int)Math.Ceiling(labelHeight * 1.25d / slot)) : 1;
        for (var i = 0; i < xTexts.Length; i += labelStep)
        {
            var text = xTexts[i];
            if (text is null) continue;

            var anchorX = _plotLeft + i * slot + slot / 2d;
            var anchorY = plotBottom + 6d;
            dc.PushTransform(new RotateTransform(LabelAngle, anchorX, anchorY));
            dc.DrawText(text, new Point(anchorX - text.Width, anchorY));
            dc.Pop();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        ToolTip = GetToolTipAt(e.GetPosition(this));
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ToolTip = null;
    }

    private string? GetToolTipAt(Point point)
    {
        var values = Values;
        if (values is null || values.Count == 0 || _plotWidth <= 0) return null;
        if (point.X < _plotLeft || point.X > _plotLeft + _plotWidth ||
            point.Y < _plotTop || point.Y > _plotTop + _plotHeight) return null;

        var index = (int)((point.X - _plotLeft) / (_plotWidth / values.Count));
        if (index < 0 || index >= values.Count) return null;

        var toolTips = ItemToolTips;
        if (toolTips is not null && index < toolTips.Count) return toolTips[index];

        var label = Labels is { } labels && index < labels.Count ? labels[index] : (index + 1).ToString(CultureInfo.CurrentCulture);
        return $"{label}: {values[index]:N1}";
    }

    private static (double Max, double Step) NiceScale(double max)
    {
        var raw = max / TickCount;
        var magnitude = Math.Pow(10d, Math.Floor(Math.Log10(raw)));
        var normalized = raw / magnitude;
        var nice = normalized switch
        {
            <= 1d => 1d,
            <= 2d => 2d,
            <= 2.5d => 2.5d,
            <= 5d => 5d,
            _ => 10d
        };

        var step = nice * magnitude;
        return (Math.Ceiling(max / step) * step, step);
    }
}
