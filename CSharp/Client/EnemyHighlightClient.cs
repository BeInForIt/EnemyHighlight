using System;
using Barotrauma;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EnemyHighlightMod
{
    internal sealed class EnemyHighlightClient : IAssemblyPlugin, IDisposable
    {
        internal static EnemyHighlightClient Instance { get; private set; }

        private Harmony harmony;
        private double nextDebugLogTime;
        private bool espEnabled = false;
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
            var keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F6))
            {
                if (!toggleKeyPressed)
                {
                    espEnabled = !espEnabled;
                    LuaCsLogger.Log("[EnemyHighlight] ESP toggled: " + espEnabled);
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
            if (!(Screen.Selected is GameScreen)) { return; }

            Camera cam = screen.Cam;
            if (cam == null) { return; }

            Character controlled = Character.Controlled;
            if (controlled == null) { return; }

            HandleToggle();

            if (!espEnabled) { return; }

            int highlightedCount = 0;
            int drawnCount = 0;

            foreach (Character character in Character.CharacterList)
            {
                if (!ShouldHighlight(character)) { continue; }
                highlightedCount++;

                if (TryDrawBox(spriteBatch, cam, controlled, character))
                {
                    drawnCount++;
                }
            }

            DrawDebugMarker(spriteBatch, highlightedCount, drawnCount);

            if (Timing.TotalTime >= nextDebugLogTime)
            {
                nextDebugLogTime = Timing.TotalTime + 2.0;
                LuaCsLogger.Log(
                    "[EnemyHighlight] Draw pass active. Highlighted characters: " +
                    highlightedCount +
                    ", drawn markers: " +
                    drawnCount
                );
            }
        }

        private static bool TryDrawBox(SpriteBatch spriteBatch, Camera cam, Character controlled, Character character)
        {
            if (spriteBatch == null) { return false; }
            if (cam == null) { return false; }
            if (controlled == null) { return false; }
            if (character == null) { return false; }

            Vector2 worldPos = character.WorldPosition;
            Vector2 screenPos = cam.WorldToScreen(worldPos);

            if (float.IsNaN(screenPos.X) || float.IsNaN(screenPos.Y)) { return false; }
            if (float.IsInfinity(screenPos.X) || float.IsInfinity(screenPos.Y)) { return false; }

            float dist = Vector2.Distance(controlled.WorldPosition, character.WorldPosition);
            if (dist <= 0.01f) { dist = 0.01f; }

            float size = MathHelper.Clamp(7000f / dist, 12f, 40f);

            int w = (int)size;
            int h = (int)(size * 1.6f);

            Rectangle rect = new Rectangle(
                (int)screenPos.X - w / 2,
                (int)screenPos.Y - h / 2,
                w,
                h
            );

            Color color = GetIndicatorColor(character);

            GUI.DrawRectangle(spriteBatch, rect, color, false, 0f, 2f);

            return true;
        }

        private static void DrawDebugMarker(SpriteBatch spriteBatch, int highlightedCount, int drawnCount)
        {
            Color markerColor;

            if (highlightedCount > 0 && drawnCount > 0)
            {
                markerColor = new Color(0, 255, 0, 220);
            }
            else if (highlightedCount > 0)
            {
                markerColor = new Color(255, 0, 255, 220);
            }
            else
            {
                markerColor = new Color(255, 255, 0, 220);
            }

            GUI.DrawRectangle(
                spriteBatch,
                new Rectangle(20, 20, 140, 40),
                markerColor,
                true,
                0f,
                1f
            );
        }

        private static bool ShouldHighlight(Character character)
        {
            if (character == null) { return false; }
            if (character.Removed) { return false; }
            if (character.IsDead) { return false; }

            return true;
        }

        private static Color GetIndicatorColor(Character character)
        {
            if (character == null) { return Color.Red * 0.85f; }

            return character.IsHuman
                ? Color.Cyan * 0.85f
                : Color.Red * 0.85f;
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
