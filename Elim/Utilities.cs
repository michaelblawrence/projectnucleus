using System.Collections.Generic;
using System;
using System.Drawing;
using System.Linq;

namespace Elim.Utils
{
    public static class Utilities
    {
        #region static classes

        /// <summary>
        /// Timing class that provides event and true/false interface to a given single tick timeout
        /// </summary>
        public class GameTimer
        {
            public enum TimerState
            {
                Idle,
                Running,
                Expired
            }

            /// <summary>
            /// init value for timer's last start time (in game seconds)
            /// </summary>
            const float LAST_TIME_INIT = -10;


            private float length;
            private float startTime = LAST_TIME_INIT;
            private float lastTime = LAST_TIME_INIT;
            private TimerState state = TimerState.Idle;

            /// <summary>
            /// The start time of the previous timeout in game seconds
            /// </summary>
            public float StartTime { get => startTime; }
            public float Length { get => length; set => length = value; }
            public float Elapsed { get => lastTime - startTime; }
            public TimerState State { get => state; }
            public object Data { get; set; }


            public delegate void OnTimerExpired(object sender, EventArgs e);
            public event OnTimerExpired TimerElapsed;


            private static List<GameTimer> _timers = new List<GameTimer>();

            /// <summary>
            /// Inits new GameTimer instance
            /// </summary>
            /// <param name="length">Length, in seconds, of the timer object</param>
            public GameTimer(float length)
            {
                this.Length = length;
            }

            /// <summary>
            /// Begins timer countdown
            /// </summary>
            /// <param name="gameTime">Gametime to begin countdown</param>
            /// <returns>This GameTimer object</returns>
            public GameTimer Start(float gameTime)
            {
                this.startTime = gameTime;
                state = TimerState.Running;
                _timers.Add(this);
                return this;
            }

            /// <summary>
            /// Updates timer's gametime and returns current state
            /// </summary>
            /// <param name="gameTime">Time, in seconds, passed since the game was started</param>
            /// <returns>Current state of the GameTimer</returns>
            public TimerState Update(float gameTime)
            {
                if (state == TimerState.Running)
                {
                    lastTime = gameTime;
                    if (Elapsed < length) return state;
                    Completed();
                    return state;
                }
                else return state;
            }

            protected void Completed()
            {
                state = TimerState.Expired;
                TimerElapsed?.Invoke(this, new EventArgs());
                _timers.Remove(this);
            }

            /// <summary>
            /// Updates all active timers' gametime
            /// </summary>
            /// <param name="gameTime">Time, in seconds, passed since the game was started</param>
            public static void UpdateGameTime(float gameTime)
            {
                for (int i = 0; i < _timers.Count; i++)
                {
                    _timers[i].Update(gameTime);
                }
            }
        }


        public static class Collisions
        {

            /// <summary>
            /// Updates bomb collision physics for player-bomb and bomb-bomb interactions for a given player
            /// </summary>
            /// <param name="p">Current player</param>
            public static void CollisionWorker(Player p)
            {
                foreach (Player player in GameManager.players)
                {
                    var bombs = player.Ball.Bombs.Where(bomb => bomb.BombCollision(p.Ball.Position)).ToList();
                    if (bombs.Count > 0) p.Ball.UpdateBombCollision(true, bombs.First().ResponsiblePlayer);
                    else p.Ball.UpdateBombCollision(false, player.Index);
                }

                foreach (Player player in GameManager.players) p.Ball.Bombs.ForEach(eachbomb =>
                {
                    var bombs = player.Ball.Bombs.Where(bomb => bomb.BombCollision(eachbomb.Position)).ToList();
                    if (bombs.Count > 0/* && bombs[0].GetHashCode() != eachbomb.GetHashCode()*/) eachbomb.UpdateBombCollision(true, bombs.First().ResponsiblePlayer);
                    else eachbomb.UpdateBombCollision(false, player.Index);
                    //eachbomb.UpdateBombCollision(player.Ball.Bombs.Any(bomb => bomb.BombCollision(eachbomb.Position)), player.Index);
                });
            }

