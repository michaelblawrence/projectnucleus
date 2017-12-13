using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elim
{
    public class UiSettings
    {
        public int FPS { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public delegate void OnUiSettingsChanged(object sender, UiSettingsEventArgs e);
        public event OnUiSettingsChanged uiSettingsChanged;

        public UiSettings(int width, int height, int fps)
        {
            this.Width = width;
            this.Height = height;
            this.FPS = fps;
            if (uiSettingsChanged != null)
                uiSettingsChanged.Invoke(this, new UiSettingsEventArgs());
        }

        public void SendMessage(UiSettingsEventArgs.EventType type, object content)
        {
            if (uiSettingsChanged != null)
                uiSettingsChanged.Invoke(this, new UiSettingsEventArgs(type, content));
        }

        public void SendMessage(UiSettingsEventArgs.EventType type, object content, int flags)
        {
            if (uiSettingsChanged != null)
                uiSettingsChanged.Invoke(this, new UiSettingsEventArgs(type, content, flags));
        }


    }

    public class UiSettingsEventArgs : EventArgs
    {
        public enum EventType
        {
            KeyUpEvent,
            KeyDownEvent,
            KeyPressEvent,
            MouseEvent,
            WindowEvent,
            SettingChangeEvent,
            None
        }

        public EventType Type { get; set; }
        public int Flags { get; set; }
        public object EventInfo { get; set; }

        public static int FLAGS_UI { get { return mFlag_ui; } }
        public static int FLAGS_UI_MOUSEDOWN { get { return mFlag_ui_moused; } }
        public static int FLAGS_UI_MOUSEUP { get { return mFlag_ui_mouseu; } }

        private static int mFlag_ui = 0x08;
        private static int mFlag_ui_moused = 0x02;
        private static int mFlag_ui_mouseu = 0x01;

        public UiSettingsEventArgs()
        {
            Type = EventType.None;
            EventInfo = null;
            Flags = 0x00;
        }
        public UiSettingsEventArgs(EventType type)
        {
            Type = type;
            EventInfo = null;
            Flags = 0x00;
        }
        public UiSettingsEventArgs(EventType type, object eventObj)
        {
            Type = type;
            EventInfo = eventObj;
            Flags = 0x00;
        }
        public UiSettingsEventArgs(EventType type, object eventObj, int flags)
        {
            Type = type;
            EventInfo = eventObj;
            Flags = flags;
        }
    }
}
