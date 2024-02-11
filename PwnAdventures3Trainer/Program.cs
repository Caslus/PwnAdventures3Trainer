using ImGuiNET;
using ClickableTransparentOverlay;
using Swed32;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PwnAdventures3Trainer
{
    public class Program : Overlay
    {
        public struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int key);

        float walkSpeedValue = 200f;
        float jumpSpeedValue = 480f;
        Vector3 playerPosition, playerEditPosition, playerLockedPosition, savedPlayerPosition;
        bool firstRender = true, showTrainer = true, editPos, lockPos, savedPosition, jumpHack;

        Swed swed = new Swed("PwnAdventure3-Win32-Shipping");
        IntPtr moduleBase, worldAddr, playerAddr, walkSpeedAddr, jumpSpeedAddr, playerPositionXAddr, playerPositionYAddr, playerPositionZAddr;

        protected override void Render()
        {
            if (firstRender)
            {
                IntPtr processPtr = swed.GetProcess().MainWindowHandle;
                Rect gameRect = new Rect();
                GetWindowRect(processPtr, ref gameRect);
                ImGui.SetNextWindowPos(new Vector2(gameRect.Left + 16, gameRect.Top + 40));
                firstRender = false;
            }
            SetupImGuiStyle();

            if (showTrainer)
            {
                ImGui.SetNextWindowSizeConstraints(new Vector2(400, 200), new Vector2(1000, 1000));
                ImGui.Begin("pwn adventure 3 trainer", ref showTrainer, ImGuiWindowFlags.AlwaysAutoResize);
                if(ImGui.BeginTabBar("items"))
                {
                    if (ImGui.BeginTabItem("player"))
                    {
                        if(ImGui.TreeNode("player position"))
                        {
                            if (!editPos)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 1f, 1f, 0.2f));
                                ImGui.InputFloat3("", ref playerPosition, "%.5g", ImGuiInputTextFlags.ReadOnly);
                                ImGui.PopStyleColor();
                                ImGui.SameLine();
                                if (ImGui.Button("edit"))
                                {
                                    editPos = true;
                                }
                            }
                            else
                            {
                                ImGui.InputFloat3("", ref playerEditPosition, "%.5g");
                                ImGui.SameLine();
                                if (ImGui.Button("cancel"))
                                {
                                    editPos = false;
                                }
                                ImGui.SameLine();
                                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.45f,0.6f,0.4f,1f));
                                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.35f, 0.5f, 0.3f, 1f));
                                ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.5f, 0.3f, 1f));
                                if (ImGui.Button("save"))
                                {
                                    if (lockPos)
                                    {
                                        playerLockedPosition = playerEditPosition;
                                    }
                                    else
                                    {
                                        teleportPlayer(playerEditPosition);
                                    }
                                    editPos = false;
                                }
                                ImGui.PopStyleColor();
                            }
                            ImGui.Checkbox("lock player position", ref lockPos);
                            if(ImGui.Button("save pos"))
                            {
                                savedPosition = true;
                                savedPlayerPosition = playerPosition;
                            }
                            if (savedPosition)
                            {
                                ImGui.SameLine();
                                if(ImGui.Button("load pos"))
                                {
                                    teleportPlayer(savedPlayerPosition);
                                }
                                ImGui.Text($"saved position: {savedPlayerPosition}");
                            }
                            ImGui.TreePop();
                        }
                        ImGui.SliderFloat("walk speed", ref walkSpeedValue, 200f, 10000f);
                        ImGui.SliderFloat("jump speed", ref jumpSpeedValue, 480f, 10000f);
                        ImGui.Checkbox("jump hack", ref jumpHack);
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("system"))
                    {
                        if (ImGui.Button("Exit"))
                        {
                            System.Environment.Exit(0);
                        }
                    }
                }
                ImGui.End();
            }
        }

        public void teleportPlayer(Vector3 position)
        {
            swed.WriteFloat(playerPositionXAddr, position[0]);
            swed.WriteFloat(playerPositionYAddr, position[1]);
            swed.WriteFloat(playerPositionZAddr, position[2]);
        }

        public void TrainerLogic()
        {
            moduleBase = swed.GetModuleBase("GameLogic.dll");
            worldAddr = swed.ReadPointer(moduleBase + 0x97D7C);
            playerAddr = swed.ReadPointer(worldAddr, 0x2C);
            walkSpeedAddr = playerAddr + 0x120;
            jumpSpeedAddr = playerAddr + 0x124;
            playerPositionXAddr = swed.ReadPointer(worldAddr, 0x1C, 0x4, 0x38, 0x8, 0x54) + 0x90;
            playerPositionYAddr = swed.ReadPointer(worldAddr, 0x1C, 0x4, 0x38, 0x8, 0x54) + 0x94;
            playerPositionZAddr = swed.ReadPointer(worldAddr, 0x1C, 0x4, 0x38, 0x8, 0x54) + 0x98;

            while (true)
            {
                if((GetAsyncKeyState(0x2D) & 0x8000) > 0)
                { 
                    while ((GetAsyncKeyState(0x2D) & 0x8000) > 0) { }
                    showTrainer = !showTrainer;
                    
                }
                swed.WriteFloat(walkSpeedAddr, walkSpeedValue);
                swed.WriteFloat(jumpSpeedAddr, jumpSpeedValue);
                playerPosition[0] = swed.ReadFloat(playerPositionXAddr);
                playerPosition[1] = swed.ReadFloat(playerPositionYAddr);
                playerPosition[2] = swed.ReadFloat(playerPositionZAddr);

                if (!editPos)
                {
                    playerEditPosition = playerPosition;
                }

                if (lockPos)
                {
                    swed.WriteFloat(playerPositionXAddr, playerLockedPosition[0]);
                    swed.WriteFloat(playerPositionYAddr, playerLockedPosition[1]);
                    swed.WriteFloat(playerPositionZAddr, playerLockedPosition[2]);
                }
                else
                {
                    playerLockedPosition = playerPosition;
                }

                if (jumpHack)
                {
                    swed.WriteBytes(moduleBase + 0x51680, "90 90 90");
                    swed.WriteBytes(moduleBase + 0x51685, "90 90");
                    swed.WriteBytes(moduleBase + 0x51687, "90 90");
                    swed.WriteBytes(moduleBase + 0x51689, "90 90 90");
                    swed.WriteBytes(moduleBase + 0x5168C, "90 90");
                    swed.WriteBytes(moduleBase + 0x5168E, "90 90");
                }
                else
                {
                    swed.WriteBytes(moduleBase + 0x51680, "8B 49 9C");
                    swed.WriteBytes(moduleBase + 0x51685, "74 07");
                    swed.WriteBytes(moduleBase + 0x51687, "8B 01");
                    swed.WriteBytes(moduleBase + 0x51689, "8B 40 50");
                    swed.WriteBytes(moduleBase + 0x5168C, "FF E0");
                    swed.WriteBytes(moduleBase + 0x5168E, "32 C0");
                }
            }
        }

        public static void SetupImGuiStyle()
        {
            var style = ImGui.GetStyle();

            style.Alpha = 1f;
            style.DisabledAlpha = 0.4000000059604645f;
            style.WindowPadding = new Vector2(10.0f, 10.0f);
            style.WindowRounding = 8.0f;
            style.WindowBorderSize = 0.0f;
            style.WindowMinSize = new Vector2(50.0f, 50.0f);
            style.WindowTitleAlign = new Vector2(0f, 0.5f);
            style.WindowMenuButtonPosition = ImGuiDir.Left;
            style.ChildRounding = 0.0f;
            style.ChildBorderSize = 1.0f;
            style.PopupRounding = 1.0f;
            style.PopupBorderSize = 1.0f;
            style.FramePadding = new Vector2(5.0f, 3.0f);
            style.FrameRounding = 3.0f;
            style.FrameBorderSize = 0.0f;
            style.ItemSpacing = new Vector2(6.0f, 6.0f);
            style.ItemInnerSpacing = new Vector2(3.0f, 2.0f);
            style.CellPadding = new Vector2(3.0f, 3.0f);
            style.IndentSpacing = 6.0f;
            style.ColumnsMinSpacing = 6.0f;
            style.ScrollbarSize = 13.0f;
            style.ScrollbarRounding = 16.0f;
            style.GrabMinSize = 20.0f;
            style.GrabRounding = 4.0f;
            style.TabRounding = 4.0f;
            style.TabBorderSize = 1.0f;
            style.TabMinWidthForCloseButton = 0.0f;
            style.ColorButtonPosition = ImGuiDir.Right;
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
            style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

            style.Colors[(int)ImGuiCol.Text] = new Vector4(0.8588235378265381f, 0.929411768913269f, 0.886274516582489f, 1.0f);
            style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.5215686559677124f, 0.5490196347236633f, 0.5333333611488342f, 1.0f);
            style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1294117718935013f, 0.1372549086809158f, 0.168627455830574f, 0.9f);
            style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.1490196138620377f, 0.1568627506494522f, 0.1882352977991104f, 1.0f);
            style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0.1372549086809158f, 0.1137254908680916f, 0.1333333402872086f, 1.0f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.168627455830574f, 0.1843137294054031f, 0.2313725501298904f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.2313725501298904f, 0.2000000029802322f, 0.2705882489681244f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.501960813999176f, 0.07450980693101883f, 0.2549019753932953f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.2000000029802322f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.239215686917305f, 0.239215686917305f, 0.2196078449487686f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.3882353007793427f, 0.3882353007793427f, 0.3725490272045135f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.6941176652908325f, 0.6941176652908325f, 0.686274528503418f, 1.0f);
            style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.6941176652908325f, 0.6941176652908325f, 0.686274528503418f, 1.0f);
            style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.658823549747467f, 0.1372549086809158f, 0.1764705926179886f, 1.0f);
            style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.7098039388656616f, 0.2196078449487686f, 0.2666666805744171f, 1.0f);
            style.Colors[(int)ImGuiCol.Button] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.Header] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.501960813999176f, 0.07450980693101883f, 0.2549019753932953f, 1.0f);
            style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.4274509847164154f, 0.4274509847164154f, 0.4980392158031464f, 1.0f);
            style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.09803921729326248f, 0.4000000059604645f, 0.7490196228027344f, 1.0f);
            style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.09803921729326248f, 0.4000000059604645f, 0.7490196228027344f, 1.0f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 0.5f);
            style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.7509804129600525f, 0.2490196138620377f, 0.4450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.6509804129600525f, 0.1490196138620377f, 0.3450980484485626f, 1.0f);
            style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.06666667014360428f, 0.1019607856869698f, 0.1450980454683304f, 1.0f);
            style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.1333333402872086f, 0.2588235437870026f, 0.4235294163227081f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.8588235378265381f, 0.929411768913269f, 0.886274516582489f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.3098039329051971f, 0.7764706015586853f, 0.196078434586525f, 1.0f);
            style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.4549019634723663f, 0.196078434586525f, 0.2980392277240753f, 1.0f);
            style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.1882352977991104f, 0.1882352977991104f, 0.2000000029802322f, 1.0f);
            style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.3098039329051971f, 0.3098039329051971f, 0.3490196168422699f, 1.0f);
            style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.2274509817361832f, 0.2274509817361832f, 0.2470588237047195f, 1.0f);
            style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.3843137323856354f, 0.6274510025978088f, 0.9176470637321472f, 1.0f);
            style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
            style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.2588235437870026f, 0.5882353186607361f, 0.9764705896377563f, 1.0f);
            style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 1.0f);
            style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.800000011920929f, 0.800000011920929f, 0.800000011920929f, 0.300000011920929f);
        }
        public static void Main(string[] args)
        {
            Program program = new Program();
            program.Start().Wait();
            Thread hackThread = new Thread(program.TrainerLogic) { IsBackground = true };
            hackThread.Start();
        }
    }
}