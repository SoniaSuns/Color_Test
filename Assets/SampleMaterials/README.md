# Color Manager 使用说明 (Updated for offset_colors_white.json)

## 功能概述
ColorManager 系统现在支持 `offset_colors_white.json` 文件格式，可以根据不同的 Delta E 值对颜色进行分组，然后为材质随机分配颜色。

## 新数据格式
`offset_colors_white.json` 文件结构：
```json
{
  "1.0": {    // Delta E 值 (1.0-5.0)
    "0.0": [  // Hue 值 (0, 40, 80, 120, 160, 200, 240, 280, 320)
      {
        "hsv": [H, S, V],      // HSV颜色值
        "rgb": [R, G, B],      // RGB颜色值
        "lab": [L, A, B],      // LAB颜色值
        "delta_e": 实际delta_e值 // 实际的Delta E值
      }
    ]
  }
}
```

## 文件结构
- `ColorManager.cs` - 主要的颜色管理脚本（支持新格式）
- `SimpleColorManager.cs` - 简化版本的管理脚本
- `ColorTestSetup.cs` - 测试设置脚本（可选）
- `offset_colors_white.json` - 颜色数据文件（需要放在 Resources 文件夹中）

## 使用步骤

### 1. 基本设置
1. 将 `ColorManager.cs` 脚本添加到场景中的任意 GameObject 上
2. 确保 `offset_colors_white.json` 文件位于 `Assets/Resources/` 文件夹中
3. 在 ColorManager 组件中设置 `Objects` 数组，包含你想要改变颜色的GameObject

### 2. 参数配置
- **Objects**: 包含Renderer组件的GameObject数组，系统会自动提取它们的材质
- **Selected Delta E**: 选择使用的 Delta E 值（1.0, 2.0, 3.0, 4.0, 5.0）

### 3. 运行时控制
- **空格键**: 重新随机分配颜色
- **数字键 1-5**: 切换不同的 Delta E 值（1.0-5.0）

## 颜色分配逻辑

1. **加载数据**: 从 JSON 文件中加载所有颜色数据
2. **解析结构**: 按 Delta E 值和 Hue 值组织颜色数据
3. **选择**: 根据设定的 Delta E 值选择对应的颜色组
4. **随机分配**: 
   - 为每个材质随机选择一个 Hue 值（0°, 40°, 80°, 120°, 160°, 200°, 240°, 280°, 320°）
   - 在该 Hue 下随机选择一个颜色样本
   - 直接使用 RGB 颜色值应用到材质

## 支持的 Hue 值
系统支持以下预定义的 Hue 值：
- 0° (红色)
- 40° (橙色)
- 80° (黄绿色)
- 120° (绿色)
- 160° (青绿色)
- 200° (青色)
- 240° (蓝色)
- 280° (紫色)
- 320° (品红色)

## Delta E 值范围
- **1.0**: 非常微小的颜色差异
- **2.0**: 轻微的颜色差异
- **3.0**: 中等的颜色差异
- **4.0**: 明显的颜色差异
- **5.0**: 显著的颜色差异

## 快速测试
如果你想快速测试功能：
1. 将 `ColorTestSetup.cs` 添加到场景中的空 GameObject 上
2. 运行场景，它会自动创建测试对象和设置 ColorManager
3. 使用键盘控制来测试不同的颜色分配

## 注意事项
- 确保材质支持 `_BaseColor` 属性（URP材质）
- JSON 文件必须放在 Resources 文件夹中以便运行时加载
- Delta E 值现在支持 1.0, 2.0, 3.0, 4.0, 5.0
- 系统会直接使用RGB值，无需HSV转换

## 调试信息
- 在Inspector中查看 `Loaded Data Info` 字段来确认数据加载状态
- Console会显示每个材质的颜色分配详情
- 如果指定的Delta E值不存在，系统会自动选择最接近的值

## 扩展功能
- 可以修改 `ParseJsonManually()` 方法来支持更多Hue值
- 可以添加颜色过渡动画
- 可以实现颜色历史记录功能
- 可以添加自定义颜色过滤条件
