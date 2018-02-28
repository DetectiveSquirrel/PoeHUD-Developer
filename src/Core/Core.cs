using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using DeveloperTool.Libs;
using ImGuiNET;
using PoeHUD.Controllers;
using PoeHUD.DebugPlug;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Models.Interfaces;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using SharpDX;
using ImGuiVector2 = System.Numerics.Vector2;
using ImGuiVector4 = System.Numerics.Vector4;
using SharpDX.Direct3D9;

namespace DeveloperTool.Core
{
    public class Core : BaseSettingsPlugin<Settings>
    {
        private const string EntDebugPrefix = "EntDebug";
        private const string LocalPlayerDebugName = "LocalPlayer";
        private const string GameControllerDebugName = "GameController";
        private const string GameDebugName = "GameController.Game";
        private const string IngameUiDebugName = "IngameUi";
        private const string UiRootDebugName = "UIRoot";
        private const string ServerDataDebugName = "ServerData";
        private static ImGuiVector2 _renderDebugwindowSize = new ImGuiVector2(784, API.GameController.Window.GetWindowRectangle().Height - 129);
        private static ImGuiVector2 _renderDebugwindowPos = new ImGuiVector2(593, 0);
        private static readonly ImGuiVector2 RenderDebugnextWindowPos = new ImGuiVector2(_renderDebugwindowSize.X + _renderDebugwindowPos.X, _renderDebugwindowSize.Y + _renderDebugwindowPos.Y);
        public static Core Instance;
        private Color _clr = Color.Pink;
        private Coroutine _coroutineRndColor;
        private bool _enableDebugHover;
        private GameController _gameController;
        private readonly List<(string name, object obj)> _objectForDebug = new List<(string name, object obj)>();
        private readonly List<RectangleF> _rectForDebug = new List<RectangleF>();
        private Random _rnd;
        private Settings _settings;
        private long _uniqueIndex;
        public static int Selected;
        public static string[] SettingName = { "Main", "Settings" };

        public Core() => PluginName = "Qvin Debug Tree";

        public override void Initialise()
        {
            base.Initialise();
            Instance = this;
            _gameController = GameController;
            _settings = Settings;
            _rnd = new Random((int) _gameController.MainTimer.ElapsedTicks);
            _coroutineRndColor = new Coroutine(() => { _clr = new Color(_rnd.Next(255), _rnd.Next(255), _rnd.Next(255), 255); }, new WaitTime(200), nameof(Core), "Random Color").Run();
            GameController.Area.OnAreaChange += Area_OnAreaChange;
        }

        private void Area_OnAreaChange(AreaController obj) { AddDefualtDebugObjects(true); }

        private void AddDefualtDebugObjects(bool removeOld)
        {
            if(removeOld)
            {
                _objectForDebug.RemoveAll(x =>
                x.name == LocalPlayerDebugName ||
                x.name == GameControllerDebugName ||
                x.name == GameDebugName ||
                x.name == IngameUiDebugName ||
                x.name == UiRootDebugName ||
                x.name == ServerDataDebugName);
            }

            _objectForDebug.Insert(0, (LocalPlayerDebugName, GameController.Game.IngameState.Data.LocalPlayer));
            _objectForDebug.Insert(1, (GameControllerDebugName, GameController));
            _objectForDebug.Insert(2, (GameDebugName, GameController.Game));
            _objectForDebug.Insert(3, (IngameUiDebugName, GameController.Game.IngameState.IngameUi));
            _objectForDebug.Insert(4, (UiRootDebugName, GameController.Game.IngameState.UIRoot));
            _objectForDebug.Insert(5, (ServerDataDebugName, GameController.Game.IngameState.ServerData));
        }

        public override void Render()
        {
            RenderDebugInformation();
            RenderNearestObjectsDebug();
        }

        private void RenderNearestObjectsDebug()
        {
            if (Settings.DebugNearestEnts.PressedOnce())
            {
                var playerPos = GameController.Player.Pos;
                var entsToDebug = GameController.EntityListWrapper.Entities.Where(x => Vector3.Distance(x.Pos, playerPos) < Settings.NearestEntsRange.Value).ToList();
                foreach (var ent in entsToDebug)
                {
                    if (_objectForDebug.Any(x => Equals(x.obj, ent))) continue;
                    _objectForDebug.Add(($"{EntDebugPrefix} [{_objectForDebug.Count}], {ent.Path}", ent));
                }
            }

            foreach (var ent in _objectForDebug)
            {
                if (!ent.name.StartsWith(EntDebugPrefix)) continue;
                var entWrapper = ent.obj as EntityWrapper;
                var screenDrawPos = GameController.Game.IngameState.Camera.WorldToScreen(entWrapper.Pos, entWrapper);
                var label = ent.name; // entWrapper.Address.ToString("x");
                label = label.Substring(label.IndexOf("[") + 1);
                label = label.Substring(0, label.IndexOf("]"));
                const FontDrawFlags drawFlags = FontDrawFlags.Center | FontDrawFlags.VerticalCenter;
                const int textSize = 20;
                var labelSize = Graphics.MeasureText(label, textSize, drawFlags);
                labelSize.Width += 10;
                labelSize.Height += 2;
                Graphics.DrawBox(new RectangleF(screenDrawPos.X - labelSize.Width / 2, screenDrawPos.Y - labelSize.Height / 2, labelSize.Width, labelSize.Height), Color.Black);
                Graphics.DrawText(label, textSize, screenDrawPos, drawFlags);
            }
        }

