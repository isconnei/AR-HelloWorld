using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TalkingAvatar.EditorTools
{
    /// <summary>
    /// Crea el asset de ajustes si falta y ofrece una captura de vista previa del avatar.
    /// </summary>
    [InitializeOnLoad]
    static class TalkingAvatarEditorTools
    {
        const string ResourcesFolder = "Assets/TalkingAvatar/Resources";
        const string SettingsPath = ResourcesFolder + "/" + AvatarChatSettings.ResourcePath + ".asset";

        static TalkingAvatarEditorTools() => EditorApplication.delayCall += () => EnsureSettingsAsset();

        static AvatarChatSettings EnsureSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AvatarChatSettings>(SettingsPath);
            if (settings != null)
                return settings;
            Directory.CreateDirectory(ResourcesFolder);
            settings = ScriptableObject.CreateInstance<AvatarChatSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        [MenuItem("Tools/Talking Avatar/Ajustes del avatar")]
        static void SelectSettings()
        {
            Selection.activeObject = EnsureSettingsAsset();
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        [MenuItem("Tools/Talking Avatar/Guardar vista previa (PNG)")]
        static void CapturePreviewMenu()
        {
            var path = EditorUtility.SaveFilePanel("Vista previa del avatar", "", "AvatarPreview.png", "png");
            if (!string.IsNullOrEmpty(path) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                CapturePreview(path);
        }

        /// <summary>Para línea de comandos: -executeMethod TalkingAvatar.EditorTools.TalkingAvatarEditorTools.CapturePreviewBatch -previewOut ruta.png</summary>
        public static void CapturePreviewBatch()
        {
            EnsureSettingsAsset();
            var args = Environment.GetCommandLineArgs();
            var output = "AvatarPreview.png";
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-previewOut")
                    output = args[i + 1];
            }
            CapturePreview(output);
        }

        static void CapturePreview(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var settings = AvatarChatSettings.Load();

            var avatar = new GameObject(settings.avatarName).AddComponent<HumanAvatar>();
            avatar.PoseForPreview(waving: true, mouthOpen: 0.6f);
            var bubble = SpeechBubble.Create(avatar.transform, HumanAvatar.BubbleAnchor);
            bubble.SayInstant($"¡Hola! Soy {settings.avatarName}. Qué gusto conocerte. ¿Cómo te llamas?");

            var cam = Camera.main;
            cam.transform.position = new Vector3(0.7f, 1.5f, 2.6f);
            cam.transform.LookAt(new Vector3(0f, 1.3f, 0f));
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.78f, 0.82f, 0.86f);
            cam.fieldOfView = 50f;
            bubble.FaceCamera(cam);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)bubble.transform);

            const int width = 900, height = 1200;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

            // En una captura de un solo frame, el SRP Batcher puede no subir aún los colores de los materiales
            // recién creados; lo desactivamos solo durante la captura.
            var srpBatching = GraphicsSettings.useScriptableRenderPipelineBatching;
            GraphicsSettings.useScriptableRenderPipelineBatching = false;
            try
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    var request = new RenderPipeline.StandardRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(cam, request))
                    {
                        RenderPipeline.SubmitRenderRequest(cam, request);
                    }
                    else
                    {
                        cam.targetTexture = rt;
                        cam.Render();
                        cam.targetTexture = null;
                    }
                }
            }
            finally
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = srpBatching;
            }

            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            rt.Release();
            Debug.Log($"[TalkingAvatar] Vista previa guardada en {Path.GetFullPath(outputPath)}");
        }
    }
}
