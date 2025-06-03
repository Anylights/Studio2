#include <Uduino.h>
#include <Adafruit_NeoPixel.h>

// 定义两个灯带的引脚和参数
#define LED_PIN_1 10 // 第一个灯带信号引脚
#define LED_PIN_2 11 // 第二个灯带信号引脚
#define LED_COUNT 52 // 每个灯带的灯珠数量

// 添加脉冲效果参数设置
#define PULSE_DURATION 200   // 脉冲效果持续时间(毫秒)
#define PULSE_TAIL_LENGTH 25 // 脉冲效果的尾部长度

// 创建两个灯带对象
Adafruit_NeoPixel strip1 = Adafruit_NeoPixel(LED_COUNT, LED_PIN_1, NEO_GRB + NEO_KHZ800);
Adafruit_NeoPixel strip2 = Adafruit_NeoPixel(LED_COUNT, LED_PIN_2, NEO_GRB + NEO_KHZ800);

Uduino uduino("uduinoBoard"); // Declare and name your object

// Servo
#include <Servo.h>
#define MAXSERVOS 8

void setup()
{
  Serial.begin(115200);

  strip1.begin();
  strip2.begin();

  strip1.setBrightness(100); // 设置为约20%亮度
  strip2.setBrightness(100);

  strip1.show();
  strip2.show();

#if defined(__AVR_ATmega32U4__) // Leonardo
  while (!Serial)
  {
  }
#elif defined(__PIC32MX__)
  delay(1000);
#endif

  uduino.addCommand("s", SetMode);
  uduino.addCommand("d", WritePinDigital);
  uduino.addCommand("a", WritePinAnalog);
  uduino.addCommand("rd", ReadDigitalPin);
  uduino.addCommand("r", ReadAnalogPin);
  uduino.addCommand("br", BundleReadPin);
  uduino.addCommand("b", ReadBundle);
  uduino.addInitFunction(DisconnectAllServos);
  uduino.addDisconnectedFunction(DisconnectAllServos);
  uduino.addCommand("SetColor", SetColor);             // 设置所有灯带相同颜色
  uduino.addCommand("SetStripColor", SetStripColor);   // 设置指定灯带的颜色
  uduino.addCommand("SetPixelColor", SetPixelColor);   // 设置指定灯带指定灯珠的颜色
  uduino.addCommand("PulseEffect", PulseEffect);       // 添加脉冲效果命令
  uduino.addCommand("ChargingEffect", ChargingEffect); // 添加充能效果命令
  // uduino.addCommand("DefaultPulseEffect", DefaultPulseEffect); // 添加默认脉冲效果命令

  uduino.addDisconnectedFunction(uduinoDisconnect);
}

void ReadBundle()
{
  char *arg = NULL;
  char *number = NULL;
  number = uduino.next();
  int len = 0;
  if (number != NULL)
    len = atoi(number);
  for (int i = 0; i < len; i++)
  {
    uduino.launchCommand(arg);
  }
}

void SetMode()
{
  int pinToMap = 100; // 100 is never reached
  char *arg = NULL;
  arg = uduino.next();
  if (arg != NULL)
  {
    pinToMap = atoi(arg);
  }
  int type;
  arg = uduino.next();
  if (arg != NULL)
  {
    type = atoi(arg);
    PinSetMode(pinToMap, type);
  }
}

void PinSetMode(int pin, int type)
{
  // TODO : vérifier que ça, ça fonctionne
  if (type != 4)
    DisconnectServo(pin);

  switch (type)
  {
  case 0: // Output
    pinMode(pin, OUTPUT);
    break;
  case 1: // PWM
    pinMode(pin, OUTPUT);
    break;
  case 2: // Analog
    pinMode(pin, INPUT);
    break;
  case 3: // Input_Pullup
    pinMode(pin, INPUT_PULLUP);
    break;
  case 4: // Servo
    SetupServo(pin);
    break;
  }
}

void WritePinAnalog()
{
  int pinToMap = 100;
  char *arg = NULL;
  arg = uduino.next();
  if (arg != NULL)
  {
    pinToMap = atoi(arg);
  }

  int valueToWrite;
  arg = uduino.next();
  if (arg != NULL)
  {
    valueToWrite = atoi(arg);

    if (ServoConnectedPin(pinToMap))
    {
      UpdateServo(pinToMap, valueToWrite);
    }
    else
    {
      analogWrite(pinToMap, valueToWrite);
    }
  }
}