            /// <summary>
            /// Detects if there is a collision between two circles
            /// </summary>
            /// <param name="x1">Position of circle 1</param>
            /// <param name="x2">Position of circle 2</param>
            /// <param name="r1">Radius of circle 1</param>
            /// <param name="r2">Radius of circle 2</param>
            /// <returns>True if the circles overlap, False otherwise</returns>
            public static bool CircleCollisionDetection(PointF x1, PointF x2, float r1, float r2)
            {
                float xDistance = x1.X - x2.X;
                float yDistance = x1.Y - x2.Y;
                float thresholdDistance = r1 + r2;
                float distSq = xDistance * xDistance + yDistance * yDistance;
                return distSq < thresholdDistance * thresholdDistance;
            }

            /// <summary>
            /// Returns two-dimensional velocity vectors of two intersecting circles after collision
            /// </summary>
            /// <param name="p1">Position of circle 1</param>
            /// <param name="p2">Position of circle 2</param>
            /// <param name="v1">Velocity of circle 1</param>
            /// <param name="v2">Velocity of circle 2</param>
            /// <returns>SizeF array (size 2) of the size of circle one and two's respective new velocities</returns>
            public static SizeF[] CircleCollision2D(PointF p1, PointF p2, SizeF v1, SizeF v2)
            {
                return CircleCollision2D(p1, p2, v1, v2, 1.0f, 1.0f);
            }

            /// <summary>
            /// Returns two-dimensional velocity vectors of two intersecting circles after collision
            /// </summary>
            /// <param name="x1">Position of circle 1</param>
            /// <param name="x2">Position of circle 2</param>
            /// <param name="v1">Velocity of circle 1</param>
            /// <param name="v2">Velocity of circle 2</param>
            /// <param name="m1">Mass of circle 1</param>
            /// <param name="m2">Mass of circle 2</param>
            /// <returns>SizeF array (size 2) of the size of circle one and two's respective new velocities</returns>
            public static SizeF[] CircleCollision2D(PointF p1, PointF p2, SizeF v1, SizeF v2, float m1, float m2)
            {
                var x1 = PtToSize(p1);
                var x2 = PtToSize(p2);


                var one_term1 = 2 * m2 / (m1 + m2);
                var x1minusx2 = x1 - x2;
                var one_term2 = Dot(v1 - v2, x1minusx2) / x1minusx2.LengthSq();
                var one_term3 = x1 - x2;

                var two_term1 = 2 * m1 / (m2 + m1);
                var x2minusx1 = x2 - x1;
                var two_term2 = Dot(v2 - v1, x2minusx1) / x2minusx1.LengthSq();
                var two_term3 = x2 - x1;

                return new SizeF[] { v1 - one_term3.Multiply(one_term1 * one_term2), v2 - two_term3.Multiply(two_term1 * two_term2) };
            }

        }

        #endregion

        #region static methods

        /// <summary>
        /// Multiplies two color values together
        /// </summary>
        /// <param name="c1">Colour 1</param>
        /// <param name="c2">Colour 2</param>
        /// <returns>Color corresponding to channel wise multiplication of c1 and c2</returns>
        public static Color ColMul(this Color c1, Color c2)
        {
            float[] f1 = new float[] { c1.A / 255.0f, c1.R / 255.0f, c1.G / 255.0f, c1.B / 255.0f };
            float[] f2 = new float[] { c2.A / 255.0f, c2.R / 255.0f, c2.G / 255.0f, c2.B / 255.0f };
            byte[] col = new byte[] { (byte)(f1[0] * f2[0] * 255), (byte)(f1[1] * f2[1] * 255), (byte)(f1[2] * f2[2] * 255), (byte)(f1[3] * f2[3] * 255) };
            return Color.FromArgb(col[0], col[1], col[2], col[3]);
        }

