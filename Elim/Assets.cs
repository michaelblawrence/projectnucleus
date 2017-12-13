using SharpDX.XInput;
using System.Collections.Generic;
using System.Drawing;
using System;
using System.Linq;
using static Elim.Global;
using static Elim.Utils.Utilities;
using static Elim.Utils.Utilities.Collisions;

namespace Elim
{
    public class Ball : RenderObject
    {
        List<Bomb> bombs = new List<Bomb>();
        PointF spawnPosition;

        bool inPlay = false;

        const float LAST_TIME_INIT = -10;

        float lastBombTime = LAST_TIME_INIT;
        float lastSteerBombTime = LAST_TIME_INIT;
        float lastDieTime = LAST_TIME_INIT;
        float lastDieTetherTime = 0;
        PointF lastDiePosition = PointF.Empty;
        private GameTimer vibTimer;

        int lastDiePlayerId = -1;
        DieMethod lastDieMethod = DieMethod.None;

        Player player;
        private Controller controller;
        private SafeArea safeArea;
        HUD hud;

        double lookDirection = 0;
        private bool vibrating = false;

        private float mass = 1;
        private float pmass = 1;
        private float dmass = 0;

        private float scorealpha = 0;

        float px, py;
        private bool InJail { get => gameTime - lastDieTetherTime < R.BALL_JAILTIME; }

        public List<Bomb> Bombs { get => bombs; set => bombs = value; }
        public DieMethod LastDieMethod { get => lastDieMethod; set => lastDieMethod = value; }
        public int LastDiePlayerId { get => lastDiePlayerId; set => lastDiePlayerId = value; }

        public bool GameCanStart { get => !GameRenderer.InPlay && ((!HUD.BeenReset) || (gameTime - HUD.LastMatchTime > Global.R.MATCH_POSTGAMETIME)); }
        public bool InPlay { get => inPlay; set => inPlay = value; }
        public float Mass { get => mass; set => mass = value; }
        public float DeltaMass { get => dmass; set => dmass = value; }

        private Gamepad prevControlerState;
        private bool allowBounces = false;
        private float siner = 0;

        public delegate void PlayerDied(object sender, EventArgs e);
        public event PlayerDied OnDie;

        public Ball(UiSettings uiSettings) : base(uiSettings)
        {

        }
        public Ball(UiSettings uiSettings, RectangleF bounds, Color color, Player player) : base(uiSettings, bounds, color)
        {
            spawnPosition = new PointF(mx, my);
            this.player = player;
            this.Index = player.Index;
            this.controller = player.Controller;
        }


        public void SetupSafeArea(SafeArea safeArea, PointF spawnPos)
        {
            this.safeArea = safeArea;
            spawnPosition.X = spawnPos.X;
            spawnPosition.Y = spawnPos.Y;
            Reset();
        }

        public void SetupHUD(HUD hud)
        {
            this.hud = hud;
        }

        public void UpdateElement(int delta, Gamepad gamepad)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;


                float leftright = gamepad.LeftThumbX / (float)short.MaxValue;
                if (leftright < R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD && leftright > -R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD) leftright = 0;
                else
                {
                    if (GameCanStart) GameRenderer.InPlay = true;
                    inPlay = true;
                }
                float updown = gamepad.LeftThumbY / (float)short.MaxValue;
                if (updown < R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD && updown > -R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD) updown = 0;
                else
                {
                    if (GameCanStart) GameRenderer.InPlay = true;
                    inPlay = true;
                }

                //right = controller.GetState().Gamepad.Buttons == GamepadButtonFlags.DPadRight;
                dx += R.BALL_SPEED / 10f * leftright * delta;
                dy -= R.BALL_SPEED / 10f * updown * delta;

                lookDirection = Math.Atan(-updown / leftright);
                if (leftright < 0) lookDirection += Math.PI;

                if (InJail || GameRenderer.CurrentState == GameRenderer.GameState.PlayersHold)
                {
                    float xDist = spawnPosition.X - mx;
                    float yDist = spawnPosition.Y - my;
                    dx *= 1 - (xDist * xDist / (Global.R.BALL_JAILRADIUS * Global.R.BALL_JAILRADIUS) * Global.R.BALL_JAILTETHERSTRENGTH);
                    dy *= 1 - (yDist * yDist / (Global.R.BALL_JAILRADIUS * Global.R.BALL_JAILRADIUS) * Global.R.BALL_JAILTETHERSTRENGTH);
                    dx += xDist * R.BALL_JAILPULL;
                    dy += yDist * R.BALL_JAILPULL;
                    //dy += 1 - (yDist * yDist / (Global.R.BALL_JAILRADIUS * Global.R.BALL_JAILRADIUS));
                }
                else
                {
                }
                float fl = 0.1f;
                scorealpha += gamepad.Buttons == GamepadButtonFlags.Y ? fl : 0;
                scorealpha *= 0.97f;
                scorealpha = Math.Min(scorealpha, 1);
                mass = Math.Max(mass + dmass, 0.1f);
                dmass += (1 - mass) * Global.R.BALL_BALLSIZESTREGNTH;
                dmass *= Global.R.BALL_BALLSIZEDAMPER;
                //if (vibrating && GameTime - lastVibrateTime > R.UTILS_CONTROLLER_VIBRATETIME) Vibrate(false);

