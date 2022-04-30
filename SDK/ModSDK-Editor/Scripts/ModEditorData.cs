using System;
using UnityEngine;

namespace ModSDK.editor.data
{
    public class ModEditorData : ScriptableObject
    {
        public string m_name = "My Mod Name";
        public string m_description = "The description of my mod.";
        public Version m_version = new Version(1, 0, 0);

        public string[] m_asset_labels = new string[0];

        public string m_steamToken;
    }
}