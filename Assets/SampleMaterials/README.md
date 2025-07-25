# Color Manager 使用说明

## 功能概述
ColorManager 系统可以从 samplecolor.json 文件中读取颜色数据，根据不同的 Delta E 值对颜色进行分组，然后为材质随机分配颜色。

## 文件结构
- `ColorManager.cs` - 主要的颜色管理脚本
- `ColorTestSetup.cs` - 测试设置脚本（可选）
- `samplecolor.json` - 颜色数据文件（需要放在 Resources 文件夹中）

## 使用步骤

### 1. 基本设置
1. 将 `ColorManager.cs` 脚本添加到场景中的任意 GameObject 上
2. 确保 `samplecolor.json` 文件位于 `Assets/Resources/` 文件夹中
3. 在 ColorManager 组件中设置 `Materials` 数组，包含你想要改变颜色的材质

### 2. 参数配置
- **Materials**: 要应用颜色的材质数组
- **Selected Delta E**: 选择使用的 Delta E 值（0.1, 0.2, 0.3, 0.4, 0.5）

### 3. 运行时控制
- **空格键**: 重新随机分配颜色
- **数字键 1-5**: 切换不同的 Delta E 值（0.1-0.5）
- **R键**: 重新分配颜色（如果使用了 ColorTestSetup）

## 颜色分配逻辑

1. **加载数据**: 从 JSON 文件中加载所有颜色数据
2. **分组**: 按 Delta E 值将颜色分为不同组
3. **选择**: 根据设定的 Delta E 值选择对应的颜色组
4. **随机分配**: 
   - 为每个材质随机选择一个 Hue 值
   - 在该 Hue 下随机选择一个颜色样本
   - 将 HSV 颜色转换为 RGB 并应用到材质

## 数据格式
JSON 文件包含以下结构：
```json
{
  "data": [
    {
      "deltaE": 0.1,
      "hues": [
        {
          "hue": 0,
          "samples": [
            { "S": 0.10, "V": 0.002611, "deltaE": 0.099999 },
            ...
          ]
        },
        ...
      ]
    },
    ...
  ]
}
```

## 快速测试
如果你想快速测试功能：
1. 将 `ColorTestSetup.cs` 添加到场景中的空 GameObject 上
2. 运行场景，它会自动创建测试对象和设置 ColorManager
3. 使用键盘控制来测试不同的颜色分配

## 注意事项
- 确保材质支持 `_BaseColor` 或 `_Color` 属性
- JSON 文件必须放在 Resources 文件夹中以便运行时加载
- Delta E 值目前支持 0.1, 0.2, 0.3, 0.4, 0.5

## 扩展功能
- 可以在 `Update()` 方法中添加更多运行时控制
- 可以修改 `AssignColorsToMaterials()` 方法来实现不同的颜色分配策略
- 可以添加动画来平滑过渡颜色变化
