using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;
using System.Reflection;
using ModSDK.editor.data;

namespace ModSDK.editor.gui
{
    /// <summary>
    /// The ModTool class is the UI Window in the Editor for configuring and building mods.
    /// </summary>
    public sealed class ModTool : EditorWindow
    {
        private static Version _sdkVersion = null;
        private static Version SDKVersion
        {
            get
            {
                if (_sdkVersion == null)
                {
                    Assembly thisAssembly = typeof(ModTool).Assembly;

                    _sdkVersion = thisAssembly.GetName().Version;
                }

                return _sdkVersion;
            }
        }
        public readonly static List<string> global_asset_labels = new List<string> { "Scene" };

        private const string modToolProfile = "modtools";
        private const string build_script = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
        private const string settings_asset = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
        private const string modsdksettings_asset = "Assets/ModSDK/globalsettings.asset";
        private const bool steamSupport = false;

        private static AddressableAssetSettings m_settings;

        private Dictionary<string, Action> m_tabs = new Dictionary<string, Action>();
        private Action m_action;

        private static ModEditorData m_data;
        private static int m_assetGroupIdx;

        [MenuItem("ModSDK/Show ModTool")]
        static void ShowWindow()
        {
            var window = GetWindow<ModTool>();

            // Initialize Mod Editor Settings.
            GetSettingsObject(settings_asset);
            GetModEditorSettings(modsdksettings_asset);

            // Initialize Tabs for the Editor to render the bodies.
            Action defaultInfo = () =>
            {
                GUILayout.Label("Welcome to the Mod Tool\n" +
                    "----------------------\n" +
                    "This ModTool is built to make it easier for the modder to assemble/build their mods.\n" +
                    $"Version: {SDKVersion}"
                );

                if (GUILayout.Button("Open Wiki"))
                {
                    Application.OpenURL("https://github.com/bsmithcompsci/Experiment-Unity3d-Modding/wiki");
                }
                if (GUILayout.Button("Found a Bug?"))
                {
                    Application.OpenURL("https://github.com/bsmithcompsci/Experiment-Unity3d-Modding/issues");
                }
                if (GUILayout.Button("Like to join my Discord?"))
                {
                    Application.OpenURL("https://discord.gg/tcBa5PCdUE");
                }
            };

            window.m_action = defaultInfo;
            window.m_tabs.Add("Info", defaultInfo);
            if (steamSupport)
            {
#pragma warning disable CS0162 // Unreachable code detected
                window.m_tabs.Add("Steam Account", () =>
                {
                    GUILayout.Label("Not supported yet.");
                });
#pragma warning restore CS0162 // Unreachable code detected
            }
            window.m_tabs.Add("Settings & Build", () =>
            {
                if (!GetSettingsObject(settings_asset))
                {
                    GUILayout.Label("Addressable Asset Settings has not been setup yet.\nPlease navigate to: Window > Asset Management > Addressables > Groups");
                    return;
                }
                if (m_data == null)
                {
                    GUILayout.Label("Error occured while generating the tool's backend data.");
                    return;
                }

                string profileId = "";
                SetProfile(modToolProfile, ref profileId);

            // Name
            m_data.m_name = EditorGUILayout.TextField("Mod Name", m_data.m_name);

            // Version | (+) Build Incrementer
            EditorGUILayout.BeginHorizontal();
                int nextRevision = (DateTime.Now.DayOfYear * 10000) +
                    (DateTime.Now.Hour * 100) +
                    DateTime.Now.Minute;
            //Debug.Log($"{m_data.m_version.Major}, {m_data.m_version.Minor}, {m_data.m_version.Build}, {nextRevision} | {DateTime.Now.DayOfYear} {DateTime.Now.Hour} {DateTime.Now.Minute}");
            m_data.m_version = new Version(m_data.m_version.Major, m_data.m_version.Minor, m_data.m_version.Build, nextRevision);
                string version = EditorGUILayout.TextField("Version", $"v{m_data.m_version}");
                try
                {
                    m_data.m_version = new Version(version.Replace("v", ""));
                }
                catch { }
                if (GUILayout.Button("+"))
                {
                    m_data.m_version = new Version(m_data.m_version.Major, m_data.m_version.Minor, m_data.m_version.Build + 1, m_data.m_version.Revision);
                }
                EditorGUILayout.EndHorizontal();

            // Description
            m_data.m_description = EditorGUILayout.TextField("Description", m_data.m_description);

            // Mask of groups to build.
            m_assetGroupIdx = EditorGUILayout.MaskField("Groups to Build", m_assetGroupIdx, GetGroups());

                if (GUILayout.Button("Build Locally"))
                {
                    Debug.ClearDeveloperConsole();
                    if (Build())
                    {
                        EditorUtility.DisplayDialog("ModTool - Build", "Successful Build", "Okay!");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("ModTool - Build", "Failure to Build - Check Console for errors!", "Okay!");
                    }
                }
                if (steamSupport)
                {
#pragma warning disable CS0162 // Unreachable code detected
                if (!string.IsNullOrEmpty(m_data.m_steamToken))
                    {
                        if (GUILayout.Button("Build & Upload to the Workshop"))
                        {
                            Debug.ClearDeveloperConsole();
                            if (Build())
                            {
                                if (Upload())
                                {
                                    EditorUtility.DisplayDialog("ModTool - Build & Upload", "Successful Build & Uploaded to Steam", "Okay!");
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("ModTool - Upload", "Failure to Upload - Check Console for errors!", "Okay!");
                                }
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("ModTool - Build", "Failure to Build - Check Console for errors!", "Okay!");
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("Note: Steam Upload UGC features are disabled. Please login to your Steam account via the 'Steam Account' sidebar tab.");
                    }
#pragma warning restore CS0162 // Unreachable code detected
            }
            });
        }

        static bool Build()
        {
            if (!GetSettingsObject(settings_asset))
            {
                return false;
            }
            string profileId = "";
            SetProfile(modToolProfile, ref profileId);

            // Override default settings for setting up Addressables.
            m_settings.BuildRemoteCatalog = true;
            m_settings.RemoteCatalogBuildPath.SetVariableByName(m_settings, "LocalBuildPath");
            m_settings.RemoteCatalogLoadPath.SetVariableByName(m_settings, "LocalLoadPath");
            m_settings.profileSettings.SetValue(profileId, "LocalBuildPath", $"[UnityEngine.Application.dataPath]/Exported/{m_data.m_name.Replace(" ", "_")}");
            m_settings.profileSettings.SetValue(profileId, "LocalLoadPath", "{UnityEngine.Application.dataPath}/Resources/Mods/" + m_data.m_name.Replace(" ", "_"));

            // Load the global build settings.
            IDataBuilder builderScript = AssetDatabase.LoadAssetAtPath<ScriptableObject>(build_script) as IDataBuilder;
            if (builderScript == null)
            {
                Debug.LogError(build_script + " couldn't be found or isn't a build script.");
                return false;
            }

            SetBuilder(builderScript);
            return BuildAddressableContent();
        }

        static bool Upload()
        {
            /*
             * Not implemented yet.
             * 
             * Potentially a Steam API will go here.
            */

            return true;
        }

        static string[] GetGroups()
        {
            string[] options = new string[m_settings?.groups.Count ?? 1];
            for (int i = 1; i < (m_settings?.groups?.Count ?? 0); i++)
            {
                // We do not want content in the default!
                if (m_settings.groups[i].Default)
                    continue;

                options[i] = m_settings.groups[i].name;
            }

            return options;
        }

        static bool GetModEditorSettings(string _settingsAsset)
        {
            m_data = AssetDatabase.LoadAssetAtPath<ScriptableObject>(_settingsAsset) as ModEditorData;

            // Create the ModEditorData scriptable object, if it doesn't exist!
            if (m_data == null)
            {
                m_data = ScriptableObject.CreateInstance<ModEditorData>();
                Assert.IsNotNull(m_data, "Mod Editor Data was not created when requested. Visit Github issues to post about this issue.");

                AssetDatabase.CreateAsset(m_data, _settingsAsset);

                AssetDatabase.SaveAssets();
            }

            // Apply globals everytime we load it successfully.
            if (m_data != null)
            {
                global_asset_labels.AddRange(m_data.m_asset_labels);

                var labels = m_settings.GetLabels();
                foreach (var label in global_asset_labels)
                {
                    // Add user-labels that aren't in the global-list.
                    if (!labels.Contains(label))
                    {
                        m_settings.AddLabel(label);
                    }
                    // Remove labels that aren't in the users-list.
                    if (!label.Equals("Scene") && !m_data.m_asset_labels.Contains(label))
                    {
                        m_settings.RemoveLabel(label);
                    }
                }
            }

            return m_data != null;
        }
        static bool GetSettingsObject(string _settingsAsset)
        {
            m_settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(_settingsAsset) as AddressableAssetSettings;

            return m_settings != null;
        }

        static void SetProfile(string _profile, ref string _profileId)
        {
            _profileId = m_settings.profileSettings.GetProfileId(_profile);
            // Initial Setup of profile
            if (String.IsNullOrEmpty(_profileId))
            {
                _profileId = m_settings.profileSettings.AddProfile(_profile, m_settings.profileSettings.GetProfileId("Default"));
            }

            Assert.IsFalse(String.IsNullOrEmpty(_profileId), $"Couldn't find a profile named, {_profile}, " +
                                "using current profile instead.");

            if (!String.IsNullOrEmpty(_profileId))
                m_settings.activeProfileId = _profileId;
        }

        static void SetBuilder(IDataBuilder _builder)
        {
            int index = m_settings.DataBuilders.IndexOf((ScriptableObject)_builder);

            Assert.IsTrue(index > 0, $"{_builder} must be added to the " +
                                        "DataBuilders list before it can be made " +
                                        "active. Using last run builder instead.");
            if (index > 0)
                m_settings.ActivePlayerDataBuilderIndex = index;
        }

        static bool BuildAddressableContent()
        {
            List<string> options = new List<string>(GetGroups());

            foreach (var sg in m_settings.groups)
            {
                if (sg.ReadOnly) continue;

                var groupSchema = sg.GetSchema<BundledAssetGroupSchema>();
                groupSchema.IncludeInBuild = (1 << options.FindIndex(0, options.Count, (string _scan) => { return sg.Name.Equals(_scan); }) & m_assetGroupIdx) != 0;
                if (groupSchema.IncludeInBuild)
                    Debug.Log("Building Mod Component: " + sg.Name);
                foreach (var entry in sg.entries)
                {
                    var addressSimple = entry.address.Split('/').Last();
                    entry.SetAddress($"{m_data.m_name}/{sg.Name}/{addressSimple}");
                    Debug.Log($"Entry: {sg.Name} <- {addressSimple}\n[{entry.address}]");
                }
                EditorUtility.SetDirty(sg);
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);

            if (!success)
            {
                Debug.LogError("Addressables build error encountered: " + result.Error);
                return false;
            }

            string modNameSanitized = m_data.m_name.Replace(" ", "_");
            string exportFilePath = m_settings.profileSettings.GetValueByName(m_settings.activeProfileId, "LocalBuildPath").Replace("[UnityEngine.Application.dataPath]", "Assets");
            foreach (var filePath in Directory.EnumerateFiles(exportFilePath))
            {
                var fpath = Path.GetFullPath(filePath);
                if (fpath.EndsWith(".meta"))
                {
                    File.Delete(fpath);
                    continue;
                }

                if ((fpath.EndsWith(".hash") || fpath.EndsWith(".json")) && !Path.GetFileNameWithoutExtension(fpath).StartsWith(m_data.m_name))
                {
                    var newName = Path.GetFullPath(Path.Combine(exportFilePath, m_data.m_name + Path.GetExtension(fpath)));
                    if (File.Exists(newName))
                        File.Delete(newName);

                    File.Move(fpath, newName);
                }
            }

            return true;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(150), GUILayout.ExpandHeight(true));

            DrawSidebar();

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));

            m_action?.Invoke();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            foreach (var p in m_tabs)
            {
                if (GUILayout.Button(p.Key))
                {
                    m_action = p.Value;
                }
            }
        }

    }
}