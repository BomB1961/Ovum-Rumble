using UnityEngine;

namespace DinoAlkkagi.Presentation
{
    public enum EggSkinFxTheme
    {
        Embercore,
        Prismhorn
    }

    public class EggSkinTheme : MonoBehaviour
    {
        [SerializeField] private EggSkinFxTheme theme = EggSkinFxTheme.Embercore;

        public EggSkinFxTheme Theme => theme;
    }
}
