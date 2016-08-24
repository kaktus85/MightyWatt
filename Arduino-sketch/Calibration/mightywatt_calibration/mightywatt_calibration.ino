// IMPORTANT!
// UNCOMMENT THE LINE THAT BELONGS TO YOUR ARDUINO VERSION, COMMENT THE OTHER LINE
// If you are using Arduino Zero (M0/M0 Pro), the sketch was tested with Arduino.org board and version of IDE.
//#define ZERO // Arduino Zero, M0 and M0 Pro (Cortex M0+, 32-bit) via Native port (USB)
#define UNO // Arduino Uno (UNO, 8-bit)
//#define DUE // Arduino Due (Cortex M3, 32-bit) via Programming port (USB)

// WARNING!
// TO MAKE THE MIGHTYWATT WORKING ON ARDUINO DUE, YOU MUST HAVE THE JUMPER ON DUE SET FOR EXTERNAL ANALOG REFERENCE

#include <Wire.h>
#if defined(ZERO) || defined(UNO)
  #define I2C Wire  
#endif
#ifdef DUE
  #define I2C Wire1
#endif
#ifdef ZERO
  #define SerialPort SerialUSB
#endif
#if defined(UNO) || defined(DUE)
  #define SerialPort Serial
#endif

// calibration procedure for FW_VERSION 2.5.7 and higher

// <Pins>
const int16_t REMOTE_PIN = 2;
const int16_t CV_PIN = 3;
const int16_t V_GAIN_PIN = 4;
const int16_t ADC_I_PIN = A1;
const int16_t ADC_V_PIN = A2;
// </Pins>

// <DAC>
//  commands
#define DAC_ADDRESS                          (0b1001100)
#define DAC_WRITE_DAC_AND_INPUT_REGISTERS    (0b00110000)
#define DAC_WRITE_CONTROL_REGISTER           (0b01000000)
#define DAC_RESET                            (0b10000000)
//  value
uint16_t dac = 0;
// </DAC>

void setup()
{      
  delay(100);
  SerialPort.begin(115200);
  #ifdef ZERO
    analogReference(AR_EXTERNAL);
  #endif
  #ifdef UNO
    analogReference(EXTERNAL);
  #endif
  #ifdef DUE
    ADC->ADC_MR |= (0b1111 << 24); // set maximum ADC tracking time
  #endif
  #if defined(ZERO) || defined(DUE)
    analogReadResolution(12);
  #endif
  pinMode(REMOTE_PIN, OUTPUT);
  pinMode(V_GAIN_PIN, OUTPUT);
  pinMode(CV_PIN, OUTPUT);
  digitalWrite(REMOTE_PIN, LOW);
  digitalWrite(V_GAIN_PIN, LOW);
  digitalWrite(CV_PIN, LOW);
  I2C.begin();  
  initDAC();
  delay(1);
  uint16_t adc;
  int16_t command = 0;
  
  // give time to open serial monitor - Zero does not reset connection on monitor opening
  #ifdef ZERO
    delay(6000);
  #endif
  
  // UNCOMMENT SECTION 1–3 FOR CALIBRATION  
  
  // Section 1: MODE_CC - current calibration
  // Current is increased each time something is received on serial line.
  // DAC value is written followed by ADC value. Use this in conjunction with external ammeter to calibrate both DAC and ADC for current.
  // Make your own calibration points, 5–10 points is enough.
  
  /*
  SerialPort.println("dac\tadc");  
  for (int16_t i = 41 ; i <= 4054; i = i + 800) // 6 calibration points
  {    
    dac = i;
    setDAC();
    delay(300); // equilibrate
    adc = readADC12bit(ADC_I_PIN);
    SerialPort.print(dac);
    SerialPort.print("\t");
    SerialPort.println(adc);
    while(SerialPort.available() == 0){}
    SerialPort.read();   // wait for keypress to increase current
  }  
  dac = 0;
  setDAC();
  */
    
    
  // Section 2: Voltmeter calibration
  // Control and measure voltage externally.
  // Voltage is read each time something is received on serial line.
  // Sending '0' sets no gain, '1' sets gain mode.
  // Make 5–10 points on each scale. Do not overlap scales (on no gain, end where gain mode begins).
  
  /*
  dac = 0;
  setDAC();  
  SerialPort.println("adc");
  while (command != 'q') // press 'q' to quit calibration
  {
    delay(300);
    adc = readADC12bit(ADC_V_PIN);
    SerialPort.println(adc);
    while(SerialPort.available() == 0){}
    command = SerialPort.read(); // wait for keypress
    if (command == '0')
    {
      digitalWrite(V_GAIN_PIN, LOW); // press '0' for no gain (full voltage scale)
    }
    if (command == '1')
    {
      digitalWrite(V_GAIN_PIN, HIGH); // press '1' for gain
    }
  }
  */
  
  
  // Section 3: MODE_CV - voltage calibration
  // Voltage is decreased each time something is received on serial line.
  // Measure voltage with external meter and compare with DAC values.
  // Make two sets of measurements, each with 5–10 points, for both voltage gain settings.
  
  /*
  digitalWrite(REMOTE_PIN, HIGH); // uncomment to enable remote (4-electrode) measurement
  digitalWrite(CV_PIN, HIGH);
  digitalWrite(V_GAIN_PIN, LOW); // uncomment for no voltage gain (full scale voltage)
  //digitalWrite(V_GAIN_PIN, HIGH); // uncomment for voltage gain
  SerialPort.println("dac");
  delay(300);
  for (int16_t i = 4054; i >= 41; i = i - 800) // 6 calibration points
  {    
    dac = i;
    setDAC();
    delay(300); // equilibrate
    SerialPort.println(dac);
    while(SerialPort.available() == 0){}
    SerialPort.read(); // wait for keypress to decrease voltage
  }  
  dac = 0;
  setDAC();
  digitalWrite(CV_PIN, LOW);  
  */
}

void loop()
{  
}

void setDAC() // sets value to DAC
{ 
  I2C.beginTransmission(DAC_ADDRESS); 
  I2C.write(DAC_WRITE_DAC_AND_INPUT_REGISTERS);
  I2C.write((dac >> 4) & 0xFF);
  I2C.write((dac & 0xF) << 4);
  I2C.endTransmission();
  delayMicroseconds(8); // settling time  
}

void initDAC() // resets the DAC
{
  I2C.beginTransmission(DAC_ADDRESS); 
  I2C.write(DAC_WRITE_CONTROL_REGISTER);
  I2C.write(DAC_RESET);
  I2C.write((uint8_t)0);
  I2C.endTransmission();
}

uint16_t readADC12bit(int16_t channel) // oversamples ADC to 12 bit (AVR) or averages 16 samples (ARM)
{  
  uint8_t i;
  uint16_t analogResult = 0;
  #if defined(ZERO) || defined(DUE)
    delayMicroseconds(1000);
  #endif
  for (i = 0; i < 16; i++)
  {
    analogResult += analogRead(channel);
  }  
  #if defined(ZERO) || defined(DUE)
    return (analogResult >> 4);
  #endif
  #ifdef UNO
    return (analogResult >> 2);
  #endif
}
