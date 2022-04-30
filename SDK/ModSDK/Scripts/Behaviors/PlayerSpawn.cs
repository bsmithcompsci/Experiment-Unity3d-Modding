using UnityEngine;

namespace ModSDK.examples.monobehaviours
{
    public class PlayerSpawn : MonoBehaviour
    {
        private const float lineLength = 2.0f;
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * lineLength);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * lineLength);
        
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.up * lineLength);
        }
    }
}
