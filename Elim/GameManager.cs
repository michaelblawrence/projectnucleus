using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using static Elim.Global;
using static Elim.Utils.Utilities;

namespace Elim
{
    public class GameManager : Renderer
    {
        public enum GameState
        {
            Menu,
            Game,
        }

        private static Renderer[] states;
        private static int current;

        private static SoundPlayer audioPlayer;
        private static int audioLevel;

        private readonly GameState[] initOnLoadStates = new GameState[] { GameState.Menu, GameState.Game };

        public static GameState CurrentState { get => (GameState)current; protected set => current = (int)value; }
        public static bool PlayingMusic { get => playingMusic; }
        public static int AudioLevel { get => audioLevel; set => audioLevel = EditMusicLevel((int)Clamp(value, 0, 10)); }

        //[DllImport("winmm.dll")]
        //public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        //[DllImport("winmm.dll")]
        //public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        public static Party players;
        private static bool playingMusic;

        public GameManager(UiSettings uiSettings) : base(uiSettings)
        {
            CurrentState = R.UTILS_LOADSTATE;
            states = new Renderer[Enum.GetValues(typeof(GameState)).Length];
            for (int i = 0; i < states.Length; i++)
            {
                switch ((GameState)i)
                {
                    case GameState.Menu:
                        states[i] = new MenuRender(uiSettings);
                        break;
                    case GameState.Game:
                        states[i] = new GameRenderer(uiSettings);
                        break;
                    default:
                        break;
                }
            }
            initPlayerObjects();
            this.initComputationObjects();
            this.initAudioObjects();
            this.initDisplayObjects();
        }

        private void initAudioObjects()
        {
            playingMusic = R.UTILS_PLAYMUSIC;

            // By the default set the volume to 0
            uint CurrVol = 0;
            // At this point, CurrVol gets assigned the volume
            //waveOutGetVolume(IntPtr.Zero, out CurrVol);
            // Calculate the volume
            ushort CalcVol = (ushort)(CurrVol & 0x0000ffff);
            // Get the volume on a scale of 1 to 10 (to fit the trackbar)
            audioLevel = CalcVol / (ushort.MaxValue / 10);

            audioPlayer = new SoundPlayer(Resources.Res.elim1);
            if (playingMusic)
                audioPlayer.PlayLooping();
        }

        public static void PlayMusic()
        {
            audioPlayer.PlayLooping();
            playingMusic = true;
        }

        public static void StopMusic()
        {
            audioPlayer.Stop();
            playingMusic = false;
        }
        public static int EditMusicLevel(int newLevel)
        {
            uint NewVolumeAllChannels = (((uint)newLevel & 0x0000ffff) | ((uint)newLevel << 16));
            // Set the volume
            //waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
            return newLevel;
        }

        private static void initPlayerObjects()
        {
            players = new Party();

            int playerId = 0;

            for (int i = 0; i < R.UTILS_MAXPLAYERS; i++)
            {
                Controller controller = new Controller((UserIndex)i);
                if (controller.IsConnected)
                {
                    Player player = new Player(controller, playerId++);
                    players.Add(player);
                }
            }
            if (players.Count == 0)
            {
                MessageBox.Show("Gameroller is not connected ... you know ;)");
            }
        }

        internal sealed override void initComputationObjects()
        {
            foreach (var state in initOnLoadStates)
            {
                states[(int)state].initComputationObjects();
            }
        }

        internal sealed override void initDisplayObjects()
        {
            foreach (var state in initOnLoadStates)
            {
                states[(int)state].initDisplayObjects();
            }
        }

        public override void RenderFrame(Graphics g, int delta)
        {
            UpdateGameTime(delta);
            GameTimer.UpdateGameTime(gameTime);
            states[current].RenderFrame(g, delta);
        }

        internal override void MessageRecieved(object sender, UiSettingsEventArgs e)
        {

        }

        public static void LoadState(GameState state)
        {
            switch (state)
            {
                case GameState.Menu:
                    throw new NotImplementedException();
                case GameState.Game:
                    CurrentState = state;
                    ((GameRenderer)states[(int)GameState.Game]).Reset();
                    break;
                default:
                    break;
            }
        }
    }

    public class BitmapAssetManager : IDisposable
    {
        Dictionary<string, Bitmap> data;
        public Dictionary<string, Bitmap> Assets { get => data; set => data = value; }

        public int Count { get => data.Count; }

        public BitmapAssetManager()
        {
            data = new Dictionary<string, Bitmap>();
        }

        public bool LoadImage(string key, Bitmap image)
        {
            try
            {
                data.Add(key, image);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                return false;
            }
            return true;
        }

        public Bitmap Get(string key)
        {
            return data[key];
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    List<string> keys = data.Keys.ToList();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        data[keys[i]].Dispose();
                        data[keys[i]] = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BitmapAssetManager() {
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
