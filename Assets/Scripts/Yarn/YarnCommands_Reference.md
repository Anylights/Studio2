# Yarn命令参考文档

## 对话系统命令

### jump_to_node
**功能**: 跳转到指定的对话节点  
**参数**: 
- `nodeName` (string): 目标节点名称  
**示例**: `<<jump_to_node "StartGame">>`

---

## 游戏管理命令

### start_game
**功能**: 开始游戏，自动判断加载存档或新游戏  
**参数**: 无  
**示例**: `<<start_game>>`

### quit_game
**功能**: 退出游戏  
**参数**: 无  
**示例**: `<<quit_game>>`

### change_scene
**功能**: 切换场景并启动指定对话节点  
**参数**: 
- `sceneName` (string): 目标场景名称
- `nodeName` (string): 对话节点名称  
**示例**: `<<change_scene "Level1_lab" "lab_start">>`

### goto_scene
**功能**: 切换场景（`change_scene`的别名）  
**参数**: 
- `sceneName` (string): 目标场景名称
- `nodeName` (string): 对话节点名称，默认"Start"  
**示例**: `<<goto_scene "MainMenu">>`

---

## 存档系统命令

### save_game
**功能**: 保存当前游戏状态  
**参数**: 无  
**示例**: `<<save_game>>`

### reset_game
**功能**: 重置游戏，删除存档并清空变量  
**参数**: 无  
**示例**: `<<reset_game>>`

---

## 音频管理命令

### play_sound
**功能**: 播放指定音频  
**参数**: 
- `soundName` (string): 音效名称
- `volume` (float): 音量，默认1.0
- `loop` (bool): 是否循环，默认false  
**示例**: `<<play_sound "BGM" 0.8 true>>`

### pause_sound
**功能**: 暂停指定音频  
**参数**: 
- `soundName` (string): 音效名称  
**示例**: `<<pause_sound "BGM">>`

### unpause_sound
**功能**: 恢复播放指定音频  
**参数**: 
- `soundName` (string): 音效名称  
**示例**: `<<unpause_sound "BGM">>`

### set_volume
**功能**: 设置指定音频音量  
**参数**: 
- `soundName` (string): 音效名称
- `volume` (float): 音量值  
**示例**: `<<set_volume "BGM" 0.5>>`

### stop_sound
**功能**: 停止指定音频  
**参数**: 
- `soundName` (string): 音效名称  
**示例**: `<<stop_sound "BGM">>`

### stop_sound_fade
**功能**: 淡出停止指定音频  
**参数**: 
- `soundName` (string): 音效名称
- `fadeDuration` (float): 淡出时长，默认1.0秒  
**示例**: `<<stop_sound_fade "BGM" 2.0>>`

### stop_all_sounds
**功能**: 立即停止所有音频  
**参数**: 无  
**示例**: `<<stop_all_sounds>>`

### stop_all_sounds_fade
**功能**: 淡出停止所有音频  
**参数**: 
- `fadeDuration` (float): 淡出时长，默认1.0秒  
**示例**: `<<stop_all_sounds_fade 3.0>>`

---

## 角色动画命令

### play_animation
**功能**: 播放角色动画触发器  
**参数**: 
- `characterName` (string): 角色名称
- `triggerName` (string): 动画触发器名称  
**示例**: `<<play_animation "Player" "Attack">>`

### stop_animation
**功能**: 停止角色动画触发器  
**参数**: 
- `characterName` (string): 角色名称
- `triggerName` (string): 动画触发器名称  
**示例**: `<<stop_animation "Player" "Attack">>`

### set_animation_bool
**功能**: 设置角色动画布尔参数  
**参数**: 
- `characterName` (string): 角色名称
- `paramName` (string): 参数名称
- `value` (bool): 布尔值  
**示例**: `<<set_animation_bool "Player" "IsRunning" true>>`

### set_animation_int
**功能**: 设置角色动画整数参数  
**参数**: 
- `characterName` (string): 角色名称
- `paramName` (string): 参数名称
- `value` (int): 整数值  
**示例**: `<<set_animation_int "Player" "Health" 100>>`

### set_animation_float
**功能**: 设置角色动画浮点参数  
**参数**: 
- `characterName` (string): 角色名称
- `paramName` (string): 参数名称
- `value` (float): 浮点值  
**示例**: `<<set_animation_float "Player" "Speed" 5.5>>`

---

## LED灯带控制命令

### set_option_colors
**功能**: 设置对话选项灯带颜色  
**参数**: 
- `strip1ColorHex` (string): 第一条灯带颜色（十六进制）
- `strip2ColorHex` (string): 第二条灯带颜色（十六进制）  
**示例**: `<<set_option_colors "#FF0000" "#00FF00">>`

