using UnityEngine;

public interface IInternalMonoBehaviour
{
    void Init();
}

public abstract class InternalMonoBehaviour<T> : MonoBehaviour, IInternalMonoBehaviour where T : MonoBehaviour
{
    public T m_exposed;

    void Start()
    {
        m_exposed = GetComponent<T>();

        if (ModManager.Instance != null)
        {
            if (ModManager.Instance.IsLoading)
            {
                ModManager.Instance.onDoneLoading.AddListener(Init);
            }
            else
            {
                Init();
            }
        }
    }
    public abstract void Init();
}
