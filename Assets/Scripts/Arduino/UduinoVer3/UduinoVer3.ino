#include<Uduino.h>
#include <Adafruit_NeoPixel.h>

// 定义两个灯带的引脚和参数
#define LED_PIN_1    10      // 第一个灯带信号引脚
#define LED_PIN_2    11      // 第二个灯带信号引脚
#define LED_COUNT    52      // 每个灯带的灯珠数量

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
  strip1.show();
  strip2.show();

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
  uduino.addCommand("SetColor", SetColor);           // 设置所有灯带相同颜色
  uduino.addCommand("SetStripColor", SetStripColor);     // 设置指定灯带的颜色
  uduino.addCommand("SetPixelColor", SetPixelColor);     // 设置指定灯带指定灯珠的颜色

  uduino.addDisconnectedFunction(uduinoDisconnect);
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
}

void printValue(int pin, int targetValue) {
  uduino.print(pin);
  uduino.print(" "); //<- Todo : Change that with Uduino delimiter
  uduino.println(targetValue);
  // TODO : Here we could put bundle read multiple pins if(Bundle) { ... add delimiter // } ...
}

void finalize() {
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

// 设置所有灯带为相同颜色
void SetColor() {
    char* arg = NULL;
    arg = uduino.next();
    int r = (arg != NULL) ? atoi(arg) : 0;
    
    arg = uduino.next();
    int g = (arg != NULL) ? atoi(arg) : 0;
    
    arg = uduino.next();
    int b = (arg != NULL) ? atoi(arg) : 0;
    
    // 设置两个灯带的所有灯珠
    for (int i = 0; i < LED_COUNT; i++) {
        strip1.setPixelColor(i, r, g, b);
        strip2.setPixelColor(i, r, g, b);
    }
    strip1.show();
    strip2.show();
}

// 设置指定灯带的颜色
void SetStripColor() {
    char* arg = NULL;
    
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
    Adafruit_NeoPixel& strip = (stripNum == 2) ? strip2 : strip1;
    
    for (int i = 0; i < LED_COUNT; i++) {
        strip.setPixelColor(i, r, g, b);
    }
    strip.show();
}

// 设置指定灯带指定灯珠的颜色
void SetPixelColor() {
    char* arg = NULL;
    
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
    Adafruit_NeoPixel& strip = (stripNum == 2) ? strip2 : strip1;
    
    // 设置指定灯珠的颜色
    if (pixelNum >= 0 && pixelNum < LED_COUNT) {
        strip.setPixelColor(pixelNum, r, g, b);
        strip.show();
    }
}

void uduinoDisconnect() {
  // 断开连接时强制关闭所有灯带
  for (int i = 0; i < LED_COUNT; i++) {
    strip1.setPixelColor(i, 0, 0, 0);
    strip2.setPixelColor(i, 0, 0, 0);
  }
  strip1.show();
  strip2.show();
  Serial.println("连接断开，已关闭所有灯带");
}
