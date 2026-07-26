using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// EventSystem 입력 모듈을 보정해 UI 클릭이 동작하도록 한다.
/// Input Actions 참조가 깨진 Input System UI 모듈은 StandaloneInputModule로 대체한다.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class UIEventSystemFixer : MonoBehaviour
{
    private void Awake()
    {
        EventSystem eventSystem = GetComponent<EventSystem>();
        if (eventSystem == null)
            eventSystem = EventSystem.current;

        if (eventSystem == null)
            return;

        BaseInputModule activeModule = eventSystem.currentInputModule;
        if (activeModule != null && activeModule.GetType().Name == "InputSystemUIInputModule")
        {
            Destroy(activeModule);
            activeModule = null;
        }

        StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule == null)
            standaloneModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();

        standaloneModule.enabled = true;
        eventSystem.enabled = true;
    }
}