        /// <summary>
        /// Multiplies a color by a value
        /// </summary>
        /// <param name="c1">Colour 1</param>
        /// <param name="value">Multiplication factor</param>
        /// <returns>Color corresponding to the clamped multiplication of c1 and value</returns>
        public static Color ColMul(this Color c1, float value)
        {
            value = Clamp(value);
            float[] f1 = new float[] { c1.A / 255.0f, c1.R / 255.0f, c1.G / 255.0f, c1.B / 255.0f };
            byte[] col = new byte[] { (byte)(f1[0] * value * 255), (byte)(f1[1] * value * 255), (byte)(f1[2] * value * 255), (byte)(f1[3] * value * 255) };
            return Color.FromArgb(col[0], col[1], col[2], col[3]);
        }

        /// <summary>
        /// Multiplies a color by a value
        /// </summary>
        /// <param name="c1">Colour 1</param>
        /// <param name="value">Multiplication factor</param>
        /// <returns>Color corresponding to the clamped multiplication of c1 and value</returns>
        public static Color ColMul(this Color c1, double value)
        {
            value = Clamp(value);
            float[] f1 = new float[] { c1.A / 255.0f, c1.R / 255.0f, c1.G / 255.0f, c1.B / 255.0f };
            byte[] col = new byte[] { (byte)(f1[0] * value * 255), (byte)(f1[1] * value * 255), (byte)(f1[2] * value * 255), (byte)(f1[3] * value * 255) };
            return Color.FromArgb(col[0], col[1], col[2], col[3]);
        }

        /// <summary>
        /// Returns the square distance between two points
        /// </summary>
        /// <param name="pt1"></param>
        /// <param name="pt2"></param>
        /// <returns>Floating point number representing pixel distance betweeen pt1 and pt2</returns>
        public static float SqDist(PointF pt1, PointF pt2)
        {
            float xDist = pt1.X - pt2.X;
            float yDist = pt1.Y - pt2.Y;
            return (xDist * xDist + yDist * yDist);
        }

        /// <summary>
        /// Clamps a number so that 0 < value < 1
        /// </summary>
        /// <param name="value">Value to be clamped</param>
        /// <returns>Clamped single floating point number</returns>
        public static float Clamp(float value) => Clamp(value, 0, 1);

        /// <summary>
        /// Clamps a number so that 0 < value < 1
        /// </summary>
        /// <param name="value">Value to be clamped</param>
        /// <returns>Clamped single floating point number</returns>
        public static double Clamp(double value) => Clamp(value, 0, 1);

        /// <summary>
        /// Clamps a number so that min < value < max
        /// </summary>
        /// <param name="value">Value to be clamped</param>
        /// <param name="min">Minimum value to be returned</param>
        /// <param name="max">Maximum value to be returned</param>
        /// <returns>Clamped single floating point number</returns>
        public static float Clamp(float value, float min, float max) => Math.Max(Math.Min(value, max), min);

        /// <summary>
        /// Clamps a number so that min < value < max
        /// </summary>
        /// <param name="value">Value to be clamped</param>
        /// <param name="min">Minimum value to be returned</param>
        /// <param name="max">Maximum value to be returned</param>
        /// <returns>Clamped double floating point number</returns>
        public static double Clamp(double value, double min, double max) => Math.Max(Math.Min(value, max), min);

        /// <summary>
        /// Draw string centered with Roboto font
        /// </summary>
        /// <param name="g">Graphics object to draw on</param>
        /// <param name="str">String containing text to draw</param>
        /// <param name="centre">Center of the text</param>
        /// <param name="size">Size of the text</param>
        /// <param name="colour">Colour of the text</param>
        public static void DrawCentredString(Graphics g, string str, PointF centre, float size, Color colour)
        {
            Brush b = new SolidBrush(colour);
            Font f = new Font("Roboto", size);
            SizeF txsize = g.MeasureString(str, f);
            g.DrawString(str, f, b, centre.X - txsize.Width / 2, centre.Y - txsize.Height / 2);

        }

