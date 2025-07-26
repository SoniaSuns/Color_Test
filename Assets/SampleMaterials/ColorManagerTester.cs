using UnityEngine;
using System.Collections.Generic;

public class ColorManagerTester : MonoBehaviour
{
    [Header("Test Settings")]
    public ColorManager colorManager;
    public bool runTests = false;
    
    void Start()
    {
        if (runTests && colorManager != null)
        {
            StartCoroutine(RunTests());
        }
    }
    
    System.Collections.IEnumerator RunTests()
    {
        yield return new WaitForSeconds(1f); // 等待ColorManager初始化
        
        Debug.Log("=== Color Manager Tests ===");
        
        // 测试不同的DeltaE值
        float[] testDeltaE = { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
        
        foreach (float deltaE in testDeltaE)
        {
            Debug.Log($"Testing DeltaE: {deltaE}");
            colorManager.ChangeDeltaE(deltaE);
            yield return new WaitForSeconds(2f); // 给时间观察颜色变化
        }
        
        // 测试多次随机分配
        Debug.Log("Testing random reassignment...");
        for (int i = 0; i < 5; i++)
        {
            colorManager.ReassignColors();
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log("=== Tests Complete ===");
    }
    
    void OnGUI()
    {
        if (colorManager == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        
        GUILayout.Label("Color Manager Tester", GUI.skin.box);
        
        GUILayout.Label($"Current Delta E: {colorManager.selectedDeltaE}");
        
        if (GUILayout.Button("Test Delta E 1.0"))
        {
            colorManager.ChangeDeltaE(1.0f);
        }
        
        if (GUILayout.Button("Test Delta E 2.0"))
        {
            colorManager.ChangeDeltaE(2.0f);
        }
        
        if (GUILayout.Button("Test Delta E 3.0"))
        {
            colorManager.ChangeDeltaE(3.0f);
        }
        
        if (GUILayout.Button("Test Delta E 4.0"))
        {
            colorManager.ChangeDeltaE(4.0f);
        }
        
        if (GUILayout.Button("Test Delta E 5.0"))
        {
            colorManager.ChangeDeltaE(5.0f);
        }
        
        if (GUILayout.Button("Random Reassign"))
        {
            colorManager.ReassignColors();
        }
        
        GUILayout.EndArea();
    }
}
