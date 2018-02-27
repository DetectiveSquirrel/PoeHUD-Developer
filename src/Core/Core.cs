using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
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

namespace DeveloperTool.Core
{
    public class DeveloperTool : BaseSettingsPlugin<Settings>
    {
        private static ImGuiVector2 _renderDebugwindowSize = new ImGuiVector2(784, API.GameController.Window.GetWindowRectangle().Height - 129);
        private static ImGuiVector2 _renderDebugwindowPos = new ImGuiVector2(593, 0);
        private static readonly ImGuiVector2 RenderDebugnextWindowPos = new ImGuiVector2(_renderDebugwindowSize.X + _renderDebugwindowPos.X, _renderDebugwindowSize.Y + _renderDebugwindowPos.Y);
        public static DeveloperTool Instance;
        private readonly List<(string name, object obj)> _objectForDebug = new List<(string name, object obj)>();
        private readonly List<RectangleF> _rectForDebug = new List<RectangleF>();
        private Color _clr = Color.Pink;
        private Coroutine _coroutineRndColor;
        private bool _enableDebugHover;
        private GameController _gameController;
        private Random _rnd;
        private Settings _settings;
        private long _uniqueIndex;

        public DeveloperTool() => PluginName = "Qvin Debug Tree";

        public override void Initialise()
        {
            base.Initialise();
            Instance = this;
            _gameController = GameController;
            GameController.Area.OnAreaChange += area => AreaChange();
            _settings = Settings;
            _rnd = new Random((int) _gameController.MainTimer.ElapsedTicks);
            _coroutineRndColor = new Coroutine(() => { _clr = new Color(_rnd.Next(255), _rnd.Next(255), _rnd.Next(255), 255); }, new WaitTime(200), nameof(DeveloperTool), "Random Color").Run();
            DebugObjects();
        }

        private void DebugObjects()
        {
            _objectForDebug.Add(("LocalPlayer", GameController.Game.IngameState.Data.LocalPlayer));
            _objectForDebug.Add(("GameController", GameController));
            _objectForDebug.Add(("GameController.Game", GameController.Game));
            _objectForDebug.Add(("IngameUi", GameController.Game.IngameState.IngameUi));
            _objectForDebug.Add(("UIRoot", GameController.Game.IngameState.UIRoot));
        }

        public override void Render()
        {
            RenderDebugInformation();
            //ImGuiNative.igShowDemoWindow(ref _tempBool);
        }

        private void AreaChange()
        {
            _objectForDebug.Clear();
            DebugObjects();
        }

        private void RenderDebugInformation()
        {
            if (_settings.ShowWindow)
            {
                _uniqueIndex = 0;
                if (_rectForDebug.Count == 0)
                    _coroutineRndColor.Pause();
                else
                    _coroutineRndColor.Resume();
                foreach (var rectangleF in _rectForDebug) Graphics.DrawFrame(rectangleF, 2, _clr);
                ImGui.SetNextWindowPos(RenderDebugnextWindowPos, Condition.Appearing, new ImGuiVector2(1, 1));
                ImGui.SetNextWindowSize(_renderDebugwindowSize, Condition.Appearing);
                var isOpen = Settings.ShowWindow.Value;
                ImGui.BeginWindow("DebugTree", ref isOpen, WindowFlags.Default);
                Settings.ShowWindow.Value = isOpen;
                if (ImGui.Button("Clear##base")) _rectForDebug.Clear();
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
                        DebugForImgui(_objectForDebug[i].obj);
                        ImGui.TreePop();
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
                    if (ImGui.Button($"Draw this##{_uniqueIndex}")) _rectForDebug.Add(el1.GetClientRect());
                    ImGui.SameLine();
                    _uniqueIndex++;
                    if (ImGui.Button($"Clear##from draw this{_uniqueIndex}")) _rectForDebug.Clear();
                }

                var oProp = obj.GetType().GetProperties(flags).Where(x => x.GetIndexParameters().Length == 0);
                //DebugImGuiFields(obj);
                oProp = oProp.OrderBy(x => x.Name).ToList();
                foreach (var propertyInfo in oProp)
                    if (propertyInfo.GetValue(obj, null).GetType().IsPrimitive || propertyInfo.GetValue(obj, null) is decimal || propertyInfo.GetValue(obj, null) is string || propertyInfo.GetValue(obj, null) is TimeSpan || propertyInfo.GetValue(obj, null) is Enum)
                    {
                        ImGui.Text($"{propertyInfo.Name}: ");
                        ImGui.SameLine();
                        var o = propertyInfo.GetValue(obj, null);
                        if (propertyInfo.Name.Contains("Address"))
                            o = Convert.ToInt64(o).ToString("X");
                        ImGui.Text($"{o}", new ImGuiVector4(1, 0.647f, 0, 1));
                        if (!propertyInfo.Name.Contains("Address")) continue;
                        ImGui.SameLine();
                        if (ImGui.SmallButton($"Copy##{o}")) ImGuiNative.igSetClipboardText(o.ToString());
                    }
                    else
                    {
                        var label = propertyInfo.ToString();
                        var o = propertyInfo.GetValue(obj, null);
                        if (o == null)
                        {
                            ImGui.Text("Null");
                            continue;
                        }

                        if (label.Contains("Framework") || label.Contains("Offsets"))
                            continue;
                        if (!propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable)))
                            if (ImGui.TreeNode(label))
                            {
                                _uniqueIndex++;
                                if (ImGui.Button($"Debug this##{_uniqueIndex}"))
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

                                DebugForImgui(o);
                                ImGui.TreePop();
                            }

                        if (!propertyInfo.PropertyType.GetInterfaces().Contains(typeof(IEnumerable))) continue;
                        ImGui.Text($"{propertyInfo.GetValue(obj, null)}", new ImGuiVector4(0.486f, 0.988f, 0, 1));
                        var i = 0;
                        var enumerable = (IEnumerable) o;
                        var items = enumerable as IList<object> ?? enumerable.Cast<object>().ToList();
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
                        foreach (var item in items)
                        {
                            if (item == null)
                                continue;
                            if (i > 500) break;
                            ImGui.Text($"{item}", new ImGuiVector4(0.486f, 0.988f, 0, 1));
                            ImGui.SameLine();
                            if (ImGui.TreeNode($" #{i} ##{item}  {item.GetHashCode()}"))
                            {
                                DebugForImgui(item);
                                ImGui.TreePop();
                            }

                            i++;
                        }
                    }
            }
            catch (Exception e)
            {
                DebugPlugin.LogMsg($"Debug Tree: {e.Message}", 1);
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
                if (obj is Element el) _rectForDebug.Add(el.GetClientRect());
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