using System;
using System.Drawing;
using System.Windows.Forms;
using SharpDX.XInput;
using System.Linq;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using static Elim.Global;
using static Elim.GameManager;
using static Elim.Utils.Utilities;
using System.Media;

namespace Elim
{
    public abstract class Renderer
    {
        protected int frameDelta;
        protected UiSettings uiSettings;
        protected float gameTime = 0;

        public Renderer(UiSettings uiSettings)
        {
            this.uiSettings = uiSettings;
            this.uiSettings.uiSettingsChanged += MessageRecieved;
        }

        internal abstract void MessageRecieved(object sender, UiSettingsEventArgs e);
        internal abstract void initComputationObjects();
        internal abstract void initDisplayObjects();
        public abstract void RenderFrame(Graphics g, int delta);

        public void UpdateGameTime(float delta)
        {
            gameTime += delta / 1000.0f;

        }

    }
    public class GameRenderer : Renderer
    {
        SafeArea safeArea;
        HUD hud;
        public static float bgGradientAngle;
        private static bool inplay;
        private static bool inplay_p;


        public enum GameState
        {
            PlayersHold = 1,
            InPlay = 2,
            StartPlay = 4 | 1,
            Paused = 8,
            Overtime = 16 | 2,
        }

        public static GameState CurrentState = GameState.PlayersHold;

        public static bool InPlay { get => ((int)CurrentState & (int)GameState.InPlay) != 0; set => CurrentState = value ? GameState.InPlay : GameState.PlayersHold; }


        public GameRenderer(UiSettings uiSettings) : base(uiSettings)
        {
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
                            case Keys.M:
                                if (GameManager.PlayingMusic) GameManager.StopMusic();
                                else GameManager.PlayMusic();
                                break;
                            case Keys.Up:
                                AudioLevel += 1;
                                break;
                            case Keys.Down:
                                AudioLevel -= 1;
                                break;
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
                                //Insert on MouseUp Event here
                                var cornershit = safeArea.Corners.Where(c => c.Contains(mousept));
                                try
                                {
                                    if (cornershit.Count() > 0) MessageBox.Show(players[cornershit.First().CornerId].Stats.ToString());
                                }
                                catch (ArgumentOutOfRangeException )
                                {

                                }
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

        internal override void initComputationObjects()
        {
        }

        internal override void initDisplayObjects()
        {
            safeArea = new SafeArea(uiSettings, new RectangleF(0, 0, uiSettings.Width, uiSettings.Height), R.BALL_COLOUR, players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                players[i].initDisplayObjects(uiSettings);
                players[i].Ball.OnDie += Ball_OnDie;
                players[i].SetSafeArea(safeArea);
                players[i].Ball.StartCollisionDetection();
            }
            hud = new HUD(uiSettings, safeArea, players);
            players.ForEach(p => p.SetupHUD(hud));
        }

        public override void RenderFrame(Graphics g, int delta)
        {
            UpdateGameTime(delta);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            frameDelta = delta;

            if (players.All(p => p.Controller.GetState().Gamepad.Buttons == GamepadButtonFlags.Back)) NewGame();
            if (InPlay && hud.MatchTime >= R.MATCH_TIMELIMIT && players.All(p => p.Ball.Bombs.Count == 0)) // If normal play conditions have come to an end
            {
                float maxscore = players.Max(p => p.Score);
                if (players.Count(p => p.Score >= maxscore) > 1)
                {
                    if (CurrentState == GameState.InPlay)
                        RunOvertime(); // (runs once) begin overtime
                }
                else
                    NewGame(); // Only one winner. Begin new game
            }
            if (InPlay && !inplay_p)
            {
                players.ForEach(p => p.Reset(true, false, true));

            }
            inplay_p = InPlay;

            var rect = new RectangleF(PointF.Empty, new SizeF(uiSettings.Width, uiSettings.Height));
            bgGradientAngle += InPlay ? 0.5f : 0.65f;
            if (CurrentState == GameState.Overtime)
            {
                bgGradientAngle += 0.3f;
                g.FillRectangle(new LinearGradientBrush(rect, R.BG_GAME_COLOUR1.ColMul(Clamp(Math.Sin(bgGradientAngle / 60.0))+0.3), R.BG_GAME_COLOUR2.ColMul(Clamp(Math.Sin(bgGradientAngle / 60.0 + 2)) + 0.3), bgGradientAngle), rect);
            }
            else
                g.FillRectangle(new LinearGradientBrush(rect, R.BG_GAME_COLOUR1, R.BG_GAME_COLOUR2, bgGradientAngle), rect);

            //if (players.Any(p => !p.Controller.IsConnected) ||
            //    new int[] { 0, 1, 2, 3 }.ToList().ConvertAll(i => new Controller((UserIndex)i).IsConnected).Count(b => b) != players.Count)
            //{
            //    initComputationObjects();
            //    initDisplayObjects();
            //}

            for (int i = 0; i < players.Count; i++)
            {
                Player p = players[i];
                p.Ball.UpdateElement(delta, p.Controller.GetState().Gamepad);
                p.Ball.DrawElement(g);
                Collisions.CollisionWorker(p);

            }

            hud.UpdateElement(delta);

            safeArea.DrawElement(g);
            hud.DrawElement(g);

            if (CurrentState == GameState.StartPlay) CurrentState= GameState.PlayersHold;
        }



        /// <summary>
        /// Generates a new game session and resets player states
        /// </summary>
        private void NewGame()
        {
            HUD.LastMatchTime = gameTime;
            HUD.BeenReset = true;
            hud.MatchTime = 0;
            CurrentState = GameState.StartPlay;
            var maxscore = players.Max(p => p.Score);
            players.ForEach(p => p.Stats.UpdateMatchResult(p.Score < maxscore ? 
                Player.Statistics.MatchResult.Loss : Player.Statistics.MatchResult.Win
                ));
            players.ForEach(p => { p.Stats.ResetMatchStats(); p.Reset(false, true, true); });
        }

        /// <summary>
        /// Resets players and starts overtime counter
        /// </summary>
        private void RunOvertime()
        {
            CurrentState = GameState.Overtime;
            //hud.MatchTime = 
            players.Reset(false, true, true);
        }

        private void Ball_OnDie(object sender, EventArgs e)
        {
            var ball = (Ball)sender;
            int deadplayer = ball.Index;
            var dieMethod = ball.LastDieMethod;
            UpdateScores(ball, deadplayer, dieMethod);
        }

        public void Reset()
        {
            NewGame();
        }

        private void UpdateScores(Ball ball, int deadplayer, Ball.DieMethod dieMethod)
        {
            switch (dieMethod)
            {
                case Ball.DieMethod.AnotherPlayer:
                    int killerplayer = ball.LastDiePlayerId;
                    players[killerplayer].Score += R.SCORE_SINGLEKILL;
                    players[killerplayer].Stats.AddKill();
                    players[killerplayer].Vibrate();
                    players[deadplayer].Stats.AddDeath();
                    break;
                case Ball.DieMethod.Suicide:
                    for (int i = 0; i < players.Count; i++)
                    {
                        if (i == deadplayer) continue;
                        if (!players[i].InPlay) continue;
                        players[i].Score += R.SCORE_SUICIDEDEATH;
                    }
                    players[deadplayer].Stats.AddDeath();
                    players[deadplayer].Stats.AddSuicide();
                    break;
            }
        }
    }
}
