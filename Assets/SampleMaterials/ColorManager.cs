using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SpeedTree.Importer;

public class ColorManager : MonoBehaviour
{
    [Header("Color Settings")]
    public GameObject[]objects;
    [Range(0.1f, 0.5f)]
    public float selectedDeltaE = 0.1f;

    public Material[] materials;
    
    [Header("Debug Info")]
    [SerializeField] private string loadedDataInfo;
    
    private ColorData colorData;
    private Dictionary<float, List<HueGroup>> deltaEGroups;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        materials = objects.Select(obj => obj.GetComponent<Renderer>()?.material).Where(mat => mat != null).ToArray();
        LoadColorData();
        OrganizeColorsByDeltaE();
        AssignColorsToMaterials();
        
    }

    void LoadColorData()
    {
        // 从Resources或StreamingAssets加载JSON文件
        TextAsset jsonFile = Resources.Load<TextAsset>("samplecolor");
        if (jsonFile == null)
        {
            // 如果Resources中没有，尝试从SampleMaterials文件夹读取
            string path = Application.dataPath + "/SampleMaterials/samplecolor.json";
            if (System.IO.File.Exists(path))
            {
                string jsonContent = System.IO.File.ReadAllText(path);
                colorData = JsonUtility.FromJson<ColorData>(jsonContent);
                loadedDataInfo = $"Loaded {colorData.data.Length} deltaE groups from file";
            }
            else
            {
                Debug.LogError("Cannot find samplecolor.json file!");
                return;
            }
        }
        else
        {
            colorData = JsonUtility.FromJson<ColorData>(jsonFile.text);
            loadedDataInfo = $"Loaded {colorData.data.Length} deltaE groups from Resources";
        }
    }

    void OrganizeColorsByDeltaE()
    {
        if (colorData == null) return;
        
        deltaEGroups = new Dictionary<float, List<HueGroup>>();
        
        foreach (var deltaEGroup in colorData.data)
        {
            deltaEGroups[deltaEGroup.deltaE] = deltaEGroup.hues.ToList();
        }
        
        Debug.Log($"Organized colors into {deltaEGroups.Count} deltaE groups");
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
            return;
        }

        List<HueGroup> selectedHueGroups = deltaEGroups[selectedDeltaE];
        
        // 为每个材质分配颜色
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null) continue;

            // 随机选择一个hue组
            HueGroup randomHueGroup = selectedHueGroups[Random.Range(0, selectedHueGroups.Count)];
            
            // 从该hue组中随机选择一个颜色样本
            ColorSample randomSample = randomHueGroup.samples[Random.Range(0, randomHueGroup.samples.Length)];
            
            // 将HSV转换为RGB颜色
            Color newColor = Color.HSVToRGB(randomHueGroup.hue / 360f, randomSample.S, randomSample.V);
            
            // 设置材质的BaseColor
            if (materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", newColor);
            }
            // if (materials[i].HasProperty("_Color"))
            // {
            //     materials[i].SetColor("_Color", newColor);
            // }
            
            Debug.Log($"Material {i}: Assigned color from Hue {randomHueGroup.hue}, S:{randomSample.S:F2}, V:{randomSample.V:F4}");
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
    public DeltaEGroup[] data;
}

[System.Serializable]
public class DeltaEGroup
{
    public float deltaE;
    public HueGroup[] hues;
}

[System.Serializable]
public class HueGroup
{
    public float hue;
    public ColorSample[] samples;
}

[System.Serializable]
public class ColorSample
{
    public float S;
    public float V;
    public float deltaE;
}
