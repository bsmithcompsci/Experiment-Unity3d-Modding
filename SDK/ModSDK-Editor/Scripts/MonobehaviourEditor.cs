using UnityEngine;
using UnityEditor;

namespace ModSDK.editor.gui
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoBehaviourEditor : Editor
    {
        /*
         * This class is meant to fix the issues of Monobehaviours unable to edit multiple objects.
        */
    }
}