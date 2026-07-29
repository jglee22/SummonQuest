using UnityEngine;

public class KenneyUIThemeBootstrap : MonoBehaviour
{
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite slotFrameSprite;

    private void Awake()
    {
        KenneyUITheme.Configure(panelSprite, buttonSprite, slotFrameSprite);
    }

    private void Start()
    {
        KenneyUITheme.ApplyAll(UIManager.Instance);
    }
}
