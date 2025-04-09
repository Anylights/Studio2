// Uduino Default Board
#include<Uduino.h>
#include <Adafruit_NeoPixel.h>
Uduino uduino("uduinoBoard"); // Declare and name your object

// Servo
#include <Servo.h>
#define MAXSERVOS 8


// 最大支持 3 个灯环（根据内存调整）
#define MAX_STRIPS 3
struct NeoStrip {
  Adafruit_NeoPixel* strip;
  uint8_t pin;
  uint16_t numPixels;
  uint32_t currentColor;
  uint32_t targetColor;
  unsigned long transitionStart;
  float transitionDuration;
  bool isActive;
};
NeoStrip strips[MAX_STRIPS] = {0}; // 初始化所有灯环为未激活


void setup()
{
  Serial.begin(9600);

#if defined (__AVR_ATmega32U4__) // Leonardo
  while (!Serial) {}
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
  uduino.addCommand("initStrip", InitStrip);    // 初始化灯环
  uduino.addCommand("setColor", SetColor);     // 设置颜色
  uduino.addCommand("setBrightness", SetBrightness); // 设置亮度
}

void ReadBundle() {
  char *arg = NULL;
  char *number = NULL;
  number = uduino.next();
  int len = 0;
  if (number != NULL)
    len = atoi(number);
  for (int i = 0; i < len; i++) {
    uduino.launchCommand(arg);
  }
}