        private void RenderDebugInformation()
        {
            if (_settings.ShowWindow)
            {
                _uniqueIndex = 0;
                var idPop = 1;
                if (_rectForDebug.Count == 0)
                    _coroutineRndColor.Pause();
                else
                    _coroutineRndColor.Resume();
                foreach (var rectangleF in _rectForDebug) Graphics.DrawFrame(rectangleF, 2, _clr);
                var isOpened = Settings.ShowWindow.Value;
                ImGuiExtension.BeginWindow($"{PluginName} Settings", ref isOpened, Settings.LastSettingPos.X, Settings.LastSettingPos.Y, Settings.LastSettingSize.X, Settings.LastSettingSize.Y);
                Settings.ShowWindow.Value = isOpened;
                ImGui.PushStyleVar(StyleVar.ChildRounding, 5.0f);
                ImGuiExtension.ImGuiExtension_ColorTabs("LeftSettings", 35, SettingName, ref Selected, ref idPop);
                ImGuiNative.igGetContentRegionAvail(out var newcontentRegionArea);
                if (ImGui.BeginChild("RightSettings", new ImGuiVector2(newcontentRegionArea.X, newcontentRegionArea.Y), true, WindowFlags.Default))
                    switch (SettingName[Selected])
                    {
                        case "Main":
                            if (ImGui.Button("Clear Debug Rects##base")) _rectForDebug.Clear();
                            ImGui.SameLine();
                            if (ImGui.Button("Clear Debug Obj##base"))
                            {
                                _objectForDebug.Clear();
                                AddDefualtDebugObjects(false);
                            }

                            ImGui.SameLine();
                            ImGui.Checkbox("F1 for debug hover", ref _enableDebugHover);
                            if (_enableDebugHover && WinApi.IsKeyDown(Keys.F1))
                            {
                                var uihover = _gameController.Game.IngameState.UIHover;
                                var formattable = $"Hover: {uihover} {uihover.Address}";
                                if (_objectForDebug.Any(x => x.name.Contains(formattable)))
                                {
                                    var findIndex = _objectForDebug.FindIndex(x => x.name.Contains(formattable));
                                    _objectForDebug[findIndex] = (formattable + "^", uihover);
                                }
                                else
                                    _objectForDebug.Add((formattable, uihover));
                            }

                            for (var i = 0; i < _objectForDebug.Count; i++)
                                if (ImGui.TreeNode($"{_objectForDebug[i].name}"))
                                {
                                    ImGuiNative.igIndent();
                                    DebugForImgui(_objectForDebug[i].obj);
                                    ImGuiNative.igUnindent();
                                    ImGui.TreePop();
                                }

                            break;
                        case "Settings":
                            Settings.DebugNearestEnts.Value = ImGuiExtension.HotkeySelector("Debug Nearest Entities", Settings.DebugNearestEnts.Value);
                            Settings.NearestEntsRange.Value = ImGuiExtension.IntSlider("Entity Debug Range", Settings.NearestEntsRange);
                            break;
                    }
                ImGui.EndChild();
                ImGui.PopStyleVar();

                // Storing window Position and Size changed by the user
                if (ImGui.GetWindowHeight() > 21)
                {
                    Settings.LastSettingPos = ImGui.GetWindowPosition();
                    Settings.LastSettingSize = ImGui.GetWindowSize();
                }
                ImGui.EndWindow();
            }
            else
                _coroutineRndColor.Pause();
        }