### set_button_mapping
**功能**: 设置按钮对应的选项索引  
**参数**: 
- `redButtonOption` (int): 红按钮对应的选项索引
- `greenButtonOption` (int): 绿按钮对应的选项索引  
**示例**: `<<set_button_mapping 0 1>>`

### set_pulse_effect
**功能**: 设置全局脉冲效果类型  
**参数**: 
- `effectType` (string): 效果类型（"default", "rainbow", "bounce", "flash", "gradient"）  
**示例**: `<<set_pulse_effect "rainbow">>`

### set_strip_pulse_effects
**功能**: 分别设置两条灯带的脉冲效果  
**参数**: 
- `strip1Effect` (string): 第一条灯带效果类型
- `strip2Effect` (string): 第二条灯带效果类型  
**示例**: `<<set_strip_pulse_effects "default" "rainbow">>`

### set_single_strip_pulse_effect
**功能**: 设置指定灯带的脉冲效果  
**参数**: 
- `stripIndex` (int): 灯带索引（0或1）
- `effectType` (string): 效果类型  
**示例**: `<<set_single_strip_pulse_effect 0 "bounce">>`

### set_gradient_pulse_params
**功能**: 设置指定灯带的渐变脉冲效果参数  
**参数**: 
- `stripIndex` (int): 灯带索引（0或1）
- `startColorHex` (string): 起始颜色（十六进制）
- `endColorHex` (string): 结束颜色（十六进制）
- `duration` (int): 持续时间（毫秒），默认200  
**示例**: `<<set_gradient_pulse_params 0 "#FF0000" "#0000FF" 300>>`

### set_both_gradient_pulse_params
**功能**: 同时设置两条灯带的渐变脉冲效果参数  
**参数**: 
- `strip1StartColor` (string): 第一条灯带起始颜色（十六进制）
- `strip1EndColor` (string): 第一条灯带结束颜色（十六进制）
- `strip1Duration` (int): 第一条灯带持续时间（毫秒）
- `strip2StartColor` (string): 第二条灯带起始颜色（十六进制）
- `strip2EndColor` (string): 第二条灯带结束颜色（十六进制）
- `strip2Duration` (int): 第二条灯带持续时间（毫秒）  
**示例**: `<<set_both_gradient_pulse_params "#FF0000" "#0000FF" 300 "#00FF00" "#FF00FF" 500>>`

### gradient_pulse_effect
**功能**: 直接触发渐变脉冲效果  
**参数**: 
- `stripIndex` (int): 灯带索引（0或1）
- `startColorHex` (string): 起始颜色（十六进制）
- `endColorHex` (string): 结束颜色（十六进制）
- `durationSeconds` (float): 持续时间（秒），默认1.0  
**示例**: `<<gradient_pulse_effect 0 "#FF0000" "#00FF00" 2.0>>`

### start_charging_effect
**功能**: 启动充能效果  
**参数**: 
- `stripIndex` (int): 灯带索引（0或1）
- `targetMapping` (int): 充能完成后的按钮映射
- `pushDistance` (float): 推进距离，默认8.0
- `pushSpeed` (float): 推进速度，默认20.0
- `decaySpeed` (float): 衰减速度，默认3.0  
**示例**: `<<start_charging_effect 0 1 10 25 5>>`

### stop_charging_effect
**功能**: 停止充能效果  
**参数**: 
- `stripIndex` (int): 灯带索引（0或1）  
**示例**: `<<stop_charging_effect 0>>`

### set_selection_delay
**功能**: 设置选项选择后的延迟时间（等待Arduino脉冲效果完成的时间）  
**参数**: 
- `delay` (float): 延迟时间（秒），必须大于等于0  
**示例**: `<<set_selection_delay 1.0>>`

---

## 摄像机效果命令

### camera_shake
**功能**: 触发摄像机震动  
**参数**: 
- `duration` (float): 震动持续时间，默认0.3秒
- `magnitude` (float): 震动强度，默认0.1  
**示例**: `<<camera_shake 0.5 0.2>>`

### camera_shake_stop
**功能**: 停止摄像机震动  
**参数**: 无  
**示例**: `<<camera_shake_stop>>`

---

## 时间线播放命令

### play_timeline
**功能**: 播放指定时间线并等待完成  
**参数**: 
- `timelineId` (string): 时间线ID  
**示例**: `<<play_timeline "CutScene01">>`

---

## 测试命令

### fade_camera
**功能**: 摄像机淡入淡出（测试用）  
**参数**: 无  
**示例**: `<<fade_camera>>`

---

## 系统内置命令

