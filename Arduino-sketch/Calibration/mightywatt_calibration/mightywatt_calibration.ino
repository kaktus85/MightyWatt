// IMPORTANT!
// UNCOMMENT THE LINE THAT BELONGS TO YOUR ARDUINO VERSION, COMMENT THE OTHER LINE
//#define ARM // Arduino Due and similar ATSAM (32bit) based boards
#define AVR // Arduino Uno and similar AVR (8bit) based boards

// WARNING!
// TO MAKE THE MIGHTYWATT WORKING ON ARDUINO DUE, YOU MUST HAVE THE JUMPER ON DUE SET FOR EXTERNAL ANALOG REFERENCE

#include <Wire.h>
#ifdef AVR
  #define I2C Wire
#endif
#ifdef ARM
  #define I2C Wire1
#endif

// calibration procedure for FW_VERSION 2.3.1 and higher

// <Pins>
const int REMOTE_PIN = 2;
const int CV_PIN = 3;
const int V_GAIN_PIN = 4;
const int ADC_I_PIN = A1;
const int ADC_V_PIN = A2;
// </Pins>

// <DAC>
//  commands
const byte DAC_ADDRESS = 0b1001100; // I2C address of DAC (A0 tied to GND)
const byte DAC_WRITE_DAC_AND_INPUT_REGISTERS = 0b00110000; // write to DAC memory 
//  value
unsigned int dac = 0;
// </DAC>

void setup()
{      
  delay(100);
  Serial.begin(115200);
  #ifdef AVR
    analogReference(EXTERNAL);
  #endif  
  #ifdef ARM
    ADC->ADC_MR |= (0b1111 << 24); // set maximum ADC tracking time
    analogReadResolution(12);
  #endif
  pinMode(REMOTE_PIN, OUTPUT);
  pinMode(V_GAIN_PIN, OUTPUT);
  pinMode(CV_PIN, OUTPUT);
  digitalWrite(REMOTE_PIN, LOW);
  digitalWrite(V_GAIN_PIN, LOW);
  digitalWrite(CV_PIN, LOW);
  I2C.begin();  
  unsigned int adc;
  int command = 0;
  
  // UNCOMMENT SECTION 1–3 FOR CALIBRATION  
  
  // Section 1: MODE_CC - current calibration
  // Current is increased each time something is received on serial line.
  // DAC value is written followed by ADC value. Use this in conjunction with external ammeter to calibrate both DAC and ADC for current.
  // Make your own calibration points, 5–10 points is enough.
  
  /*
  Serial.println("dac\tadc");
  delay(100);
  for (int i = 41 ; i <= 4054; i = i + 800) // 6 calibration points
  {    
    dac = i;
    setDAC();
    delay(300); // equilibrate
    adc = readADC12bit(ADC_I_PIN);
    Serial.print(dac);
    Serial.print("\t");
    Serial.println(adc);
    while(Serial.available() == 0){}
    Serial.read();   // wait for keypress to increase current
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
  Serial.println("adc");
  while (command != 'q') // press 'q' to quit calibration
  {
    delay(300);
    adc = readADC12bit(ADC_V_PIN);
    Serial.println(adc);
    while(Serial.available() == 0){}
    command = Serial.read(); // wait for keypress
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
  // digitalWrite(REMOTE_PIN, HIGH); // uncomment to enable remote (4-electrode) measurement
  digitalWrite(CV_PIN, HIGH);
  digitalWrite(V_GAIN_PIN, LOW); // uncomment for no voltage gain (full scale voltage)
  // digitalWrite(V_GAIN_PIN, HIGH); // uncomment for voltage gain
  Serial.println("dac");
  delay(300);
  for (int i = 4054; i >= 41; i = i - 800) // 6 calibration points
  {    
    dac = i;
    setDAC();
    delay(300); // equilibrate
    Serial.println(dac);
    while(Serial.available() == 0){}
    Serial.read(); // wait for keypress to decrease voltage
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

unsigned int readADC12bit(int channel) // oversamples ADC to 12 bit (AVR) or averages 16 samples (ARM)
{  
  byte i;
  unsigned int analogResult = 0;
  for (i = 0; i < 16; i++)
  {
    analogResult += analogRead(channel);
  }
  #ifdef AVR
    return (analogResult >> 2);
  #endif
  #ifdef ARM
    return (analogResult >> 4);
  #endif
}
