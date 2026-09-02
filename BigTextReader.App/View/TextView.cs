using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Globalization;
using BigTextReader.Core.Sources;

namespace BigTextReader.App.View
{
    internal class TextView : FrameworkElement, IScrollInfo
    {
        public TextView()
        {
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
                new PropertyMetadata((long)0, OnLineCountChanged));
        private const double FontSize = 14;

        private bool _canHorizontallyScroll;
        private bool _canVerticallyScroll;
        private ScrollViewer? _scrollOwner;
        private Vector _offset;
        private Size _viewport;

        // FormattedText fields
        private Typeface _typeface = new Typeface(
            new FontFamily("Consolas"),
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal
        );
        private double _lineHeight = 16;
        private double _pixelsPerDip = 1.0;
        private double _charWidth = 8;
        private readonly Brush _foreground = Brushes.Black;

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
        private double _maxMeasuredWidth;
        public double ExtentWidth => _maxMeasuredWidth;
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

        public void MouseWheelDown()
        {
            SetVerticalOffset(VerticalOffset + WheelSize);
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(HorizontalOffset - WheelSize);
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(HorizontalOffset + WheelSize);
        }

        public void MouseWheelUp()
        {
            SetVerticalOffset(VerticalOffset - WheelSize);
        }

        public void PageDown()
        {
            SetVerticalOffset(VerticalOffset + ViewportHeight);
        }

        public void PageLeft()
        {
            SetHorizontalOffset(HorizontalOffset - ViewportWidth);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + ViewportWidth);
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - ViewportHeight);
        }

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
            offset = Math.Max(0, Math.Min(offset, Math.Max(0, ExtentHeight - ViewportHeight)));
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

            for (int i = 0; i < visible; i++)
            {
                if (i + start >= LineCount)
                {
                    break;
                }

                FormattedText line = new(
                    Source.GetLine(start + i),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    FontSize,
                    _foreground,
                    _pixelsPerDip
                )
                {
                    Trimming = TextTrimming.None,
                    MaxLineCount = 1,
                };

                if (line.WidthIncludingTrailingWhitespace > _maxMeasuredWidth)
                {
                    _maxMeasuredWidth = line.WidthIncludingTrailingWhitespace;
                    ScrollOwner?.InvalidateScrollInfo();
                }

                ctx.DrawText(line, new Point(-HorizontalOffset, i * _lineHeight - firstLineOffset));
            }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textView = (TextView)d;
            textView._offset.Y = 0;
            textView._offset.X = 0;
            textView.InvalidateMeasure();
            textView.InvalidateVisual();
            textView.ScrollOwner?.InvalidateScrollInfo();
        }

        private static void OnLineCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textView = (TextView)d;
            textView.InvalidateVisual();
            textView.ScrollOwner?.InvalidateScrollInfo();
        }
    }
}
