﻿using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;
using ImGuiVector2 = System.Numerics.Vector2;

namespace DeveloperTool.Core
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            ShowWindow = true;
            DebugNearestEnts = Keys.NumPad6;
            NearestEntsRange = new RangeNode<int>(300, 1, 2000);
            var centerPos = BasePlugin.API.GameController.Window.GetWindowRectangle().Center;
            LastSettingSize = new ImGuiVector2(620, 376);
            LastSettingPos = new ImGuiVector2(centerPos.X - LastSettingSize.X / 2, centerPos.Y - LastSettingSize.Y / 2);
        }
        
        [Menu("Show Developer Information")]
        public ToggleNode ShowWindow { get; set; }
        public HotkeyNode DebugNearestEnts { get; set; }
        public RangeNode<int> NearestEntsRange { get; set; }

        public ImGuiVector2 LastSettingPos { get; set; }
        public ImGuiVector2 LastSettingSize { get; set; }
    }
}