using System;
using UnityEditor;
using UnityEngine.Assertions;

namespace ModSDK.editor
{
    /// <summary>
    /// Mod Settings is a class that handle any type of settings that unity doesn't normally give access to.
    /// </summary>
    public static class ModSettings
    {
        public static void SetupProjectSettings()
        {
            // Thank you for pointing this out! https://bladecast.pro/unity-tutorial/create-tags-by-script 
            var asset = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset");
            Assert.IsNotNull(asset, "TagManager is not initialized, possible Unity3d Corruption!");

            if (asset == null)
                return;

            var sObj = new SerializedObject(asset);
            var tagsProperty = sObj.FindProperty("tags");
            var layersProperty = sObj.FindProperty("layers");

            // Handle Tags
            tagsProperty.ClearArray();
            foreach (var tag in Enum.GetNames(typeof(ModObjectTags)))
            {
                tagsProperty.InsertArrayElementAtIndex(0);
                tagsProperty.GetArrayElementAtIndex(0).stringValue = tag;
            }

            // Handle Layers
            string[] layers = new string[32];
            string[] unityLayers = new string[]
            {
                "Default",
                "TransparentFX",
                "Ignore Raycast",
                "_UNUSED",
                "Water",
                "UI"
            };
            string[] userLayers = Enum.GetNames(typeof(ModPhysicLayers));

            layersProperty.ClearArray();
            for (int i = 0; i < layers.Length; i++)
            {
                if (i < unityLayers.Length)
                    layers[i] = unityLayers[i];
                else if (i - unityLayers.Length < userLayers.Length)
                    layers[i] = userLayers[i - unityLayers.Length];

                layersProperty.InsertArrayElementAtIndex(i);
                layersProperty.GetArrayElementAtIndex(i).stringValue = layers[i];
            }

            sObj.ApplyModifiedPropertiesWithoutUndo();
            sObj.Update();
        }
    }
}
