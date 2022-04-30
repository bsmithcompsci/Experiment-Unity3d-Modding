using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ModSDK.examples.monobehaviours
{
    public class AddressableObjectSpawner : MonoBehaviour
    {
        public bool destructAfterCreation = true;
        public AssetReferenceGameObject ObjectAddress;
    }
}
