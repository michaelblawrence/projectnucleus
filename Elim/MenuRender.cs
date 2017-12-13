using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static Elim.Global;
using static Elim.GameManager;
using static Elim.Utils.Utilities;
using System.Linq;
using System.Collections.Generic;

namespace Elim
{
    public class MenuRender : Renderer, IDisposable
    {
        const string ASSET_MENU_LABEL = "labels";
        const string ASSET_MENU_CIRCLES = "circles";

        public static float bgGradientAngle = 0;

        private static PointF zonePos;
        float zoneRad;
        float zoneAlpha = 0;

        float lastCountdownTime = 0;
        int numactiveplayers = 0;
        bool drawCountDown = false;

        BitmapAssetManager imgs;

        public MenuRender(UiSettings uiSettings) : base(uiSettings)
        {
            imgs = new BitmapAssetManager();
        }
        internal override void initComputationObjects()
        {

        }

        internal override void initDisplayObjects()
        {
            bool error = false;
            error = !imgs.LoadImage(ASSET_MENU_LABEL, Resources.Res.assets_menu_labels);
            error = !imgs.LoadImage(ASSET_MENU_CIRCLES, Resources.Res.assets_menu_zones_1beta);
            if (error) MessageBox.Show("Error. Check console");

            zonePos = new PointF(uiSettings.Width / 2, uiSettings.Height * 582 / 1080);
            zoneRad = uiSettings.Height * 179.0f / 1080;
        }

        public override void RenderFrame(Graphics g, int delta)
        {
            UpdateGameTime(delta);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            RenderBackground(g);

            float progress = Clamp((gameTime - lastCountdownTime) / R.MENU_STARTCOUNTDOWN);

            if (drawCountDown)
            {
                float ii =progress * 3;
                DrawCentredString(g, (new string[] { "3", "2", "1!" })[(int) Clamp(ii, 0, 2)], zonePos, R.MENU_UI_FONTSIZE, Color.FromArgb((int)(255 * (1 - ii % 1)), R.SAFEAREA_SCORECOLOUR));
            }
            bool pDrawCountDown = drawCountDown;
            drawCountDown = false;
            CountdownWorker(g, progress);

            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                p.Ball.UpdateElement(delta, p.Controller.GetState().Gamepad);
                p.Ball.DrawElement(g);
                Collisions.CollisionWorker(p);


            }
            if (pDrawCountDown)
            {
                float ii = progress * 3;
                DrawCentredString(g, (new string[] { "3", "2", "1!" })[(int)Clamp(ii, 0, 2)], zonePos, R.MENU_UI_FONTSIZE, Color.FromArgb((int)(zoneAlpha * 255 * (1 - ii % 1)), R.SAFEAREA_SCORECOLOUR));
            }
        }

        private void RenderBackground(Graphics g)
        {
            var rect = new RectangleF(PointF.Empty, new SizeF(uiSettings.Width, uiSettings.Height));
            g.FillRectangle(new LinearGradientBrush(rect, R.BG_MENU_COLOUR1, R.BG_MENU_COLOUR2, bgGradientAngle += 0.5f), rect);
            g.DrawImageUnscaled(imgs.Get(ASSET_MENU_LABEL), Point.Empty);
            g.DrawImageUnscaled(imgs.Get(ASSET_MENU_CIRCLES), Point.Empty);
        }

        private void CountdownWorker(Graphics g, float progress)
        {
            var activeplayers = players.Where(PlayerOverPlayZone).ToList();
            if (numactiveplayers != activeplayers.Count)
            {
                lastCountdownTime = gameTime;
                if (activeplayers.Count == R.UTILS_MINPLAYERS) lastCountdownTime = gameTime - R.MENU_STARTCOUNTDOWN * zoneAlpha;
            }
            else if (activeplayers.Count >= R.UTILS_MINPLAYERS)
            {
                RunPlayCountdown(activeplayers, progress, g);
            }
            else
            {
                zoneAlpha *= 1 - R.MENU_STARTCOUNTFADERATE / 100.0f;
            }
            numactiveplayers = activeplayers.Count;

            if (zoneAlpha > 0.001)
            {
                using (var brush = new SolidBrush(Color.FromArgb((int)(200 * zoneAlpha), R.MENU_UI_ZONECOLOUR)))
                {
                    g.FillEllipse(brush, zonePos.X - zoneRad, zonePos.Y - zoneRad, 2 * zoneRad, 2 * zoneRad);
                }
            }
        }


        /// <summary>
        /// Begins the countdown to a game with the provided players
        /// </summary>
        private void RunPlayCountdown(List<Player> activeplayers, float progress, Graphics g)
        {
            float zoneDelta = (1 - zoneAlpha) * progress;
            zoneDelta = zoneDelta * zoneDelta;
            zoneAlpha += zoneDelta;
            

            drawCountDown = true;
            if (zoneAlpha >= 0.8 && progress>=1)
            {
                PlayGame();
            }
        }

        private static void PlayGame()
        {
            players.ForEach(p => p.Reset(true, true, true));
            LoadState(GameState.Game);
        }

        private bool PlayerOverPlayZone(Player p)
        {
            float checkRad = zoneRad - R.BALL_RADIUS;
            return SqDist(p.Ball.Position, zonePos) < checkRad * checkRad;
        }

        internal override void MessageRecieved(object sender, UiSettingsEventArgs e)
        {
            KeyEventArgs eventArgsK;
            MouseEventArgs eventArgsM;

            if ((e.Flags & UiSettingsEventArgs.FLAGS_UI) != 0)
                switch (e.Type)
                {
                    case UiSettingsEventArgs.EventType.KeyUpEvent:
                        eventArgsK = (KeyEventArgs)(e.EventInfo);
                        //Insert on KeyUp Event here
                        switch (eventArgsK.KeyCode)
                        {

                            default:
                                break;
                        }

                        break;
                    case UiSettingsEventArgs.EventType.KeyDownEvent:
                        eventArgsK = (KeyEventArgs)(e.EventInfo);
                        //Insert on KeyDown Event here

                        break;
                    case UiSettingsEventArgs.EventType.KeyPressEvent:
                        eventArgsK = (KeyEventArgs)(e.EventInfo);
                        //Insert on KeyPress Event here

                        break;
                    case UiSettingsEventArgs.EventType.MouseEvent:
                        if (e.EventInfo.GetType() == typeof(EventArgs))
                        {
                            //Insert on MouseClick Event here
                        }
                        else
                        {
                            eventArgsM = (MouseEventArgs)(e.EventInfo);
                            PointF mousept = new PointF(eventArgsM.X, eventArgsM.Y);

                            if ((e.Flags & UiSettingsEventArgs.FLAGS_UI_MOUSEDOWN) != 0)
                            {
                                //Insert on MouseDown Event here
                            }
                            else if ((e.Flags & UiSettingsEventArgs.FLAGS_UI_MOUSEUP) != 0)
                            {
                                MessageBox.Show(String.Format("X:{0}, Y:{1}", mousept.X, mousept.Y));
                            }
                            else
                            {
                                if (eventArgsM.Button == MouseButtons.Left)
                                {
                                    //Insert on MouseDrag Event here
                                }
                            }
                        }
                        break;
                }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    imgs.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MenuRender() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
