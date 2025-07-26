using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SimpleJsonColorManager : MonoBehaviour
{
    [Header("Color Settings")]
    public GameObject[] objects;
    [Range(1.0f, 5.0f)]
    public float selectedDeltaE = 1.0f;

    public Material[] materials;
    
    [Header("Debug Info")]
    [SerializeField] private string loadedDataInfo;
    
    // 简化的数据结构
    private Dictionary<string, List<SimpleColorData>> colorDatabase;

    void Start()
    {
        materials = objects.Select(obj => obj.GetComponent<Renderer>()?.material).Where(mat => mat != null).ToArray();
        LoadColorDataSimple();
        AssignColorsToMaterials();
    }

    void LoadColorDataSimple()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("offset_colors_white");
        if (jsonFile == null)
        {
            Debug.LogError("Cannot find offset_colors_white.json file in Resources!");
            return;
        }

        colorDatabase = new Dictionary<string, List<SimpleColorData>>();
        
        // 使用正则表达式和字符串操作进行简单解析
        ParseWithRegex(jsonFile.text);
        
        loadedDataInfo = $"Loaded {colorDatabase.Count} color combinations";
        Debug.Log(loadedDataInfo);
    }

    void ParseWithRegex(string jsonText)
    {
        // 移除所有换行和多余空格
        jsonText = jsonText.Replace("\n", "").Replace("\r", "").Replace("  ", " ");
        
        // 预定义的deltaE和hue值
        float[] deltaEValues = { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
        float[] hueValues = { 0.0f, 40.0f, 80.0f, 120.0f, 160.0f, 200.0f, 240.0f, 280.0f, 320.0f };
        
        foreach (float deltaE in deltaEValues)
        {
            foreach (float hue in hueValues)
            {
                string key = $"{deltaE}-{hue}";
                colorDatabase[key] = new List<SimpleColorData>();
                
                // 查找RGB模式
                string searchPattern = $"\"{deltaE.ToString("F1")}\":";
                int deltaEIndex = jsonText.IndexOf(searchPattern);
                
                if (deltaEIndex >= 0)
                {
                    string huePattern = $"\"{hue.ToString("F1")}\":[";
                    int hueIndex = jsonText.IndexOf(huePattern, deltaEIndex);
                    
                    if (hueIndex >= 0)
                    {
                        // 简单查找RGB值
                        ExtractRGBValues(jsonText, hueIndex, key);
                    }
                }
            }
        }
    }

    void ExtractRGBValues(string jsonText, int startIndex, string key)
    {
        int currentIndex = startIndex;
        int maxSearch = 10000; // 限制搜索范围
        int searchEnd = Mathf.Min(startIndex + maxSearch, jsonText.Length);
        
        while (currentIndex < searchEnd)
        {
            int rgbIndex = jsonText.IndexOf("\"rgb\":[", currentIndex);
            if (rgbIndex == -1 || rgbIndex >= searchEnd) break;
            
            int rgbStart = rgbIndex + 7; // "rgb":[ 的长度
            int rgbEnd = jsonText.IndexOf("]", rgbStart);
            
            if (rgbEnd > rgbStart)
            {
                string rgbStr = jsonText.Substring(rgbStart, rgbEnd - rgbStart);
                string[] rgbParts = rgbStr.Split(',');
                
                if (rgbParts.Length >= 3)
                {
                    if (float.TryParse(rgbParts[0].Trim(), out float r) &&
                        float.TryParse(rgbParts[1].Trim(), out float g) &&
                        float.TryParse(rgbParts[2].Trim(), out float b))
                    {
                        var colorData = new SimpleColorData
                        {
                            r = r,
                            g = g,
                            b = b
                        };
                        
                        colorDatabase[key].Add(colorData);
                    }
                }
            }
            
            currentIndex = rgbEnd + 1;
            
            // 检查是否到了下一个hue或deltaE
            int nextHueIndex = jsonText.IndexOf("\":", currentIndex);
            if (nextHueIndex >= 0 && nextHueIndex < currentIndex + 50)
            {
                break; // 可能到了下一个部分
            }
        }
        
        Debug.Log($"Extracted {colorDatabase[key].Count} colors for {key}");
    }

    void AssignColorsToMaterials()
    {
        if (materials == null || materials.Length == 0 || colorDatabase == null)
        {
            Debug.LogWarning("Materials array is empty or color data not loaded!");
            return;
        }

        // 查找适用的颜色组合
        var availableKeys = colorDatabase.Keys.Where(k => k.StartsWith(selectedDeltaE.ToString("F1")) && colorDatabase[k].Count > 0).ToList();
        
        if (availableKeys.Count == 0)
        {
            Debug.LogError($"No color data found for DeltaE {selectedDeltaE}!");
            return;
        }

        Debug.Log($"Found {availableKeys.Count} available color groups for DeltaE {selectedDeltaE}");
        
        // 为每个材质分配颜色
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;

            // 随机选择一个颜色组
            string randomKey = availableKeys[Random.Range(0, availableKeys.Count)];
            var colorGroup = colorDatabase[randomKey];
            
            if (colorGroup.Count == 0) continue;
            
            // 随机选择一个颜色
            var randomColor = colorGroup[Random.Range(0, colorGroup.Count)];
            
            // 创建Unity颜色
            Color newColor = new Color(randomColor.r, randomColor.g, randomColor.b);
            
            // 设置材质颜色
            if (materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", newColor);
            }
            else if (materials[i].HasProperty("_Color"))
            {
                materials[i].SetColor("_Color", newColor);
            }
            
            Debug.Log($"Material {i}: Assigned color RGB({randomColor.r:F2},{randomColor.g:F2},{randomColor.b:F2}) from {randomKey}");
        }
    }

    [ContextMenu("Reassign Colors")]
    public void ReassignColors()
    {
        AssignColorsToMaterials();
    }

    public void ChangeDeltaE(float newDeltaE)
    {
        selectedDeltaE = newDeltaE;
        AssignColorsToMaterials();
    }
}

[System.Serializable]
public class SimpleColorData
{
    public float r, g, b;
}
