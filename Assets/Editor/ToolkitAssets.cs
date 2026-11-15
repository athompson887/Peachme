// The HUD's PanelSettings asset, made and kept right in code (same pattern as
// FlightSim2026's ToolkitAssets): the fields that matter are written here next
// to why, and put back if anyone changes them in the Inspector.
//
// Runs after every compile; writes only when something is missing or wrong.

using PeachMe.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PeachMe.EditorTools
{
    [InitializeOnLoad]
    internal static class ToolkitAssets
    {
        private const string ThemePath = "Assets/UI/PeachTheme.tss";

        static ToolkitAssets()
        {
            EditorApplication.delayCall += () => Ensure(false);
        }

        [MenuItem("PeachMe/Make UI Panel")]
        private static void FromMenu()
        {
            Ensure(true);
        }

        private static void Ensure(bool said)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling
                || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += () => Ensure(said);
                return;
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                // A warning, not an error: an error in the log pauses play.
                Debug.LogWarning($"[Toolkit] {ThemePath} has not been imported - the panel is left as it is.");
                return;
            }

            string path = RunHud.PanelAssetPath;
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            bool made = false;
            if (panel == null)
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, path);
                made = true;
            }

            bool changed = made;
            if (panel.themeStyleSheet != theme) { panel.themeStyleSheet = theme; changed = true; }

            // Portrait phone: lay out against 1080x1920 and scale to the device,
            // matching width and height equally so tall and squat phones both read.
            if (panel.scaleMode != PanelScaleMode.ScaleWithScreenSize) { panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; changed = true; }
            if (panel.referenceResolution != new Vector2Int(1080, 1920)) { panel.referenceResolution = new Vector2Int(1080, 1920); changed = true; }
            if (!Mathf.Approximately(panel.match, 0.5f)) { panel.match = 0.5f; changed = true; }

            if (changed)
            {
                EditorUtility.SetDirty(panel);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Toolkit] {(made ? "made" : "put right")} {path} with the theme {ThemePath}");
            }
            else if (said)
            {
                Debug.Log($"[Toolkit] {path} is already right");
            }
        }
    }
}
