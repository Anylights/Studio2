# CameraShake 摄像机震动系统

## 概述
CameraShake脚本提供了专业的摄像机震动效果，支持Cinemachine Impulse系统和传统Transform震动两种模式，可在代码中直接调用和在Yarn对话中使用。

## 功能特性
- **双模式支持**: 自动检测并优先使用Cinemachine Impulse，备用传统Transform震动
- **自动配置**: 自动查找和配置Cinemachine组件
- **Yarn集成**: 支持在对话中触发震动效果
- **灵活控制**: 可动态切换震动模式

## 设置步骤

### 1. 添加脚本到摄像机
将`CameraShake.cs`脚本添加到主摄像机GameObject上。

### 2. Cinemachine设置（推荐）
如果使用Cinemachine：
1. 确保场景中有`CinemachineVirtualCamera`
2. 脚本会自动添加`CinemachineImpulseSource`组件
3. 在Virtual Camera上添加`CinemachineImpulseListener`组件

### 3. 配置参数
在Inspector中可以调整以下参数：

#### 基础设置
- **Default Duration**: 默认震动持续时间（秒）
- **Default Magnitude**: 默认震动强度
- **Enable Debug Logs**: 是否启用调试日志

#### Cinemachine设置
- **Use Cinemachine**: 是否优先使用Cinemachine（推荐开启）
- **Impulse Source**: CinemachineImpulseSource组件（可自动配置）
- **Virtual Camera**: CinemachineVirtualCamera引用（可自动查找）

## 使用方法

### 在代码中使用
```csharp
// 使用默认参数震动
CameraShake.Instance.Shake();

// 自定义震动参数
CameraShake.Instance.Shake(0.5f, 0.2f); // 持续0.5秒，强度0.2

// 停止震动
CameraShake.Instance.StopShake();

// 检查是否正在震动
bool isShaking = CameraShake.Instance.IsShaking();

// 切换震动模式
CameraShake.Instance.SetUseCinemachine(true); // 使用Cinemachine
CameraShake.Instance.SetUseCinemachine(false); // 使用传统Transform

// 检查当前模式
bool usingCinemachine = CameraShake.Instance.IsUsingCinemachine();
```

### 在Yarn对话中使用
```yarn
// 触发震动（使用默认参数）
<<camera_shake>>

// 触发自定义震动
<<camera_shake 0.5 0.2>>

// 停止震动
<<camera_shake_stop>>
```

## 震动模式对比

### Cinemachine Impulse模式（推荐）
**优点:**
- 更自然的震动效果
- 支持复杂的衰减曲线
- 不直接修改Transform，避免与其他系统冲突
- 支持多个冲击源叠加
- 更好的性能

**要求:**
- 需要CinemachineVirtualCamera
- 需要CinemachineImpulseListener

### 传统Transform模式（备用）
**优点:**
- 无需额外组件
- 简单直接
- 兼容性好

**缺点:**
- 直接修改Transform可能与其他系统冲突
- 震动效果相对简单

## 充能系统集成
在充能效果中，震动会自动触发：
- **按钮按下时**: 短暂轻微震动（0.2秒，强度0.05）
- **充能完成时**: 较强震动（0.5秒，强度0.15）

## 自动配置说明
脚本会在Start()时自动执行以下配置：
1. 检查是否启用Cinemachine模式
2. 查找或添加CinemachineImpulseSource组件
3. 查找场景中的CinemachineVirtualCamera
4. 如果Cinemachine组件不完整，自动切换到传统模式

## 注意事项
- 脚本使用单例模式，确保场景中只有一个CameraShake实例
- Cinemachine模式需要在Virtual Camera上手动添加CinemachineImpulseListener组件
- 如果场景中没有Virtual Camera，会自动切换到传统模式
- 两种模式可以在运行时动态切换
- 传统模式基于摄像机的localPosition，确保摄像机不是场景根对象的直接子对象
- 如果需要更复杂的震动效果，可以修改DoShake协程中的算法 