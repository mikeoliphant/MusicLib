using SkiaSharp;
using SkiaSharp.Views.Android;
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using UILayout;

namespace AndroidPlayer
{
    public class LayoutView : SkiaLayout
    {
        public SKCanvasView CanvasView { get; private set; }

        public LayoutView(Activity activity)
        {
            CanvasView = new SKCanvasView(activity);

            CanvasView.Touch += CanvasView_Touch;
            CanvasView.PaintSurface += OnPaintSurface;
        }

        public override void UpdateLayout()
        {
            base.UpdateLayout();

            CanvasView.Invalidate();
        }

        public override void AddDirtyRect(in RectF dirty)
        {
            base.AddDirtyRect(dirty);

            CanvasView.PostInvalidate();
        }

        private void CanvasView_Touch(object? sender, Android.Views.View.TouchEventArgs e)
        {
            Touch touch = new();

            switch (e.Event.Action)
            {
                case Android.Views.MotionEventActions.Down:
                    touch.TouchState = ETouchState.Pressed;
                    break;
                case Android.Views.MotionEventActions.Move:
                    touch.TouchState = ETouchState.Moved;
                    break;
                case Android.Views.MotionEventActions.Up:
                    touch.TouchState = ETouchState.Released;
                    break;
            }

            touch.Position = new Vector2(e.Event.GetX(), e.Event.GetY());

            HandleTouch(touch);

            e.Handled = true;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if ((e.Info.Width != Bounds.Width) || (e.Info.Height != Bounds.Height))
            {
                SetBounds(new RectF(0, 0, e.Info.Width, e.Info.Height));

                UpdateLayout();
            }

            GraphicsContext.Canvas = e.Surface.Canvas;

            if (!Bounds.IsEmpty)
                Draw();
        }
    }
}