        #endregion

        #region utils classes

        /// <summary>
        /// Length (Magnitude) of two dimensional vectors
        /// </summary>
        /// <param name="pt">2 dimensional vector expressed as a SizeF object</param>
        /// <returns>Scalar magnitude of pt vector</returns>
        public static float Length(this SizeF pt)
        {
            var sq = pt.LengthSq();
            return (float)Math.Sqrt(sq);
        }

        /// <summary>
        /// Square of the length (Magnitude) of two dimensional vectors
        /// </summary>
        /// <param name="pt">2 dimensional vector expressed as a SizeF object</param>
        /// <returns>Scalar square magnitude of pt vector</returns>
        public static float LengthSq(this SizeF pt)
        {
            var sq = pt.Multiply(pt);
            return sq.Width + sq.Height;
        }

        /// <summary>
        /// Multiplication of 2 two dimensional vectors
        /// </summary>
        /// <param name="pt">vector 1 for multiplication expressed as a SizeF object</param>
        /// <param name="point">vector 2 for multiplication expressed as a SizeF object</param>
        /// <returns>new two dimensional vector of the product of pt and point</returns>
        public static SizeF Multiply(this SizeF pt, SizeF point)
        {
            return new SizeF(pt.Width * point.Width, pt.Height * point.Height);
        }

        /// <summary>
        /// Multiplication of a two dimensional vector and a scalar value
        /// </summary>
        /// <param name="pt">vector 1 for multiplication expressed as a SizeF object</param>
        /// <param name="factor">scalar for multiplication</param>
        /// <returns>new two dimensional vector of the product of pt and factor</returns>
        public static SizeF Multiply(this SizeF pt, float factor)
        {
            return new SizeF(pt.Width * factor, pt.Height * factor);
        }

        /// <summary>
        /// Multiplication of 2 two dimensional vectors
        /// </summary>
        /// <param name="pt">vector 1 for multiplication expressed as a PointF object</param>
        /// <param name="point">vector 2 for multiplication expressed as a PointF object</param>
        /// <returns>new two dimensional vector of the product of pt and point</returns>
        public static PointF Multiply(this PointF pt, PointF point)
        {
            return new PointF(pt.X * point.X, pt.Y * point.Y);
        }

        /// <summary>
        /// Dot product of 2 two dimensional vectors
        /// </summary>
        /// <param name="pt">vector 1 for dot product expressed as a SizeF object</param>
        /// <param name="point">vector 2 for dot product expressed as a SizeF object</param>
        /// <returns>new two dimensional vector of the dot product of pt and point</returns>
        public static float Dot(this SizeF pt, SizeF point)
        {
            return pt.Width * point.Width + pt.Height * point.Height;
        }

        /// <summary>
        /// Dot product of 2 two dimensional vectors
        /// </summary>
        /// <param name="pt">vector 1 for dot product expressed as a PointF object</param>
        /// <param name="point">vector 2 for dot product expressed as a PointF object</param>
        /// <returns>new two dimensional vector of the dot product of pt and point</returns>
        public static float Dot(this PointF pt, PointF point)
        {
            return pt.X * point.X + pt.Y * point.Y;
        }

        /// <summary>
        /// Offset of a 2 two dimensional vector by a set scalar value
        /// </summary>
        /// <param name="pt">vector 1 for offset expressed as a SizeF object</param>
        /// <param name="point">scalar for omnidirectional offset</param>
        /// <returns>new two dimensional vector of pt offset by point</returns>
        public static SizeF Add(this SizeF pt, float point)
        {
            return new SizeF(pt.Width + point, pt.Height + point);
        }

        /// <summary>
        /// Converts a two dimensional vector expressed as a PointF to a SizeF
        /// </summary>
        /// <param name="pt">two dimensional vector</param>
        /// <returns>SizeF representation</returns>
        public static SizeF PtToSize(PointF pt)
        {
            return new SizeF(pt.X, pt.Y);
        }

        #endregion
    }
}