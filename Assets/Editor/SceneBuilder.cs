// Builds Assets/Scenes/Run.unity in code, so the scene is never hand-edited
// YAML and can always be rebuilt from scratch: PeachMe > Build Run Scene.
// Also puts it first in the build settings.

using PeachMe.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace PeachMe.EditorTools
{
    internal static class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Run.unity";

        [MenuItem("PeachMe/Build Run Scene")]
        private static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return; // user cancelled

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ChaseCamera>();
            camGo.transform.position = new Vector3(0f, 4.2f, -6.5f);

            // Sun
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.None; // grey-box: cheap on phones
            sunGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            // The run: driver, input, view, HUD, voice.
            var run = new GameObject("Run");
            run.AddComponent<RunInput>();
            run.AddComponent<RunDriver>();
            run.AddComponent<RunView>();
            run.AddComponent<RunHud>();
            run.AddComponent<AudioSource>();
            run.AddComponent<Commentary>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[PeachMe] built {ScenePath} and made it the first build scene");
        }
    }
}
