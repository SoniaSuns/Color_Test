using UnityEngine;

public class ColorTestSetup : MonoBehaviour
{
    [Header("Test Setup")]
    public GameObject prefab; // 可以是一个简单的Cube或Sphere
    public int objectCount = 8; // 创建的对象数量
    public float spacing = 2f; // 对象之间的间距
    
    private GameObject[] testObjects;
    private Material[] testMaterials;
    private ColorManager colorManager;
    
    void Start()
    {
        CreateTestObjects();
        SetupColorManager();
    }
    
    void CreateTestObjects()
    {
        testObjects = new GameObject[objectCount];
        testMaterials = new Material[objectCount];
        
        for (int i = 0; i < objectCount; i++)
        {
            // 创建测试对象
            GameObject obj;
            if (prefab != null)
            {
                obj = Instantiate(prefab);
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            }
            
            // 设置位置
            float x = (i % 4) * spacing;
            float z = (i / 4) * spacing;
            obj.transform.position = new Vector3(x, 0, z);
            obj.name = $"TestObject_{i}";
            
            // 创建材质实例
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                renderer.material = mat;
                testMaterials[i] = mat;
            }
            
            testObjects[i] = obj;
        }
    }
    
    void SetupColorManager()
    {
        // 获取或创建ColorManager
        colorManager = FindObjectOfType<ColorManager>();
        if (colorManager == null)
        {
            GameObject colorManagerObj = new GameObject("ColorManager");
            colorManager = colorManagerObj.AddComponent<ColorManager>();
        }
        
        // 分配材质数组
        colorManager.materials = testMaterials;
        
        Debug.Log($"Created {objectCount} test objects with materials for ColorManager");
    }
    
    // 在Inspector中添加按钮来重新分配颜色
    [ContextMenu("Reassign Colors")]
    public void ReassignColors()
    {
        if (colorManager != null)
        {
            colorManager.ReassignColors();
        }
    }
    
    void Update()
    {
        // 按R键重新分配颜色
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReassignColors();
        }
        
        // 按数字键切换不同的deltaE值
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            colorManager?.ChangeDeltaE(0.1f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            colorManager?.ChangeDeltaE(0.2f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            colorManager?.ChangeDeltaE(0.3f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            colorManager?.ChangeDeltaE(0.4f);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            colorManager?.ChangeDeltaE(0.5f);
        }
    }
    
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 20), "Controls:");
        GUI.Label(new Rect(10, 30, 300, 20), "Space: Random reassign colors");
        GUI.Label(new Rect(10, 50, 300, 20), "R: Reassign colors");
        GUI.Label(new Rect(10, 70, 300, 20), "1-5: Change Delta E (0.1-0.5)");
        
        if (colorManager != null)
        {
            GUI.Label(new Rect(10, 100, 300, 20), $"Current Delta E: {colorManager.selectedDeltaE}");
        }
    }
}
