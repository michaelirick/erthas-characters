using UnityEngine;
using UnityEditor;


namespace Bozo.ModularCharacters
{
[InitializeOnLoad]
    public static class CharacterProjectIcons
    {
        static CharacterProjectIcons()
        {
            EditorApplication.projectWindowItemOnGUI += DrawIcon;
        }

        private static void DrawIcon(string guid, Rect rect)
        {
            // Only replace icons in list view
            if (rect.height > 20)
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);

            CharacterObject character =
                AssetDatabase.LoadAssetAtPath<CharacterObject>(path);

            if (character == null || character.icon == null)
                return;

            Rect iconRect = new Rect(
                rect.x,
                rect.y,
                16,
                16
            );

            GUI.DrawTexture(
                iconRect,
                character.icon,
                ScaleMode.ScaleToFit
            );
        }
    }
}