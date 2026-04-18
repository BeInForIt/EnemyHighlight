using System;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EnemyHighlightMod
{
    internal sealed class EnemyHighlightClient : IAssemblyPlugin, IDisposable
    {
        private readonly struct CharacterScreenData
        {
            public CharacterScreenData(Vector2 worldAnchor, Vector2 screenBase, Vector2 screenTop, Rectangle boxRect, float distance, float health01, string label, bool isOnScreen)
            {
                WorldAnchor = worldAnchor;
                ScreenBase = screenBase;
                ScreenTop = screenTop;
                BoxRect = boxRect;
                Distance = distance;
                Health01 = health01;
                Label = label;
                IsOnScreen = isOnScreen;
            }

            public Vector2 WorldAnchor { get; }
            public Vector2 ScreenBase { get; }
            public Vector2 ScreenTop { get; }
            public Rectangle BoxRect { get; }
            public float Distance { get; }
            public float Health01 { get; }
            public string Label { get; }
            public bool IsOnScreen { get; }
        }

        internal static EnemyHighlightClient Instance { get; private set; }

        private Harmony harmony;
        private bool espEnabled;
        private bool toggleKeyPressed;

        public void Initialize()
        {
            Instance = this;
            harmony = new Harmony("enemyhighlight.mod");
            harmony.PatchAll();
            LuaCsLogger.Log("[EnemyHighlight] Initialized.");
        }

        public void OnLoadCompleted()
        {
            LuaCsLogger.Log("[EnemyHighlight] Load completed.");
        }

        public void PreInitPatching()
        {
        }

        public void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
                harmony = null;
            }

            Instance = null;
            LuaCsLogger.Log("[EnemyHighlight] Disposed.");
        }

        private void HandleToggle()
        {
            KeyboardState keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.F6))
            {
                if (!toggleKeyPressed)
                {
                    espEnabled = !espEnabled;
                    GUI.AddMessage("Enemy Highlight: " + (espEnabled ? "ON" : "OFF"), GUIStyle.Green, lifeTime: 2.0f, playSound: false, font: GUIStyle.Font);
                }

                toggleKeyPressed = true;
            }
            else
            {
                toggleKeyPressed = false;
            }
        }

        internal void DrawEnemyIndicators(GameScreen screen, SpriteBatch spriteBatch)
        {
            if (screen == null) { return; }
            if (spriteBatch == null) { return; }
            if (GUI.DisableHUD) { return; }
            if (Screen.Selected is not GameScreen) { return; }

            Camera cam = screen.Cam;
            Character controlled = Character.Controlled;

            HandleToggle();

            if (!espEnabled) { return; }
            if (cam == null) { return; }
            if (controlled == null) { return; }

            foreach (Character character in Character.CharacterList)
            {
                if (!ShouldHighlight(controlled, character)) { continue; }
                TryDrawTargetVisuals(spriteBatch, cam, controlled, character);
            }
        }

        private static bool TryDrawTargetVisuals(SpriteBatch spriteBatch, Camera cam, Character controlled, Character character)
        {
            if (spriteBatch == null) { return false; }
            if (cam == null) { return false; }
            if (controlled == null) { return false; }
            if (character == null) { return false; }
            if (!TryGetCharacterScreenData(cam, controlled, character, out CharacterScreenData data)) { return false; }

            Color color = GetIndicatorColor(character);

            if (!data.IsOnScreen)
            {
                DrawOffScreenIndicator(spriteBatch, cam, data, color);
                return true;
            }

            DrawCornerBox(spriteBatch, data.BoxRect, color);
            DrawHealthBar(spriteBatch, data.BoxRect, data.Health01, color);
            DrawLabel(spriteBatch, data.Label, new Vector2(data.ScreenTop.X, data.BoxRect.Top - 28), color, GUIStyle.Font, 4);
            DrawLabel(spriteBatch, ((int)MathF.Round(data.Distance)).ToString(), new Vector2(data.ScreenBase.X, data.BoxRect.Bottom + 10), Color.White, GUIStyle.SmallFont, 3);

            return true;
        }

        private static bool TryGetCharacterScreenData(Camera cam, Character controlled, Character character, out CharacterScreenData data)
        {
            data = default;

            if (cam == null) { return false; }
            if (controlled == null) { return false; }
            if (character == null) { return false; }

            Vector2 baseWorld = character.DrawPosition;
            if (baseWorld == Vector2.Zero) { baseWorld = character.WorldPosition; }

            Vector2 topWorld = character.AnimController?.GetLimb(LimbType.Head)?.body?.DrawPosition ?? (baseWorld + Vector2.UnitY * 100.0f);
            Vector2 screenBase = cam.WorldToScreen(baseWorld);
            Vector2 screenTop = cam.WorldToScreen(topWorld);

            if (!IsFinite(screenBase) || !IsFinite(screenTop)) { return false; }

            float verticalSpan = Math.Abs(screenBase.Y - screenTop.Y);
            if (verticalSpan < 12.0f)
            {
                float distance = Vector2.Distance(controlled.WorldPosition, character.WorldPosition);
                verticalSpan = MathHelper.Clamp(7000.0f / Math.Max(distance, 1.0f), 14.0f, 44.0f);
            }

            float height = MathHelper.Clamp(verticalSpan * 2.4f, 32.0f, 180.0f);
            float width = MathHelper.Clamp(height * 0.62f, 18.0f, 120.0f);

            Rectangle boxRect = new Rectangle(
                (int)MathF.Round(screenBase.X - width * 0.5f),
                (int)MathF.Round(screenBase.Y - height * 0.5f),
                Math.Max(1, (int)MathF.Round(width)),
                Math.Max(1, (int)MathF.Round(height))
            );

            float distanceValue = Vector2.Distance(controlled.WorldPosition, character.WorldPosition);
            float health01 = GetHealth01(character);
            string label = !string.IsNullOrWhiteSpace(character.Info?.DisplayName) ? character.Info.DisplayName : character.Name;
            bool isOnScreen =
                screenBase.X >= 0.0f &&
                screenBase.X <= GameMain.GraphicsWidth &&
                screenBase.Y >= 0.0f &&
                screenBase.Y <= GameMain.GraphicsHeight;

            data = new CharacterScreenData(baseWorld, screenBase, screenTop, boxRect, distanceValue, health01, label, isOnScreen);
            return true;
        }

        private static void DrawCornerBox(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            if (rect.Width <= 0 || rect.Height <= 0) { return; }

            float thickness = 2.0f;
            float cornerWidth = Math.Max(6.0f, rect.Width * 0.3f);
            float cornerHeight = Math.Max(6.0f, rect.Height * 0.22f);

            Vector2 topLeft = new Vector2(rect.Left, rect.Top);
            Vector2 topRight = new Vector2(rect.Right, rect.Top);
            Vector2 bottomLeft = new Vector2(rect.Left, rect.Bottom);
            Vector2 bottomRight = new Vector2(rect.Right, rect.Bottom);

            GUI.DrawLine(spriteBatch, topLeft, topLeft + Vector2.UnitX * cornerWidth, color, width: thickness);
            GUI.DrawLine(spriteBatch, topLeft, topLeft + Vector2.UnitY * cornerHeight, color, width: thickness);

            GUI.DrawLine(spriteBatch, topRight, topRight - Vector2.UnitX * cornerWidth, color, width: thickness);
            GUI.DrawLine(spriteBatch, topRight, topRight + Vector2.UnitY * cornerHeight, color, width: thickness);

            GUI.DrawLine(spriteBatch, bottomLeft, bottomLeft + Vector2.UnitX * cornerWidth, color, width: thickness);
            GUI.DrawLine(spriteBatch, bottomLeft, bottomLeft - Vector2.UnitY * cornerHeight, color, width: thickness);

            GUI.DrawLine(spriteBatch, bottomRight, bottomRight - Vector2.UnitX * cornerWidth, color, width: thickness);
            GUI.DrawLine(spriteBatch, bottomRight, bottomRight - Vector2.UnitY * cornerHeight, color, width: thickness);
        }

        private static void DrawHealthBar(SpriteBatch spriteBatch, Rectangle boxRect, float health01, Color color)
        {
            int barWidth = Math.Max(28, boxRect.Width + 14);
            int barHeight = 7;
            int x = boxRect.Center.X - barWidth / 2;
            int y = boxRect.Top - 12;

            Rectangle backgroundRect = new Rectangle(x, y, barWidth, barHeight);
            Rectangle fillRect = new Rectangle(x + 1, y + 1, Math.Max(0, (int)MathF.Round((barWidth - 2) * health01)), Math.Max(1, barHeight - 2));
            Color fillColor = Color.Lerp(GUIStyle.Red, GUIStyle.Green, health01);

            GUI.DrawRectangle(spriteBatch, backgroundRect, Color.Black * 0.75f, true);
            GUI.DrawRectangle(spriteBatch, backgroundRect, color * 0.9f, false, 0.0f, 1.0f);
            if (fillRect.Width > 0)
            {
                GUI.DrawRectangle(spriteBatch, fillRect, fillColor * 0.95f, true);
            }

            DrawLabel(spriteBatch, ((int)MathF.Round(health01 * 100.0f)).ToString() + "%", new Vector2(boxRect.Center.X, y - 10), Color.White, GUIStyle.SmallFont, 2);
        }

        private static void DrawLabel(SpriteBatch spriteBatch, string text, Vector2 centerPosition, Color color, GUIFont font, int backgroundPadding)
        {
            if (spriteBatch == null) { return; }
            if (font == null) { font = GUIStyle.Font; }
            if (string.IsNullOrWhiteSpace(text)) { return; }

            Vector2 size = font.MeasureString(text);
            Vector2 drawPosition = new Vector2(
                (float)Math.Floor(centerPosition.X - size.X * 0.5f),
                (float)Math.Floor(centerPosition.Y - size.Y * 0.5f)
            );

            GUI.DrawString(spriteBatch, drawPosition, text, color, Color.Black * 0.65f, backgroundPadding, font);
        }

        private static void DrawOffScreenIndicator(SpriteBatch spriteBatch, Camera cam, CharacterScreenData data, Color color)
        {
            Sprite sprite = GUIStyle.EnemyIcon.Value?.Sprite ?? GUI.Arrow;
            if (sprite == null) { return; }

            GUI.DrawIndicator(spriteBatch, data.WorldAnchor, cam, 0.0f, sprite, color, createOffset: true, scaleMultiplier: 0.75f, overrideAlpha: 0.95f);
        }

        private static bool ShouldHighlight(Character controlled, Character character)
        {
            if (character == null) { return false; }
            if (controlled == null) { return false; }
            if (character.Removed) { return false; }
            if (character.IsDead) { return false; }
            if (character == controlled) { return false; }

            if (!character.IsHuman) { return true; }

            return character.TeamID != controlled.TeamID;
        }

        private static Color GetIndicatorColor(Character character)
        {
            if (character == null) { return Color.Red; }

            return character.IsHuman
                ? new Color(255, 196, 96, 235)
                : new Color(255, 84, 84, 235);
        }

        private static float GetHealth01(Character character)
        {
            if (character == null) { return 0.0f; }

            float maxVitality = Math.Max(character.MaxVitality, 0.0f);
            if (maxVitality <= 0.0f) { return 0.0f; }

            float vitality = character.CharacterHealth != null ? character.CharacterHealth.DisplayedVitality : character.Vitality;
            if (float.IsNaN(vitality) || float.IsInfinity(vitality))
            {
                vitality = character.Vitality;
            }

            return MathHelper.Clamp(vitality / maxVitality, 0.0f, 1.0f);
        }

        private static bool IsFinite(Vector2 value)
        {
            return
                !float.IsNaN(value.X) &&
                !float.IsNaN(value.Y) &&
                !float.IsInfinity(value.X) &&
                !float.IsInfinity(value.Y);
        }
    }

    [HarmonyPatch(typeof(GameScreen), "Draw")]
    internal static class GameScreenDrawPatch
    {
        private static void Postfix(GameScreen __instance, double deltaTime, GraphicsDevice graphics, SpriteBatch spriteBatch)
        {
            if (EnemyHighlightClient.Instance == null) { return; }
            if (spriteBatch == null) { return; }

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                GUI.SamplerState,
                null,
                GameMain.ScissorTestEnable,
                null,
                null
            );

            EnemyHighlightClient.Instance.DrawEnemyIndicators(__instance, spriteBatch);

            spriteBatch.End();
        }
    }
}
