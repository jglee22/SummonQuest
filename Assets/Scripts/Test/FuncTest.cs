using UnityEngine;

public class FuncTest : MonoBehaviour
{
    public CharacterDexUI characterDexUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //characterDexUI = FindAnyObjectByType<CharacterDexUI>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) // 혹은 원하는 키
        {
            characterDexUI.Open(SaveManager.Instance.LoadOwnedCharacters());
        }
    }
}