                if (mx >= uiSettings.Width - R.BALL_RADIUS)
                {
                    dx = -dx * R.BALL_WALLBOUNCINESS;
                    mx = uiSettings.Width - R.BALL_RADIUS;
                    Vibrate(R.UTILS_CONTROLLER_BOUNCEVIBRATETIME, R.UTILS_CONTROLLER_BOUNCEVIBRATESTRENGTH * (dx * dx / 256.0f));
                }

                if (mx < R.BALL_RADIUS)
                {
                    dx = -dx * R.BALL_WALLBOUNCINESS;
                    mx = R.BALL_RADIUS;
                    Vibrate(R.UTILS_CONTROLLER_BOUNCEVIBRATETIME, R.UTILS_CONTROLLER_BOUNCEVIBRATESTRENGTH * (dx * dx / 256.0f));
                }

                if (my < R.BALL_RADIUS)
                {
                    dy = -dy * R.BALL_WALLBOUNCINESS;
                    my = R.BALL_RADIUS;
                    Vibrate(R.UTILS_CONTROLLER_BOUNCEVIBRATETIME, R.UTILS_CONTROLLER_BOUNCEVIBRATESTRENGTH * (dy * dy / 256.0f));
                }

                if (my >= uiSettings.Height - R.BALL_RADIUS)
                {
                    dy = -dy * R.BALL_WALLBOUNCINESS;
                    my = uiSettings.Height - R.BALL_RADIUS;
                    Vibrate(R.UTILS_CONTROLLER_BOUNCEVIBRATETIME, R.UTILS_CONTROLLER_BOUNCEVIBRATESTRENGTH * (dy * dy / 256.0f));
                }
                mx += dx;
                my += dy;
                dx *= R.BALL_DRAG;
                dy *= R.BALL_DRAG;

                if (gamepad.Buttons == GamepadButtonFlags.A && prevControlerState.Buttons != GamepadButtonFlags.A)
                {
                    Fire();
                }
                else if (gamepad.Buttons != GamepadButtonFlags.A && prevControlerState.Buttons == GamepadButtonFlags.A)
                {
                    LightFuse();
                }
                if (gamepad.LeftTrigger > 0.6f && prevControlerState.LeftTrigger <= 0.6f)
                {
                    FireSteerer();
                    if (GameManager.CurrentState == GameManager.GameState.Menu)mass *= 1 + R.BALL_BALLSIZEINCREMENT;
                }
                else if (prevControlerState.LeftTrigger > 0.6f && gamepad.LeftTrigger <= 0.6f)
                {
                    LightSteererFuse();
                }
                if (gamepad.RightTrigger > 0.6f && prevControlerState.RightTrigger <= 0.6f)
                {
                    if (GameRenderer.CurrentState == GameRenderer.GameState.Overtime)
                    {
                        FireSteerer();

                    }
                    else
                    {
                        Fire();
                        if (GameManager.CurrentState == GameManager.GameState.Menu) mass *= 1 + R.BALL_BALLSIZEINCREMENT;
                    }
                }
                else if (prevControlerState.RightTrigger > 0.6f && gamepad.RightTrigger <= 0.6f)
                {
                    if (GameRenderer.CurrentState == GameRenderer.GameState.Overtime)
                    {
                    LightSteererFuse();
                    }
                    else
                    {
                        LightFuse();
                    }
                }
                prevControlerState = gamepad;

                if (hud.MatchTime - HUD.LastMatchTime >= R.MATCH_TIMELIMIT) bombs.ForEach(bomb => bomb.LightFuse());

                BounceWorker();
                bombs.RemoveAll(bomb => bomb.Exploded);

                foreach (var bomb in bombs)
                {
                    bomb.UpdateElement(delta, gamepad);
                }


