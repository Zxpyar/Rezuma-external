using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClickableTransparentOverlay;
using ImGuiNET;
using System.Runtime.InteropServices;
using Vortice.Mathematics;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Net.Http.Headers;

namespace basic_esp
{
    public class Renderer : Overlay
    {

        // Import von GetAsyncKeyState
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Screen size
        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SM_CXSCREEN = 0;
        const int SM_CYSCREEN = 1;
        public Vector2 screensize = new Vector2(GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));

        // Entity data
        private ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localplayer = new Entity();
        private readonly object entitylock = new object();

        // UI options
        private bool enableESP = true;
        private bool useCornerBoxes = false; // Option to switch to Corner Boxes
        private Vector4 enemycolor = new Vector4(1, 0, 0, 1); // Red for enemies
        private Vector4 teamcolor = new Vector4(0, 1, 0, 1); // Green for teammates
        private Vector4 hpcolor = new Vector4(1, 1, 1, 1);
        private Vector4 namecolor = new Vector4(1, 1, 1, 1);
        private Vector4 linecolor = new Vector4(1, 1, 1, 1);
        private Vector4 weaponespcolor = new Vector4(1, 1, 1, 1);
        private Vector4 filledboxcolor = new Vector4(0.5f, 0.5f, 0.5f, 0.5f);
        private Vector4 staticbarcolor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector4 gradientBox = new Vector4(0.0f, 0.8f, 0.8f, 0.5f);
        private Vector4 textColor = new Vector4(1f, 1.0f, 1.0f, 1.0f);
        private float boxThickness = 1.0f; // Box thickness
        private float lineThickness = 1.0f; // Line thickness
        private bool drawLines = true; // Option to enable/disable lines
        private bool drawTopLine = false; // Option to draw line at the top of the screen
        private bool drawBottomLine = false; // Option to draw line at the bottom of the screen
        private bool drawCrosshairLine = false; // Option to draw a crosshair (center) line
        public bool drawhealthbar = false;
        public bool teamesp = false;
        public bool hptext = false;
        public bool enablename = false;
        public bool weaponesp = false;
        public bool watermark = true;
        public bool useFilledBoxes = false;
        public bool enablebox = true;
        public bool skeleHead = false;
        public bool statitchealthbar = false;
        public bool tbcombat = false;
        public bool gradientfillbox = false;
        public bool bombPlanted = false;


        //bombtimeer
        public bool bombTimer = false;
        public bool bomePlanted = false;
        public int timeLeft = -1;

        //aimbot anfang
        public bool aimbot = false;
        public bool aimOnTeam = false;
        public bool drawAimFov = false;
        public Vector2 screenSize = new Vector2(1920, 1080);
        public float FOV = 50;
        public Vector4 circleColor = new Vector4(1, 1, 1, 1);

        //bones
        public Vector4 boneColor = new Vector4(1, 1, 1, 1);
        public float boneThickness = 4;
        public bool skeletons = false;

        string fontpath = @"C:\Windows\Fonts\arial.ttf";
        // Crosshair-Farbe
        private Vector4 crosshairColor = new Vector4(1.0f, 0.0f, 0.0f, 1.0f); // Standard Rot

        // Crosshair-Größen
        private float crosshairWidth = 10.0f;  // Breite der horizontalen Linie
        private float crosshairHeight = 10.0f; // Höhe der vertikalen Linie

        // Linien-Dicke
        private float crosshairThickness = 1.5f; // Standarddicke


        // Hotkey-related state
        private bool isMenuVisible = true; // Controls if the menu is visibleüü
        private const int VK_INSERT = 0x2D; // insert key

        ImDrawListPtr drawList;

        public int switchTabs = 0;

        // Render loop

        [DllImport("user32.dll")]
        static extern nint FindWindow(string lpClassName, string lpWindowname);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(nint hWnd);
        [DllImport("user32.dll")]
        static extern nint SetActiveWindow(nint hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);


        static void selectWindow(string windowName)
        {
            var windowHwnd = FindWindow(null, windowName);

            SetForegroundWindow(windowHwnd);
            SetActiveWindow(windowHwnd);
        }


        protected override void Render()
        {
            // Überprüfen, ob der Hotkey gedrückt wurde
            if ((GetAsyncKeyState(VK_INSERT) & 0x8000) != 0)
            {
                isMenuVisible = !isMenuVisible; // Menü anzeigen oder verstecken
                System.Threading.Thread.Sleep(150); // Verzögerung, um Key-Spamming zu verhindern
            }

            // Nur rendern, wenn das Menü sichtbar ist
            if (isMenuVisible)
            {
                var colors = ImGui.GetStyle().Colors;
                ImGui.SaveIniSettingsToDisk(null);

                selectWindow("Overlay");
                ApplyCustomStyle();
                ReplaceFont(fontpath, 16, FontGlyphRangeType.English);

                ImGui.SetNextWindowSize(new Vector2(900, 538), ImGuiCond.FirstUseEver); // Set initial window size
                ImGui.Begin("##hiddenTitleBar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);

                // Hintergrund für den Titelbereich zeichnen
                float windowWidth = ImGui.GetWindowSize().X;
                float titleBarHeight = 30.0f; // Höhe des Titelbereichs
                                              //ImGui.GetWindowDrawList().AddRectFilled(
                                              //    ImGui.GetWindowPos(),
                                              //    new Vector2(ImGui.GetWindowPos().X + windowWidth, ImGui.GetWindowPos().Y + titleBarHeight),  // aus grünen für keine frabe oben
                                              //    ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.3f, 0.6f, 1.0f)) // Blau
                                              //);

                // Text zentrieren

                string title = "Rezuma External";
                float textWidth = ImGui.CalcTextSize(title).X;
                float textX = ImGui.GetWindowPos().X + (windowWidth - textWidth) / 2.0f;
                float textY = ImGui.GetWindowPos().Y + (titleBarHeight - ImGui.CalcTextSize(title).Y) / 2.0f + 14;

                ImGui.GetWindowDrawList().AddText(new Vector2(textX - 375, textY), ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), title);

                colors[(int)ImGuiCol.Button] = new Vector4(0.13f, 0.13f, 0.17f, 1.0f);
                colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.19f, 0.2f, 0.25f, 1.0f);
                colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
                colors[(int)ImGuiCol.CheckMark] = new Vector4(0.74f, 0.58f, 0.98f, 1.0f);

                ImGui.SetCursorPosX(20 + 330);
                ImGui.SetCursorPosY(15);


                ImGui.BeginChild("kind1", new Vector2(550.0f, 50.0f));

                if (ImGui.Button("Aimbot", new Vector2(100.0f, 30.0f)))
                {
                    switchTabs = 0;
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(1f, 1f, 1f, 0.5f));
                }

