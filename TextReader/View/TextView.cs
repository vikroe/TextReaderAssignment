using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Globalization;

namespace TextReader.App.View
{
    internal class TextView : FrameworkElement, IScrollInfo
    {
        static TextView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(TextView),
                new FrameworkPropertyMetadata(typeof(TextView))
            );
        }

        private const double LineSize = 16;
        private const double WheelSize = 3 * LineSize;

        private bool canHorizontallyScroll;
        private bool canVerticallyScroll;
        private ScrollViewer scrollOwner;
        private Vector offset;
        private Size viewport;

        // FormattedText fields
        private Typeface _typeface = new Typeface(
            new FontFamily("Consolas"),
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal
        );
        private double _fontSize = 14;
        private Brush _foreground = Brushes.Black;

        public bool CanHorizontallyScroll
        {
            get => canHorizontallyScroll; 
            set => canHorizontallyScroll = value; 
        }

        public bool CanVerticallyScroll
        {
            get => canVerticallyScroll; 
            set => canVerticallyScroll = value;
        }
        public ScrollViewer ScrollOwner
        {
            get => scrollOwner;
            set => scrollOwner = value;
        }

        public double ExtentHeight => 1000 * LineSize;
        public double ExtentWidth => 1680;
        public double HorizontalOffset { get => offset.X; }
        public double VerticalOffset { get => offset.Y; }
        public double ViewportHeight { get => viewport.Height; }
        public double ViewportWidth { get => viewport.Width; }

        public void LineDown()
        {
            SetVerticalOffset(VerticalOffset + LineSize);
        }

        public void LineLeft()
        {
            SetHorizontalOffset(HorizontalOffset - LineSize);
        }

        public void LineRight()
        {
            SetHorizontalOffset(HorizontalOffset + LineSize);
        }

        public void LineUp()
        {
            SetVerticalOffset(VerticalOffset - LineSize);
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
            SetHorizontalOffset(HorizontalOffset - WheelSize);
        }

        public void PageRight()
        {
            SetHorizontalOffset(HorizontalOffset + WheelSize);
        }

        public void PageUp()
        {
            SetVerticalOffset(VerticalOffset - ViewportHeight);
        }

        public void SetHorizontalOffset(double offset)
        {
            offset = Math.Max(0, Math.Min(offset, Math.Max(0, ExtentWidth - ViewportWidth)));
            if (Math.Abs(offset - this.offset.X) < 0.01) return;

            this.offset.X = offset;
            ScrollOwner?.InvalidateScrollInfo();
            InvalidateVisual();
        }

        public void SetVerticalOffset(double offset)
        {
            offset = Math.Max(0, Math.Min(offset, Math.Max(0, ExtentHeight - ViewportHeight)));
            if (Math.Abs(offset - this.offset.Y) < 0.01) return;
            
            this.offset.Y = offset;
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
            if (viewport != finalSize)
            {
                viewport = finalSize;
                ClampOffset();
                ScrollOwner?.InvalidateScrollInfo();
            }
            return finalSize;
        }

        private void ClampOffset()
        {
            offset.Y = Math.Max(0, Math.Min(offset.Y, ExtentHeight - ViewportHeight));
            offset.X = Math.Max(0, Math.Min(offset.X, ExtentWidth - ViewportWidth));
        }

        protected override void OnRender(DrawingContext ctx) 
        {
            double start = (long)(VerticalOffset / LineSize);
            double visible = (int)((VerticalOffset + ViewportHeight) / LineSize - start);

            for (int i = 0; i < visible; i++)
            {
                FormattedText line = new FormattedText(
                    "hello hello + " + (i + start),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    _typeface,
                    _fontSize,
                    _foreground
                );
                ctx.DrawText(line, new Point(0, i * LineSize));
            }
        }
    }
}
