using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SimpleColorManager : MonoBehaviour
{
    [Header("Color Settings")]
    public GameObject[] objects;
    [Range(1.0f, 5.0f)]
    public float selectedDeltaE = 1.0f;

    public Material[] materials;
    
    [Header("Debug Info")]
    [SerializeField] private string loadedDataInfo;
    
    // 简化的数据结构：DeltaE -> Hue -> List<ColorSample>
    private Dictionary<float, Dictionary<float, List<SimpleColorSample>>> colorDatabase;

    void Start()
    {
        materials = objects.Select(obj => obj.GetComponent<Renderer>()?.material).Where(mat => mat != null).ToArray();
        LoadColorData();
        AssignColorsToMaterials();
    }

    void LoadColorData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("offset_colors_white");
        if (jsonFile == null)
        {
            Debug.LogError("Cannot find offset_colors_white.json file in Resources!");
            return;
        }

        colorDatabase = new Dictionary<float, Dictionary<float, List<SimpleColorSample>>>();
        
        // 使用正则表达式或字符串处理来解析JSON
        ParseJsonData(jsonFile.text);
        
        loadedDataInfo = $"Loaded {colorDatabase.Count} deltaE groups from Resources";
        Debug.Log(loadedDataInfo);
    }

    void ParseJsonData(string jsonText)
    {
        // 移除换行和多余空格
        jsonText = jsonText.Replace("\n", "").Replace("\r", "").Replace(" ", "");
        
        // 查找所有的deltaE块
        string[] deltaEBlocks = jsonText.Split(new string[] { "\"," }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string block in deltaEBlocks)
        {
            if (block.Contains("\":"))
            {
                ParseDeltaEBlock(block);
            }
        }
    }

    void ParseDeltaEBlock(string block)
    {
        try
        {
            // 提取deltaE值
            int deltaEStart = block.IndexOf("\"") + 1;
            int deltaEEnd = block.IndexOf("\":");
            if (deltaEStart < deltaEEnd)
            {
                string deltaEStr = block.Substring(deltaEStart, deltaEEnd - deltaEStart);
                if (float.TryParse(deltaEStr, out float deltaE))
                {
                    colorDatabase[deltaE] = new Dictionary<float, List<SimpleColorSample>>();
                    
                    // 查找所有hue块
                    ParseHueBlocks(block, deltaE);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to parse deltaE block: {e.Message}");
        }
    }

    void ParseHueBlocks(string block, float deltaE)
    {
        // 简化处理：查找常见的hue值
        float[] commonHues = { 0.0f, 40.0f, 80.0f, 120.0f, 160.0f, 200.0f, 240.0f, 280.0f, 320.0f };
        
        foreach (float hue in commonHues)
        {
            string huePattern = $"\"{hue}\":[";
            int hueIndex = block.IndexOf(huePattern);
            if (hueIndex >= 0)
            {
                colorDatabase[deltaE][hue] = new List<SimpleColorSample>();
                
                // 查找该hue下的颜色样本
                ParseColorSamples(block, deltaE, hue, hueIndex);
            }
        }
    }

    void ParseColorSamples(string block, float deltaE, float hue, int startIndex)
    {
        // 查找rgb数组
        string rgbPattern = "\"rgb\":[";
        int rgbIndex = block.IndexOf(rgbPattern, startIndex);
        
        while (rgbIndex >= 0 && rgbIndex < block.Length)
        {
            try
            {
                // 提取RGB值
                int rgbStart = rgbIndex + rgbPattern.Length;
                int rgbEnd = block.IndexOf("]", rgbStart);
                if (rgbEnd > rgbStart)
                {
                    string rgbStr = block.Substring(rgbStart, rgbEnd - rgbStart);
                    string[] rgbValues = rgbStr.Split(',');
                    
                    if (rgbValues.Length >= 3)
                    {
                        if (float.TryParse(rgbValues[0], out float r) &&
                            float.TryParse(rgbValues[1], out float g) &&
                            float.TryParse(rgbValues[2], out float b))
                        {
                            var sample = new SimpleColorSample
                            {
                                hue = hue,
                                r = r,
                                g = g,
                                b = b,
                                deltaE = deltaE
                            };
                            
                            colorDatabase[deltaE][hue].Add(sample);
                        }
                    }
                }
                
                // 查找下一个rgb
                rgbIndex = block.IndexOf(rgbPattern, rgbEnd);
                
                // 如果超出了当前hue的范围，停止
                string nextHuePattern = "\":";
                int nextHueIndex = block.IndexOf(nextHuePattern, rgbEnd);
                if (nextHueIndex >= 0 && nextHueIndex < rgbIndex)
                {
                    break;
                }
            }
            catch
            {
                break;
            }
        }
    }

    void AssignColorsToMaterials()
    {
        if (materials == null || materials.Length == 0 || colorDatabase == null)
        {
            Debug.LogWarning("Materials array is empty or color data not loaded!");
            return;
        }

        // 检查选择的deltaE是否存在
        if (!colorDatabase.ContainsKey(selectedDeltaE))
        {
            Debug.LogError($"DeltaE value {selectedDeltaE} not found in color data!");
            // 使用最接近的deltaE值
            float closestDeltaE = colorDatabase.Keys.OrderBy(x => Mathf.Abs(x - selectedDeltaE)).First();
            selectedDeltaE = closestDeltaE;
            Debug.Log($"Using closest deltaE value: {selectedDeltaE}");
        }

        var selectedHueGroups = colorDatabase[selectedDeltaE];
        var availableHues = selectedHueGroups.Keys.ToArray();
        
        if (availableHues.Length == 0)
        {
            Debug.LogWarning("No hue groups found for selected deltaE!");
            return;
        }
        
        // 为每个材质分配颜色
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;

            // 随机选择一个hue
            float randomHue = availableHues[Random.Range(0, availableHues.Length)];
            var hueSamples = selectedHueGroups[randomHue];
            
            if (hueSamples.Count == 0) continue;
            
            // 从该hue组中随机选择一个颜色样本
            var randomSample = hueSamples[Random.Range(0, hueSamples.Count)];
            
            // 创建颜色
            Color newColor = new Color(randomSample.r, randomSample.g, randomSample.b);
            
            // 设置材质的BaseColor
            if (materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", newColor);
            }
            else if (materials[i].HasProperty("_Color"))
            {
                materials[i].SetColor("_Color", newColor);
            }
            
            Debug.Log($"Material {i}: Assigned color from Hue {randomHue}, RGB:({randomSample.r:F2},{randomSample.g:F2},{randomSample.b:F2}), DeltaE:{randomSample.deltaE:F1}");
        }
    }

    // 公共方法，允许运行时重新分配颜色
    [ContextMenu("Reassign Colors")]
    public void ReassignColors()
    {
        AssignColorsToMaterials();
    }

    // 允许在运行时更改deltaE值并重新分配颜色
    public void ChangeDeltaE(float newDeltaE)
    {
        selectedDeltaE = newDeltaE;
        AssignColorsToMaterials();
    }

    void Update()
    {
        // 按键控制
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReassignColors();
        }
        
        // 数字键切换deltaE
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeDeltaE(1.0f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeDeltaE(2.0f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeDeltaE(3.0f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeDeltaE(4.0f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ChangeDeltaE(5.0f);
    }
}

[System.Serializable]
public class SimpleColorSample
{
    public float hue;
    public float r, g, b;
    public float deltaE;
}