                ImGui.SameLine(0);

                if (ImGui.Button("Visuals", new Vector2(100.0f, 30.0f)))
                    switchTabs = 1;

                ImGui.SameLine(0);

                if (ImGui.Button("Colors", new Vector2(100.0f, 30.0f)))
                    switchTabs = 2;

                ImGui.SameLine(0);

                if (ImGui.Button("CrossHair", new Vector2(100.0f, 30.0f)))
                    switchTabs = 3;

                ImGui.SameLine(0);

                if (ImGui.Button("Misc", new Vector2(100.0f, 30.0f)))
                    switchTabs = 4;

                ImGui.EndChild();

                // Setze den Cursor unter den Titelbereich
                ImGui.SetCursorPosY(titleBarHeight + 5.0f);

                // Tab bar for different sections
                if (ImGui.BeginTabBar("##tabs", ImGuiTabBarFlags.FittingPolicyResizeDown))
                {
                    PurpleStyle();

                    //paste imgui here
                    switch (switchTabs)
                    {
                        case 0: // Aimbot
                            ImGui.BeginGroup();

                            ImGui.BeginChild("child1", new Vector2(450, 200));

                            ImGui.Text("All ts prolly detected so dont use xD");

                            ImGui.Checkbox("Aimbot", ref aimbot);
                            ShowTooltip("Automaticlly locks on the players head");
                            ImGui.Checkbox("Draw FOV", ref drawAimFov);
                            ShowTooltip("Draws the FOV circle");

                            ImGui.SliderFloat("FOV", ref FOV, 10, 300);
                            ImGui.Separator();
                            ImGui.Text("Change FOV circle color");
                            ImGui.ColorEdit4("##circlecolor", ref circleColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf);

                            ImGui.EndChild();
                            ImGui.SameLine();

                            ImGui.BeginChild("child2", new Vector2(400, 200));

                            ImGui.Checkbox("Triggerbot", ref tbcombat);
                            ShowTooltip("Automaticlly shoots when ur on the player");
                            ImGui.Text("Keybind: Hold X");

                            ImGui.EndChild();

                            ImGui.EndGroup();
                            break;

                        case 1: // Visuals 
                            ImGui.BeginGroup();
                            ImGui.BeginChild("child1", new Vector2(450, 120));
                            ImGui.Text("ESP Settings");
                            ImGui.Checkbox("Enable ESP", ref enableESP);
                            ShowTooltip("Enable or disable the ESP rendering.");

                            ImGui.Checkbox("team esp", ref teamesp);
                            ShowTooltip("enable/disable team esp");

                            ImGui.Checkbox("Skeletons", ref skeletons);
                            ImGui.Checkbox("Render Head", ref skeleHead);
                            ShowTooltip("Shows the Head skeleton");

                            ImGui.EndChild();
                            ImGui.SameLine();

                            ImGui.BeginChild("child2", new Vector2(400, 100));

                            ImGui.Checkbox("box esp", ref enablebox);
                            ShowTooltip("Enable or disable ESP boxes .");

                            ImGui.Checkbox("Use Corner Boxes", ref useCornerBoxes);
                            ShowTooltip("Enable or disable corner-style ESP boxes.");

                            ImGui.Checkbox("Use filled Boxes", ref useFilledBoxes);
                            ShowTooltip("Enable or disable filled ESP boxes draws a filling in the box.");

                            ImGui.Checkbox("Gradient Fill Box", ref gradientfillbox);

                            ImGui.EndChild();

                            ImGui.Separator();

                            ImGui.Checkbox("hp text", ref hptext);
                            ShowTooltip("shows a hp number 69/100 next to the healthbar");

                            ImGui.Checkbox("Draw Health Bar", ref drawhealthbar);
                            ShowTooltip("Enable or disable health bars for enemies.");

                            ImGui.Checkbox("Static Health Bar", ref statitchealthbar);
                            ShowTooltip("Makes the health bar static");

                            ImGui.Separator();

                            ImGui.Checkbox("name esp", ref enablename);
                            ShowTooltip("show the name of the enemy");

                            ImGui.Checkbox("weapon esp", ref weaponesp);
                            ShowTooltip("Shows the weapon the enemy is currently holding");

                            ImGui.Separator();

                            // Line and Box Settings
                            ImGui.Text("Box and Line Settings");
                            ImGui.SliderFloat("Box Thickness", ref boxThickness, 1.0f, 5.0f, "%.1f");
                            ShowTooltip("Adjust the thickness of ESP boxes.");
                            ImGui.SliderFloat("Line Thickness", ref lineThickness, 1.0f, 5.0f, "%.1f");
                            ShowTooltip("Adjust the thickness of ESP lines.");
                            ImGui.Checkbox("snapline esp", ref drawLines);
                            ShowTooltip("Enable or disable drawing lines from the player to entities.");
                            ImGui.Checkbox("Draw Top snapLine", ref drawTopLine);
                            ShowTooltip("Enable to draw a line at the top of the screen.");
                            ImGui.Checkbox("Draw Bottom snapLine", ref drawBottomLine);
                            ShowTooltip("Enable to draw a line at the bottom of the screen.");

                            ImGui.Separator();

                            // Crosshair
                            ImGui.Text("Sniper Crosshair");
                            ImGui.Checkbox("Sniper Crosshair", ref drawCrosshairLine);
                            ShowTooltip("Enable or disable a sniper-style crosshair.");
                            ImGui.EndGroup();

                            break;

                        case 2:
                            ImGui.BeginGroup();
                            ImGui.Text("Team Color");
                            ImGui.ColorEdit4("##teamcolor", ref teamcolor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf);
                            ImGui.SliderFloat("Team Transparency", ref teamcolor.W, 0.0f, 1.0f);
                            ShowTooltip("Adjust the transparency of the team ESP.");

                            ImGui.Separator();

                            // Enemy Color
                            ImGui.Text("box esp Color");
                            ImGui.ColorEdit4("##enemycolor", ref enemycolor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf);
                            ImGui.SliderFloat("Enemy Transparency", ref enemycolor.W, 0.0f, 1.0f);
                            ShowTooltip("Adjust the transparency of the enemy ESP.");

                            ImGui.Separator();

                            ImGui.Text("Gradient Color");
                            ImGui.ColorEdit4("##gradient Color", ref gradientBox, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf);

                            ImGui.Separator();

                            ImGui.Text("snapline color");
                            ImGui.ColorEdit4("##snapline color", ref linecolor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf);
                            ImGui.SliderFloat("snapline transparency", ref linecolor.W, 0.0f, 1.0f);
                            ShowTooltip("change enemy snapline color");

                            ImGui.Separator();

                            ImGui.Text("Skeletons Color");
                            ImGui.ColorEdit4("##skelcolor", ref boneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf);
                            ShowTooltip("Adjust the transparency of the Bones.");

                            ImGui.EndGroup();

                            break;

                        case 3:
                            ImGui.BeginGroup();

                            // Crosshair Customizat
                            ImGui.Text("Crosshair Customization");
                            ImGui.ColorEdit4("Crosshair Color", ref crosshairColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.AlphaPreviewHalf);
                            ImGui.SliderFloat("Crosshair Width", ref crosshairWidth, 1.0f, 50.0f, "%.1f");
                            ShowTooltip("Adjust the width of the horizontal crosshair line.");
                            ImGui.SliderFloat("Crosshair Height", ref crosshairHeight, 1.0f, 50.0f, "%.1f");
                            ShowTooltip("Adjust the height of the vertical crosshair line.");
                            ImGui.SliderFloat("Crosshair Thickness", ref crosshairThickness, 0.1f, 5.0f, "%.1f");
                            ShowTooltip("Adjust the thickness of the crosshair lines.");

                            ImGui.EndGroup();
                            break;

                        case 4:
                            ImGui.BeginGroup();

                            ImGui.Checkbox("watermark", ref watermark);
                            ShowTooltip("Enable or disable a watermark.");

                            //ImGui.Checkbox("Bomb Timer", ref bombTimer);

                            ImGui.EndGroup();
                            break;
                    }

                }

