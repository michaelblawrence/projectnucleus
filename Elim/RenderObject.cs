using System;
using System.Drawing;

namespace Elim
{
    public abstract class RenderObject : IDisposable
    {
        public float x { get { return mx; } set { mx = value; cx = value; } }
        public float y { get { return my; } set { my = value; cy = value; } }
        public float dx { get; set; }
        public float dy { get; set; }
        public float w { get { return mw; } set { mw = value; } }
        public float h { get { return mw; } set { mh = value; } }
        public Color Colour { get { return color; } set { color = value; brush = new SolidBrush(color); pen = new Pen(brush); } }
        public RectangleF rect { get { return new RectangleF(mx - mw / 2, my - mh / 2, mw, mh); } }
        public int Index { get => index; set => index = value; }
        public PointF ResetPosition { get; set; }
        public PointF Position { get => new PointF(mx, my); set { x = value.X; y = value.Y; } }
        public UiSettings BaseSettings { get => uiSettings; }
        public float GameTime { get => gameTime; }

        protected float mx, my;
        protected float cx, cy;
        protected Brush brush;
        protected Pen pen;
        protected Color color;
        protected float gameTime = 0;
        protected float radius = 0;
        protected UiSettings uiSettings;
        private int index;
        protected float mw, mh;

        protected static int playState = 1;
        protected static float def_radius = 15;
        protected static Color def_color = Color.Black;

        public RenderObject(UiSettings uiSettings)
        {
            w = def_radius * 2;
            h = def_radius * 2;
            x = uiSettings.Width / 2.0f;
            y = uiSettings.Width / 2.0f;
            ResetPosition = new PointF(x, y);
            w = h = def_radius * 2;
            color = def_color;
            brush = new SolidBrush(color);
            pen = new Pen(brush);
            this.uiSettings = uiSettings;
        }

        public RenderObject(UiSettings uiSettings, RectangleF bounds, Color color) : this(uiSettings)
        {
            this.x = bounds.X;
            this.y = bounds.Y;
            ResetPosition = new PointF(x, y);
            this.w = bounds.Width;
            this.h = bounds.Height;
            this.color = color;
            brush = new SolidBrush(color);
            pen = new Pen(brush);
            this.uiSettings = uiSettings;
        }

        public RenderObject(UiSettings uiSettings, float x, float radius, Color color) : this(uiSettings)
        {
            this.x = x;
            this.w = this.h = radius > 0 ? radius * 2 : def_radius * 2;
            this.color = color;
        }

        public RenderObject SetIndex(int index)
        {
            this.index = index;
            return this;
        }

        /// <summary>
        /// Sets RenderObject's play state to a ENABLED value
        /// </summary>
        public static void PlaySimulation()
        {
            playState = 1;
        }

        /// <summary>
        /// Sets RenderObject's play state to a DISABLED value
        /// </summary>
        public static void PauseSimulation()
        {
            playState = 0;
        }

        public static Brush GetWhiteBrushFromAlpha(float alpha)
        {
            return new SolidBrush(Color.FromArgb((int)(Math.Min(1, Math.Max(alpha, 0)) * 255), Color.White));
        }

        /// <summary>
        /// Draws RenderObject on to given graphics object
        /// </summary>
        /// <param name="g">Graphics instance onto which to draw</param>
        public abstract void DrawElement(Graphics g);

        /// <summary>
        /// Updates the current state of the RenderObject (usually called every game frame)
        /// </summary>
        /// <param name="delta">The amount of game milliseconds that have passed since the last call of this method</param>
        public abstract void UpdateElement(int delta);

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ((IDisposable)brush).Dispose();
                    pen.Dispose();
                }
                disposedValue = true;
            }
        }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }

}
