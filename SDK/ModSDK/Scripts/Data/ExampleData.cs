using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ModSDK.examples.scriptable
{
    [Serializable]
    public struct ExampleStructData
    {
        public int x;
        public float y;
        public string z;
    }

    [CreateAssetMenu(menuName = "Mod/Example", fileName = "ExampleData")]
    public class ExampleData : ScriptableObject
    {
        /*
         * The point of doing ScriptableObjects is to pass data between the mod to the game.
         * Now, you could do a Monobehaviour and pass data via that way. Though this is another way to deal with data.
         * 
         * The question you should ask yourself, when debating between a Monobehaviour and a ScriptableObject 
         * is that do you want this data to be transferred globally between objects.
         * Monobehaviours are more representive of the object, rather than all objects.
         * Example of this would be:
         * I have a Faction, this faction holds characters, vehicles and weapons. Well somewhere in my internal game-code I would 
         * use the faction to display UI elements about spawning vehicles, weapons and maybe randomize the available player spawn skins 
         * with the characters.
         * Verses, I have a vehicle and that vehicle may hold data about it's current RPM and other data that is specific about that object's current state.
         * Which would not be a ScriptableObject instead it's a Monobehaviour.
         * 
         * Mind that ScriptableObjects will have to be carried over some how... Either a specialized asset label that categorizes the ScriptableObject or \
         * MonoBehaviour holds this ScriptableObject.
         * 
         * Note:
         * All properties and fields must be serializable to be de-serialized when the internal side reads this scriptable object.
        */

        public new string name;
        public string description;

        public ExampleStructData data;

        public AssetReferenceGameObject asset;
    }

}