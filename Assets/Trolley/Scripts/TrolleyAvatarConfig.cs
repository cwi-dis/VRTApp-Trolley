using System;
using UnityEngine;

[Serializable]
public class TrolleyAvatarConfig
{
    public enum AvatarBodyType { Masculine, Feminine }

    [Tooltip("'Masculine' or 'Feminine'")]
    public string bodyType = "";
    [Tooltip("Skin tone swatch index, 0-5")]
    public int skinToneIndex = 0;
    [Tooltip("Hair colour swatch index, 0-5")]
    public int hairColorIndex = 0;

    public string ToLogString() => $"body:{bodyType},skin:{skinToneIndex},hair:{hairColorIndex}";
}