void WritePinDigital()
{
  int pinToMap = -1;
  char *arg = NULL;
  arg = uduino.next();
  if (arg != NULL)
    pinToMap = atoi(arg);

  int writeValue;
  arg = uduino.next();
  if (arg != NULL && pinToMap != -1)
  {
    writeValue = atoi(arg);
    digitalWrite(pinToMap, writeValue);
  }
}

void ReadAnalogPin()
{
  int pinToRead = -1;
  char *arg = NULL;
  arg = uduino.next();
  if (arg != NULL)
  {
    pinToRead = atoi(arg);
    if (pinToRead != -1)
      printValue(pinToRead, analogRead(pinToRead));
  }
}

void ReadDigitalPin()
{
  int pinToRead = -1;
  char *arg = NULL;
  arg = uduino.next();
  if (arg != NULL)
  {
    pinToRead = atoi(arg);
  }

  if (pinToRead != -1)
    printValue(pinToRead, digitalRead(pinToRead));
}

void BundleReadPin()
{
  int pinToRead = -1;
  char *arg = NULL;
  arg = uduino.next();
  if (arg != NULL)
  {
    pinToRead = atoi(arg);
    if (pinToRead != -1)
    {
      if (pinToRead < 13)
        printValue(pinToRead, digitalRead(pinToRead));
      else
        printValue(pinToRead, analogRead(pinToRead));
    }
  }
}

Servo myservo;
void loop()
{
  uduino.update();
}

void printValue(int pin, int targetValue)
{
  uduino.print(pin);
  uduino.print(" "); //<- Todo : Change that with Uduino delimiter
  uduino.println(targetValue);
  // TODO : Here we could put bundle read multiple pins if(Bundle) { ... add delimiter // } ...
}

void finalize()
{
  strip1.clear();
  strip2.clear();
  strip1.show();
  strip2.show();
}

/* SERVO CODE */
Servo servos[MAXSERVOS];
int servoPinMap[MAXSERVOS];
/*
  void InitializeServos() {
  for (int i = 0; i < MAXSERVOS - 1; i++ ) {
    servoPinMap[i] = -1;
    servos[i].detach();
  }
  }
*/
void SetupServo(int pin)
{
  if (ServoConnectedPin(pin))
    return;

  int nextIndex = GetAvailableIndexByPin(-1);
  if (nextIndex == -1)
    nextIndex = 0;
  servos[nextIndex].attach(pin);
  servoPinMap[nextIndex] = pin;
}

void DisconnectServo(int pin)
{
  servos[GetAvailableIndexByPin(pin)].detach();
  servoPinMap[GetAvailableIndexByPin(pin)] = 0;
}

bool ServoConnectedPin(int pin)
{
  if (GetAvailableIndexByPin(pin) == -1)
    return false;
  else
    return true;
}

int GetAvailableIndexByPin(int pin)
{
  for (int i = 0; i < MAXSERVOS - 1; i++)
  {
    if (servoPinMap[i] == pin)
    {
      return i;
    }
    else if (pin == -1 && servoPinMap[i] < 0)
    {
      return i; // return the first available index
    }
  }
  return -1;
}

void UpdateServo(int pin, int targetValue)
{
  int index = GetAvailableIndexByPin(pin);
  servos[index].write(targetValue);
  delay(10);
}

void DisconnectAllServos()
{
  for (int i = 0; i < MAXSERVOS; i++)
  {
    servos[i].detach();
    digitalWrite(servoPinMap[i], LOW);
    servoPinMap[i] = -1;
  }
}

// 设置所有灯带为相同颜色
void SetColor()
{
  char *arg = NULL;
  arg = uduino.next();
  int r = (arg != NULL) ? atoi(arg) : 0;

  arg = uduino.next();
  int g = (arg != NULL) ? atoi(arg) : 0;

  arg = uduino.next();
  int b = (arg != NULL) ? atoi(arg) : 0;

  // 设置两个灯带的所有灯珠
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, r, g, b);
    strip2.setPixelColor(i, r, g, b);
  }
  strip1.show();
  strip2.show();
}

// 设置指定灯带的颜色
void SetStripColor()
{
  char *arg = NULL;

  // 获取灯带编号（1或2）
  arg = uduino.next();
  int stripNum = (arg != NULL) ? atoi(arg) : 1;

  // 获取RGB值
  arg = uduino.next();
  int r = (arg != NULL) ? atoi(arg) : 0;

  arg = uduino.next();
  int g = (arg != NULL) ? atoi(arg) : 0;

  arg = uduino.next();
  int b = (arg != NULL) ? atoi(arg) : 0;

  // 选择对应的灯带设置颜色
  Adafruit_NeoPixel &strip = (stripNum == 2) ? strip2 : strip1;

  for (int i = 0; i < LED_COUNT; i++)
  {
    strip.setPixelColor(i, r, g, b);
  }
  strip.show();
}

