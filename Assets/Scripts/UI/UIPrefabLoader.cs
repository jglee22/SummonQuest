using UnityEngine;

public static class UIPrefabLoader
{
    private const string PrefabRoot = "Prefabs/";

    public static GameObject LoadStageSlot()
    {
        return Resources.Load<GameObject>($"{PrefabRoot}StageSlot");
    }

    public static GameObject LoadStageSelectionPanel()
    {
        return Resources.Load<GameObject>($"{PrefabRoot}StageSelectionPanel");
    }
}
