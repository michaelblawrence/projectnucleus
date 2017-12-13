using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elim
{
    public class Global
    {
        // Loadable vars here

        public const double RAD_CONV = Math.PI / 180.0;

        public static Global R;

        public int UTILS_MAXPLAYERS = 4;
        public int UTILS_MINPLAYERS = 2;
        public float UTILS_SIZE_SCALE = 1.4f;
        //public GameManager.GameState UTILS_LOADSTATE = GameManager.GameState.Menu;
        public GameManager.GameState UTILS_LOADSTATE = GameManager.GameState.Game;
        public float UTILS_CONTROLLER_ANOLOUGE_THRESHOLD = 0.14f;
        public bool UTILS_LIGHTFUSEONRELEASE = true;
        public float UTILS_CONTROLLER_DEFVIBRATETIME = 0.1f;
        public float UTILS_CONTROLLER_VIBRATESTRENGTH = 0.72f;
        public float UTILS_CONTROLLER_BOUNCEVIBRATETIME = 0.08f;
        public float UTILS_CONTROLLER_BOUNCEVIBRATESTRENGTH = 0.34f;

        public float MATCH_TIMELIMIT = 30;
        public float MATCH_POSTGAMETIME = 3.2f;

        public float MENU_STARTCOUNTDOWN = 4.6f;
        public float MENU_STARTCOUNTFADERATE = 1f;
        public float MENU_UI_FONTSIZE = 140.0f;
        public Color MENU_UI_COUNTCOLOUR = Color.FromArgb(250, 255, 255, 255);
        public Color MENU_UI_ZONECOLOUR = Color.FromArgb(255, 147, 0, 239);
        
        public float SCORE_SINGLEKILL = 1;
        public float SCORE_SUICIDEDEATH = 1;

        public float UI_FONTSIZE = 40.0f;
        public float UI_FONTSIZETIMER = 70.0f;
        public float UI_HUDSCOREPOSITION = 0.4f;
        public float UI_HUDTIMEYPADDING = 80f;
        public float UI_HUD1DPTIMERANGE = 2.1f;
        public bool UTILS_PLAYMUSIC = false;

        public float BALL_SPEED = 1.08f;
        public float BALL_DRAG = 0.89f;
        public float BALL_RADIUS = 10;
        public Color BALL_COLOUR = Color.White;
        public float BALL_DEATHSHADOWTIME = 0.5f;
        public Color BALL_DEATHSHADOWCOLOUR = Color.FromArgb(200, 100, 100, 100);
        public float BALL_FIREFRICTION = 0.8f;
        public float BALL_WALLBOUNCINESS = 1.3f;
        public float BALL_JAILTIME = 1.0f;
        public float BALL_JAILPROTECTIONTIME = 0.75f;
        public float BALL_JAILRADIUS = 200f;
        public float BALL_JAILPULL = 0.03f;
        public float BALL_JAILTETHERWIDTH = 3f;
        public float BALL_JAILTETHERSTRENGTH = 0.80f;
        public float BALL_BALLSIZESTREGNTH = 0.03f;
        public float BALL_BALLSIZEINCREMENT = 0.7f;
        public float BALL_BALLSIZEDAMPER = 0.75f;

        public float BOMB_RADIUS = 16f;
        public float BOMB_SPEEDFRICTION = 1.9f;
        public float BOMB_STROKESIZE = 3f;
        public Color BOMB_COLOUR = Color.FromArgb(249, 252, 184, 39);
        public Color BOMB_COLOURSHADE = Color.FromArgb(50, 0, 0, 0);
        public Color BOMB_COLOURSTROKE = Color.FromArgb(200, 0, 0, 0);
        public float BOMB_SHADEOFFSET = 5f;
        public float BOMB_SHADESCALE = 1.2f;
        public float BOMB_RELOADTIME = 0.55f;
        public float BOMB_OT_RELOADTIME = 0.25f;

        public float BOMB_STEER_SPEED = 0.45f;
        public float BOMB_STEER_SPEEDFRICTION = 12f;
        public float BOMB_STEER_DRAG = 0.89f;
        public float BOMB_STEER_RELOADTIME = 5f;
        public Color BOMB_STEER_COLOUR = Color.FromArgb(249, 252, 140, 29);
        public float BOMB_STEER_OT_RELOADTIME = 0.5f;

        public float BOMB_EXPL_RADIUS = 120f;
        public float BOMB_EXPL_FUSETIME = 0.34f;
        public float BOMB_EXPL_DEFUSETIME = 0.45f;
        public float BOMB_EXPL_GROWRATE = 1850f;
        public Color BOMB_EXPL_COLOUR = Color.FromArgb(255, 255, 255, 255);
        public float BOMB_EXPL_OT_RADIUS = 96f;


        public Color[] PLAYER_COLOURS = new Color[] {
            Color.FromArgb(183, 44, 9),
            Color.FromArgb(32, 95, 214),
            Color.FromArgb(32, 214, 98),
            Color.FromArgb(214, 116, 32)
        };
        public float PLAYER_INDICATORRADIUS = 6.5f;
        public float PLAYER_INDICATORCENTRERADIUS = 0.85f;

        public float SAFEAREA_RADIUS = 130f;
        public Color SAFEAREA_COLOUR = Color.FromArgb(80, 255, 255, 255);
        public Color SAFEAREA_SCORECOLOUR = Color.FromArgb(80, 255, 255, 255);
        public Color SAFEAREA_HIGHSCORECOLOUR = Color.FromArgb(250, 255, 255, 255);

        public Color BG_GAME_COLOUR1 = Color.FromArgb(138, 47, 198);
        public Color BG_GAME_COLOUR2 = Color.FromArgb(60, 43, 168);
        public Color BG_MENU_COLOUR1 = Color.FromArgb(109, 28, 164);
        public Color BG_MENU_COLOUR2 = Color.FromArgb(43, 27, 138);

        public static void Init()
        {
            R = new Global();
            R.Scale();
        }

        public void Scale()
        {
            BALL_RADIUS *= UTILS_SIZE_SCALE;
            BALL_JAILRADIUS *= UTILS_SIZE_SCALE;
            BOMB_EXPL_RADIUS *= UTILS_SIZE_SCALE;
            PLAYER_INDICATORCENTRERADIUS *= UTILS_SIZE_SCALE;
            PLAYER_INDICATORRADIUS *= UTILS_SIZE_SCALE;
            SAFEAREA_RADIUS *= UTILS_SIZE_SCALE;

            UI_FONTSIZE *= UTILS_SIZE_SCALE;
            UI_FONTSIZETIMER *= UTILS_SIZE_SCALE;
        }
    }
}
