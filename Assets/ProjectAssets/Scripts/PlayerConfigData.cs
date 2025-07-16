using UnityEngine;

[CreateAssetMenu(menuName = "Player Configuration Data")]
public class PlayerConfigData : ScriptableObject
{
    [System.Serializable]
    public class PlayerConfig
    {
        public string deviceId;
        public string controlScheme;
        public int displayIndex;
    }

    public PlayerConfig[] playerConfigs;
    public int maxPlayers = 4; // Moved to ScriptableObject
}