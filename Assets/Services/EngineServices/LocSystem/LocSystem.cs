using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
public class LocSystem : MonoBehaviour, IService
{
    public string language = LANG_EN;
    
    public const string LANG_EN = "en";
    public const string LANG_RU = "ru";
    
    public static List<string> langs = new List<string>() { LANG_EN, LANG_RU };

    public void Init()
    {
        if(CMS.GetSingleComponent<ConfigMain>().rewriteLocWithRus)
            language = LANG_RU;
        else
            language = LANG_EN;
    }

    public void UpdateTexts()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allObjects)
        {
            // Получаем компонент, который реализует интерфейс UITextSetter
            UITextSetter textSetter = obj.GetComponent<UITextSetter>();

            // Если компонент найден, вызываем метод UpdateUI()
            if (textSetter != null)
            {
                textSetter.UpdateUI();
            }
        }
    }
}

[Serializable]
public class LocString
{
    public string en;
    public string ru;

    Dictionary<string, FieldInfo> field = new Dictionary<string, FieldInfo>();

    public string GetText()
    {
        if (field == null) field = new Dictionary<string, FieldInfo>();
        if (!field.ContainsKey(G.LocSystem.language))
        {
            Type type = this.GetType();
            FieldInfo fieldInfo = type.GetField(G.LocSystem.language);
            field.Add(G.LocSystem.language, fieldInfo);
        }

        return field[G.LocSystem.language].GetValue(this) as string;
    }

    public LocString(string en, string ru)
    {
        this.en = en;
        this.ru = ru;
    }

public override string ToString()
    {
        return GetText();
    }
}