// 设置指定灯带指定灯珠的颜色
void SetPixelColor()
{
  char *arg = NULL;

  // 获取灯带编号（1或2）
  arg = uduino.next();
  int stripNum = (arg != NULL) ? atoi(arg) : 1;

  // 获取灯珠编号（0-51）
  arg = uduino.next();
  int pixelNum = (arg != NULL) ? atoi(arg) : 0;

  // 获取RGB值
  arg = uduino.next();
  int r = (arg != NULL) ? atoi(arg) : 0;

  arg = uduino.next();
  int g = (arg != NULL) ? atoi(arg) : 0;

  arg = uduino.next();
  int b = (arg != NULL) ? atoi(arg) : 0;

  // 选择对应的灯带
  Adafruit_NeoPixel &strip = (stripNum == 2) ? strip2 : strip1;

  // 设置指定灯珠的颜色
  if (pixelNum >= 0 && pixelNum < LED_COUNT)
  {
    strip.setPixelColor(pixelNum, r, g, b);
    strip.show();
  }
}

void uduinoDisconnect()
{
  // 断开连接时强制关闭所有灯带
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 0, 0, 0);
    strip2.setPixelColor(i, 0, 0, 0);
  }
  strip1.show();
  strip2.show();
  Serial.println("连接断开，已关闭所有灯带");
}

// 修改PulseEffect函数定义，添加效果类型参数和颜色参数
void PulseEffect()
{
  char *arg = NULL;

  // 获取灯带编号（0或1，对应Unity中的选项索引）
  arg = uduino.next();
  int stripIndex = (arg != NULL) ? atoi(arg) : 0;

  // 获取效果类型
  arg = uduino.next();
  String effectType = (arg != NULL) ? String(arg) : "default";

  // 选择对应的灯带
  Adafruit_NeoPixel &strip = (stripIndex == 1) ? strip2 : strip1;

  // 根据效果类型选择不同的实现
  if (effectType == "rainbow")
  {
    // 彩虹效果
    RainbowPulseEffect(strip);
  }
  else if (effectType == "bounce")
  {
    // 弹跳效果
    BouncePulseEffect(strip);
  }
  else if (effectType == "flash")
  {
    // 闪烁效果
    FlashPulseEffect(strip);
  }
  else if (effectType == "gradient")
  {
    // 渐变效果 - 需要更多参数：两个颜色 + 持续时间
    // 获取第一个颜色的RGB值
    arg = uduino.next();
    int r1 = (arg != NULL) ? atoi(arg) : 255;
    arg = uduino.next();
    int g1 = (arg != NULL) ? atoi(arg) : 0;
    arg = uduino.next();
    int b1 = (arg != NULL) ? atoi(arg) : 0;

    // 获取第二个颜色的RGB值
    arg = uduino.next();
    int r2 = (arg != NULL) ? atoi(arg) : 0;
    arg = uduino.next();
    int g2 = (arg != NULL) ? atoi(arg) : 255;
    arg = uduino.next();
    int b2 = (arg != NULL) ? atoi(arg) : 0;

    // 获取持续时间（毫秒）
    arg = uduino.next();
    int duration = (arg != NULL) ? atoi(arg) : PULSE_DURATION;

    // 调用渐变脉冲效果
    GradientPulseEffect(strip, stripIndex, r1, g1, b1, r2, g2, b2, duration);
  }
  else
  {
    // 默认效果和其他效果 - 获取RGB颜色值
    arg = uduino.next();
    int r = (arg != NULL) ? atoi(arg) : 255;

    arg = uduino.next();
    int g = (arg != NULL) ? atoi(arg) : 0;

    arg = uduino.next();
    int b = (arg != NULL) ? atoi(arg) : 0;

    // 默认效果，传递接收到的颜色参数
    DefaultPulseEffect(strip, stripIndex, r, g, b);
  }
}

