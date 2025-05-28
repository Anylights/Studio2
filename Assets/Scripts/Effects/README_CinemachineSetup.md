# Cinemachine摄像机震动快速设置指南

## 概述
本指南将帮助你快速设置Cinemachine摄像机震动系统，获得更专业的震动效果。

## 步骤1: 创建Virtual Camera

### 1.1 添加Virtual Camera
1. 在Hierarchy中右键点击
2. 选择 `Cinemachine` > `Virtual Camera`
3. 将新创建的Virtual Camera命名为"Main Virtual Camera"

### 1.2 配置Virtual Camera
1. 选择Virtual Camera
2. 在Inspector中设置：
   - **Follow**: 拖入你想要跟随的目标（可选）
   - **Look At**: 拖入你想要看向的目标（可选）
   - **Priority**: 设置为10（确保它是主要摄像机）

## 步骤2: 添加Impulse Listener

### 2.1 在Virtual Camera上添加组件
1. 选择Virtual Camera
2. 点击 `Add Component`
3. 搜索并添加以下任一组件：
   - `Cinemachine Impulse Listener` (推荐)
   - `Cinemachine Independent Impulse Listener` (替代选项)

### 2.2 配置Impulse Listener
在Impulse Listener组件中：
- **Use 2D Distance**: 如果是2D游戏，勾选此选项
- **Channel Filter**: 保持默认（-1表示监听所有频道）

**注意**: 如果使用`Independent Impulse Listener`，确保它的设置与Impulse Source的频道匹配。

## 步骤3: 配置CameraShake脚本

### 3.1 添加CameraShake脚本
1. 选择主摄像机（Main Camera）
2. 添加 `CameraShake` 脚本

### 3.2 配置参数
在CameraShake组件中：
- **Use Cinemachine**: ✅ 勾选
- **Default Duration**: 0.3（默认震动时长）
- **Default Magnitude**: 0.1（默认震动强度）
- **Enable Debug Logs**: ✅ 勾选（用于调试）

脚本会自动：
- 查找Virtual Camera
- 添加Impulse Source组件
- 配置震动系统

## 步骤4: 测试震动效果

### 4.1 在代码中测试
```csharp
// 触发震动
CameraShake.Instance.Shake(0.5f, 0.2f);
```

### 4.2 在Yarn对话中测试
```yarn
<<camera_shake 0.5 0.2>>
```

### 4.3 检查控制台
如果启用了调试日志，你应该看到：
```
CameraShake初始化完成，使用模式：Cinemachine
摄像机震动开始：持续时间=0.5s, 强度=0.2, 模式=Cinemachine
```

## 常见问题解决

### Q: 震动没有效果
**A: 检查以下项目：**
1. Virtual Camera是否有Impulse Listener组件
2. Virtual Camera的Priority是否足够高
3. 主摄像机是否被Virtual Camera控制
4. 控制台是否显示"使用模式：Cinemachine"
5. **频道匹配**: 确保Impulse Source和Impulse Listener的Channel设置匹配

### Q: 控制台显示"未找到CinemachineVirtualCamera"
**A: 解决方案：**
1. 确保场景中有Virtual Camera
2. 确保Virtual Camera是激活状态
3. 重新运行游戏

### Q: 使用Independent Impulse Listener但没有效果
**A: 检查频道设置：**
1. 在CameraShake脚本的Impulse Source中，记下Channel值（默认为0）
2. 在Independent Impulse Listener中，设置Channel Filter为相同值
3. 或者将两者都设置为-1（监听所有频道）

### Q: 想要调整震动效果
**A: 可以调整以下参数：**
1. **震动强度**: 调用Shake()时的magnitude参数
2. **震动时长**: 调用Shake()时的duration参数
3. **衰减曲线**: 在Impulse Source的Impulse Definition中调整

### Q: 想要验证设置是否正确
**A: 启用调试日志：**
1. 在CameraShake组件中勾选"Enable Debug Logs"
2. 运行游戏并触发震动
3. 查看控制台输出：
   ```
   CameraShake初始化完成，使用模式：Cinemachine
   已自动添加CinemachineImpulseSource组件
   Cinemachine冲击已触发: 方向=(x,y,z), 强度=0.2, 持续时间=0.5
   ```

## 高级配置

### 自定义震动曲线
1. 选择有CameraShake脚本的摄像机
2. 展开 `Impulse Source` > `Impulse Definition`
3. 调整 `Impulse Shape` 和 `Custom Impulse Shape` 来自定义震动曲线

### 多频道震动
如果需要不同类型的震动效果：
1. 在Impulse Source中设置不同的 `Channel`
2. 在Impulse Listener中设置对应的 `Channel Filter`
3. 在代码中可以创建多个Impulse Source来处理不同类型的震动

## 性能优化建议
- Cinemachine震动比传统Transform震动性能更好
- 避免同时触发过多震动效果
- 合理设置震动强度，避免过度震动影响用户体验 