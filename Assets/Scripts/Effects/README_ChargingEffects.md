# 充能效果音效和震动系统

## 概述
为充能效果系统添加了音效播放和专业的摄像机震动功能，支持Cinemachine Impulse系统，提升用户体验的沉浸感。

## 新增功能

### 1. 音效系统集成
- **充能按钮按下时**: 播放"Energy"音效
- **充能完成时**: 优先播放"EnergyComplete"音效，如果不存在则播放"Energy"音效

### 2. 专业摄像机震动效果
- **双模式支持**: 优先使用Cinemachine Impulse，备用传统Transform震动
- **充能按钮按下时**: 短暂轻微震动（0.2秒，强度0.05）
- **充能完成时**: 较强震动（0.5秒，强度0.15）
- **自动配置**: 自动检测和配置Cinemachine组件

## 设置步骤

### 1. 添加CameraShake脚本
将`CameraShake.cs`脚本添加到主摄像机GameObject上。

### 2. 配置Cinemachine（推荐）
为了获得最佳震动效果，建议设置Cinemachine：
1. 创建CinemachineVirtualCamera
2. 在Virtual Camera上添加CinemachineImpulseListener组件
3. CameraShake脚本会自动配置其余组件

详细设置请参考：`README_CinemachineSetup.md`

### 3. 配置音效
在AudioManager的sounds数组中添加以下音效：
- **Energy**: 充能时播放的音效
- **EnergyComplete**: 充能完成时播放的音效（可选）

如果没有EnergyComplete音效，系统会自动使用Energy音效作为备用。

## 代码修改说明

### RgbController.cs
- `HandleChargingEffect()`: 添加了音效播放和震动触发
- `OnChargingComplete()`: 添加了充能完成时的特殊音效和震动

### AudioManager.cs
- 新增`HasSound()`方法：检查指定音效是否存在

### CameraShake.cs (重写)
- **双模式支持**: Cinemachine Impulse + 传统Transform震动
- **自动配置**: 自动查找和配置Cinemachine组件
- **Yarn命令支持**: 支持在对话中触发震动
- **单例模式**: 确保全局唯一实例

## 震动系统特性

### Cinemachine模式（推荐）
- 更自然的震动效果和衰减曲线
- 不直接修改Transform，避免冲突
- 支持多个冲击源叠加
- 更好的性能表现

### 传统模式（备用）
- 无需额外组件，兼容性好
- 自动降级，确保系统稳定性

## Yarn命令支持

### 摄像机震动命令
```yarn
// 使用默认参数震动
<<camera_shake>>

// 自定义震动参数（持续时间，强度）
<<camera_shake 0.5 0.2>>

// 停止震动
<<camera_shake_stop>>
```

## 调试选项
所有相关脚本都包含调试日志选项，可以在Inspector中启用/禁用：
- RgbController: `enableDebugLogs`
- CameraShake: `enableDebugLogs`

## 使用示例

### 在代码中手动触发震动
```csharp
// 轻微震动
CameraShake.Instance.Shake(0.2f, 0.05f);

// 强烈震动
CameraShake.Instance.Shake(0.5f, 0.15f);

// 检查当前使用的震动模式
bool usingCinemachine = CameraShake.Instance.IsUsingCinemachine();
```

## 注意事项
1. **Cinemachine设置**: 确保Virtual Camera上有ImpulseListener组件
2. **音效配置**: 确保AudioManager实例存在且已正确配置音效
3. **自动降级**: 如果Cinemachine组件不完整，会自动切换到传统模式
4. **系统稳定性**: 音效和震动效果可以独立工作，即使其中一个组件缺失也不会影响充能功能
5. **空值检查**: 所有效果都有完善的空值检查，确保系统稳定性

## 性能优化
- Cinemachine震动比传统Transform震动性能更好
- 音效播放前会检查是否已在播放，避免重复播放
- 震动效果会自然衰减，无需手动清理 