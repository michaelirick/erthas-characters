using UnityEditor;
using UnityEngine;

namespace Bozo.ModularCharacters
{
    [CustomEditor(typeof(CharacterObject))]
    public class CharacterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CharacterObject myObj = (CharacterObject)target;
            serializedObject.Update();
            float size = 128f;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));

            if (myObj.icon != null)
            {

                // Define a clean Unity-style frame using GUIStyle
                GUIStyle frameStyle = new GUIStyle(GUI.skin.box);

                frameStyle.padding = new RectOffset(10, 10, 10, 10);
                frameStyle.margin = new RectOffset(5, 5, 5, 5);
                frameStyle.margin = new RectOffset(5, 5, 5, 5);

                GUILayout.BeginHorizontal(frameStyle);
                Rect previewRect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(true));
                EditorGUI.DrawPreviewTexture(previewRect, myObj.icon, null, ScaleMode.ScaleToFit);
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("data"));
            serializedObject.ApplyModifiedProperties();
        }

        public override Texture2D RenderStaticPreview(
        string assetPath,
        Object[] subAssets,
        int width,
        int height)
        {
            CharacterObject character = (CharacterObject)target;

            if (character.icon == null)
                return base.RenderStaticPreview(
                    assetPath, subAssets, width, height);

            Texture2D preview = new Texture2D(
                width, height, TextureFormat.RGBA32, false);

            RenderTexture rt = RenderTexture.GetTemporary(
                width, height, 0, RenderTextureFormat.ARGB32);

            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(character.icon, rt);

                RenderTexture.active = rt;

                preview.ReadPixels(
                    new Rect(0, 0, width, height),
                    0, 0);

                preview.Apply();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }

            return preview;
        }
    }
}
