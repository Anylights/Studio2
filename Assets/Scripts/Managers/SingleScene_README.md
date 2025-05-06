# 单场景模式转换指南

## 简介

本项目已从多场景模式转换为单场景模式，以解决Uduino在场景切换时的连接问题。本文档提供完整的设置和使用指南。

## 主要变更

1. 不再使用Unity的场景加载系统，而是将所有内容放在同一个场景中
2. 使用GameObject的激活/停用来模拟场景切换
3. SceneContentManager负责管理不同"场景内容"的显示与隐藏
4. SaveSystem已修改为使用新的场景内容切换方式

## 设置步骤

### 1. 创建主场景

1. 创建一个新的空场景作为主场景
2. 添加以下父物体结构:
   - SceneManagers (用于所有管理器)
     - SceneContentManager
     - SaveSystem
     - UduinoManager (单一实例)
   - CommonContent (所有场景共享的内容)
     - Camera
     - Lights
     - UI
   - SceneContents (所有场景内容)
     - Start_Scene (第一个场景内容)
     - Lab_Scene (第二个场景内容)
     - ... (其他场景内容)

### 2. 导入场景内容

对于每个现有场景:

1. 打开原始场景
2. 选择所有场景特定物体
3. 创建一个空物体作为父物体，并将其命名为场景名称
4. 将所选物体设为该父物体的子物体
5. 复制整个父物体
6. 切换到主场景
7. 粘贴到SceneContents下
8. 确保所有非当前场景的内容都被设置为禁用状态

### 3. 配置SceneContentManager

1. 选择SceneContentManager物体
2. 在Inspector面板中:
   - 添加所有场景内容到列表中
   - 设置初始场景名称
   - 创建和配置过渡画面

### 4. 测试场景切换

1. 确保Uduino实例已正确配置
2. 运行主场景
3. 测试场景内容切换功能
4. 验证Uduino连接是否保持稳定

## 使用方法

### 切换场景内容

在Yarn脚本中，继续使用之前的命令:

```yarn
<<change_scene "Lab_Scene" "lab">>
```

这个命令现在会切换场景内容而非加载新场景。

### 管理Uduino连接

由于现在使用单场景模式，Uduino连接在整个游戏过程中都会保持稳定。无需任何特殊处理!

### 查看当前场景内容

可以通过SceneContentManager查询当前场景内容:

```csharp
string currentScene = SceneContentManager.Instance.GetCurrentSceneName();
```

## 故障排除

### 场景内容切换问题

- 确保SceneContentManager正确配置
- 检查场景内容的父GameObject名称与配置中的名称一致
- 验证过渡画面是否正确设置

### Uduino连接问题

如果仍有连接问题:

1. 检查Arduino设备是否正确连接
2. 确认使用的是单一Uduino实例
3. 重启Unity编辑器和Arduino设备

## 注意事项

1. 不要使用Unity的场景加载系统(SceneManager.LoadScene)
2. 确保所有Uduino相关代码仅引用单一实例
3. 添加新场景内容时，记得更新SceneContentManager配置 