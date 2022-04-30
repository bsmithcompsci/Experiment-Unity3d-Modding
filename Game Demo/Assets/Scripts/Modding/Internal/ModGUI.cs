using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class ModGUI : MonoBehaviour
{
    Dictionary<ModInfo, bool> nextState = new Dictionary<ModInfo, bool>();
    Dictionary<ModInfo, List<IResourceLocation>> scenes = new Dictionary<ModInfo, List<IResourceLocation>>();

    private void Start()
    {
        
    }

    private void OnGUI()
    {
        var indentLevel = 0;

        if (GUILayout.Button("Open Mods Folder"))
        {
            Application.OpenURL($"file://{ModManager.ModPath}");
        }

        GUILayout.Label(ModManager.ModPath);

        GUILayout.BeginVertical();
        foreach (var mod in ModManager.Instance.Mods)
        {
            if (!nextState.ContainsKey(mod))
                nextState.Add(mod, mod.loaded);

            GUILayout.BeginHorizontal();

            var previousValue = nextState[mod];
            nextState[mod] = GUILayout.Toggle(nextState[mod], mod.modName + " [" + (mod.loaded ? "Loaded" : "Unloaded") + "]");
            if (nextState[mod] != previousValue)
            {
                Debug.Log(mod.modName);
                if (mod.loaded)
                {
                    mod.Unload();
                    scenes[mod].Clear();
                }
                else
                {
                    mod.Load(() =>
                    {
                        Debug.Log("Loaded Mod and populating scene elements");
                        mod.GetAllAssetsOfLabel<SceneInstance>("Scene", (IResourceLocation _location) =>
                        {
                            if (_location.ResourceType == typeof(SceneInstance))
                            {
                                Debug.Log($"Scene: {_location.PrimaryKey} {mod.modName}");
                                if (!scenes.ContainsKey(mod))
                                    scenes.Add(mod, new List<IResourceLocation>());

                                scenes[mod].Add(_location);
                            }
                            else if (_location.ResourceType == typeof(SceneInstance))
                            {

                            }
                        });
                    });
                }
            }

            if (mod.loaded)
            {
                GUILayout.Space(indentLevel * 20);

                if (scenes.ContainsKey(mod))
                    foreach (var instance in scenes[mod])
                    {
                        if (GUILayout.Button(instance.PrimaryKey))
                        {
                            Addressables.LoadSceneAsync(instance, LoadSceneMode.Single);
                        }
                    }
            }

            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }
}