void SetMode() {
  int pinToMap = 100; //100 is never reached
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

void PinSetMode(int pin, int type) {
  //TODO : vérifier que ça, ça fonctionne
  if (type != 4)
    DisconnectServo(pin);

  switch (type) {
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

void WritePinAnalog() {
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

    if (ServoConnectedPin(pinToMap)) {
      UpdateServo(pinToMap, valueToWrite);
    } else {
      analogWrite(pinToMap, valueToWrite);
    }
  }
}

void WritePinDigital() {
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

void ReadAnalogPin() {
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

void ReadDigitalPin() {
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

void BundleReadPin() {
  int pinToRead = -1;
  char *arg = NULL;
  arg = uduino.next();
  if (arg != NULL)
  {
    pinToRead = atoi(arg);
    if (pinToRead != -1) {
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
  UpdateAllStrips(); // 更新所有灯环颜色过渡
}

void printValue(int pin, int targetValue) {
  uduino.print(pin);
  uduino.print(" "); //<- Todo : Change that with Uduino delimiter
  uduino.println(targetValue);
  // TODO : Here we could put bundle read multiple pins if(Bundle) { ... add delimiter // } ...
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
void SetupServo(int pin) {
  if (ServoConnectedPin(pin))
    return;

  int nextIndex = GetAvailableIndexByPin(-1);
  if (nextIndex == -1)
    nextIndex = 0;
  servos[nextIndex].attach(pin);
  servoPinMap[nextIndex] = pin;
}


void DisconnectServo(int pin) {
  servos[GetAvailableIndexByPin(pin)].detach();
  servoPinMap[GetAvailableIndexByPin(pin)] = 0;
}

bool ServoConnectedPin(int pin) {
  if (GetAvailableIndexByPin(pin) == -1) return false;
  else return true;
}

int GetAvailableIndexByPin(int pin) {
  for (int i = 0; i < MAXSERVOS - 1; i++ ) {
    if (servoPinMap[i] == pin) {
      return i;
    } else if (pin == -1 && servoPinMap[i] < 0) {
      return i; // return the first available index
    }
  }
  return -1;
}

void UpdateServo(int pin, int targetValue) {
  int index = GetAvailableIndexByPin(pin);
  servos[index].write(targetValue);
  delay(10);
}

void DisconnectAllServos() {
  for (int i = 0; i < MAXSERVOS; i++) {
    servos[i].detach();
    digitalWrite(servoPinMap[i], LOW);
    servoPinMap[i] = -1;
  }
}

/* LIGHT CODE */
// 初始化灯环指令格式：initStrip <stripID> <pin> <numPixels>
void InitStrip() {
  char* arg = uduino.next();
  int stripID = (arg != NULL) ? atoi(arg) : 0;
  
  arg = uduino.next();
  int pin = (arg != NULL) ? atoi(arg) : 0;
  
  arg = uduino.next();
  int numPixels = (arg != NULL) ? atoi(arg) : 0;

    //这里没有进行Debug输出
  Serial.print("初始化灯环: ID=");
  Serial.print(stripID);
  Serial.print(" Pin=");
  Serial.print(pin);
  Serial.print(" NumPixels=");
  Serial.println(numPixels);


 if (stripID >= MAX_STRIPS) return;

  if (strips[stripID].strip != nullptr) {
    delete strips[stripID].strip; // 释放旧实例
    Serial.println("释放旧灯环实例");
  }

  // 创建新的灯环实例
  strips[stripID].strip = new Adafruit_NeoPixel(numPixels, pin, NEO_GRB + NEO_KHZ800);
  if (strips[stripID].strip == nullptr) {
    Serial.println("错误：无法创建灯环对象");
    return;
  }
  
  // 初始化灯环
  strips[stripID].strip->begin();
  strips[stripID].pin = pin;
  strips[stripID].numPixels = numPixels;
  
  // 确保灯环为黑色
  strips[stripID].strip->clear(); // 清除所有像素
  strips[stripID].currentColor = strips[stripID].strip->Color(0, 0, 0);
  strips[stripID].targetColor = strips[stripID].currentColor;
  strips[stripID].strip->setBrightness(50);
  strips[stripID].isActive = true;
  
  // 强制设置所有灯珠为黑色
  for (int i = 0; i < numPixels; i++) {
    strips[stripID].strip->setPixelColor(i, 0, 0, 0);
  }
  
  // 显示更新
  strips[stripID].strip->show();
  
  Serial.println("灯环初始化完成并设置为黑色");
}

// 设置颜色指令格式：setColor <stripID> <R> <G> <B> <duration>
void SetColor() {
  char* arg = uduino.next();
  int stripID = (arg != NULL) ? atoi(arg) : 0;
  
  arg = uduino.next();
  int r = (arg != NULL) ? atoi(arg) : 0;
  
  arg = uduino.next();
  int g = (arg != NULL) ? atoi(arg) : 0;
  
  arg = uduino.next();
  int b = (arg != NULL) ? atoi(arg) : 0;
  
  arg = uduino.next();
  float duration = (arg != NULL) ? atof(arg) : 0.0f;

  if (!strips[stripID].isActive) return;

  strips[stripID].targetColor = strips[stripID].strip->Color(r, g, b);
  strips[stripID].transitionStart = millis();
  strips[stripID].transitionDuration = duration * 1000; // 转换为毫秒
}

// 设置亮度指令格式：setBrightness <stripID> <brightness(0-255)>
void SetBrightness() {
  char* arg = uduino.next();
  int stripID = (arg != NULL) ? atoi(arg) : 0;
  
  arg = uduino.next();
  int brightness = (arg != NULL) ? atoi(arg) : 0;
  
  if (strips[stripID].isActive) {
    strips[stripID].strip->setBrightness(brightness);
    strips[stripID].strip->show();
  }
}

// 其他代码保持不变...

// 更新所有灯环颜色过渡
void UpdateAllStrips() {
  for (int i = 0; i < MAX_STRIPS; i++) {
    if (!strips[i].isActive) continue;

    NeoStrip* strip = &strips[i];
    if (strip->transitionStart == 0) continue;

    float progress = (millis() - strip->transitionStart) / strip->transitionDuration;
    if (progress >= 1.0f) {
      strip->currentColor = strip->targetColor;
      strip->transitionStart = 0;
    } else {
      // 线性插值计算当前颜色
      uint8_t r = Lerp((strip->currentColor >> 16) & 0xFF, (strip->targetColor >> 16) & 0xFF, progress);
      uint8_t g = Lerp((strip->currentColor >> 8) & 0xFF, (strip->targetColor >> 8) & 0xFF, progress);
      uint8_t b = Lerp(strip->currentColor & 0xFF, strip->targetColor & 0xFF, progress);
      strip->currentColor = strip->strip->Color(r, g, b);
    }

    // 更新所有灯珠
    for (int j = 0; j < strip->numPixels; j++) {
      strip->strip->setPixelColor(j, strip->currentColor);
    }
    strip->strip->show();
  }
}

// 颜色插值辅助函数
uint8_t Lerp(uint8_t a, uint8_t b, float t) {
  return a + (uint8_t)((b - a) * t);
}
