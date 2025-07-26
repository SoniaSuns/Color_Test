using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class ColorManager : MonoBehaviour
{
    [Header("Color Settings")]
    public GameObject[]objects;
    [Range(1.0f, 5.0f)]
    public float selectedDeltaE = 1.0f;

    public Material[] materials;
    
    [Header("Debug Info")]
    [SerializeField] private string loadedDataInfo;
    
    private ColorData colorData;
    private Dictionary<float, Dictionary<float, ColorSample[]>> deltaEGroups;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        deltaEGroups = new Dictionary<float, Dictionary<float, ColorSample[]>>();
        
        // 添加调试信息
        Debug.Log($"JSON file loaded, content length: {jsonFile.text.Length}");
        Debug.Log($"First 200 characters: {jsonFile.text.Substring(0, Mathf.Min(200, jsonFile.text.Length))}");
        
        // 手动解析JSON - 使用更简单的方法
        ParseJsonManually(jsonFile.text);
        
        loadedDataInfo = $"Loaded {deltaEGroups.Count} deltaE groups";
        Debug.Log(loadedDataInfo);
        
        // 额外的调试信息
        foreach (var deltaEPair in deltaEGroups)
        {
            Debug.Log($"DeltaE {deltaEPair.Key}: {deltaEPair.Value.Count} hue groups");
            foreach (var huePair in deltaEPair.Value)
            {
                Debug.Log($"  Hue {huePair.Key}: {huePair.Value.Length} samples");
            }
        }
    }
    
    void ParseJsonManually(string jsonText)
    {
        try
        {
            // 先尝试一个简单的测试解析
            Debug.Log("Starting JSON parsing...");
            
            // 移除多余的空白字符
            jsonText = System.Text.RegularExpressions.Regex.Replace(jsonText, @"\s+", " ");
            
            // 预定义的deltaE和hue值
            float[] deltaEValues = { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
            float[] hueValues = { 0.0f, 40.0f, 80.0f, 120.0f, 160.0f, 200.0f, 240.0f, 280.0f, 320.0f };
            
            foreach (float deltaE in deltaEValues)
            {
                deltaEGroups[deltaE] = new Dictionary<float, ColorSample[]>();
                
                // 更简单的字符串匹配
                string deltaEKey = $"\"{deltaE.ToString("F1")}\":";
                Debug.Log($"Looking for deltaE pattern: {deltaEKey}");
                
                int deltaEIndex = jsonText.IndexOf(deltaEKey);
                if (deltaEIndex == -1)
                {
                    Debug.LogWarning($"DeltaE pattern {deltaEKey} not found in JSON");
                    continue;
                }
                
                Debug.Log($"Found deltaE {deltaE} at index {deltaEIndex}");
                
                foreach (float hue in hueValues)
                {
                    var samples = ExtractSamplesSimple(jsonText, deltaE, hue, deltaEIndex);
                    if (samples.Count > 0)
                    {
                        deltaEGroups[deltaE][hue] = samples.ToArray();
                        Debug.Log($"Successfully loaded {samples.Count} samples for DeltaE {deltaE}, Hue {hue}");
                    }
                }
                
                Debug.Log($"DeltaE {deltaE} loaded with {deltaEGroups[deltaE].Count} hue groups");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse JSON: {e.Message}\nStackTrace: {e.StackTrace}");
        }
    }
    
    List<ColorSample> ExtractSamplesSimple(string jsonText, float deltaE, float hue, int deltaEStartIndex)
    {
        var samples = new List<ColorSample>();
        
        try
        {
            // 在deltaE块内搜索hue
            string hueKey = $"\"{hue.ToString("F1")}\":";
            Debug.Log($"Looking for hue pattern: {hueKey} after index {deltaEStartIndex}");
            
            int hueIndex = jsonText.IndexOf(hueKey, deltaEStartIndex);
            if (hueIndex == -1)
            {
                // 尝试其他可能的格式
                string altHueKey = $"\"{hue.ToString("F0")}\":";
                hueIndex = jsonText.IndexOf(altHueKey, deltaEStartIndex);
                if (hueIndex == -1)
                {
                    return samples;
                }
            }
            
            Debug.Log($"Found hue {hue} at index {hueIndex}");
            
            // 查找下一个deltaE的开始位置作为搜索边界
            int nextDeltaEIndex = jsonText.Length;
            foreach (float nextDeltaE in new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f })
            {
                if (nextDeltaE > deltaE)
                {
                    string nextDeltaEKey = $"\"{nextDeltaE.ToString("F1")}\":";
                    int nextIndex = jsonText.IndexOf(nextDeltaEKey, deltaEStartIndex + 1);
                    if (nextIndex != -1 && nextIndex < nextDeltaEIndex)
                    {
                        nextDeltaEIndex = nextIndex;
                    }
                }
            }
            
            // 限制搜索范围
            int searchLimit = nextDeltaEIndex - hueIndex;
            if (searchLimit > 10000) searchLimit = 10000; // 安全限制
            
            // 查找RGB值
            int searchEnd = hueIndex + searchLimit;
            int currentPos = hueIndex;
            
            while (currentPos < searchEnd && currentPos < jsonText.Length)
            {
                int rgbIndex = jsonText.IndexOf("\"rgb\":", currentPos);
                if (rgbIndex == -1 || rgbIndex >= searchEnd) break;
                
                var sample = ExtractSingleRGBSample(jsonText, rgbIndex);
                if (sample != null)
                {
                    samples.Add(sample);
                }
                
                currentPos = rgbIndex + 10; // 移动到下一个可能的位置
                
                // 检查是否遇到了下一个hue
                string nextHuePattern = "\":";
                int nextHueIndex = jsonText.IndexOf(nextHuePattern, currentPos);
                if (nextHueIndex != -1 && nextHueIndex < currentPos + 100)
                {
                    // 可能遇到了下一个hue，检查是否是数字开头
                    int quoteIndex = jsonText.LastIndexOf("\"", nextHueIndex);
                    if (quoteIndex != -1 && nextHueIndex - quoteIndex < 10)
                    {
                        string potentialHue = jsonText.Substring(quoteIndex + 1, nextHueIndex - quoteIndex - 1);
                        if (float.TryParse(potentialHue, out float _))
                        {
                            break; // 确实是下一个hue
                        }
                    }
                }
            }
            
            Debug.Log($"Extracted {samples.Count} samples for DeltaE {deltaE}, Hue {hue}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to extract samples for DeltaE {deltaE}, Hue {hue}: {e.Message}");
        }
        
        return samples;
    }
    
    ColorSample ExtractSingleRGBSample(string jsonText, int rgbIndex)
    {
        try
        {
            // 查找RGB数组的开始
            int arrayStart = jsonText.IndexOf("[", rgbIndex);
            if (arrayStart == -1) return null;
            
            int arrayEnd = jsonText.IndexOf("]", arrayStart);
            if (arrayEnd == -1) return null;
            
            string rgbStr = jsonText.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
            string[] rgbValues = rgbStr.Split(',');
            
            if (rgbValues.Length >= 3)
            {
                var sample = new ColorSample();
                sample.rgb = new float[3];
                
                for (int i = 0; i < 3; i++)
                {
                    if (float.TryParse(rgbValues[i].Trim(), out float value))
                    {
                        sample.rgb[i] = value;
                    }
                }
                
                return sample;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to extract RGB sample: {e.Message}");
        }
        
        return null;
    }
    
    void AssignColorsToMaterials()
    {
        if (materials == null || materials.Length == 0 || deltaEGroups == null)
        {
            Debug.LogWarning("Materials array is empty or color data not loaded!");
            return;
        }

        // 选择指定的deltaE组
        if (!deltaEGroups.ContainsKey(selectedDeltaE))
        {
            Debug.LogError($"DeltaE value {selectedDeltaE} not found in color data!");
            Debug.Log($"Available DeltaE values: {string.Join(", ", deltaEGroups.Keys)}");
            return;
        }

        Dictionary<float, ColorSample[]> selectedHueGroups = deltaEGroups[selectedDeltaE];
        var hueKeys = selectedHueGroups.Keys.ToArray();
        
        // 检查是否有可用的hue组
        if (hueKeys.Length == 0)
        {
            Debug.LogError($"No hue groups found for DeltaE {selectedDeltaE}!");
            return;
        }
        
        // 过滤出有效的hue组（包含颜色样本的）
        var validHueKeys = new List<float>();
        foreach (float hue in hueKeys)
        {
            if (selectedHueGroups[hue] != null && selectedHueGroups[hue].Length > 0)
            {
                validHueKeys.Add(hue);
            }
        }
        
        if (validHueKeys.Count == 0)
        {
            Debug.LogError($"No valid color samples found for DeltaE {selectedDeltaE}!");
            return;
        }
        
        Debug.Log($"Found {validHueKeys.Count} valid hue groups for DeltaE {selectedDeltaE}");
        
        // 为每个材质分配颜色
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;

            // 随机选择一个有效的hue
            float randomHue = validHueKeys[Random.Range(0, validHueKeys.Count)];
            ColorSample[] hueSamples = selectedHueGroups[randomHue];
            
            // 再次检查样本数组是否有效
            if (hueSamples == null || hueSamples.Length == 0)
            {
                Debug.LogWarning($"No samples found for Hue {randomHue}, skipping material {i}");
                continue;
            }
            
            // 从该hue组中随机选择一个颜色样本
            ColorSample randomSample = hueSamples[Random.Range(0, hueSamples.Length)];
            
            // 检查颜色样本是否有效
            if (randomSample == null || randomSample.rgb == null || randomSample.rgb.Length < 3)
            {
                Debug.LogWarning($"Invalid color sample for material {i}, skipping");
                continue;
            }
            
            // 使用RGB值创建颜色（因为新格式已经包含RGB）
            Color newColor = new Color(randomSample.R, randomSample.G, randomSample.B);
            
            // 设置材质的BaseColor
            if (materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", newColor);
            }
            else if (materials[i].HasProperty("_Color"))
            {
                materials[i].SetColor("_Color", newColor);
            }
            else
            {
                Debug.LogWarning($"Material {i} doesn't have _BaseColor or _Color property");
            }
            
            Debug.Log($"Material {i}: Assigned color from Hue {randomHue}, RGB:({randomSample.R:F2},{randomSample.G:F2},{randomSample.B:F2}), DeltaE:{randomSample.delta_e:F2}");
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

    // Update is called once per frame
    void Update()
    {
        // // 可以在这里添加实时更新逻辑，比如按键切换deltaE值
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     ReassignColors();
        // }
    }
}

// 数据结构类
[System.Serializable]
public class ColorData
{
    // 使用Dictionary来存储DeltaE -> Hue -> ColorSample[]的结构
    // 由于JsonUtility不支持Dictionary，我们需要手动解析
}

[System.Serializable]
public class ColorSample
{
    public float[] hsv;
    public float[] rgb;
    public float[] lab;
    public float delta_e;
    
    // 便于访问的属性
    public float H => hsv != null && hsv.Length > 0 ? hsv[0] : 0f;
    public float S => hsv != null && hsv.Length > 1 ? hsv[1] : 0f;
    public float V => hsv != null && hsv.Length > 2 ? hsv[2] : 0f;
    
    public float R => rgb != null && rgb.Length > 0 ? rgb[0] : 0f;
    public float G => rgb != null && rgb.Length > 1 ? rgb[1] : 0f;
    public float B => rgb != null && rgb.Length > 2 ? rgb[2] : 0f;
}