                ImGui.End();

            }


            // Draw the overlay
            DrawOverlay(screensize);
            drawList = ImGui.GetWindowDrawList();
            if (drawAimFov && aimbot)
            {
                drawList.AddCircle(new Vector2(screenSize.X / 2, screenSize.Y / 2), FOV, ImGui.ColorConvertFloat4ToU32(circleColor));
            }

            if (bombTimer)
            {
                if (bombPlanted)
                {
                    ImGui.Begin("Bombtimer");
                    ImGui.Text("Bomb planted");
                    ImGui.Text($"Seconds before explosion: {timeLeft}");
                }
            }

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity)) // Prüfen, ob die Entität auf dem Bildschirm ist
                    {
                        // Box ESP
                        if (enablebox)
                        {
                            if (useCornerBoxes)
                            {
                                DrawCornerBox(entity); // Zeichne Corner Boxes
                            }
                            else
                            {
                                DrawBox(entity); // Zeichne normale Boxen
                            }
                        }

                        if (weaponesp)
                        {
                            DrawWeapons();
                        }

                        // Gefüllte Boxen
                        if (useFilledBoxes)
                        {
                            DrawFilledBox(entity); // Zeichne gefüllte Boxen
                        }

                        if (gradientfillbox)
                        {
                            drawGradientFillBox(entity);
                        }



                        if (drawhealthbar)
                        {
                            DrawHealthBar(entity);
                        }

                        if (statitchealthbar)
                        {
                            statichealthbar(entity);
                        }
                    }

                    if (enablename)
                    {
                        DrawName(entity, 20);
                    }

                }
                if (drawCrosshairLine)
                {
                    DrawCrosshair();
                }


                if (drawLines)
                {
                    foreach (var entity in entities)
                    {
                        if (drawTopLine)
                        {
                            DrawTopLine(entity);
                        }
                        if (drawBottomLine)
                        {
                            DrawBottomLine(entity);
                        }
                    }
                }


            }

            if (skeletons)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        DrawSkeletons(entity);
                    }

                    if (skeleHead)
                    {
                        DrawSkelHead(entity);
                    }
                }
            }

            if (watermark)
            {
                // Get ImGui IO and Style pointers
                ImGuiIOPtr io = ImGui.GetIO();
                var style = ImGui.GetStyle();
                var colors = style.Colors;

                // Calculate FPS (limit is handled elsewhere)
                float fps = io.Framerate;

                // Get the current clock time (HH:mm:ss format)
                string currentTime = DateTime.Now.ToString("HH:mm:ss");

                // Define watermark text with FPS and current clock time
                string watermarkText = $"Rezuma | {fps.ToString("0", CultureInfo.InvariantCulture)} FPS | {currentTime}";

                // Get the size of the text
                Vector2 textSize = ImGui.CalcTextSize(watermarkText);

                // Padding around the text
                float padding = 8.0f;

                // Background dimensions
                Vector2 backgroundSize = new Vector2(textSize.X + 2 * padding, textSize.Y + 2 * padding);

                // Position for the top-right corner
                Vector2 windowSize = ImGui.GetMainViewport().Size;  // Use viewport size for multi-window setups
                Vector2 watermarkPosition = new Vector2(windowSize.X - backgroundSize.X - 10, 10); // Fixed margin of 10px

                // Background top-left and bottom-right corners
                Vector2 backgroundTopLeft = watermarkPosition;
                Vector2 backgroundBottomRight = watermarkPosition + backgroundSize;

                // Save current style for later restoration
                var originalWindowBgColor = colors[(int)ImGuiCol.TitleBgActive];
                var originalTextColor = colors[(int)ImGuiCol.Text];
                var originalBorderColor = colors[(int)ImGuiCol.Border];

                // Set custom style for the watermark background and text
                colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.1f, 0.1f, 0.1f, 0.7f); // Semi-transparent background color
                colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White text color
                colors[(int)ImGuiCol.Border] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White border color

                // Ensure drawlist is initialized
                ImDrawListPtr drawlist = ImGui.GetBackgroundDrawList();

                // Draw white border around the watermark
                drawlist.AddRect(
                    backgroundTopLeft,
                    backgroundBottomRight,
                    ImGui.ColorConvertFloat4ToU32(colors[(int)ImGuiCol.Border]),
                    0.0f, // Corner rounding

                    (ImDrawFlags).0f // Border thickness
                );

                // Draw rounded background rectangle for watermark
                drawlist.AddRectFilled(
                    backgroundTopLeft,
                    backgroundBottomRight,
                    ImGui.ColorConvertFloat4ToU32(colors[(int)ImGuiCol.TitleBgActive]),
                    8.0f // Corner rounding
                );

                // Draw the watermark text
                drawlist.AddText(watermarkPosition + new Vector2(padding, padding), ImGui.ColorConvertFloat4ToU32(colors[(int)ImGuiCol.Text]), watermarkText);

                // Restore the original style
                colors[(int)ImGuiCol.TitleBgActive] = originalWindowBgColor;
                colors[(int)ImGuiCol.Text] = originalTextColor;
                colors[(int)ImGuiCol.Border] = originalBorderColor;
            }
        }


        public void DrawSkeletons(Entity entity)
        {
            uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);

            float currentBoneThickness = boneThickness / entity.distance;

            drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness); // Neck to head
            drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness); // Neck to left shoulder
            drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness); // Neck to right shoulder
            drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness); // Left shoulder to left arm
            drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness); // Right shoulder to right arm
            drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness); // Neck to waist
            drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness); // Waist to left knee
            drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness); // Waist to right knee
            drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness); // Left knee to left foot
            drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness); // Right knee to right foot


        }

        void drawGradientFillBox(Entity entity)
        {
            // Berechnung der Höhe basierend auf Kopf- und Fußposition
            float entityHeight = Math.Abs(entity.ViewPosition2D.Y - entity.position2D.Y);
            float boxWidth = entityHeight / 1.70f; // Breite auf die Hälfte der Höhe setzen

            // Offset, um die Box über die Kopfposition anzuheben
            float headOffset = entityHeight * 0.12f; // 12% der Höhe als Offset

            // Berechnung der oberen linken und unteren rechten Ecke der Box
            Vector2 rectTop = new Vector2(entity.position2D.X - boxWidth / 2, entity.ViewPosition2D.Y - headOffset);
            Vector2 rectBottom = new Vector2(entity.position2D.X + boxWidth / 2, entity.position2D.Y);

            // Berechnung der inneren Fläche (Füllung innerhalb der Box)
            float fillPadding = 1.5f; // Abstand zum Rand der Box
            Vector2 fillTop = new Vector2(rectTop.X + fillPadding, rectTop.Y + fillPadding);
            Vector2 fillBottom = new Vector2(rectBottom.X - fillPadding, rectBottom.Y - fillPadding);

            // Gradient Schrittweite (Höhe jedes Streifens)
            int gradientSteps = 150; // Anzahl der Schritte im Verlauf
            float gradientHeight = rectBottom.Y - rectTop.Y; // Tatsächliche Höhe der Box
            float stepHeight = gradientHeight / gradientSteps;

            // Erstelle die Übergangsfarben (unten: Schwarz, oben: Original Farbe)
            Vector4 startColor = new Vector4(1.0f, 0.2f, 0.6f, gradientBox.W); // Schwarz mit Alpha-Wert
            Vector4 endColor = gradientBox;

            // Zeichnen des Verlaufs
            for (int i = 0; i < gradientSteps; i++)
            {
                // Berechne die Interpolationsfarbe für diesen Schritt
                float t = (float)i / (gradientSteps - 1); // Normalisiertes t (0 bis 1)
                Vector4 currentColor = new Vector4(
                    startColor.X * (1 - t) + endColor.X * t, // Rot
                    startColor.Y * (1 - t) + endColor.Y * t, // Grün
                    startColor.Z * (1 - t) + endColor.Z * t, // Blau
                    startColor.W * (1 - t) + endColor.W * t  // Alpha
                );

                // Berechne den oberen und unteren Rand dieses Streifens
                float stripTopY = rectTop.Y + i * stepHeight;
                float stripBottomY = rectTop.Y + (i + 1) * stepHeight;

                // Verhindere das Zeichnen außerhalb der Boxgrenzen
                if (stripBottomY > rectBottom.Y)
                {
                    stripBottomY = rectBottom.Y;
                }

                if (stripTopY >= rectBottom.Y)
                {
                    break; // Beende die Schleife, wenn der Gradient außerhalb der Box ist
                }

                Vector2 stripTop = new Vector2(rectTop.X, stripTopY);
                Vector2 stripBottom = new Vector2(rectBottom.X, stripBottomY);

                // Zeichne den Streifen mit der aktuellen Farbe
                drawList.AddRectFilled(stripTop, stripBottom, ImGui.ColorConvertFloat4ToU32(currentColor));
            }
        }

        private void DrawWeapons()
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            try
            {
                List<Entity> tempEntities = new List<Entity>(entities).ToList();
                foreach (Entity entity in tempEntities)
                {
                    if (entity != null)
                    {
                        drawList.AddText(entity.position2D, ImGui.ColorConvertFloat4ToU32(textColor), $"{entity.currentWeaponName}");
                    }
                }
            }
            catch { }

        }

        private void DrawSkelHead(Entity entity)
        {
            uint uintColor = ImGui.ColorConvertFloat4ToU32(boneColor);

            float currentBoneThickness = boneThickness / entity.distance;

            drawList.AddCircle(entity.bones2d[2], 5 + currentBoneThickness, uintColor);// Circle on the head


        }

        bool EntityOnScreen(Entity entity)
        {
            if (entity.position2D.X > 0 && entity.position2D.X < screensize.X && entity.position2D.Y > 0 && entity.position2D.Y < screensize.Y)
            {
                return true;
            }
            return false;
        }

        // Draw ESP box around the entity
        // Draw ESP box around the entity
        private void DrawBox(Entity entity)
        {
            // Berechnung der Höhe basierend auf Kopf- und Fußposition
            float entityHeight = Math.Abs(entity.ViewPosition2D.Y - entity.position2D.Y);
            float boxWidth = entityHeight / 1.70f; // Breite auf die Hälfte der Höhe setzen

            // Offset, um die Box über die Kopfposition hinaus anzuheben
            float headOffset = entityHeight * 0.12f; // 10% der Höhe als Kopf-Offset

            // Berechnung der oberen linken und unteren rechten Ecke der Box
            Vector2 rectTop = new Vector2(entity.position2D.X - boxWidth / 2, entity.ViewPosition2D.Y - headOffset);
            Vector2 rectBottom = new Vector2(entity.position2D.X + boxWidth / 2, entity.position2D.Y);

            // Farbwahl basierend auf dem Team
            Vector4 boxColor = localplayer.team == entity.team ? teamcolor : enemycolor;


            // Outline-Farbe (leichtes Grau, damit sie nicht zu dunkel ist)
            Vector4 outlineColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f); // Grau, etwas heller als Schwarz

            // Stärke der Outline
            float outlineThickness = boxThickness + 0.3f;

            // Zeichnen der Umrandung
            drawList.AddRect(rectTop - new Vector2(1, 1), rectBottom + new Vector2(1, 1), ImGui.ColorConvertFloat4ToU32(outlineColor), 0.0f, ImDrawFlags.None, outlineThickness);

            // Zeichnen der Box
            drawList.AddRect(rectTop, rectBottom, ImGui.ColorConvertFloat4ToU32(boxColor), 0.0f, ImDrawFlags.None, boxThickness);
        }

        private void DrawFilledBox(Entity entity)
        {
            // Berechnung der Höhe basierend auf Kopf- und Fußposition
            float entityHeight = Math.Abs(entity.ViewPosition2D.Y - entity.position2D.Y);
            float boxWidth = entityHeight / 1.70f; // Breite auf die Hälfte der Höhe setzen

            // Offset, um die Box über die Kopfposition hinaus anzuheben
            float headOffset = entityHeight * 0.12f; // 12% der Höhe als Kopf-Offset

            // Berechnung der oberen linken und unteren rechten Ecke der Box
            Vector2 rectTop = new Vector2(entity.position2D.X - boxWidth / 2, entity.ViewPosition2D.Y - headOffset);
            Vector2 rectBottom = new Vector2(entity.position2D.X + boxWidth / 2, entity.position2D.Y);

            // Berechnung der inneren Fläche (Füllung innerhalb der Box)
            float fillPadding = 1.5f; // Abstand zum Rand der Box
            Vector2 fillTop = new Vector2(rectTop.X + fillPadding, rectTop.Y + fillPadding);
            Vector2 fillBottom = new Vector2(rectBottom.X - fillPadding, rectBottom.Y - fillPadding);

            // Farbe für die Füllung (individuell einstellbar)
            Vector4 fillColor = filledboxcolor;

            // Zeichnen der Füllung innerhalb der Box
            drawList.AddRectFilled(fillTop, fillBottom, ImGui.ColorConvertFloat4ToU32(fillColor));
        }



        // lebens anzeigen esp code 
        private Dictionary<Entity, float> healthDisplayValues = new Dictionary<Entity, float>();
        private const float HealthAnimationSpeed = 5.0f; // Geschwindigkeit des Übergangs (höher = schneller)

        // Easing-Funktion für glattere Bewegung
        private float SmoothStep(float from, float to, float deltaTime)
        {
            float t = Math.Clamp(deltaTime * HealthAnimationSpeed, 0f, 1f);
            t = t * t * (3f - 2f * t); // Smoothstep-Funktion
            return from + (to - from) * t;
        }

        private void DrawHealthBar(Entity entity)
        {
            // Initialisierung des angezeigten Gesundheitswerts
            if (!healthDisplayValues.ContainsKey(entity))
                healthDisplayValues[entity] = entity.health;

            // Delta-Zeit (Zeit zwischen Frames)
            float deltaTime = ImGui.GetIO().DeltaTime;

            // Sanfte Interpolation des Gesundheitswerts
            float currentDisplayedHealth = healthDisplayValues[entity];
            float targetHealth = entity.health;
            healthDisplayValues[entity] = SmoothStep(currentDisplayedHealth, targetHealth, deltaTime);

            // Berechnung der Healthbar
            float displayedHealth = healthDisplayValues[entity]; // Animierte Gesundheit
            float entityHeight = entity.position2D.Y - entity.ViewPosition2D.Y;
            entityHeight *= 1.12f;
            float boxLeft = entity.ViewPosition2D.X - entityHeight / 2.7f;
            float boxRight = entity.position2D.X + entityHeight / 2.7f;

            float barPercentWidth = 0.035f;
            float barPixelWidth = barPercentWidth * (boxRight - boxLeft);
            float barHeight = entityHeight * (displayedHealth / 100f);

            // Positionen für die Healthbar
            Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2D.Y - barHeight);
            Vector2 barBottom = new Vector2(boxLeft, entity.position2D.Y);

            // Berechne die Farbe basierend auf der Gesundheit (0 = Orange, 1 = Grün)
            float healthRatio = displayedHealth / 100f;
            Vector4 barColor = new Vector4(1 - healthRatio, healthRatio, 0, 1); // Von Grün zu Orange

            // Zeichne die gefüllte Gesundheitsleiste
            drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(barColor));

            // Umrandung für die Gesundheitsleiste
            Vector2 outlineTopLeft = new Vector2(barTop.X - 1, barTop.Y - 1); // Offset für die Umrandung
            Vector2 outlineBottomRight = new Vector2(barBottom.X + 1, barBottom.Y + 1);
            Vector4 outlineColor = new Vector4(0, 0, 0, 1); // Schwarz für die Umrandung

            drawList.AddRect(outlineTopLeft, outlineBottomRight, ImGui.ColorConvertFloat4ToU32(outlineColor));

            // Lebensanzeige als Text neben der Gesundheitsleiste
            if (hptext)
            {
                string healthText = $"{(int)displayedHealth}HP"; // Anzeige der tatsächlichen Gesundheit
                Vector2 textPos = new Vector2(boxLeft - barPixelWidth - 50, entity.position2D.Y - barHeight - 10); // Position des Textes
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(hpcolor), healthText);
            }
        }




        // Name ESP function
        private void DrawName(Entity entity, int yOffset)
        {
            // Calculate the position for the text
            Vector2 textSize = ImGui.CalcTextSize(entity.name); // Get the size of the text
            Vector2 textLocation = new Vector2(
                entity.ViewPosition2D.X - textSize.X / 2, // Center the text horizontally
                entity.position2D.Y   // Offset the text vertically
            );

            // Draw the text on the screen
            drawList.AddText(textLocation, ImGui.ColorConvertFloat4ToU32(namecolor), $"{entity.name}");
        }




        private void statichealthbar(Entity entity)
        {
            // Initialisierung des angezeigten Gesundheitswerts
            if (!healthDisplayValues.ContainsKey(entity))
                healthDisplayValues[entity] = entity.health;

            // Sanfte Interpolation des Gesundheitswerts
            float currentDisplayedHealth = healthDisplayValues[entity];
            float targetHealth = entity.health;

            // Berechnung der Healthbar
            float displayedHealth = healthDisplayValues[entity]; // Animierte Gesundheit
            float entityHeight = entity.position2D.Y - entity.ViewPosition2D.Y;
            entityHeight *= 1.12f;
            float boxLeft = entity.ViewPosition2D.X - entityHeight / 3.3f;
            float boxRight = entity.position2D.X + entityHeight / 3.3f;

            float barPixelWidth = 2;
            float barHeight = entityHeight * (displayedHealth / 100f);

            // Positionen für die Healthbar
            Vector2 barTop = new Vector2(boxLeft - barPixelWidth, entity.position2D.Y - barHeight);
            Vector2 barBottom = new Vector2(boxLeft, entity.position2D.Y);

            // Statische Farbe der Healthbar (z. B. Grün)
            Vector4 staticbarColor = new Vector4(0, 1, 0, 1); // Statische grüne Farbe

            // Zeichne die gefüllte Gesundheitsleiste
            drawList.AddRectFilled(barTop, barBottom, ImGui.ColorConvertFloat4ToU32(staticbarcolor));

            // Umrandung für die Gesundheitsleiste
            Vector2 outlineTopLeft = new Vector2(barTop.X - 1, barTop.Y - 1); // Offset für die Umrandung
            Vector2 outlineBottomRight = new Vector2(barBottom.X + 1, barBottom.Y + 1);
            Vector4 outlineColor = new Vector4(0, 0, 0, 1); // Schwarz für die Umrandung

            drawList.AddRect(outlineTopLeft, outlineBottomRight, ImGui.ColorConvertFloat4ToU32(outlineColor));

            // Lebensanzeige als Text neben der Gesundheitsleiste
            if (hptext)
            {
                string healthText = $"{(int)displayedHealth}HP"; // Anzeige der tatsächlichen Gesundheit
                Vector2 textPos = new Vector2(boxLeft - barPixelWidth - 50, entity.position2D.Y - barHeight - 10); // Position des Textes
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(hpcolor), healthText);
            }
        }
        private void DrawCornerBox(Entity entity)
        {
            // Berechnung der Höhe basierend auf Kopf- und Fußposition
            float entityHeight = Math.Abs(entity.ViewPosition2D.Y - entity.position2D.Y);
            float boxWidth = entityHeight / 1.70f; // Gleiche Breite wie bei der vollständigen Box

            // Offset, um die Box über die Kopfposition hinaus anzuheben
            float headOffset = entityHeight * 0.12f;

            // Eckpunkte der Box
            Vector2 topLeft = new Vector2(entity.position2D.X - boxWidth / 2, entity.ViewPosition2D.Y - headOffset);
            Vector2 topRight = new Vector2(entity.position2D.X + boxWidth / 2, entity.ViewPosition2D.Y - headOffset);
            Vector2 bottomLeft = new Vector2(entity.position2D.X - boxWidth / 2, entity.position2D.Y);
            Vector2 bottomRight = new Vector2(entity.position2D.X + boxWidth / 2, entity.position2D.Y);

            // Boxfarbe basierend auf dem Team
            Vector4 boxColor = localplayer.team == entity.team ? teamcolor : enemycolor;

            // Outline-Farbe (leichtes Grau)
            Vector4 outlineColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f); // Grau für die Umrandung

            // Längenanpassung der Ecken (20% der Boxhöhe)
            float cornerLength = entityHeight * 0.13f;

            // Umrandung der Ecken zeichnen
            float outlineThickness = boxThickness + 0.3f;

            // Top-left corner (Outline)
            drawList.AddLine(topLeft - new Vector2(1, 1), new Vector2(topLeft.X + cornerLength, topLeft.Y) - new Vector2(1, 1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);
            drawList.AddLine(topLeft - new Vector2(1, 1), new Vector2(topLeft.X, topLeft.Y + cornerLength) - new Vector2(1, 1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);

            // Top-right corner (Outline)
            drawList.AddLine(topRight - new Vector2(-1, 1), new Vector2(topRight.X - cornerLength, topRight.Y) - new Vector2(-1, 1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);
            drawList.AddLine(topRight - new Vector2(-1, 1), new Vector2(topRight.X, topRight.Y + cornerLength) - new Vector2(-1, 1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);

            // Bottom-left corner (Outline)
            drawList.AddLine(bottomLeft - new Vector2(1, -1), new Vector2(bottomLeft.X + cornerLength, bottomLeft.Y) - new Vector2(1, -1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);
            drawList.AddLine(bottomLeft - new Vector2(1, -1), new Vector2(bottomLeft.X, bottomLeft.Y - cornerLength) - new Vector2(1, -1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);

            // Bottom-right corner (Outline)
            drawList.AddLine(bottomRight - new Vector2(-1, -1), new Vector2(bottomRight.X - cornerLength, bottomRight.Y) - new Vector2(-1, -1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);
            drawList.AddLine(bottomRight - new Vector2(-1, -1), new Vector2(bottomRight.X, bottomRight.Y - cornerLength) - new Vector2(-1, -1), ImGui.ColorConvertFloat4ToU32(outlineColor), outlineThickness);

            // Ecken der Box zeichnen
            // Top-left corner
            drawList.AddLine(topLeft, new Vector2(topLeft.X + cornerLength, topLeft.Y), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);
            drawList.AddLine(topLeft, new Vector2(topLeft.X, topLeft.Y + cornerLength), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);

            // Top-right corner
            drawList.AddLine(topRight, new Vector2(topRight.X - cornerLength, topRight.Y), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);
            drawList.AddLine(topRight, new Vector2(topRight.X, topRight.Y + cornerLength), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);

            // Bottom-left corner
            drawList.AddLine(bottomLeft, new Vector2(bottomLeft.X + cornerLength, bottomLeft.Y), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);
            drawList.AddLine(bottomLeft, new Vector2(bottomLeft.X, bottomLeft.Y - cornerLength), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);

            // Bottom-right corner
            drawList.AddLine(bottomRight, new Vector2(bottomRight.X - cornerLength, bottomRight.Y), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);
            drawList.AddLine(bottomRight, new Vector2(bottomRight.X, bottomRight.Y - cornerLength), ImGui.ColorConvertFloat4ToU32(boxColor), boxThickness);
        }

        // Draw a line from the center of the screen to the entity
        private void DrawLine(Entity entity)
        {
            Vector4 lineColor = localplayer.team == entity.team ? teamcolor : enemycolor;
            drawList.AddLine(new Vector2(screensize.X / 2, screensize.Y / 2), entity.position2D, ImGui.ColorConvertFloat4ToU32(lineColor), lineThickness);
        }

        // Draw the top line
        private void DrawTopLine(Entity entity)
        {
            Vector2 start = new Vector2(screensize.X / 2, 0);

            drawList.AddLine(start, entity.position2D, ImGui.ColorConvertFloat4ToU32(linecolor), lineThickness);
        }

        // Draw the bottom line
        private void DrawBottomLine(Entity entity)
        {
            Vector2 start = new Vector2(screensize.X / 2, screensize.Y);

            drawList.AddLine(start, entity.position2D, ImGui.ColorConvertFloat4ToU32(linecolor), lineThickness);
        }

        // Draw the crosshair 
        private void DrawCrosshair()
        {
            // Berechne Bildschirmmitte
            Vector2 center = new Vector2(screensize.X / 2, screensize.Y / 2);

            // Horizontaler Teil des Crosshairs
            Vector2 start = new Vector2(center.X - crosshairWidth, center.Y);
            Vector2 end = new Vector2(center.X + crosshairWidth, center.Y);
            drawList.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(crosshairColor), crosshairThickness);

            // Vertikaler Teil des Crosshairs
            start = new Vector2(center.X, center.Y - crosshairHeight);
            end = new Vector2(center.X, center.Y + crosshairHeight);
            drawList.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(crosshairColor), crosshairThickness);
        }




        public void PurpleStyle()
        {
            var colors = ImGui.GetStyle().Colors;

            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1f, 0.1f, 0.13f, 1.0f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

            // Border
            colors[(int)ImGuiCol.Border] = new Vector4(0.44f, 0.37f, 0.61f, 0.50f);
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.24f);

            // Text
            colors[(int)ImGuiCol.Text] = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
            colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

            // Headers
            colors[(int)ImGuiCol.Header] = new Vector4(0.13f, 0.13f, 0.17f, 1.0f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.19f, 0.2f, 0.25f, 1.0f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

            // Buttons
            colors[(int)ImGuiCol.Button] = new Vector4(0.13f, 0.13f, 0.17f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.19f, 0.2f, 0.25f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
            colors[(int)ImGuiCol.CheckMark] = new Vector4(0.74f, 0.58f, 0.98f, 1.0f);

            // Popups
            colors[(int)ImGuiCol.PopupBg] = new Vector4(0.1f, 0.1f, 0.13f, 0.92f);

            // Slider
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.44f, 0.37f, 0.61f, 0.54f);
            colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.74f, 0.58f, 0.98f, 0.54f);

            // Frame BG
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.13f, 0.13f, 0.17f, 1.0f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.19f, 0.2f, 0.25f, 1.0f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

            // Tabs
            colors[(int)ImGuiCol.Tab] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(0.24f, 0.24f, 0.32f, 1.0f);
            colors[(int)ImGuiCol.TabSelected] = new Vector4(0.26f, 0.26f, 0.33f, 1.0f);

            // Title
            colors[(int)ImGuiCol.TitleBg] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);

            // Scrollbar
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.1f, 0.1f, 0.13f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.16f, 0.16f, 0.21f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.19f, 0.2f, 0.25f, 1.0f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.24f, 0.24f, 0.32f, 1.0f);

            // Separator
            colors[(int)ImGuiCol.Separator] = new Vector4(0.44f, 0.37f, 0.61f, 1.0f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.74f, 0.58f, 0.98f, 1.0f);
            colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.84f, 0.58f, 1.0f, 1.0f);

            // Resize Grip
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.44f, 0.37f, 0.61f, 0f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.74f, 0.58f, 0.98f, 0f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.84f, 0.58f, 1.0f, 0f);

            // Docking
            colors[(int)ImGuiCol.DockingPreview] = new Vector4(0.44f, 0.37f, 0.61f, 1.0f);

            // Setting style
            var style = ImGui.GetStyle();
            style.TabRounding = 4;
            style.ScrollbarRounding = 9;
            style.WindowRounding = 7;
            style.GrabRounding = 3;
            style.FrameRounding = 3;
            style.PopupRounding = 4;
            style.ChildRounding = 4;
        }

        // Update entities
        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }

        // Update local player
        public void UpdateLocalPlayer(Entity newentity)
        {
            lock (entitylock)
            {
                localplayer = newentity;
            }
        }

        // Get local player
        public Entity GetLocalPlayer()
        {
            lock (entitylock)
            {
                return localplayer;
            }
        }

        // Draw the overlay
        void DrawOverlay(Vector2 screenSize)
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.Begin("Overlay", ImGuiWindowFlags.NoDecoration
              | ImGuiWindowFlags.NoBackground
              | ImGuiWindowFlags.NoBringToFrontOnFocus
              | ImGuiWindowFlags.NoMove
              | ImGuiWindowFlags.NoInputs
              | ImGuiWindowFlags.NoCollapse
              | ImGuiWindowFlags.NoScrollbar
              | ImGuiWindowFlags.NoScrollWithMouse
            );
        }

        // Apply custom styling to ImGui
        void ApplyCustomStyle()
        {

            var style = ImGui.GetStyle();
            style.WindowRounding = 5.0f;
            style.FrameRounding = 5.0f;
            style.GrabRounding = 5.0f;

            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = new Vector4(0.1f, 0.1f, 0.1f, 0.9f);
            colors[(int)ImGuiCol.FrameBg] = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            colors[(int)ImGuiCol.Button] = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.8f, 0.1f, 0.1f, 1.0f);
        }

        // Tooltip helper function
        void ShowTooltip(string description)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(description);
                ImGui.EndTooltip();
            }
        }
    }
}
