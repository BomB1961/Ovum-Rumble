using UnityEngine;

namespace DinoAlkkagi.Presentation
{
    public enum EggSkinFxTheme
    {
        Embercore,
        Prismhorn,
        Tidecrest
    }

    public class EggSkinTheme : MonoBehaviour
    {
        [SerializeField] private EggSkinFxTheme theme = EggSkinFxTheme.Embercore;

        public EggSkinFxTheme Theme => theme;
    }
}
