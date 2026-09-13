using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.Globalization;
using BigTextReader.Core.Search;
using BigTextReader.Core.Sources;
using BigTextReader.Core;

namespace BigTextReader.App.View
{
    internal class TextView : FrameworkElement, IScrollInfo
    {
        public TextView()
        {
            Focusable = true;
            Unloaded += (_, _) => StopScrollAnimation();

            _pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            MeasureFont();
        }

        public ILineSource Source
        {
            get => (ILineSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(ILineSource),
            typeof(TextView),
            new FrameworkPropertyMetadata(null, OnSourceChanged));

        public long LineCount
        {
            get { return (long)GetValue(LineCountProperty); }
            set { SetValue(LineCountProperty, value); }
        }
        public static readonly DependencyProperty LineCountProperty =
            DependencyProperty.Register(
                nameof(LineCount),
                typeof(long),
                typeof(TextView),
                new PropertyMetadata((long)0, OnScrollInfoChanged));

        public long MaxLineBytes
        {
            get { return (long)GetValue(MaxLineBytesProperty); }
            set { SetValue(MaxLineBytesProperty, value); }
        }
        public static readonly DependencyProperty MaxLineBytesProperty =
            DependencyProperty.Register(
                nameof(MaxLineBytes),
                typeof(long),
                typeof(TextView),
                new PropertyMetadata((long)0, OnScrollInfoChanged));

        public SearchResults SearchResults
        {
            get { return (SearchResults)GetValue(SearchResultsProperty); }
            set { SetValue(SearchResultsProperty, value); }
        }
        public static readonly DependencyProperty SearchResultsProperty =
            DependencyProperty.Register(
                nameof(SearchResults),
                typeof(SearchResults),
                typeof(TextView),
                new PropertyMetadata(SearchResults.Empty, OnHighlightChanged));

        public int CurrentSearchResult
        {
            get { return (int)GetValue(CurrentSearchResultProperty); }
            set { SetValue(CurrentSearchResultProperty, value); }
        }
        public static readonly DependencyProperty CurrentSearchResultProperty =
            DependencyProperty.Register(
                nameof(CurrentSearchResult),
                typeof(int),
                typeof(TextView),
                new PropertyMetadata(-1, OnHighlightChanged));


        // Text fields
        private const double FontSize = 14;
        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private ScrollViewer? _scrollOwner;
        private Vector _offset;
        private Size _viewport;

        private Typeface _typeface = new Typeface(
            new FontFamily("Consolas"),
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal
        );

        // Text sizes
        private double _lineHeight = 16;
        private double _pixelsPerDip = 1.0;
        private double _charWidth = 8;

        // Animation fields
        private double _targetOffsetY;
        private bool _isAnimatingScroll;
        private long _lastFrameTimestamp;
        private const double ScrollTimeConstantSeconds = 0.045;
        private const double ScrollSnapThreshold = 0.5;
        private const double MaxAnimatedDistanceViewports = 3;

        // Colors
        private readonly Brush _foreground = Brushes.Black;
        private static readonly Brush _matchBrush = Freeze(Color.FromRgb(0xFF, 0xE0, 0x7A));
        private static readonly Brush _currentMatchBrush = Freeze(Color.FromRgb(0xFF, 0x9B, 0x3D));
        private static Brush Freeze(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public bool CanHorizontallyScroll
        {
            get => _canHorizontallyScroll; 
            set => _canHorizontallyScroll = value; 
        }

        public bool CanVerticallyScroll
        {
            get => _canVerticallyScroll; 
            set => _canVerticallyScroll = value;
        }
        public ScrollViewer? ScrollOwner
        {
            get => _scrollOwner;
            set => _scrollOwner = value;
        }

        public double ExtentHeight => LineCount * _lineHeight;
        public double ExtentWidth => Math.Min(MaxLineBytes, Globals.MaxRenderedLineLength) * _charWidth;
        private double WheelSize => 3 * _lineHeight;
        public double HorizontalOffset { get => _offset.X; }
        public double VerticalOffset { get => _offset.Y; }
        public double ViewportHeight { get => _viewport.Height; }
        public double ViewportWidth { get => _viewport.Width; }

        private void MeasureFont()
        {
            FormattedText zero = new(
                "0",
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                _typeface,
                FontSize,
                _foreground,
                _pixelsPerDip);
            _lineHeight = Math.Ceiling(zero.Height);
            _charWidth = zero.WidthIncludingTrailingWhitespace;
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            double firstLine = _lineHeight > 0 ? _offset.Y / _lineHeight : 0;
            _pixelsPerDip = newDpi.PixelsPerDip;
            MeasureFont();
            _offset.Y = firstLine * _lineHeight;
            InvalidateVisual();
            ScrollOwner?.InvalidateScrollInfo();
        }

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + _lineHeight);
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - _charWidth);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + _charWidth);
        }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - _lineHeight);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            if (rectangle.IsEmpty || ViewportHeight <= 0)
            {
                return Rect.Empty;
            }

            double top = rectangle.Top + VerticalOffset;
            double bottom = top + rectangle.Height;

            double newOffset = VerticalOffset;

            if (top < VerticalOffset)
                newOffset = top;
            else if (bottom > VerticalOffset + ViewportHeight)
                newOffset = bottom - ViewportHeight;

            SetVerticalOffset(newOffset);

            return new Rect(
                rectangle.X,
                top - VerticalOffset,
                rectangle.Width,
                rectangle.Height
            );
        }

        public void MouseWheelDown() => AnimateVerticalBy(WheelSize);

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - WheelSize);
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + WheelSize);
        }

        public void MouseWheelUp() => AnimateVerticalBy(-WheelSize);

        private void AnimateVerticalBy(double delta)
        {
            double maximum = Math.Max(0, ExtentHeight - ViewportHeight);

            double from = _isAnimatingScroll ? _targetOffsetY : _offset.Y;
            _targetOffsetY = Math.Clamp(from + delta, 0, maximum);

            if (Math.Abs(_targetOffsetY - _offset.Y) < ScrollSnapThreshold)
            {
                StopScrollAnimation();
                return;
            }

            StartScrollAnimation();
        }

        private void AnimateVerticalTo(double target)
        {
            target = Math.Clamp(target, 0, Math.Max(0, ExtentHeight - ViewportHeight));

            if (Math.Abs(target - _offset.Y) > MaxAnimatedDistanceViewports * ViewportHeight)
            {
                SetVerticalOffset(target);
                return;
            }

            AnimateVerticalBy(target - (_isAnimatingScroll ? _targetOffsetY : _offset.Y));
        }

        private void StartScrollAnimation()
        {
            if (_isAnimatingScroll) return;

            _isAnimatingScroll = true;
            _lastFrameTimestamp = Stopwatch.GetTimestamp();
            CompositionTarget.Rendering += OnRenderingFrame;
        }

        private void StopScrollAnimation()
        {
            if (!_isAnimatingScroll) return;

            _isAnimatingScroll = false;
            CompositionTarget.Rendering -= OnRenderingFrame;
        }

        private void OnRenderingFrame(object? sender, EventArgs e)
        {
            long now = Stopwatch.GetTimestamp();
            double elapsedSeconds = Stopwatch.GetElapsedTime(_lastFrameTimestamp, now).TotalSeconds;
            _lastFrameTimestamp = now;

            double alpha = 1 - Math.Exp(-elapsedSeconds / ScrollTimeConstantSeconds);
            double next = _offset.Y + (_targetOffsetY - _offset.Y) * alpha;

            if (Math.Abs(_targetOffsetY - next) < ScrollSnapThreshold)
            {
                next = _targetOffsetY;
                StopScrollAnimation();
            }

            _offset.Y = next;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateVisual();
        }

        public void ScrollToLine(long line, int column)
        {
            AnimateVerticalTo(line * _lineHeight - (ViewportHeight - _lineHeight) / 2);

            if (column >= 0)
                SetHorizontalOffset(column * _charWidth - (ViewportWidth - _charWidth) / 2);
        }

        public void PageDown() => AnimateVerticalBy(ViewportHeight);

        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + ViewportWidth);
        }

        public void PageUp() => AnimateVerticalBy(-ViewportHeight);

        public void SetHorizontalOffset(double offset)
        {
            offset = Math.Max(
                0,
                Math.Min(offset, Math.Max(0, ExtentWidth - ViewportWidth)));
            if (Math.Abs(offset - this._offset.X) < 0.01) return;

            this._offset.X = offset;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateVisual();
        }

        public void SetVerticalOffset(double offset)
        {
            StopScrollAnimation();

            offset = Math.Max(0, Math.Min(offset, Math.Max(0, ExtentHeight - ViewportHeight)));
            _targetOffsetY = offset;

            if (Math.Abs(offset - this._offset.Y) < 0.01) return;

            this._offset.Y = offset;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(
                double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? 0 : availableSize.Height
            );
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_viewport != finalSize)
            {
                _viewport = finalSize;
                ClampOffset();
                ScrollOwner?.InvalidateScrollInfo();
            }
            return finalSize;
        }

        private void ClampOffset()
        {
            _offset.Y = Math.Max(0, Math.Min(_offset.Y, ExtentHeight - ViewportHeight));
            _offset.X = Math.Max(0, Math.Min(_offset.X, ExtentWidth - ViewportWidth));
        }

        protected override void OnRender(DrawingContext ctx) 
        {
            long start = (long)(VerticalOffset / _lineHeight);
            double firstLineOffset = VerticalOffset - start * _lineHeight;
            int visible = (int)((VerticalOffset + ViewportHeight) / _lineHeight - start + 1);

            if (Source == null)
                return;

            var results = SearchResults;
            var hits = results.InLineRange(start, visible);
            int hitCursor = 0;

            SearchHit? currentHit = CurrentSearchResult >= 0 && CurrentSearchResult < results.Count
                ? results.Hits[CurrentSearchResult]
                : null;

            for (int i = 0; i < visible && i + start < LineCount; i++)
            {
                long lineNumber = start + i;

                while (hitCursor < hits.Length && hits[hitCursor].Line < lineNumber) hitCursor++;

                var text = Source.GetLine(lineNumber);

                int firstChar = (int)(HorizontalOffset / _charWidth);
                if (firstChar >= text.Length) continue;
                if (char.IsLowSurrogate(text[firstChar])) firstChar--;

                int count = Math.Min((int)(ViewportWidth / _charWidth) + 2, text.Length - firstChar);
                double x = firstChar * _charWidth - HorizontalOffset;
                double y = i * _lineHeight - firstLineOffset;
                var origin = new Point(x, y);

                FormattedText line = new(
                    text.Substring(firstChar, count),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    FontSize,
                    _foreground,
                    _pixelsPerDip)
                {
                    Trimming = TextTrimming.None,
                    MaxLineCount = 1,
                };

                for (int h = hitCursor; h < hits.Length && hits[h].Line == lineNumber; h++)
                {
                    var hit = hits[h];

                    if (!hit.HasColumn) continue;

                    int highlightStart = Math.Max(0, hit.Column - firstChar);
                    int highlightEnd = Math.Min(count, hit.Column - firstChar + results.Pattern.Length);
                    if (highlightEnd <= highlightStart) continue;

                    var geometry = line.BuildHighlightGeometry(
                        origin, highlightStart, highlightEnd - highlightStart);
                    if (geometry is null) continue;

                    bool isCurrent = currentHit == hit;
                    ctx.DrawGeometry(isCurrent ? _currentMatchBrush : _matchBrush, null, geometry);
                }

                ctx.DrawText(line, origin);
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textView = (TextView)d;
            textView._offset.Y = 0;
            textView._offset.X = 0;
            textView._targetOffsetY = 0;
            textView.StopScrollAnimation();
            textView.InvalidateMeasure();
            textView.InvalidateVisual();
            textView.ScrollOwner?.InvalidateScrollInfo();

            if (e.NewValue is not null)
                textView.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => textView.Focus());
        }

        private static void OnScrollInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textView = (TextView)d;
            textView.InvalidateVisual();
            textView.ScrollOwner?.InvalidateScrollInfo();
        }

        private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((TextView)d).InvalidateVisual();
    }
}