### wait
**功能**: 等待指定时间后继续对话  
**参数**: 
- `duration` (float): 等待时间（秒）  
**示例**: `<<wait 2.5>>`

---

## 时间管理命令

**注意**: 使用时间命令前，需要在场景中的任意GameObject上添加`TimeCommands`组件。

### get_datetime
**功能**: 获取当前完整日期时间并设置为Yarn变量  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）
- `format` (string): 时间格式，默认"yyyy-MM-dd HH:mm:ss"  
**示例**: `<<get_datetime "current_time" "yyyy年MM月dd日 HH:mm:ss">>`

### get_time
**功能**: 获取当前时间（仅时间部分）并设置为Yarn变量  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）
- `format` (string): 时间格式，默认"HH:mm:ss"  
**示例**: `<<get_time "now_time" "HH:mm">>`

### get_date
**功能**: 获取当前日期（仅日期部分）并设置为Yarn变量  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）
- `format` (string): 日期格式，默认"yyyy-MM-dd"  
**示例**: `<<get_date "today" "MM/dd/yyyy">>`

### get_timestamp
**功能**: 获取Unix时间戳并设置为Yarn变量（浮点数类型）  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）  
**示例**: `<<get_timestamp "timestamp">>`

### get_hour
**功能**: 获取当前小时数（0-23）并设置为Yarn变量  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）  
**示例**: `<<get_hour "current_hour">>`

### get_weekday
**功能**: 获取当前星期几并设置为Yarn变量（0=周日，1=周一，...，6=周六）  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）  
**示例**: `<<get_weekday "day_of_week">>`

### start_countdown
**功能**: 开始一个基于真实物理时间的倒计时  
**参数**: 
- `durationInSeconds` (float): 倒计时持续时间（秒）
- `targetNodeName` (string): 倒计时结束后要跳转的节点名（可选）  
**示例**: `<<start_countdown 300 "death_scene">>` （5分钟后跳转到death_scene节点）

### stop_countdown
**功能**: 停止当前运行的倒计时  
**参数**: 无  
**示例**: `<<stop_countdown>>`

### get_countdown_remaining
**功能**: 获取倒计时剩余时间并设置为Yarn变量  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）  
**示例**: `<<get_countdown_remaining "remaining_time">>`

### is_countdown_active
**功能**: 检查倒计时是否正在运行并设置为Yarn变量  
**参数**: 
- `variableName` (string): 要设置的变量名（会自动添加$前缀）  
**示例**: `<<is_countdown_active "countdown_running">>`

**时间格式说明**:
- `yyyy`: 四位年份 (2024)
- `MM`: 两位月份 (01-12)
- `dd`: 两位日期 (01-31)
- `HH`: 24小时制小时 (00-23)
- `mm`: 分钟 (00-59)
- `ss`: 秒 (00-59)

**使用示例**:
```yarn
// 在Yarn文件开头声明变量
<<declare $current_time = "">>
<<declare $hour = 0.0>>

// 获取当前时间
<<get_time "current_time" "HH:mm">>
<<get_hour "hour">>

现在是{$current_time}。
<<if $hour < 12>>
早上好！
<<else>>
下午好！
<<endif>>
```

**倒计时功能使用示例**:
```yarn
// 声明倒计时相关变量
<<declare $remaining_time = 0.0>>
<<declare $countdown_active = false>>

莎文: 我会在五分钟后死去。
// 开始5分钟（300秒）倒计时，结束后跳转到death_scene节点
<<start_countdown 300 "death_scene">>

// 在其他地方可以检查倒计时状态
<<is_countdown_active "countdown_active">>
<<if $countdown_active == true>>
    // 获取剩余时间并显示
    <<get_countdown_remaining "remaining_time">>
    还剩{$remaining_time}秒...
    
    // 如果需要取消倒计时
    <<stop_countdown>>
<<endif>>
```

**创建death_scene节点来处理倒计时结束**:
```yarn
title: death_scene
---
// 倒计时结束后执行的逻辑
莎文突然倒下了...
// 这里可以添加死亡相关的逻辑
===
```

---

## 使用说明

1. **颜色格式**: 使用标准十六进制格式，如 "#FF0000"（红色）
2. **布尔值**: 使用 true/false
3. **索引值**: 灯带索引为0或1，选项索引从0开始
4. **效果类型**: "default", "rainbow", "bounce", "flash", "gradient"
5. **持续时间**: 渐变效果的持续时间使用毫秒为单位，如300表示300毫秒
6. **音效名称**: 需与AudioManager中配置的音效名称一致
7. **角色名称**: 需与CharacterAnimationManager中配置的角色名称一致 