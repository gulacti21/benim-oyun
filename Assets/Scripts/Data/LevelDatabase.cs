using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "MISKETR/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public LevelData[] levels;

    public int Count => levels != null ? levels.Length : 0;

    public LevelData Get(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length)
        {
            return null;
        }

        return levels[index];
    }
}
