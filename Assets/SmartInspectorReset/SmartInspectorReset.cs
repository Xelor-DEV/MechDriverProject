#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SmartInspectorReset
{
    [MenuItem("Tools/Smart Inspector/Reset to Default")]
    public static void ResetToDefault()
    {
        if (!Application.isEditor)
        {
            Debug.LogWarning("Smart Inspector reset can only be used in Unity Editor.");
            return;
        }

        EditorPrefs.DeleteKey("SI_ShowInfo");
        EditorPrefs.DeleteKey("SI_ShowSettings");
        EditorPrefs.DeleteKey("SI_ShowSearchBar");
        EditorPrefs.DeleteKey("SI_ShowButtons");
        EditorPrefs.DeleteKey("SI_MatchWord");
        EditorPrefs.DeleteKey("SI_MaxSlots");
        EditorPrefs.DeleteKey("SI_ViewType");
        EditorPrefs.DeleteKey("SI_SlotHeight");

        Debug.Log("Smart Inspector reset to default settings!");
    }
}
#endif