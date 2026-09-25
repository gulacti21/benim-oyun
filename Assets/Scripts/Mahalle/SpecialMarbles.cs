using UnityEngine;
public static class SpecialMarbles
{
    public const int FirstSkin=6, Count=4, PurchasePrice=400, RepairPrice=130, MaxLife=150;
    public static readonly string[] Descriptions={"Daha ağır darbe, daha düşük hız.","Dar aralıklara sığar; isabeti daha zor.","Daha canlı seker; duracağı yeri iyi hesapla.","Daha uzun yuvarlanır; mesafeyi iyi ayarla."};
    private static readonly Color[] Accents={new Color(.95f,.7f,.32f),new Color(.73f,.94f,1f),new Color(.98f,.87f,.32f),new Color(.66f,1f,.68f)};
    public static bool IsSpecial(int skin)=>skin>=FirstSkin&&skin<FirstSkin+Count;
    public static Color Accent(int skin)=>IsSpecial(skin)?Accents[skin-FirstSkin]:Color.Lerp(Campaign.SkinColors[(Mathf.Clamp(skin,0,5)+2)%6],Color.white,.4f);
}