        private void DebugForImgui(object obj)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            try
            {
                if (obj is IEntity)
                {
                    Dictionary<string, long> comp;
                    object ro;
                    if (obj is EntityWrapper wrapper)
                    {
                        ro = wrapper;
                        comp = wrapper.InternalEntity.GetComponents();
                    }
                    else
                    {
                        ro = (Entity) obj;
                        comp = ((Entity) obj).GetComponents();
                    }

                    if (ImGui.TreeNode($"Components {comp.Count} ##{ro.GetHashCode()}"))
                    {
                        var method = ro is EntityWrapper ? typeof(EntityWrapper).GetMethod("GetComponent") : typeof(Entity).GetMethod("GetComponent");
                        foreach (var c in comp)
                        {
                            ImGui.Text(c.Key, new ImGuiVector4(1, 0.412f, 0.706f, 1));
                            ImGui.SameLine();
                            var type = Type.GetType("PoeHUD.Poe.Components." + c.Key + ", PoeHUD, Version=6.3.9600.0, Culture=neutral, PublicKeyToken=null");
                            if (type == null)
                            {
                                ImGui.Text(" - undefiend", new ImGuiVector4(1, 0.412f, 0.706f, 1));
                                continue;
                            }

                            if (method == null) continue;
                            var generic = method.MakeGenericMethod(type);
                            var g = generic.Invoke(ro, null);
                            if (!ImGui.TreeNode($"##{ro.GetHashCode()}{c.Key.GetHashCode()}")) continue;
                            _uniqueIndex++;
                            if (ImGui.Button($"Debug this##{_uniqueIndex}"))
                            {
                                var formattableString = $"{obj}->{c.Key}";
                                if (_objectForDebug.Any(x => x.name.Contains(formattableString)))
                                {
                                    var findIndex = _objectForDebug.FindIndex(x => x.name.Contains(formattableString));
                                    _objectForDebug[findIndex] = (formattableString + "^", g);
                                }
                                else
                                    _objectForDebug.Add((formattableString, g));
                            }

                            DebugForImgui(g);
                            ImGui.TreePop();
                        }

                        ImGui.TreePop();
                    }
                }

                if (obj is Element el1)
                {
                    ImGui.SameLine();
                    _uniqueIndex++;
                    if (ImGui.Button($"Draw this##{_uniqueIndex}"))
                        _rectForDebug.Add(el1.GetClientRect());
                    ImGui.SameLine();
                    _uniqueIndex++;
                    if (ImGui.Button($"Clear##from draw this{_uniqueIndex}")) _rectForDebug.Clear();
                }

                var oProp = obj.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);
                var ordered1 = oProp.OrderBy(x => x.PropertyType.GetInterfaces().Contains(typeof(IEnumerable))); //We want to show arrays and lists last
                oProp = ordered1.ThenBy(x => x.Name).ToList();
                foreach (var propertyInfo in oProp)
                {
                    if (propertyInfo.ReflectedType.IsSubclassOf(typeof(RemoteMemoryObject))) //We don't need to see this shit
                        if (propertyInfo.Name == "M" || propertyInfo.Name == "Game" || propertyInfo.Name == "Offsets")
                            continue;
                    if (propertyInfo.ReflectedType.IsSubclassOf(typeof(Component))) //...and this one too
                        if (propertyInfo.Name == "Owner")
                            continue;
                    try
                    {
                        var value = propertyInfo.GetValue(obj, null);
                        if (
                                //propertyInfo.GetValue(obj, null).GetType().IsPrimitive  //Wanna get null or what?
                                propertyInfo.PropertyType.IsPrimitive || value is decimal || value is string || value is TimeSpan || value is Enum)
                        {
                            ImGui.Text($"{propertyInfo.Name}: ");
                            ImGui.SameLine(0f, 0f);
                            var o = propertyInfo.GetValue(obj, null);
                            if (propertyInfo.Name.Contains("Address"))
                                o = Convert.ToInt64(o).ToString("X");
                            //if (!propertyInfo.Name.Contains("Address")) continue; //We want to copy any thing we need

                            ImGui.PushStyleColor(ColorTarget.Text, new ImGuiVector4(1, 0.647f, 0, 1));
                            ImGui.PushStyleColor(ColorTarget.Button, new ImGuiVector4(0, 0, 0, 0));
                            ImGui.PushStyleColor(ColorTarget.ButtonHovered, new ImGuiVector4(0.25f, 0.25f, 0.25f, 1));
                            ImGui.PushStyleColor(ColorTarget.ButtonActive, new ImGuiVector4(1, 1, 1, 1));
                            if (ImGui.SmallButton($"{o}##{o}{o.GetHashCode()}"))
                                ImGuiNative.igSetClipboardText(o.ToString());
                            ImGui.PopStyleColor(4);
                        }
                        else
                        {
                            var label = propertyInfo.Name;
                            var o = propertyInfo.GetValue(obj, null);
                            if (o == null)
                            {
                                ImGui.Text(label + ": ");
                                ImGui.SameLine(0f, 0f);
                                ImGui.PushStyleColor(ColorTarget.Text, new ImGuiVector4(1, 0.366f, 0.366f, 1));
                                ImGui.Text("Null");
                                ImGui.PopStyleColor(1);
                                continue;
                            }

                            if (label.Contains("Framework") || label.Contains("Offsets"))
                                continue;
                            if (!propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
                            {
                                if (ImGui.TreeNode(label))
                                {
                                    _uniqueIndex++;
                                    ImGui.SameLine();
                                    if (ImGui.SmallButton($"Debug this##{_uniqueIndex}"))
                                    {
                                        var formattable = $"{label}->{o}";
                                        if (_objectForDebug.Any(x => x.name.Contains(formattable)))
                                        {
                                            var findIndex = _objectForDebug.FindIndex(x => x.name.Contains(formattable));
                                            _objectForDebug[findIndex] = (formattable + "^", o);
                                        }
                                        else
                                            _objectForDebug.Add((formattable, o));
                                    }

                                    ImGuiNative.igIndent();
                                    DebugForImgui(o);
                                    ImGuiNative.igUnindent();
                                    ImGui.TreePop();
                                }

                                continue;
                            }

                            if (ImGui.TreeNode($"{propertyInfo.Name}:")) //Hide arrays to tree node
                            {
                                var enumerable = (IEnumerable) o;
                                var items = enumerable as IList<object> ?? enumerable.Cast<object>().ToList();
                                var gArgs = o.GetType().GenericTypeArguments.ToList();
                                if (gArgs.Any(x => x == typeof(Element) || x.IsSubclassOf(typeof(Element)))) //We need to draw it ONLY for UI Elements
                                {
                                    _uniqueIndex++;
                                    if (ImGui.Button($"Draw Childs##{_uniqueIndex}"))
                                    {
                                        var tempi = 0;
                                        foreach (var item in items)
                                        {
                                            var el = (Element) item;
                                            _rectForDebug.Add(el.GetClientRect());
                                            tempi++;
                                            if (tempi > 1000) break;
                                        }
                                    }

                                    ImGui.SameLine();
                                    _uniqueIndex++;
                                    if (ImGui.Button($"Draw Childs for Childs##{_uniqueIndex}")) DrawChilds(items);
                                    ImGui.SameLine();
                                    _uniqueIndex++;
                                    if (ImGui.Button($"Draw Childs for Childs Only Visible##{_uniqueIndex}")) DrawChilds(items, true);
                                    ImGui.SameLine();
                                    _uniqueIndex++;
                                    if (ImGui.Button($"Clear##from draw childs##{_uniqueIndex}")) _rectForDebug.Clear();
                                }

                                var i = 0;
                                foreach (var item in items)
                                {
                                    if (item == null)
                                    {
                                        ImGui.Text($"Null", new ImGuiVector4(0.486f, 0.988f, 0, 1));
                                        continue;
                                    }

                                    if (i > 500) break;
                                    if (ImGui.TreeNode($"[{i}]")) //Draw only index
                                    {
                                        DebugForImgui(item);
                                        ImGui.TreePop();
                                    }
                                    else
                                    {
                                        ImGui.SameLine();
                                        ImGui.Text($"{item}", new ImGuiVector4(0.486f, 0.988f, 0, 1));
                                    }

                                    i++;
                                }

                                ImGuiNative.igUnindent();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ImGui.Text($"Error reading property: {propertyInfo.Name}, Error: {ex}", new ImGuiVector4(1, 0, 0, 1));
                    }
                }
            }
            catch (Exception e)
            {
                DebugPlugin.LogMsg($"Debug Tree: {e}", 1);
            }
        }

        private void DrawChilds(object obj, bool onlyVisible = false)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            if (obj is IEnumerable enumerable)
            {
                var tempi = 0;
                foreach (var item in enumerable)
                {
                    var el = (Element) item;
                    if (onlyVisible)
                        if (!el.IsVisible)
                            continue;
                    _rectForDebug.Add(el.GetClientRect());
                    tempi++;
                    if (tempi > 1000) break;
                    var oProp = item.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);
                    foreach (var propertyInfo in oProp) DrawChilds(propertyInfo.GetValue(item, null));
                }
            }
            else
            {
                if (obj is Element el)
                    _rectForDebug.Add(el.GetClientRect());
            }
        }

        /*
        private void DebugImGuiFields(object obj)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var fields = obj.GetType().GetFields(flags);
            foreach (var fieldInfo in fields)
            {
                ImGui.PushStyleColor(ColorTarget.Text, new ImGuiVector4(0.529f, 0.808f, 0.922f, 1));
                ImGui.Text($"{fieldInfo.Name} -=> {fieldInfo.GetValue(obj)}");
                ImGui.PopStyleColor();
            }
        }
        */
    }
}