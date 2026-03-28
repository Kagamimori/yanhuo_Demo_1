using TMPro;
using UnityEngine;
using System.Linq;

public class FontManager : MonoBehaviour
{
    public static FontManager Instance;
    public TMP_FontAsset chineseFont;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (chineseFont == null)
        {
            // 尝试加载字体
            chineseFont = Resources.Load<TMP_FontAsset>("Fonts/NotoSansSC-Black SDF");
            if (chineseFont == null)
                chineseFont = Resources.Load<TMP_FontAsset>("NotoSansSC-Black SDF");
        }

        if (chineseFont != null)
        {
            ApplyFontToAllTexts();
            InvokeRepeating("ApplyFontToNewTexts", 1f, 1f); // 每秒检查一次新文本
        }
    }

    public void ApplyFontToAllTexts()
    {
        if (chineseFont == null) return;
        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (var text in allTexts)
        {
            if (text.font != chineseFont)
                text.font = chineseFont;
        }
        Debug.Log($"已设置 {allTexts.Length} 个文本的字体");
    }

    void ApplyFontToNewTexts()
    {
        ApplyFontToAllTexts();
    }
}