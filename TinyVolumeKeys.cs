using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace TinyVolumeKeys
{
    public class Preferences
    {
        public int[] Keys = new int[] { 120, 121, 122 };
        public int[] Modifiers = new int[] { 0, 0, 0 };
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool first;
            using (Mutex mutex = new Mutex(true, "Local\\TinyVolumeKeys-" + Environment.UserName, out first))
            {
                if (!first)
                {
                    MessageBox.Show("Tiny Volume Keys is already running. Open it from its information icon in the system tray.", "Tiny Volume Keys");
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly ComboBox[] keys = new ComboBox[3];
        private readonly ComboBox[] modifiers = new ComboBox[3];
        private readonly string[] actions = { "Mute / unmute", "Volume down", "Volume up" };
        private readonly int[] modifierValues = { 0, 2, 1, 4, 3, 6, 5, 7 };
        private readonly string[] modifierNames = { "None", "Ctrl", "Alt", "Shift", "Ctrl + Alt", "Ctrl + Shift", "Alt + Shift", "Ctrl + Alt + Shift" };
        private readonly Label status = new Label();
        private readonly Button pause = new Button();
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly ToolStripMenuItem trayPause = new ToolStripMenuItem("Pause shortcuts");
        private readonly string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TinyVolumeKeys", "settings.xml");
        private Preferences active;
        private bool paused;
        private bool exiting;
        private bool released;

        public MainForm()
        {
            Text = "Tiny Volume Keys";
            Icon = SystemIcons.Application;
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(504, 363);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(248, 249, 251);

            Label title = AddLabel("Your keyboard. Simple volume controls.", 22, 18, 464, 30);
            title.Font = new Font(Font, FontStyle.Bold);
            AddLabel("Choose a function key for each action, then click Apply.", 22, 51, 464, 25);
            AddLabel("Action", 22, 91, 150, 24);
            AddLabel("Modifier", 181, 91, 166, 24);
            AddLabel("Function key", 361, 91, 120, 24);
            for (int i = 0; i < 3; i++)
            {
                int y = 119 + i * 41;
                AddLabel(actions[i], 22, y + 3, 150, 28);
                modifiers[i] = new ComboBox { Left = 181, Top = y, Width = 166, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = actions[i] + " modifier" };
                modifiers[i].Items.AddRange(modifierNames);
                keys[i] = new ComboBox { Left = 361, Top = y, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = actions[i] + " key" };
                keys[i].Items.Add("Disabled");
                for (int f = 1; f <= 11; f++) keys[i].Items.Add("F" + f);
                Controls.Add(modifiers[i]);
                Controls.Add(keys[i]);
            }
            AddLabel("Mapped keys replace their normal action while active. F12 is reserved by Windows.\nClose this window to keep shortcuts running in the system tray.", 22, 247, 464, 39).Font = new Font("Segoe UI", 8.5F);
            status.SetBounds(22, 287, 464, 27);
            status.AccessibleName = "Shortcut status";
            Controls.Add(status);
            Button apply = AddButton("Apply", 22, 320, 94);
            apply.Click += delegate { ApplySettings(); };
            AcceptButton = apply;
            pause.Text = "Pause";
            pause.SetBounds(126, 320, 94, 30);
            pause.Click += delegate { TogglePause(); };
            Controls.Add(pause);
            AddButton("Hide to tray", 230, 320, 124).Click += delegate { Hide(); };
            AddButton("Exit", 364, 320, 117).Click += delegate { ExitApp(); };

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Open settings", null, delegate { OpenWindow(); });
            menu.Items.Add(trayPause);
            trayPause.Click += delegate { TogglePause(); };
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { ExitApp(); });
            tray.Icon = SystemIcons.Information;
            tray.Text = "Tiny Volume Keys";
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { OpenWindow(); };
            tray.Visible = true;
            FormClosing += OnClosing;
            Shown += delegate { InitializeShortcuts(); };
        }

        private Label AddLabel(string text, int x, int y, int width, int height)
        {
            Label label = new Label { Text = text, Left = x, Top = y, Width = width, Height = height };
            Controls.Add(label);
            return label;
        }

        private Button AddButton(string text, int x, int y, int width)
        {
            Button button = new Button { Text = text, Left = x, Top = y, Width = width, Height = 30, UseVisualStyleBackColor = true };
            Controls.Add(button);
            return button;
        }

        private void InitializeShortcuts()
        {
            Preferences loaded = new Preferences();
            string warning = null;
            if (File.Exists(settingsPath))
            {
                try
                {
                    using (FileStream file = File.OpenRead(settingsPath))
                        loaded = (Preferences)new XmlSerializer(typeof(Preferences)).Deserialize(file);
                    if (!Valid(loaded)) throw new InvalidDataException("Invalid shortcuts.");
                }
                catch (Exception ex)
                {
                    if (!(ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException)) throw;
                    loaded = new Preferences();
                    warning = "Saved settings could not be read. Defaults restored.";
                }
            }
            for (int i = 0; i < 3; i++)
            {
                keys[i].SelectedIndex = loaded.Keys[i] == 0 ? 0 : loaded.Keys[i] - 111;
                modifiers[i].SelectedIndex = Array.IndexOf(modifierValues, loaded.Modifiers[i]);
            }
            string error;
            if (TryRegister(loaded, out error))
            {
                active = loaded;
                ShowStatus(warning ?? "Active - shortcuts work across your desktop.", warning != null);
            }
            else
            {
                active = loaded;
                paused = true;
                UpdatePauseUI();
                ShowStatus(error, true);
            }
        }

        private bool Valid(Preferences value)
        {
            if (value == null || value.Keys == null || value.Modifiers == null || value.Keys.Length != 3 || value.Modifiers.Length != 3) return false;
            HashSet<string> seen = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                if (value.Keys[i] != 0 && (value.Keys[i] < 112 || value.Keys[i] > 122)) return false;
                if (Array.IndexOf(modifierValues, value.Modifiers[i]) < 0) return false;
                if (value.Keys[i] != 0 && !seen.Add(value.Keys[i] + ":" + value.Modifiers[i])) return false;
            }
            return true;
        }

        private void ApplySettings()
        {
            Preferences proposed = new Preferences();
            for (int i = 0; i < 3; i++)
            {
                proposed.Keys[i] = keys[i].SelectedIndex == 0 ? 0 : keys[i].SelectedIndex + 111;
                proposed.Modifiers[i] = modifierValues[modifiers[i].SelectedIndex];
            }
            if (!Valid(proposed))
            {
                ShowStatus("Choose a different shortcut for each action.", true);
                return;
            }
            Preferences previous = active;
            bool wasPaused = paused;
            UnregisterAll();
            string error;
            if (!TryRegister(proposed, out error))
            {
                string restoreError;
                if (!wasPaused && previous != null && !TryRegister(previous, out restoreError))
                {
                    paused = true;
                    UpdatePauseUI();
                    error = "Shortcuts unavailable. Choose other keys and Apply.";
                }
                ShowStatus(error, true);
                return;
            }
            active = proposed;
            paused = false;
            UpdatePauseUI();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
                string temporary = settingsPath + ".tmp";
                using (FileStream file = File.Create(temporary))
                    new XmlSerializer(typeof(Preferences)).Serialize(file, active);
                if (File.Exists(settingsPath)) File.Replace(temporary, settingsPath, null);
                else File.Move(temporary, settingsPath);
                ShowStatus("Saved - shortcuts are active.", false);
            }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)) throw;
                ShowStatus("Active, but settings could not be saved to disk.", true);
            }
        }

        private bool TryRegister(Preferences value, out string error)
        {
            for (int i = 0; i < 3; i++)
            {
                // Mute toggles once per press; volume changes repeat when held.
                uint flags = (uint)value.Modifiers[i] | (i == 0 ? 0x4000U : 0U);
                if (value.Keys[i] != 0 && !Native.RegisterHotKey(Handle, i + 1, flags, (uint)value.Keys[i]))
                {
                    UnregisterAll();
                    error = actions[i] + ": key in use. Choose another shortcut.";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private void UnregisterAll()
        {
            for (int i = 1; i <= 3; i++) Native.UnregisterHotKey(Handle, i);
        }

        private void TogglePause()
        {
            if (active == null) return;
            if (paused)
            {
                string error;
                if (!TryRegister(active, out error)) { ShowStatus(error, true); return; }
                paused = false;
            }
            else
            {
                UnregisterAll();
                paused = true;
            }
            UpdatePauseUI();
            ShowStatus(paused ? "Paused - function keys work normally." : "Active - shortcuts work across your desktop.", false);
        }

        private void UpdatePauseUI()
        {
            pause.Text = paused ? "Resume" : "Pause";
            trayPause.Text = paused ? "Resume shortcuts" : "Pause shortcuts";
            tray.Text = paused ? "Tiny Volume Keys - paused" : "Tiny Volume Keys - active";
        }

        private void ShowStatus(string message, bool error)
        {
            status.Text = message;
            status.ForeColor = error ? Color.Firebrick : Color.FromArgb(30, 104, 65);
        }

        private void OpenWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApp()
        {
            exiting = true;
            Close();
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            if (!exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !released)
            {
                released = true;
                if (IsHandleCreated) UnregisterAll();
                tray.Visible = false;
                ContextMenuStrip menu = tray.ContextMenuStrip;
                tray.Dispose();
                if (menu != null) menu.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312 && !paused)
            {
                int id = m.WParam.ToInt32();
                if (id >= 1 && id <= 3)
                {
                    ushort mediaKey = (ushort)(id == 1 ? 0xAD : id == 2 ? 0xAE : 0xAF);
                    Native.INPUT[] inputs = new Native.INPUT[2];
                    inputs[0].type = inputs[1].type = 1;
                    inputs[0].data.keyboard.wVk = inputs[1].data.keyboard.wVk = mediaKey;
                    inputs[1].data.keyboard.dwFlags = 2;
                    if (Native.SendInput(2, inputs, Marshal.SizeOf(typeof(Native.INPUT))) != 2)
                        ShowStatus("Windows blocked the volume command. Try from your desktop.", true);
                }
                return;
            }
            base.WndProc(ref m);
        }
    }

    internal static class Native
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint count, INPUT[] inputs, int size);

        [StructLayout(LayoutKind.Sequential)]
        internal struct INPUT { internal uint type; internal InputUnion data; }
        [StructLayout(LayoutKind.Explicit)]
        internal struct InputUnion
        {
            [FieldOffset(0)] internal KEYBDINPUT keyboard;
            [FieldOffset(0)] internal MOUSEINPUT mouse;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBDINPUT
        {
            internal ushort wVk, wScan;
            internal uint dwFlags, time;
            internal UIntPtr dwExtraInfo;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx, dy;
            internal uint mouseData, dwFlags, time;
            internal UIntPtr dwExtraInfo;
        }
    }
}
