using ModSDK.examples.monobehaviours;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(AddressableObjectSpawner))]
public class InternalAddresableObjectSpawner : InternalMonoBehaviour<AddressableObjectSpawner>
{
    public override void Init()
    {
        if (m_exposed.ObjectAddress != null)
        {
            Spawn();
            return;
        }

        // If the object address is not loaded yet, lets start loading it.
        m_exposed.ObjectAddress.LoadAssetAsync<GameObject>().Completed += InternalAddresableObjectSpawner_Completed;
    }

    private void InternalAddresableObjectSpawner_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> obj)
    {
        Assert.IsTrue(m_exposed.ObjectAddress.IsValid(), "Asset failed load.");
        if (!m_exposed.ObjectAddress.IsValid())
            return;

        Spawn();
    }
    private void Spawn()
    {
        var requestAssetObject = ModManager.Instance.InstantiateAsync(m_exposed.ObjectAddress, transform.position, transform.rotation);
        GameObject requestedAsset = null;
        if (requestAssetObject != null)
        {
            // Spawn the expected asset.
            requestedAsset = requestAssetObject.Value.Result;
        }

        if (requestedAsset == null)
            return;

        if (m_exposed.destructAfterCreation)
        {
            // Destroy these components.
            Destroy(this);
            Destroy(m_exposed);
        }
    }
}
