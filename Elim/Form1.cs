using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Elim.Global;

namespace Elim
{
    public partial class GameWindow : Form
    {
        Timer uiTimer;
        UiSettings uiSettings;
        GameManager manager;
        DateTime uiLastFrameTime;
        int uiMouseState; // 0=None; 1=BtnDown


        public GameWindow()
        {
            InitializeComponent();
            Global.Init();
            uiSettings = new UiSettings(this.ClientSize.Width, this.ClientSize.Height, 140);
            initUI(uiSettings);
        }

        private void initUI(UiSettings info)
        {
            manager = new GameManager(uiSettings);

            uiLastFrameTime = DateTime.Now;

            uiTimer = new Timer();
            uiTimer.Interval = (1000 / info.FPS);
            uiTimer.Tick += UiScreenRefresh;
            uiTimer.Start();
        }

        private void UiScreenRefresh(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void GameWindow_Paint(object sender, PaintEventArgs e)
        {
            DateTime nowTime = DateTime.Now;
            int frameDelta = (nowTime - uiLastFrameTime).Milliseconds;

            e.Graphics.Clear(Color.Black);

            manager.RenderFrame(e.Graphics, frameDelta);

            e.Graphics.Flush();

            uiLastFrameTime = nowTime;
        }

        private void GameWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.KeyUpEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_Click(object sender, EventArgs e)
        {
            //uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_MouseDown(object sender, MouseEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI | UiSettingsEventArgs.FLAGS_UI_MOUSEDOWN);
            uiMouseState = 1;
        }

        private void GameWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (uiMouseState != 0)
                uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_MouseUp(object sender, MouseEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.MouseEvent, e, UiSettingsEventArgs.FLAGS_UI | UiSettingsEventArgs.FLAGS_UI_MOUSEUP);
            uiMouseState = 0;
        }

        private void GameWindow_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape) { Close(); return; }
            //uiSettings.SendMessage(UiSettingsEventArgs.EventType.KeyPressEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }

        private void GameWindow_KeyDown(object sender, KeyEventArgs e)
        {
            uiSettings.SendMessage(UiSettingsEventArgs.EventType.KeyDownEvent, e, UiSettingsEventArgs.FLAGS_UI);
        }
    }


}