                px = mx;
                py = my;
                pmass = mass;
            }
        }

        private void BounceWorker()
        {
            if (!allowBounces) return;
            var collidingballs = GameManager.players.Where(p=>p.Index!=Index && CircleCollisionDetection(this.Position, p.Ball.Position, R.BALL_RADIUS * Mass, R.BALL_RADIUS * p.Ball.Mass)).ToList();
            int n = collidingballs.Count();
            float collidespeed = 0;
            for (int i = 0; i < n; i++)
            {
                var _player = collidingballs.ElementAt(i);
                var _ball = _player.Ball;
                this.mx = px;
                this.my = py;
                this.mass = pmass;
                float reldx = this.dx - _ball.dx;
                float reldy = this.dy - _ball.dy;
                float v = (float)Math.Sqrt(reldx * reldx + reldy * reldy);
                collidespeed += v;
                SizeF[] newSpeeds = CircleCollision2D(this.Position, _ball.Position, new SizeF(this.dx, this.dy)/*.Add(dmass)*/, new SizeF(_ball.dx, _ball.dy)/*.Add(_ball.DeltaMass)*/, Mass, _ball.Mass);
                this.dx = newSpeeds[0].Width; this.dy = newSpeeds[0].Height;
                _ball.dx = newSpeeds[1].Width; _ball.dy = newSpeeds[1].Height;
                _player.Vibrate(R.UTILS_CONTROLLER_BOUNCEVIBRATETIME /** v / 26.0f*/, R.UTILS_CONTROLLER_BOUNCEVIBRATESTRENGTH * v / 13.0f);
            }
            if (n > 0)
                Vibrate(R.UTILS_CONTROLLER_BOUNCEVIBRATETIME /** collidespeed / 26.0f*/, R.UTILS_CONTROLLER_BOUNCEVIBRATESTRENGTH * collidespeed / 13.0f);
        }

        public void StartCollisionDetection()
        {
            allowBounces = true;
        }

        public void UpdateBombCollision(bool collided, int playerId)
        {
            if (!inPlay) return;
            if (gameTime - lastDieTime <= R.BALL_JAILPROTECTIONTIME + R.BALL_JAILTIME) return;
            if (GameRenderer.CurrentState == GameRenderer.GameState.Overtime && player.Score < GameManager.players.MaxScore) return;
            if (collided && !InJail)
            { 
                DieMethod dm = DieMethod.None;
                if (Index == playerId) dm = DieMethod.Suicide;
                else if (playerId >= 0) dm = DieMethod.AnotherPlayer;
                Die(dm, playerId);
            }
        }

        private void Die(DieMethod dieMethod, int playerId)
        {
            Vibrate(R.UTILS_CONTROLLER_DEFVIBRATETIME);
            LastDieMethod = dieMethod;
            LastDiePlayerId = playerId;
            OnDie.Invoke(this, new EventArgs());
            lastDieTime = gameTime;
            Reset();
        }

        public void Vibrate(float vibLength)
        {
            Vibrate(true, vibLength, R.UTILS_CONTROLLER_VIBRATESTRENGTH);
        }
        public void Vibrate(float vibLength, float vibStrength)
        {
            Vibrate(true, vibLength, vibStrength);
        }

        private void Vibrate(bool enabled, float vibLength, float vibStrength)
        {
            vibrating = enabled;
            Vibration v = new Vibration()
            {
                LeftMotorSpeed = enabled ? (ushort)(ushort.MaxValue * vibStrength) : (ushort)0,
                RightMotorSpeed = enabled ? (ushort)(ushort.MaxValue * vibStrength) : (ushort)0
            };
            controller.SetVibration(v);
            if (enabled)
            {
                vibTimer = new GameTimer(vibLength);
                vibTimer.TimerElapsed += (sender, e) => { Vibrate(false, -1, -1); };
                vibTimer.Start(gameTime);
            }
        }

        public void Reset()
        {
            lastDiePosition.X = mx;
            lastDiePosition.Y = my;
            x = spawnPosition.X;
            y = spawnPosition.Y;
            bombs.ForEach(bomb => bomb.LightFuse());
            lastDieTetherTime = gameTime;
        }

        public void ResetSteerer()
        {
            lastSteerBombTime = gameTime;
        }

        /// <summary>
        /// Determines if player firing of any bomb type is allowe
        /// </summary>
        /// <returns>Returns true if currently allows, false otherwise</returns>
        private bool FiringAllowed()
        {
            if (hud.MatchOver) return false;
            if (hud.TimeRemaining < 0 && GameRenderer.CurrentState != GameRenderer.GameState.Overtime) return false;
            if (InJail || GameRenderer.CurrentState == GameRenderer.GameState.PlayersHold) return false;
            if (GameManager.players.Count(p => p.Score == GameManager.players.Max(pl=>pl.Score)) == 1 && GameRenderer.CurrentState == GameRenderer.GameState.Overtime) return false;    
            if (InJail) return false;
            if (GameManager.CurrentState != GameManager.GameState.Game) return false;

            return true;
        }

        private void Fire()
        {
            if (!FiringAllowed()) return;
            bool ot = GameRenderer.CurrentState == GameRenderer.GameState.Overtime;
            if (gameTime - lastBombTime > (ot ? R.BOMB_OT_RELOADTIME : R.BOMB_RELOADTIME) && !InJail)
            {
                dx *= 1 - R.BALL_FIREFRICTION;
                dy *= 1 - R.BALL_FIREFRICTION;
                lastBombTime = gameTime;
                Bomb b = new Bomb(uiSettings, new RectangleF(mx, my, R.BOMB_RADIUS, R.BOMB_RADIUS), R.BOMB_COLOUR, Index);
                b.SetSpeed(dx * R.BOMB_SPEEDFRICTION, dy * R.BOMB_SPEEDFRICTION);
                if (!R.UTILS_LIGHTFUSEONRELEASE)
                    b.LightFuse();
                bombs.Add(b);
                player.PlaySound(Player.SoundFX.DropBomb);
            }
        }

        private void LightFuse()
        {
            var norms = bombs.Where(b => b.Type == Bomb.BombType.Normal);
            if (R.UTILS_LIGHTFUSEONRELEASE && norms.Count() > 0)
                bombs.Last().LightFuse();
        }

        private void FireSteerer()
        {
            if (!FiringAllowed()) return;
            bool ot = GameRenderer.CurrentState == GameRenderer.GameState.Overtime;
            if (gameTime - lastSteerBombTime > (ot ? R.BOMB_STEER_OT_RELOADTIME : R.BOMB_STEER_RELOADTIME) && !InJail)
            {
                dx *= 1 - R.BALL_FIREFRICTION;
                dy *= 1 - R.BALL_FIREFRICTION;
                lastSteerBombTime = gameTime;
                Bomb b = new Bomb(uiSettings, new RectangleF(mx, my, R.BOMB_RADIUS, R.BOMB_RADIUS), R.BOMB_STEER_COLOUR, Index);
                b.SetSpeed(dx * R.BOMB_STEER_SPEEDFRICTION, dy * R.BOMB_STEER_SPEEDFRICTION);
                b.MakeSteerable();
                b.SteerEnabled = true;
                bombs.Add(b);
                player.PlaySound(Player.SoundFX.DropBomb);
            }
        }

        private void LightSteererFuse()
        {
            var steers = bombs.Where(b => b.Type == Bomb.BombType.Steered);
            if (steers.Count() > 0)
                steers.Last().LightFuse();
        }

        public override void DrawElement(Graphics g)
        {
            foreach (var bomb in bombs)
            {
                bomb.DrawElement(g);
            }
            if (InJail || GameRenderer.CurrentState == GameRenderer.GameState.PlayersHold)
            {
                float width = R.BALL_JAILTETHERWIDTH;
                
                if (!IsInsideOwnJail())
                    g.DrawLine(new Pen(new SolidBrush(Color.FromArgb(80, Color.White)), width), spawnPosition, Position);
            }
            float bw = w * mass;
            float bh = h * mass;
            float br = R.BALL_RADIUS * mass;
            g.FillEllipse(brush, mx - bw / 2, my - bh / 2, bw, bh); // draw white bomb circle
            Font font = new Font("Roboto", 20);
            float padding = 9;
            float othermax = GameManager.players.MaxScore;
            if (othermax == player.Score) othermax = GameManager.players.Where(p => p.Index != player.Index).Max(p => p.Score);
            string scoretext = player.Score + ":" + othermax;
            g.DrawString(scoretext, font, new SolidBrush(Color.FromArgb((int)(255 * scorealpha), Color.White)), mx + bw / 2 + padding, my - padding); // draw white bomb circle
            float lookrad = R.PLAYER_INDICATORRADIUS * (1 + (mass - 1) * 0.4f);
            float maxR = br - lookrad;
            Color lookcol = R.PLAYER_COLOURS[Index];
            bool protect = (gameTime - lastDieTime <= R.BALL_JAILPROTECTIONTIME + R.BALL_JAILTIME) || (GameRenderer.CurrentState == GameRenderer.GameState.Overtime && player.Score < GameManager.players.MaxScore);
            Brush lookBrush = new SolidBrush(Color.FromArgb(protect ? (int)(Math.Sin(siner += 0.2f) * 90) + 165 : 255,lookcol));
            float indicatorangle = 360 * Math.Min(1, (gameTime - lastSteerBombTime) / (GameRenderer.CurrentState == GameRenderer.GameState.Overtime ? R.BOMB_STEER_OT_RELOADTIME : R.BOMB_STEER_RELOADTIME));
            if (double.IsNaN(lookDirection)) // drawing indicator in centre of ball when there is no direction input
            {
                lookrad *= R.PLAYER_INDICATORCENTRERADIUS;
                g.FillPie(lookBrush, mx - lookrad,
                    my - lookrad,
                    lookrad * 2, lookrad * 2, 0, indicatorangle);
            }
            else // drawing indicator in direction of ball steering
            {
                g.FillPie(lookBrush, mx - lookrad + maxR * (float)Math.Cos(lookDirection),
                    my - lookrad + maxR * (float)Math.Sin(lookDirection),
                    lookrad * 2, lookrad * 2, 0, indicatorangle);
            }

            if (gameTime - lastDieTime <= R.BALL_DEATHSHADOWTIME)
            {
                g.FillEllipse(new SolidBrush(R.BALL_DEATHSHADOWCOLOUR.ColMul(lookcol)), lastDiePosition.X - bw / 2, lastDiePosition.Y - bh / 2, bw, bh);
            }

        }


        private bool IsInsideOwnJail()
        {
            //SafeArea.Corner corner = null;
            //return safeArea != null && !(corner = safeArea.Corners[index]).Contains(Position);
            return safeArea != null && safeArea.Corners[Index].Contains(Position);
        }

        public override void UpdateElement(int delta)
        {
            throw new NotImplementedException();
        }


        public enum DieMethod
        {
            None = -1,
            AnotherPlayer = 0,
            Suicide = 1,
        }
    }

    public class Bomb : RenderObject
    {
        public enum BombType
        {
            Normal,
            Steered
        }

        private int bombState = -1; // 0= Growing; 1 = MAX RADIUS
        private float fuseTime = -1;
        private float stateTime = -1;
        private bool steerEnabled = false;

        private float px, py;

        private BombType type = BombType.Normal;

        private float bombRadius = 0;

        private bool exploded = false;
        private int responsiblePlayer;

        public bool Exploded { get => exploded; set => exploded = value; }
        public bool FuseLit { get => fuseTime >= 0; }
        public bool Idle { get => bombState < 0; }
        internal int ResponsiblePlayer { get => responsiblePlayer; set => responsiblePlayer = value; }
        public BombType Type { get => type; set => type = value; }
        public bool SteerEnabled { get => steerEnabled; set => steerEnabled = value; }

        public Bomb(UiSettings uiSettings) : base(uiSettings)
        {

        }
        public Bomb(UiSettings uiSettings, RectangleF bounds, Color color, int playerId) : base(uiSettings, bounds, color)
        {
            responsiblePlayer = playerId;
        }

        public void UpdateElement(int delta, Gamepad gamepad)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;

                if (steerEnabled)
                {
                    float leftright = gamepad.RightThumbX / (float)short.MaxValue;
                    if (leftright < R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD && leftright > -R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD) leftright = 0;
                    float updown = gamepad.RightThumbY / (float)short.MaxValue;
                    if (updown < R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD && updown > -R.UTILS_CONTROLLER_ANOLOUGE_THRESHOLD) updown = 0;

                    //right = controller.GetState().Gamepad.Buttons == GamepadButtonFlags.DPadRight;
                    dx += R.BOMB_STEER_SPEED / 10f * leftright * delta;
                    dy -= R.BOMB_STEER_SPEED / 10f * updown * delta;
                }

                if (mx >= uiSettings.Width - R.BALL_RADIUS)
                {
                    dx = -dx * R.BALL_WALLBOUNCINESS;
                    mx = uiSettings.Width - R.BALL_RADIUS;
                }

                if (mx < R.BALL_RADIUS)
                {
                    dx = -dx * R.BALL_WALLBOUNCINESS;
                    mx = R.BALL_RADIUS;
                }

                if (my < R.BALL_RADIUS)
                {
                    dy = -dy * R.BALL_WALLBOUNCINESS;
                    my = R.BALL_RADIUS;
                }

                if (my >= uiSettings.Height - R.BALL_RADIUS)
                {
                    dy = -dy * R.BALL_WALLBOUNCINESS;
                    my = uiSettings.Height - R.BALL_RADIUS;
                }

                mx += dx;
                my += dy;
                //float drag = 0.89f;
                dx *= R.BOMB_STEER_DRAG;
                dy *= R.BOMB_STEER_DRAG;

                BounceWorker();
                px = mx; py = my;


                if (FuseLit && (gameTime - fuseTime) >= R.BOMB_EXPL_FUSETIME && Idle)
                {
                    Explode();
                    bombState = 0;
                }



                if (bombRadius > 0)
                {
                    int prevbombState = bombState;
                    float nextradius = bombRadius * 1 + R.BOMB_EXPL_GROWRATE / 100;
                    float radius = (GameRenderer.CurrentState == GameRenderer.GameState.Overtime) ? R.BOMB_EXPL_OT_RADIUS : R.BOMB_EXPL_RADIUS;
                    if (nextradius < R.BOMB_EXPL_RADIUS)
                    {
                        bombRadius = nextradius;
                    }
                    else if (bombRadius < R.BOMB_EXPL_RADIUS)
                    {
                        bombRadius = R.BOMB_EXPL_RADIUS;
                        bombState = 1;
                    }
                    else
                    {
                        bombState = 1;
                    }

                    if (bombState == 1)
                    {
                        if (prevbombState != bombState)
                        {
                            stateTime = gameTime;
                        }
                        if (gameTime - stateTime > R.BOMB_EXPL_DEFUSETIME)
                        {
                            bombRadius = 0;
                            bombState++;
                            exploded = true;
                        }
                    }
                }
            }
        }

        private void BounceWorker()
        {
            for (int i = 0; i < GameManager.players.Count; i++)
            {
                Player player = GameManager.players[i];
                if (player.Index == Index) continue;
                var collidingbombs = player.Ball.Bombs.Where(b=> CircleCollisionDetection(this.Position, b.Position, R.BOMB_RADIUS, R.BOMB_RADIUS)).ToList();
                int n = collidingbombs.Count();
                for (int ii = 0; i < n; i++)
                {
                    var _bombs = collidingbombs.ElementAt(ii);
                    this.mx = px;
                    this.my = py;
                    float reldx = this.dx - _bombs.dx;
                    float reldy = this.dy - _bombs.dy;
                    float v = (float)Math.Sqrt(reldx * reldx + reldy * reldy);
                    SizeF[] newSpeeds = CircleCollision2D(this.Position, _bombs.Position, new SizeF(this.dx, this.dy)/*.Add(dmass)*/, new SizeF(_bombs.dx, _bombs.dy)/*.Add(_ball.DeltaMass)*/);
                    this.dx = newSpeeds[0].Width; this.dy = newSpeeds[0].Height;
                    _bombs.dx = newSpeeds[1].Width; _bombs.dy = newSpeeds[1].Height;
                }
            }
            
        }

        public void SetSpeed(float dx, float dy)
        {
            this.dx = dx;
            this.dy = dy;
        }

        public void MakeSteerable()
        {
            type = BombType.Steered;
        }

        private void Explode()
        {
            bombRadius = Math.Max(bombRadius, R.BOMB_RADIUS / 2);
        }

        public void LightFuse()
        {
            if (!FuseLit && Idle)
                fuseTime = gameTime;
        }

        public void UpdateBombCollision(bool collided, int playerId)
        {
            if (collided && Idle)
            {
                responsiblePlayer = playerId;
                Explode();
            }
        }

        public bool BombCollision(PointF ball)
        {
            if (Idle) return false;
            return CircleCollisionDetection(Position, ball, bombRadius, R.BALL_RADIUS);
        }

        public override void DrawElement(Graphics g)
        {
            if (exploded) return;
            Pen p = new Pen(new SolidBrush(R.BOMB_COLOURSTROKE), R.BOMB_STROKESIZE);
            float shade_w = R.BOMB_SHADESCALE * w;
            float shade_h = R.BOMB_SHADESCALE * h;
            g.FillEllipse(new SolidBrush(R.BOMB_COLOURSHADE),
                mx - shade_w / 2 + R.BOMB_SHADEOFFSET * (float)Math.Cos(GameRenderer.bgGradientAngle * RAD_CONV),
                my - shade_h / 2 + R.BOMB_SHADEOFFSET * (float)Math.Sin(GameRenderer.bgGradientAngle * RAD_CONV)
                , shade_w, shade_h);
            g.FillEllipse(brush, mx - w / 2, my - h / 2, w, h);
            g.DrawEllipse(p, mx - w / 2, my - h / 2, w, h);

            if (bombRadius > 0)
                g.FillEllipse(new SolidBrush(R.BOMB_EXPL_COLOUR),
                    mx - bombRadius, my - bombRadius,
                    bombRadius * 2, bombRadius * 2);
        }

        public override void UpdateElement(int delta)
        {
            throw new NotImplementedException();
        }
    }

    public class Food : RenderObject
    {
        public Food(UiSettings uiSettings) : base(uiSettings)
        {

        }
        public Food(UiSettings uiSettings, RectangleF bounds, Color color) : base(uiSettings, bounds, color)
        {

        }

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;
                mx += dx;
                my += dy;
                //float drag = 0.89f;
                //dx *= drag;
                //dy *= drag;
            }
        }

        public override void DrawElement(Graphics g)
        {
            g.FillEllipse(brush, mx - w / 2, my - h / 2, w, h);
        }
    }

    public class HUD : RenderObject
    {
        private SafeArea safeArea;
        private Party players;

        private static float lastMatchTime = 0;
        private float matchTime = 0;

        public static float LastMatchTime { get => lastMatchTime; set => lastMatchTime = value; }
        public float MatchTime { get => matchTime; set => matchTime = value; }
        public bool MatchOver { get => MatchTime > R.MATCH_TIMELIMIT && !GameRenderer.InPlay; }
        public float TimeRemaining { get => R.MATCH_TIMELIMIT - matchTime; }
        public static bool BeenReset { get; set; }


        public HUD(UiSettings uiSettings, SafeArea safeArea, Party players) : base(uiSettings)
        {
            this.safeArea = safeArea;
            this.players = players;
        }
        public HUD(UiSettings uiSettings, RectangleF bounds, Color color, SafeArea safeArea, Party players) : base(uiSettings, bounds, color)
        {
            this.safeArea = safeArea;
            this.players = players;
        }

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;
                if (GameRenderer.InPlay)
                    matchTime += playState * delta / 1000.0f;
            }
        }

        public override void DrawElement(Graphics g)
        {
            if (GameRenderer.InPlay)
            {
                string clock = "";
                if (GameRenderer.CurrentState == GameRenderer.GameState.Overtime)
                {
                    clock = String.Format("{0:0}", Math.Abs(TimeRemaining));
                    DrawCentredString(g, "OVERTIME",
                        new PointF(uiSettings.Width / 2 ,
                        //uiSettings.Height / 2 ),
                        R.UI_HUDTIMEYPADDING),
                        R.MENU_UI_FONTSIZE, Color.FromArgb(255, Color.White));
                }
                else
                {
                    clock = String.Format(TimeRemaining < R.UI_HUD1DPTIMERANGE ? "{0:0.0}" : "{0:0}", Math.Max(TimeRemaining, 0));
                }

                float shadowdepth = 3;
                DrawCentredString(g, clock,
                    new PointF(uiSettings.Width / 2+ shadowdepth,
                    //uiSettings.Height / 2+ shadowdepth),
                    //R.UI_HUDTIMEYPADDING),
                    uiSettings.Height - R.UI_HUDTIMEYPADDING),
                    R.UI_FONTSIZETIMER, Color.Black);
                DrawCentredString(g, clock,
                    new PointF(uiSettings.Width / 2 ,
                    //uiSettings.Height / 2 ),
                    uiSettings.Height - R.UI_HUDTIMEYPADDING),
                    R.UI_FONTSIZETIMER, Color.FromArgb(255, Color.White));
            }

            if (players.Count == 0) return;
            float maxScore = players.Max(p => p.Score);

            for (int i = 0; i < players.Count; i++)
            {
                var pt = safeArea.Corners[i].GetPointOnDiagonol(R.UI_HUDSCOREPOSITION);
                DrawCentredString(g, String.Format("{0:0}", players[i].Score), pt, R.UI_FONTSIZE,
                    players[i].Score >= maxScore ? R.SAFEAREA_HIGHSCORECOLOUR :  R.SAFEAREA_SCORECOLOUR);
            }
        }

    }

    public class SafeArea : RenderObject
    {
        private RectangleF[] areaRectangles = new RectangleF[4];
        private Corner[] corners = new Corner[4];
        private int cornerCount = 4;

        public RectangleF[] AreaRectangles { get => areaRectangles; set => areaRectangles = value; }
        public Corner[] Corners { get => corners; set => corners = value; }

        public SafeArea(UiSettings uiSettings, int corners) : base(uiSettings)
        {
            cornerCount = corners;
            SetupCorners(uiSettings);
        }
        public SafeArea(UiSettings uiSettings, RectangleF bounds, Color color, int corners) : base(uiSettings, bounds, color)
        {
            cornerCount = corners;
            SetupCorners(uiSettings);
        }

        private void SetupCorners(UiSettings uiSettings)
        {
            SizeF size = new SizeF(R.SAFEAREA_RADIUS, R.SAFEAREA_RADIUS);
            areaRectangles[(int)Corner.GameCorner.TopLeft] = new RectangleF(new PointF(0, 0), size);
            areaRectangles[(int)Corner.GameCorner.BottomRight] = new RectangleF(new PointF(uiSettings.Width - size.Width, uiSettings.Height - size.Height), size);
            areaRectangles[(int)Corner.GameCorner.TopRight] = new RectangleF(new PointF(uiSettings.Width - size.Width, 0), size);
            areaRectangles[(int)Corner.GameCorner.BottomLeft] = new RectangleF(new PointF(0, uiSettings.Height - size.Height), size);

            for (int i = 0; i < 4; i++)
            {
                corners[i] = new Corner(areaRectangles[i], (Corner.GameCorner)i); 
            }
        }

        public override void UpdateElement(int delta)
        {
            if (playState != 0)
            {
                gameTime += playState * delta / 1000.0f;
                mx += dx;
                my += dy;
                float drag = 0.89f;
                dx *= drag;
                dy *= drag;
            }
        }

        public override void DrawElement(Graphics g)
        {
            Brush b = new SolidBrush(R.SAFEAREA_COLOUR);

            if (cornerCount >= 1) g.FillPolygon(b, corners[(int)Corner.GameCorner.TopLeft].GetPoints());
            if (cornerCount >= 2) g.FillPolygon(b, corners[(int)Corner.GameCorner.BottomRight].GetPoints());
            if (cornerCount >= 3) g.FillPolygon(b, corners[(int)Corner.GameCorner.TopRight].GetPoints());
            if (cornerCount >= 4) g.FillPolygon(b, corners[(int)Corner.GameCorner.BottomLeft].GetPoints());

            //int i = -1;
            //if (cornerCount >= 1)
            //{
            //    i = (int)Triangle.GameCorner.TopLeft;
            //    g.FillPolygon(b, new PointF[] {
            //    new PointF(corners[i].Left, corners[i].Top),
            //    new PointF(corners[i].Left, corners[i].Bottom),
            //    new PointF(corners[i].Right, corners[i].Top),
            //});
            //}

            //if (cornerCount >= 2)
            //{
            //    g.FillPolygon(b, new PointF[] {
            //    new PointF(corners[1].Right, corners[1].Top),
            //    new PointF(corners[1].Right, corners[1].Bottom),
            //    new PointF(corners[1].Left, corners[1].Bottom),
            //});
            //}

            //if (cornerCount >= 3)
            //{
            //    g.FillPolygon(b, new PointF[] {
            //    new PointF(corners[2].Right, corners[2].Top),
            //    new PointF(corners[2].Right, corners[2].Bottom),
            //    new PointF(corners[2].Left, corners[2].Top),
            //});
            //}

            //if (cornerCount >= 4)
            //{
            //    g.FillPolygon(b, new PointF[] {
            //    new PointF(corners[3].Left, corners[3].Top),
            //    new PointF(corners[3].Left, corners[3].Bottom),
            //    new PointF(corners[3].Right, corners[3].Bottom),
            //});
            //}
        }

        public class Corner
        {
            public enum GameCorner
            {
                TopLeft = 0,
                BottomRight = 1,
                TopRight = 2,
                BottomLeft = 3
            }

            RectangleF rect;
            GameCorner corner;

            private PointF[] pts = new PointF[3];
            private bool calcPts = false;
            private PointF centre;
            private bool calcCentre = false;
            private int cornerId = -1;

            public PointF[] Points { get => calcPts ? pts : GetPoints(); }
            public PointF Centre { get { if (calcCentre) return centre; else { centre = new PointF(Points.Average(p => p.X), Points.Average(p => p.Y)); calcCentre = true; return centre; } } }

            public int CornerId { get => cornerId; set => cornerId = value; }

            public Corner(RectangleF rect, GameCorner corner)
            {
                this.corner = corner;
                this.rect = rect;
                cornerId = (int)corner;
            }

            public PointF[] GetPoints()
            {
                int i = (int)corner;
                PointF[] pts;
                switch (corner)
                {
                    case GameCorner.TopLeft:
                        pts = new PointF[] {
                            new PointF(rect.Left, rect.Top),
                            new PointF(rect.Left, rect.Bottom),
                            new PointF(rect.Right, rect.Top),
                        };
                        break;
                    case GameCorner.BottomRight:
                        pts = new PointF[] {
                            new PointF(rect.Right, rect.Bottom),
                            new PointF(rect.Right, rect.Top),
                            new PointF(rect.Left, rect.Bottom),
                        };
                        break;
                    case GameCorner.TopRight:
                        pts = new PointF[] {
                            new PointF(rect.Right, rect.Top),
                            new PointF(rect.Right, rect.Bottom),
                            new PointF(rect.Left, rect.Top),
                        };
                        break;
                    case GameCorner.BottomLeft:
                        pts = new PointF[] {
                            new PointF(rect.Left, rect.Bottom),
                            new PointF(rect.Left, rect.Top),
                            new PointF(rect.Right, rect.Bottom),
                        };
                        break;
                    default:
                        return null;
                }

                this.pts = pts;
                calcPts = true;
                return pts;
            }

            private float Sign(PointF p1, PointF p2, PointF p3)
            {
                return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
            }

            public bool Contains(PointF pt)
            {
                if (!rect.Contains(pt)) return false;
                bool b1, b2, b3;

                b1 = Sign(pt, Points[0], Points[1]) < 0.0f;
                b2 = Sign(pt, Points[1], Points[2]) < 0.0f;
                b3 = Sign(pt, Points[2], Points[0]) < 0.0f;

                return ((b1 == b2) && (b2 == b3));
            }

            public PointF GetPointOnDiagonol(float a)
            {
                return new PointF(Centre.X * (1 - a) + Points[0].X * a, Centre.Y * (1 - a) + Points[0].Y * a);
            }
        }
    }
}
