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

        /// <summary>
        /// Para línea de comandos: -executeMethod TalkingAvatar.EditorTools.TalkingAvatarEditorTools.CapturePreviewBatch
        /// -previewOut ruta.png [-previewKind Kingfisher|Human]
        /// </summary>
        public static void CapturePreviewBatch()
        {
            EnsureSettingsAsset();
            var args = Environment.GetCommandLineArgs();
            var output = "AvatarPreview.png";
            AvatarKind? kind = null;
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-previewOut")
                    output = args[i + 1];
                else if (args[i] == "-previewKind" && Enum.TryParse(args[i + 1], true, out AvatarKind parsed))
                    kind = parsed;
            }
            CapturePreview(output, kind);
        }

        static void CapturePreview(string outputPath, AvatarKind? kind = null)
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Copia de los ajustes para poder elegir otro avatar sin modificar el asset.
            var settings = UnityEngine.Object.Instantiate(AvatarChatSettings.Load());
            if (kind.HasValue)
                settings.avatarKind = kind.Value;

            var avatar = TalkingAvatarController.CreateAvatar(settings, null);
            avatar.PoseForPreview(greeting: true, mouthOpen: 0.6f);
            var bubble = SpeechBubble.Create(avatar);
            bubble.SayInstant($"¡Hola! Soy {settings.CharacterName}. Qué gusto conocerte. ¿Cómo te llamas?");

            // Encuadre automático: el avatar completo más la burbuja.
            var bounds = new Bounds(avatar.transform.position + avatar.BubbleAnchor, Vector3.zero);
            foreach (var r in avatar.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(r.bounds);
            bounds.Encapsulate(avatar.transform.position + avatar.BubbleAnchor + Vector3.up * (0.12f * avatar.BubbleScale));

            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.78f, 0.82f, 0.86f);
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.01f;
            var distance = bounds.extents.magnitude / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.9f;
            cam.transform.position = bounds.center + new Vector3(0.3f, 0.05f, 1f).normalized * distance;
            cam.transform.LookAt(bounds.center);

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
