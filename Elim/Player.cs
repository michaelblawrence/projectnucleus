using System;
using System.Drawing;
using SharpDX.XInput;
using static Elim.Global;
using System.Collections.Generic;
using System.Linq;
using System.Media;

namespace Elim
{
    public class Party : List<Player> {

        public float MaxScore { get => this.Max(p => p.Score); }

        public void Reset() => this.ForEach(p => p.Reset());
        public void Reset(bool resetScore, bool resetBall, bool resetBombs) => this.ForEach(p => p.Reset(resetScore, resetBall, resetBombs));

    }
    public class Player
    {
        private int index;
        private Ball ball;
        private Controller controller;
        private PointF spawnPos;
        private float score = 0;
        private Statistics stats;
        private SoundPlayer audioPlayer;

        public Ball Ball { get => ball; set => ball = value; }
        public int Index { get => index; }
        public Controller Controller { get => controller; }
        public float Score { get => score; set => score = value; }
        public bool InPlay { get => ball.InPlay; }
        public Statistics Stats { get => stats; set => stats = value; }
        public SoundPlayer AudioPlayer { get => audioPlayer; set => audioPlayer = value; }

        public Player(Controller controller, int index)
        {
            this.index = index;
            this.controller = controller;
            this.stats = new Statistics(index);
            audioPlayer = new SoundPlayer();
        }

        public void initDisplayObjects(UiSettings uiSettings)
        {
            spawnPos = new PointF(30 + index * 100, 40 + index * 100);
            ball = new Ball(uiSettings, new RectangleF(spawnPos.X, spawnPos.Y, R.BALL_RADIUS * 2, R.BALL_RADIUS * 2), R.BALL_COLOUR, this);
        }

        public void SetSafeArea(SafeArea safeArea)
        {
            spawnPos = new PointF(safeArea.AreaRectangles[index].X + safeArea.AreaRectangles[index].Width / 2,
                            safeArea.AreaRectangles[index].Y + safeArea.AreaRectangles[index].Height / 2);
            ball.SetupSafeArea(safeArea, spawnPos);
        }

        public void SetupHUD(HUD hud)
        {
            ball.SetupHUD(hud);
        }

        public void Vibrate(float vibLength)
        {
            ball.Vibrate(vibLength);
        }

        public void Vibrate(float vibLength, float vibStrength)
        {
            ball.Vibrate(vibLength, vibStrength);
        }

        public void Vibrate()
        {
            ball.Vibrate(R.UTILS_CONTROLLER_DEFVIBRATETIME);
        }

        public enum SoundFX
        {
            DropBomb
        }

        public void PlaySound(SoundFX sound)
        {
            switch (sound)
            {
                case SoundFX.DropBomb:
                    audioPlayer.Stream = Resources.Res.sfx_bomb_drop;
                    audioPlayer.Play();

                    break;
                default:
                    break;
            }
        }

        public void Reset()
        {
            Reset(true, true, true);
        }

        public void Reset(bool resetScore, bool resetBall, bool resetBombs)
        {
            if (resetBombs)
                ball.Bombs.Clear();
            if (resetBall)
                ball.Reset();
            if (resetScore)
                score = 00;
        }


        public class Statistics
        {
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Suicides { get; set; }

            public int MatchKillCount { get; set; }
            public int MatchDeathCount { get; set; }
            public int MatchSuicideCount { get; set; }

            public int MatchWins { get; set; }
            public int MatchLosses { get; set; }

            public int Index = -1;

            public Statistics(int playerIndex)
            {
                Kills = 0;
                Deaths = 0;
                Suicides = 0;
                MatchKillCount = 0;
                MatchDeathCount = 0;
                MatchSuicideCount = 0;
                MatchWins = 0;
                MatchLosses = 0;

                Index = playerIndex;

            }

            public override string ToString()
            {
                return String.Format("Player {0} - K{1} : D{2} (S{3})", Index + 1, Kills, Deaths, Suicides);
            }

            public void UpdateMatchResult(MatchResult result)
            {
                switch (result)
                {
                    case MatchResult.None:
                        break;
                    case MatchResult.Win:
                        MatchWins++;
                        break;
                    case MatchResult.Loss:
                        MatchLosses++;
                        break;
                    case MatchResult.Draw:
                        MatchWins++;
                        break;
                    default:
                        break;
                }
            }

            public void AddKill()
            {
                Kills++;
                MatchKillCount++;
            }

            public void AddDeath()
            {
                Deaths++;
                MatchDeathCount++;
            }
            public void AddSuicide()
            {
                Suicides++;
                MatchSuicideCount++;
            }

            public enum MatchResult { None, Win, Loss, Draw }

            public void ResetMatchStats()
            {
                MatchKillCount = 0;
                MatchDeathCount = 0;
                MatchSuicideCount = 0;
            }
        }
    }
}
