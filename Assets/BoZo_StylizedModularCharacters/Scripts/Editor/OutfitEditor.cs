using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bozo.ModularCharacters
{
    [CustomEditor(typeof(Outfit))]
    [CanEditMultipleObjects]
    public class OutfitEditor : Editor
    {
        private bool showColors;
        public override void OnInspectorGUI()
        {
            Outfit outfit = (Outfit)target;
            Color originalColor = GUI.color;
            GUIStyle frameStyle = new GUIStyle(GUI.skin.box);

            serializedObject.Update();


            EditorGUILayout.Space(20);

            GUILayout.Label("Character Creator Settings");
            GUILayout.BeginVertical(frameStyle);
            GUILayout.BeginHorizontal(frameStyle);
            GUILayout.BeginVertical(frameStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OutfitName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("PackName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OutfitIcon"));

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ColorChannels"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("material"));
            //EditorGUILayout.PropertyField(serializedObject.FindProperty("materialIndex"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("materialPriority"));
            EditorGUILayout.Space(20);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TextureCatagory"));

            GUILayout.BeginHorizontal(frameStyle);

            DrawBoolProperty(
                serializedObject,
                "supportDecals",
                "Supports Decal"
            );

            DrawBoolProperty(
                serializedObject,
                "supportPatterns",
                "Supports Pattern"
            );

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(frameStyle);

            DrawBoolProperty(
                serializedObject,
                "mergeTexture",
                "Merge Texture"
            );

            DrawBoolProperty(
                serializedObject,
                "mergeMesh",
                "Merge Mesh"
            );

            GUILayout.EndHorizontal();

#if MAGICACLOTH2
            //ExtraMaps
            EditorGUILayout.PropertyField(serializedObject.FindProperty("MC2Map"));
#endif
            GUILayout.BeginHorizontal(frameStyle);

            EditorGUILayout.LabelField(
                "Available In Character Creator",
                GUILayout.Width(200)
            );

            EditorGUILayout.Space(20);

            SerializedProperty showCharacterCreator =
                serializedObject.FindProperty("showCharacterCreator");

            bool hasMixedValues =
                showCharacterCreator.hasMultipleDifferentValues;

            string buttonText;

            if (hasMixedValues)
            {
                buttonText = "(Mixed)";
            }
            else if (showCharacterCreator.boolValue)
            {
                buttonText = "(Available)";
            }
            else
            {
                GUI.color = Color.yellow;
                buttonText = "(Hidden)";
            }

            if (GUILayout.Button(buttonText))
            {
                // When mixed, clicking sets all selected outfits to Available.
                // Otherwise, it toggles the current shared value.
                showCharacterCreator.boolValue =
                    hasMixedValues || !showCharacterCreator.boolValue;
            }

            GUI.color = originalColor;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            if (outfit.OutfitIcon != null)
            {

                // Control the size of the image
                float size = 128;
                Rect rect = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                EditorGUI.DrawTextureTransparent(rect, outfit.OutfitIcon.texture);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space(20);

            GUILayout.Label("Outfit Settings");
            GUILayout.BeginVertical(frameStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("Type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("AttachPoint"));

            GUILayout.BeginHorizontal(frameStyle);

            DrawBoolProperty(
                serializedObject,
                "customShader",
                "Uses Custom Shader"
            );

            DrawBoolProperty(
                serializedObject,
                "AttachInEditMode",
                "Follow Skeleton In Edit Mode"
            );

            GUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUI.indentLevel++;

            showColors = EditorGUILayout.Foldout(showColors, "Color Properties", true);
            if (showColors)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("alpha"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("colors"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultColors"));


                GUILayout.BeginHorizontal(frameStyle);

                GUILayout.Label("Pattern", GUILayout.Width(80));
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("pattern"),
                    GUIContent.none,
                    GUILayout.MinWidth(80)
                );

                GUILayout.Label("Pattern Size", GUILayout.Width(80));
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("patternSize"),
                    GUIContent.none,
                    GUILayout.MinWidth(50)
                );
                GUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("patternColors"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("decalDatas"));

            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("optionalPieces"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("additionalBones"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requiredTags"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("categories"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("LinkedColorSets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("IncompatibleSets"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("HideTypes"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inheritColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inheritedRange"));
            EditorGUI.indentLevel--;

            GUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            //base.OnInspectorGUI();
        }

        private void DrawBoolProperty(
            SerializedObject serializedObject,
            string propertyName,
            string label)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);

            GUIContent labelContent = new GUIContent(label);

            // Make the label only as wide as its text.
            float labelWidth =
                EditorStyles.label.CalcSize(labelContent).x;

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(
                labelContent,
                GUILayout.Width(labelWidth)
            );

            GUILayout.Space(4);

            EditorGUILayout.PropertyField(
                property,
                GUIContent.none,
                GUILayout.Width(18)
            );

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }


}
