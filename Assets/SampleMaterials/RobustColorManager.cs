using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RobustColorManager : MonoBehaviour
{
    [Header("Color Settings")]
    public GameObject[] objects;
    [Range(1.0f, 5.0f)]
    public float selectedDeltaE = 1.0f;

    public Material[] materials;
    
    [Header("Debug Info")]
    [SerializeField] private string loadedDataInfo;
    
    // 简单但可靠的数据结构
    private Dictionary<string, List<Color>> colorDatabase;

    void Start()
    {
        materials = objects.Select(obj => obj.GetComponent<Renderer>()?.material).Where(mat => mat != null).ToArray();
        LoadAndParseColors();
        AssignColorsToMaterials();
    }

    void LoadAndParseColors()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("offset_colors_white");
        if (jsonFile == null)
        {
            Debug.LogError("Cannot find offset_colors_white.json file in Resources!");
            return;
        }

        colorDatabase = new Dictionary<string, List<Color>>();
        string jsonText = jsonFile.text;
        
        Debug.Log($"JSON loaded, length: {jsonText.Length}");
        
        // 预定义要搜索的组合
        float[] deltaEValues = { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
        float[] hueValues = { 0.0f, 40.0f, 80.0f, 120.0f, 160.0f, 200.0f, 240.0f, 280.0f, 320.0f };
        
        foreach (float deltaE in deltaEValues)
        {
            foreach (float hue in hueValues)
            {
                string key = $"{deltaE:F1}-{hue:F1}";
                colorDatabase[key] = ExtractColorsForKey(jsonText, deltaE, hue);
                
                if (colorDatabase[key].Count > 0)
                {
                    Debug.Log($"Loaded {colorDatabase[key].Count} colors for {key}");
                }
            }
        }
        
        int totalGroups = colorDatabase.Values.Count(list => list.Count > 0);
        loadedDataInfo = $"Loaded {totalGroups} color groups";
        Debug.Log(loadedDataInfo);
    }

    List<Color> ExtractColorsForKey(string jsonText, float deltaE, float hue)
    {
        var colors = new List<Color>();
        
        try
        {
            // 查找deltaE块
            string deltaEPattern = $"\"{deltaE:F1}\"";
            int deltaEIndex = jsonText.IndexOf(deltaEPattern);
            if (deltaEIndex == -1)
            {
                return colors;
            }
            
            // 查找hue块（在deltaE块之后）
            string huePattern = $"\"{hue:F1}\"";
            int hueIndex = jsonText.IndexOf(huePattern, deltaEIndex);
            if (hueIndex == -1)
            {
                return colors;
            }
            
            // 限制搜索范围 - 找到下一个deltaE或文件结尾
            int searchLimit = jsonText.Length;
            foreach (float nextDelta in new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f })
            {
                if (nextDelta > deltaE)
                {
                    int nextDeltaIndex = jsonText.IndexOf($"\"{nextDelta:F1}\"", deltaEIndex + 1);
                    if (nextDeltaIndex != -1 && nextDeltaIndex < searchLimit)
                    {
                        searchLimit = nextDeltaIndex;
                    }
                }
            }
            
            // 在限定范围内查找所有RGB值
            int currentPos = hueIndex;
            while (currentPos < searchLimit)
            {
                int rgbIndex = jsonText.IndexOf("\"rgb\":", currentPos);
                if (rgbIndex == -1 || rgbIndex >= searchLimit) break;
                
                // 查找RGB数组
                int arrayStart = jsonText.IndexOf("[", rgbIndex);
                int arrayEnd = jsonText.IndexOf("]", arrayStart);
                
                if (arrayStart != -1 && arrayEnd != -1 && arrayEnd > arrayStart)
                {
                    string rgbStr = jsonText.Substring(arrayStart + 1, arrayEnd - arrayStart - 1);
                    string[] rgbParts = rgbStr.Split(',');
                    
                    if (rgbParts.Length >= 3)
                    {
                        if (float.TryParse(rgbParts[0].Trim(), out float r) &&
                            float.TryParse(rgbParts[1].Trim(), out float g) &&
                            float.TryParse(rgbParts[2].Trim(), out float b))
                        {
                            colors.Add(new Color(r, g, b));
                        }
                    }
                }
                
                currentPos = rgbIndex + 10;
                
                // 检查是否到了下一个hue
                int nextQuoteIndex = jsonText.IndexOf("\":", currentPos);
                if (nextQuoteIndex != -1 && nextQuoteIndex < currentPos + 200)
                {
                    // 检查引号前的内容是否是数字（可能是下一个hue）
                    int prevQuoteIndex = jsonText.LastIndexOf("\"", nextQuoteIndex);
                    if (prevQuoteIndex != -1)
                    {
                        string possibleHue = jsonText.Substring(prevQuoteIndex + 1, nextQuoteIndex - prevQuoteIndex - 1);
                        if (float.TryParse(possibleHue, out float _))
                        {
                            break; // 到达下一个hue
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error extracting colors for DeltaE {deltaE}, Hue {hue}: {e.Message}");
        }
        
        return colors;
    }

    void AssignColorsToMaterials()
    {
        if (materials == null || materials.Length == 0 || colorDatabase == null)
        {
            Debug.LogWarning("Materials array is empty or color data not loaded!");
            return;
        }

        // 查找当前deltaE的所有可用颜色组
        var availableGroups = colorDatabase.Keys
            .Where(key => key.StartsWith($"{selectedDeltaE:F1}-") && colorDatabase[key].Count > 0)
            .ToList();

        if (availableGroups.Count == 0)
        {
            Debug.LogError($"No color groups found for DeltaE {selectedDeltaE}!");
            Debug.Log($"Available keys: {string.Join(", ", colorDatabase.Keys.Take(10))}");
            return;
        }

        Debug.Log($"Found {availableGroups.Count} color groups for DeltaE {selectedDeltaE}");

        // 为每个材质分配颜色
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;

            // 随机选择一个颜色组
            string randomGroup = availableGroups[Random.Range(0, availableGroups.Count)];
            var colors = colorDatabase[randomGroup];
            
            if (colors.Count == 0) continue;

            // 随机选择一个颜色
            Color randomColor = colors[Random.Range(0, colors.Count)];

            // 设置材质颜色
            if (materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", randomColor);
            }
            else if (materials[i].HasProperty("_Color"))
            {
                materials[i].SetColor("_Color", randomColor);
            }

            Debug.Log($"Material {i}: Assigned color RGB({randomColor.r:F2},{randomColor.g:F2},{randomColor.b:F2}) from {randomGroup}");
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReassignColors();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeDeltaE(1.0f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeDeltaE(2.0f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeDeltaE(3.0f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeDeltaE(4.0f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ChangeDeltaE(5.0f);
    }
}