// 将原始PulseEffect代码移动到这个函数
void DefaultPulseEffect(Adafruit_NeoPixel &strip, int stripIndex, int r, int g, int b)
{
  // 直接使用传入的RGB颜色值

  // 先关闭所有灯珠
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 0, 0, 0);
    strip2.setPixelColor(i, 0, 0, 0);
  }
  strip1.show();
  strip2.show();

  // 计算每个灯珠的延迟时间
  int delayPerLed = PULSE_DURATION / LED_COUNT;

  // 执行脉冲效果 - 带尾部的流动效果
  for (int i = 0; i < LED_COUNT + PULSE_TAIL_LENGTH; i++)
  {
    // 先清除所有LED
    for (int j = 0; j < LED_COUNT; j++)
    {
      strip.setPixelColor(j, 0, 0, 0);
    }

    // 设置"尾巴"的每个LED
    for (int j = 0; j < PULSE_TAIL_LENGTH; j++)
    {
      int pixelIndex = i - j;

      // 确保像素索引在有效范围内
      if (pixelIndex >= 0 && pixelIndex < LED_COUNT)
      {
        // 根据在尾部的位置计算亮度
        float intensity = 1.0f - ((float)j / PULSE_TAIL_LENGTH);

        // 应用亮度到RGB值
        int finalR = r * intensity;
        int finalG = g * intensity;
        int finalB = b * intensity;

        strip.setPixelColor(pixelIndex, finalR, finalG, finalB);
      }
    }

    strip.show();
    delay(delayPerLed);
  }

  // 脉冲效果结束后再次关闭所有灯珠
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip.setPixelColor(i, 0, 0, 0);
  }
  strip.show();

  // 恢复默认颜色
  delay(100); // 短暂延迟以明确区分效果
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 50, 50, 50);
    strip2.setPixelColor(i, 50, 50, 50);
  }
  strip1.show();
  strip2.show();
}

// 添加彩虹效果
void RainbowPulseEffect(Adafruit_NeoPixel &strip)
{
  // 清除灯带
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip.setPixelColor(i, 0, 0, 0);
  }
  strip.show();

  // 彩虹效果实现
  for (int j = 0; j < 3; j++)
  { // 重复3次
    for (int i = 0; i < 256; i += 5)
    { // 加快速度，每次跳过5个颜色值
      for (int p = 0; p < LED_COUNT; p++)
      {
        // 使用wheel函数产生彩虹色
        strip.setPixelColor(p, Wheel((i + p) & 255));
      }
      strip.show();
      delay(5);
    }
  }

  // 恢复默认颜色
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 50, 50, 50);
    strip2.setPixelColor(i, 50, 50, 50);
  }
  strip1.show();
  strip2.show();
}

// 弹跳效果
void BouncePulseEffect(Adafruit_NeoPixel &strip)
{
  // 清除灯带
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip.setPixelColor(i, 0, 0, 0);
  }
  strip.show();

  // 弹跳效果实现
  for (int j = 0; j < 3; j++)
  { // 重复3次
    // 正向
    for (int i = 0; i < LED_COUNT; i++)
    {
      strip.clear();
      strip.setPixelColor(i, 0, 255, 255);
      strip.show();
      delay(10);
    }

    // 反向
    for (int i = LED_COUNT - 1; i >= 0; i--)
    {
      strip.clear();
      strip.setPixelColor(i, 0, 255, 255);
      strip.show();
      delay(10);
    }
  }

  // 恢复默认颜色
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 50, 50, 50);
    strip2.setPixelColor(i, 50, 50, 50);
  }
  strip1.show();
  strip2.show();
}

// 闪烁效果
void FlashPulseEffect(Adafruit_NeoPixel &strip)
{
  // 闪烁效果实现
  for (int j = 0; j < 5; j++)
  {
    // 全亮
    for (int i = 0; i < LED_COUNT; i++)
    {
      strip.setPixelColor(i, 255, 255, 255);
    }
    strip.show();
    delay(100);

    // 全灭
    for (int i = 0; i < LED_COUNT; i++)
    {
      strip.setPixelColor(i, 0, 0, 0);
    }
    strip.show();
    delay(100);
  }

  // 恢复默认颜色
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 50, 50, 50);
    strip2.setPixelColor(i, 50, 50, 50);
  }
  strip1.show();
  strip2.show();
}

// 彩虹色辅助函数
uint32_t Wheel(byte WheelPos)
{
  WheelPos = 255 - WheelPos;
  if (WheelPos < 85)
  {
    return strip1.Color(255 - WheelPos * 3, 0, WheelPos * 3);
  }
  if (WheelPos < 170)
  {
    WheelPos -= 85;
    return strip1.Color(0, WheelPos * 3, 255 - WheelPos * 3);
  }
  WheelPos -= 170;
  return strip1.Color(WheelPos * 3, 255 - WheelPos * 3, 0);
}

