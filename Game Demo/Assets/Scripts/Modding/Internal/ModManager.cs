using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class ModManager : MonoBehaviour
{
    private static ModManager instance;
    public static ModManager Instance 
    {
        get 
        {
            Assert.IsNotNull(instance, "Mod Manager was requested, while the mod manager wasn't loaded into this scene.");
            return instance;
        }
    }

    public static string ModPath
    {
        get
        {
            return Path.Combine(Application.dataPath, "Resources", "Mods").Replace("\\", "/");
        }
    }

    public UnityEvent onReady;

    public UnityEvent<ModInfo> onLoadedMod;
    public UnityEvent<ModInfo> onUnloadedMod;
    public UnityEvent<ModInfo, Exception> onErrorMod;

    public UnityEvent onDoneLoading;

    private Dictionary<string, IResourceLocation> resourceLocations = new Dictionary<string, IResourceLocation>();
    private List<AsyncOperationHandle> loadedGameObjects = new List<AsyncOperationHandle>();

    private Dictionary<string, ModInfo> modDictionary = new Dictionary<string, ModInfo>();
    private List<ModInfo> loadingMods = new List<ModInfo>();
    private List<ModInfo> mods = new List<ModInfo>();

    private Dictionary<Type, Type> internalMonoBehaviourConversion;

    public bool IsLoading { get { return loadingMods.Count > 0; } }

    [RuntimeInitializeOnLoadMethod]
    static void InitializeManager()
    {
        if (instance == null)
        {
            GameObject gameobject = new GameObject();
            instance = gameobject.AddComponent<ModManager>();
            gameobject.name = "Global - Mod Manager";
        }
    }

    public ModInfo[] Mods
    {
        get
        {
            return mods.ToArray();
        }
    }

    void Awake()
    {
        if (instance != null)
            GameObject.Destroy(instance);

        internalMonoBehaviourConversion = new Dictionary<Type, Type>() { };

        // Load the Internal Types.
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        List<Type> internalMonobehaviours = new List<Type>();
        foreach (Assembly assembly in assemblies)
        {
            List<Type> types = assembly.GetTypes().Where(type => (typeof(IInternalMonoBehaviour).IsAssignableFrom(type) && !type.IsAbstract)).ToList();
            internalMonobehaviours.AddRange(types);
        }

        foreach(var type in internalMonobehaviours)
        {
            var genericT = type.BaseType.GetGenericArguments()[0];
            if (genericT != null)
            {
                internalMonoBehaviourConversion.Add(genericT, type);
            }
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        FindMods(ModPath);

        onReady?.Invoke();
    }

    private void Start()
    {
        UpdateScene();
        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
    }

    private void SceneManager_activeSceneChanged(Scene _prev, Scene _next)
    {
        UpdateScene();
    }

    private void UpdateScene()
    {
        // Do this horrible thing of looking up for all components of types.

        foreach (var searchType in internalMonoBehaviourConversion.Keys)
        {
            var lookup = GameObject.FindObjectsOfType(searchType);
            if (lookup != null)
            {
                foreach (var obj in lookup)
                {
                    var behaviour = obj as MonoBehaviour;
                    if (behaviour != null)
                    {
                        behaviour.gameObject.AddComponent(internalMonoBehaviourConversion[searchType]);
                    }
                }
            }
        }
    }


    public void FindMods(string _path)
    {
        DirectoryInfo modDirectory = new DirectoryInfo(_path);

        // Load the sub-directories first.
        foreach (DirectoryInfo dir in modDirectory.GetDirectories())
        {
            FindMods(dir.FullName);
        }

        // Load any mod file that we see.
        List<string> loadedMods = new List<string>(PlayerPrefs.GetString("loadedMods").Split(';'));

        foreach (FileInfo file in modDirectory.GetFiles())
        {
            if (file.Extension == ".json")
            {
                AcknowledgeMod(file, loadedMods.Contains(file.Name));
            }
        }
    }

    internal void LoadMod(ModInfo _modPrototype, Action _callback = null)
    {
        loadingMods.Add(_modPrototype);

        // Find and load.
        AsyncOperationHandle<IResourceLocator> operation = Addressables.LoadContentCatalogAsync(_modPrototype.modAbsolutePath);

        // Events.
        operation.Completed += (AsyncOperationHandle<IResourceLocator> _handle) =>
        {
            Debug.Log($"Completed Loading Mod: {_handle.Result.LocatorId} [{_handle.Result.GetType()}]");
            // Mod is loaded...
            try
            {
                _modPrototype.locator = _handle.Result;
                _modPrototype.loaded = true;
                onLoadedMod?.Invoke(_modPrototype);
            }
            catch (Exception ex)
            {
                onErrorMod?.Invoke(_modPrototype, ex);
            }

            loadingMods.Remove(_modPrototype);
            if (loadingMods.Count != 0)
                onDoneLoading?.Invoke();

            modDictionary.Add(_modPrototype.modName, _modPrototype);

            _callback?.Invoke();
        };
    }

    public void UnloadMod(string _modName)
    {
        if (modDictionary.ContainsKey(_modName))
        {
            ModInfo mod = modDictionary[_modName];
            // Release the Resources...
            if (mod.locator != null)
                Addressables.RemoveResourceLocator(mod.locator);
            modDictionary.Remove(_modName);
            mod.loaded = false;
        }
    }

    public void CleanupModGameObjects(string name)
    {
        foreach (AsyncOperationHandle handle in loadedGameObjects)
        {
            if (handle.Result != null)
                Addressables.ReleaseInstance(handle);
        }
        loadedGameObjects.Clear();
    }

    public AsyncOperationHandle<GameObject>? InstantiateAsync(AssetReference key, Vector3 position, Vector3 direction, Transform parent = null)
    {
        return InstantiateAsync(key, position, Quaternion.LookRotation(direction), parent);
    }
    public AsyncOperationHandle<GameObject>? InstantiateAsync(string key, Vector3 position, Vector3 direction, Transform parent = null)
    {
        return InstantiateAsync(key, position, Quaternion.LookRotation(direction), parent);
    }
    public AsyncOperationHandle<GameObject>? InstantiateAsync(AssetReference key, Vector3 position, Quaternion direction, Transform parent = null)
    {
        var handle = key.InstantiateAsync(position, direction, parent);

        return handle;
    }
    public AsyncOperationHandle<GameObject>? InstantiateAsync(string key, Vector3 position, Quaternion direction, Transform parent = null)
    {
        if (!resourceLocations.ContainsKey(key))
        {
            // Try looking for the asset in the mods.
            var asset = SearchForAsset(key);
            if (asset != null)
            {
                resourceLocations.Add(key, asset);
            }
            else
            {
                Debug.LogError("The object you are looking for doesn't exist: " + key);
                return null;
            }
        }
        InstantiationParameters instParams = new InstantiationParameters(position, direction, parent);
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(resourceLocations[key], instParams, true);

        loadedGameObjects.Add(handle);
        
        return handle;
    }

    public void AcknowledgeMod(FileInfo _file, bool _loadMod)
    {
        string modName = _file.Name;
        modName = modName.Replace(".json", "");
        modName = modName.Replace("_", " ");
        modName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(modName.ToLower());

        ModInfo modPrototype = new ModInfo
        {
            m_manager = this,
            modFile = _file,
            modAbsolutePath = _file.FullName,
            modName = modName,
            locator = null
        };
        mods.Add(modPrototype);
        if (_loadMod)
        {
            LoadMod(modPrototype);
        }
    }

    public IResourceLocation SearchForAsset(string _assetString)
    {
        foreach (var mod in modDictionary.Values)
        {
            var obj = mod.SearchForAsset(_assetString);
            if (obj != null)
                return obj;
        }

        return null;
    }
}

[Serializable]
public class ModInfo : IDisposable
{
    internal ModManager m_manager;
    public FileInfo modFile;
    public string modAbsolutePath;
    public string modName;
    public bool loaded;
    public IResourceLocator locator;

    public void Load(Action _callback)
    {
        m_manager.LoadMod(this, _callback); 
    }
    public void Unload()
    {
        m_manager.UnloadMod(modName);
    }

    /// <summary>
    /// Every Asset Related with the label will be executed here. Each Callback is a single object, scene or script.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_label"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    public void GetAllAssetsOfLabel<T>(string _label, Action<IResourceLocation> _callback)
    {
        Assert.IsNotNull(locator, "Locator is null!");
        locator.Locate(_label, typeof(T), out IList<IResourceLocation> result);
        foreach(var item in result)
        {
            _callback(item);
        }
    }

    public IResourceLocation SearchForAsset(string assetString)
    {
        if (locator.Locate(assetString, typeof(object), out IList<IResourceLocation> locs))
        {
            return locs[0];
        }

        return null;
    }

    public void Dispose()
    {
        Addressables.Release(locator);
    }
}