// 充能效果
void ChargingEffect()
{
  char *arg = NULL;

  // 获取灯带编号（0或1）
  arg = uduino.next();
  int stripIndex = (arg != NULL) ? atoi(arg) : 0;

  // 获取当前充能位置（0-51）
  arg = uduino.next();
  int chargePosition = (arg != NULL) ? atoi(arg) : 0;

  // 获取RGB颜色值
  arg = uduino.next();
  int r = (arg != NULL) ? atoi(arg) : 255;

  arg = uduino.next();
  int g = (arg != NULL) ? atoi(arg) : 0;

  arg = uduino.next();
  int b = (arg != NULL) ? atoi(arg) : 0;

  // 选择对应的灯带
  Adafruit_NeoPixel &strip = (stripIndex == 1) ? strip2 : strip1;

  // 清除灯带
  strip.clear();

  // 颜色和亮度双重渐变效果
  if (chargePosition >= 0 && chargePosition < LED_COUNT)
  {
    for (int i = 0; i <= chargePosition; i++)
    {
      // 颜色和亮度双重渐变效果
      if (i == 0)
      {
        // 第一个灯珠：0%亮度（完全关闭）
        strip.setPixelColor(i, 0, 0, 0);
      }
      else if (i == chargePosition)
      {
        // 充能位置：100%亮度，目标颜色
        strip.setPixelColor(i, r, g, b);
      }
      else
      {
        // 中间灯珠：颜色从白色渐变到目标颜色，亮度使用对数变化
        float progress = (float)i / (float)chargePosition; // 0.0 到 1.0

        // 对数亮度渐变：使用平方函数让大部分灯珠保持较暗
        // progress^2 会让前面的灯珠很暗，只有接近末端的才明显变亮
        float brightnessFactor = progress * progress;   // 平方函数
        int brightness = (int)(brightnessFactor * 100); // 0% 到 100%

        // 颜色渐变：从白色(255,255,255)到目标颜色(r,g,b)
        int whiteR = 255;
        int whiteG = 0;
        int whiteB = 0;

        int currentR = whiteR + (int)((r - whiteR) * progress);
        int currentG = whiteG + (int)((g - whiteG) * progress);
        int currentB = whiteB + (int)((b - whiteB) * progress);

        // 应用对数亮度到渐变后的颜色
        int finalR = (currentR * brightness) / 100;
        int finalG = (currentG * brightness) / 100;
        int finalB = (currentB * brightness) / 100;

        strip.setPixelColor(i, finalR, finalG, finalB);
      }
    }
  }

  strip.show();
}

// 渐变脉冲效果
void GradientPulseEffect(Adafruit_NeoPixel &strip, int stripIndex, int r1, int g1, int b1, int r2, int g2, int b2, int duration)
{
  // 先关闭所有灯珠
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 0, 0, 0);
    strip2.setPixelColor(i, 0, 0, 0);
  }
  strip1.show();
  strip2.show();

  // 计算每个灯珠的延迟时间
  int delayPerLed = duration / LED_COUNT;

  // 执行渐变脉冲效果 - 带尾部的流动效果
  for (int i = 0; i < LED_COUNT + PULSE_TAIL_LENGTH; i++)
  {
    // 先清除所有LED
    for (int j = 0; j < LED_COUNT; j++)
    {
      strip.setPixelColor(j, 0, 0, 0);
    }

    // 设置"尾巴"的每个LED
    for (int j = 0; j < PULSE_TAIL_LENGTH; j++)
    {
      int pixelIndex = i - j;

      // 确保像素索引在有效范围内
      if (pixelIndex >= 0 && pixelIndex < LED_COUNT)
      {
        // 根据在尾部的位置计算亮度
        float intensity = 1.0f - ((float)j / PULSE_TAIL_LENGTH);

        // 计算当前像素在整个灯带中的位置比例（0.0到1.0）
        float positionRatio = (float)pixelIndex / (float)(LED_COUNT - 1);

        // 根据位置比例计算颜色渐变：从颜色1到颜色2
        int gradientR = r1 + (int)((r2 - r1) * positionRatio);
        int gradientG = g1 + (int)((g2 - g1) * positionRatio);
        int gradientB = b1 + (int)((b2 - b1) * positionRatio);

        // 应用亮度到渐变后的RGB值
        int finalR = gradientR * intensity;
        int finalG = gradientG * intensity;
        int finalB = gradientB * intensity;

        strip.setPixelColor(pixelIndex, finalR, finalG, finalB);
      }
    }

    strip.show();
    delay(delayPerLed);
  }

  // 脉冲效果结束后再次关闭所有灯珠
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip.setPixelColor(i, 0, 0, 0);
  }
  strip.show();

  // 恢复默认颜色
  delay(100); // 短暂延迟以明确区分效果
  for (int i = 0; i < LED_COUNT; i++)
  {
    strip1.setPixelColor(i, 50, 50, 50);
    strip2.setPixelColor(i, 50, 50, 50);
  }
  strip1.show();
  strip2.show();
}
