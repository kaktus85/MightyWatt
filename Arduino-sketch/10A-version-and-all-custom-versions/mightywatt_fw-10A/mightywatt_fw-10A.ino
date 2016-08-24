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
#include <math.h>

// <Device Information>
#define FW_VERSION "2.5.9" // Universal for Zero, UNO and DUE
#define BOARD_REVISION "r2.5" // minimum MightyWatt board revision for this sketch is 2.4
#define DVM_INPUT_RESISTANCE 330000 // differential input resistance
// </Device Information>

// <Pins>
const int16_t REMOTE_PIN = 2;
const int16_t CV_PIN = 3;
const int16_t V_GAIN_PIN = 4;
const int16_t LED_PIN = 5;
const int16_t ADC_I_PIN = A1;
const int16_t ADC_V_PIN = A2;
const int16_t ADC_T_PIN = A3;
// </Pins>

// <DAC>
//  commands
#define DAC_ADDRESS                          (0b1001100)
#define DAC_WRITE_DAC_AND_INPUT_REGISTERS    (0b00110000)
#define DAC_WRITE_CONTROL_REGISTER           (0b01000000)
#define DAC_RESET                            (0b10000000)
//  value
uint16_t dac = 0;
//  calibration
const uint16_t IDAC_SLOPE = 10446;
const int16_t IDAC_INTERCEPT = 3;
const uint16_t VDAC0_SLOPE = 32019;
const int16_t VDAC0_INTERCEPT = 7;
const uint16_t VDAC1_SLOPE = 5514;
const int16_t VDAC1_INTERCEPT = -2;
// </DAC>

// <Serial>
uint8_t commandByte; // first byte of TX/RX data
uint8_t serialData[7]; // data TX/RX
const uint32_t SERIAL_TIMEOUT = 1275000; // approx. 5.1 seconds for receiving all the data
// commands
const uint8_t QDC = 30; // query device capabilities
const uint8_t IDN = 31; // identification request
// set values
uint16_t setCurrent = 0;
uint16_t setVoltage = 0;
uint32_t setPower = 0;
uint32_t setResistance = 0;
// </Serial>

// <Watchdog>
uint16_t watchdogCounter = 0;
const uint16_t WATCHDOG_TIMEOUT = 2000;
uint8_t isWatchdogEnabled = 0;
// </Watchdog>

// <Thermal Management>
uint16_t seriesResistance = 0; // series resistance of cables, resistors etc.; only for dissipated power correction, in mOhm
const uint8_t SERIES_RESISTANCE_ID = 28;
uint32_t externallyDissipatedPower; // the power that is lost on series resistance in 4-wire mode
const uint32_t MAX_POWER = 75000; // 75 Watts maximum power
const uint8_t TEMPERATURE_THRESHOLD = 110;
uint8_t temperature = 25;
//  thermistor values
const float THERMISTOR_B = 3455;
const float THERMISTOR_T0 = 298.15;
const float THERMISTOR_R0 = 10000;
const float THERMISTOR_R1 = 1000;
// </Thermal Management>

// <Voltmeter>
uint16_t voltage = 0;
uint8_t vRange = 0; // 0 = gain 1, 1 = gain 5.7
//  hysteresis
uint16_t voltageRangeDown; // switch gain when going under
// calibration constants
const uint16_t VADC0_SLOPE = 31689;
const uint16_t VADC1_SLOPE = 5455;
const int16_t VADC0_INTERCEPT = 75;
const int16_t VADC1_INTERCEPT = 4;
// serial communication
uint8_t remoteStatus = 0; // 0 = local, 1 = remote
const uint8_t REMOTE_ID = 29;
// </Voltmeter>

// <Ammeter>
uint16_t current = 0;
//  calibration constants
const uint16_t IADC_SLOPE = 10453;
const int16_t IADC_INTERCEPT = 28;
// </Ammeter>

// <Status>
const uint8_t READY = 0;
// others stop device automatically
const uint8_t CURRENT_OVERLOAD = 1; // both ADC and DAC
const uint8_t VOLTAGE_OVERLOAD = 2; // both ADC and DAC
const uint8_t POWER_OVERLOAD = 4;
const uint8_t OVERHEAT = 8;
// current status
uint8_t loadStatus = READY;
// </Status>

// <Operating Mode>
const uint8_t MODE_CC = 0;
const uint8_t MODE_CV = 1;
const uint8_t MODE_CP = 2;
const uint8_t MODE_CR = 3;
const uint8_t MODE_CVIP = 4;
const uint8_t MODE_MPPT = 5;
uint8_t mode = MODE_CC;
// </Operating Mode>

// <Computed Values>
uint32_t power = 0;
uint32_t previousPower = 0; // used for MPPT
uint32_t previousPreviousPower = 0; // used for MPPT
#define MPPT_CURRENT_DOWN     0
#define MPPT_CURRENT_UP       1
uint8_t MPPTAction = MPPT_CURRENT_UP;
uint8_t MPPTPreviousAction = MPPT_CURRENT_DOWN;
uint32_t resistance = DVM_INPUT_RESISTANCE; // approx. input resistance of voltmeter
// </Computed Values>

#define MAX_IDAC (((4095L * IDAC_SLOPE) >> 12) + IDAC_INTERCEPT)
#define MAX_IADC (((4080L * IADC_SLOPE) >> 12) + IADC_INTERCEPT)
#define MAX_VDAC (((4095L * VDAC0_SLOPE) >> 12) + VDAC0_INTERCEPT)
#define MAX_VADC (((4080L * VADC0_SLOPE) >> 12) + VADC0_INTERCEPT)

void setup()
{      
  delayMicroseconds(10000); // delay to give the hardware some time to stabilize, it is usually not needed but just for peace of mind…
  SerialPort.begin(115200);  
  while(!Serial); // wait for the initialization of serial port  
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
  pinMode(LED_PIN, OUTPUT);
  digitalWrite(LED_PIN, LOW);
  digitalWrite(REMOTE_PIN, LOW);
  setVrange(0);
  digitalWrite(CV_PIN, LOW);
  voltageRangeDown = getVRangeSwitching();

  I2C.begin();
  initDAC();
  delay(1);
  setMode(MODE_CC);
  setI(0);

  // system watchdog  
  #ifdef UNO
    cli();
    asm("WDR");
    WDTCSR |= (1 << WDCE) | (1 << WDE);
    WDTCSR = (1 << WDE) | (1 << WDP3) | (1 << WDP0);
    sei();  
  #endif    
}

void loop()
{
  // <Watchdog>
  #ifdef UNO
    asm("WDR"); // system watchdog reset
  #endif
  watchdogCounter++;
  if ((watchdogCounter > WATCHDOG_TIMEOUT) && (isWatchdogEnabled == 1))
  {
    isWatchdogEnabled = 0;
    setMode(MODE_CC);
    setI(0);
    setVrange(0);
    setRemote(0);
    SerialPort.end();
    SerialPort.begin(115200);
    loadStatus = READY;
  }
  // </Watchdog>

  getValues();
  controlLoop();
  serialMonitor();  
}

void enableWatchdog()
{
  // communication watchdog
  watchdogCounter = 0;
  isWatchdogEnabled = 1;  
}

void getValues()
{
  // temperature
  getTemperature();
  // voltage
  getVoltage();
  // current
  getCurrent();
  // power
  previousPreviousPower = previousPower;
  previousPower = power;
  power = voltage;
  power *= current;
  power /= 1000;

  if (remoteStatus) // 4-wire mode
  {
    // some power is dissipated outside MightyWatt and is excluded from the maximum power dissipation
    externallyDissipatedPower = current;
    externallyDissipatedPower *= externallyDissipatedPower;
    externallyDissipatedPower /= 66667;
    externallyDissipatedPower *= seriesResistance;
    externallyDissipatedPower /= 15;
    if (power > (MAX_POWER + externallyDissipatedPower))
    {
      setStatus(POWER_OVERLOAD);
    }
  }
  else // 2-wire mode
  {
    if (power > MAX_POWER)
    {
      setStatus(POWER_OVERLOAD);
    }
  }
  // resistance
  if (current)
  {
    resistance = voltage;
    resistance *= 1000;
    resistance /= current;  
  }
  else
  {
    resistance = DVM_INPUT_RESISTANCE;
  }
}

void controlLoop()
{
  switch (mode)
  {
  case MODE_CC:
    {      
      // do nothing
      break;
    }
  case MODE_CV:
    {      
      // do nothing
      break;
    }
  case MODE_CP:
    {
      if (power < setPower)
      {
        plusCurrent();
      }
      else if (power > setPower)
      {
        minusCurrent();
      }
      break;
    } 
  case MODE_CR:
    {
      if (resistance < setResistance)
      {
        minusCurrent();
      }
      else if (resistance > setResistance)
      {
        plusCurrent();
      }
      break;
    }     
    case MODE_CVIP: // software constant voltage mode with opposed polarity for special purposes
    {
      if (voltage < setVoltage)
      {
        plusCurrent();
      }
      else if (voltage > setVoltage)
      {
        minusCurrent();
      }
      break;
    }
    case MODE_MPPT: // maximum power point tracker for photovoltaic panels, uses perturb and observe (hill climbing) in CC mode
    {
      uint8_t action = MPPTAction;
      if (current == 0)
      {
        MPPTAction = MPPT_CURRENT_UP;
      }
      else if (voltage == 0)
      {
        MPPTAction = MPPT_CURRENT_DOWN;
      }
      else if (MPPTAction != MPPTPreviousAction) // different former actions - choose the one that led to more favourable outcome
      {
        if (power + previousPreviousPower < 2 * previousPower)
        {
          MPPTAction = MPPTPreviousAction; 
        }
      }
      else if (previousPower > power) // both former action were the same, then judge if the last one was efficient or not; if the previous power was larger, reverse the action, otherwise stay with the current course
      {
        if (MPPTAction == MPPT_CURRENT_UP)
        {
          MPPTAction = MPPT_CURRENT_DOWN;
        }
        else
        {
          MPPTAction = MPPT_CURRENT_UP;
        }
      }
      
      // perform the computed action
      if (MPPTAction == MPPT_CURRENT_UP)
      {
        plusCurrent();    
        if (MPPTAction == MPPTPreviousAction)
        {
          plusCurrent(); 
        }
      }
      else
      {
        minusCurrent();
        if (MPPTAction == MPPTPreviousAction)
        {
          minusCurrent(); 
        }
      }

      MPPTPreviousAction = action;
    }
  }    
}

void serialMonitor()
{
  if (SerialPort.available() > 0)  
  {
    enableWatchdog();
    uint32_t timeOut = 0;
    commandByte = SerialPort.read();
    for (uint8_t i = 0; i < ((commandByte & 0b01100000) >> 5); i++)
    {
      while(SerialPort.available() == 0) 
      {
        delayMicroseconds(4);
        timeOut++; // watchdog counter
        if (timeOut > SERIAL_TIMEOUT)
        {
          break;
        }
      } // waits for data
      if (timeOut > SERIAL_TIMEOUT)
      {
        break;
      }
      serialData[i] = SerialPort.read(); // assigns data
    }      

    if (timeOut <= SERIAL_TIMEOUT)
    {      
      if (commandByte & 0b10000000)
      {        
        // set command (write to Arduino)
        setLoad(commandByte & 0b00011111);
        sendMessage(0);
      }
      else
      {
        sendMessage(commandByte & 0b00011111);
      }
    }   
  }
}

void getVoltage() // gets voltage and saves it to the global variable "voltage"
{
  uint16_t adcResult = readADC12bit(ADC_V_PIN);
  if (adcResult > 4080)
  {
    if (vRange == 0) // more than maximum range
    {
      setStatus(VOLTAGE_OVERLOAD);
    }
    else 
    {
      setVrange(0);
      getVoltage();
      return;
    }    
  }

  int32_t newVoltage = adcResult;
  if (vRange == 0)
  {
    // computation for larger range (no gain)
    newVoltage = ((newVoltage * VADC0_SLOPE) >> 12) + VADC0_INTERCEPT;
    if ((newVoltage < 0) || (adcResult == 0))
    {
      voltage = 0;
    }      
    else
    {
      voltage = newVoltage;
    }
    if ((voltage < voltageRangeDown) && (adcResult > 5))
    {
      setVrange(1); // next measurement will be in smaller (gain) range
    }
  }
  else
  {
    // computation for smaller range (with gain)
    newVoltage = ((newVoltage * VADC1_SLOPE) >> 12) + VADC1_INTERCEPT;
    if ((newVoltage < 0) || (adcResult < 10))
    {
      voltage = 0;
      setVrange(0); // against latching
    }      
    else
    {
      voltage = newVoltage;
    }
  } 
}

void getCurrent() // gets current from ADC and saves it to the global variable "current"
{
  int32_t adcResult = readADC12bit(ADC_I_PIN);
  if (adcResult > 4080)
  {
    setStatus(CURRENT_OVERLOAD);
  }
  int32_t newCurrent = adcResult;
  newCurrent = ((newCurrent * IADC_SLOPE) >> 12) + IADC_INTERCEPT;
  if ((newCurrent < 0) || (adcResult == 0))
  {
    current = 0;
  }
  else
  {
    current = newCurrent;
  }
  
  // led indication of flowing current
  if (current > (MAX_IADC / 200))
  {
     digitalWrite(LED_PIN, HIGH);
  }
  else if (current < (MAX_IADC / 300))
  {
     digitalWrite(LED_PIN, LOW);
  }
}

void getTemperature() // gets temperature from ADC and saves it to the global variable "temperature"
{
  int16_t adc = analogRead(ADC_T_PIN);  
  #if defined(ZERO) || defined(DUE)
    float t = 1/((log(THERMISTOR_R1*adc/(4096-adc))-log(THERMISTOR_R0))/THERMISTOR_B + 1/THERMISTOR_T0)-273.15;
  #endif
  #ifdef UNO
    float t = 1/((log(THERMISTOR_R1*adc/(1024-adc))-log(THERMISTOR_R0))/THERMISTOR_B + 1/THERMISTOR_T0)-273.15;
  #endif
  if (t < 0)
  {
    temperature = 0;
  }
  else if (t > 255)
  {
    temperature = 255;
  }
  else
  {
    temperature = t;
  }

  if (temperature > TEMPERATURE_THRESHOLD)
  {
    setStatus(OVERHEAT);
  }
}

uint16_t getVRangeSwitching()
{
  uint32_t Vadc = VADC1_SLOPE;
  Vadc = (((4080 * Vadc) >> 12) + VADC1_INTERCEPT) << 5;
  Vadc /= 33; // 97% of full-scale value
  return Vadc;
}

void setI(uint16_t value) // set current, unit = 1 mA
{  
  //setMode(MODE_CC);
  setCurrent = value;
  int32_t dacValue = value;
  dac = 0;

  if (setCurrent > 0)
  {
    dacValue = ((dacValue - IDAC_INTERCEPT) << 12) / IDAC_SLOPE;
  }

  if (dacValue > 4095)
  {
    setStatus(CURRENT_OVERLOAD);
  }
  else if (dacValue > 0)
  {
    dac = dacValue;
  }
  setDAC();
}

void plusCurrent()
{
  if (dac < 4095)
  {
    dac++;
    setDAC();
  }
}

void minusCurrent()
{
  if (dac > 0)
  {
    dac--;
    setDAC();
  }
}

void setV(uint16_t value)
{
 // setMode(MODE_CV);
  setVoltage = value;
  int32_t dacValue = value;
  if (vRange == 0)
  {
    // no gain
    dacValue = ((dacValue - VDAC0_INTERCEPT) << 12) / VDAC0_SLOPE;
  }
  else
  {
    // gain
    dacValue = ((dacValue - VDAC1_INTERCEPT) << 12) / VDAC1_SLOPE;
  }  
  
  if (dacValue > 4095)
  {
    if (vRange == 0)
    {
      dac = 4095;
      setDAC();
      setStatus(VOLTAGE_OVERLOAD);
    }
    else
    {
      setVrange(0);
      setV(setVoltage);
    }
  }
  else if (dacValue >= 0)
  {
    dac = dacValue;
    setDAC();
  }
}

void setMode(uint8_t newMode)
{
  switch (newMode)
  {
  case MODE_CC:
    {
      if (mode == MODE_CV)
      {
        dac = 0;
        setDAC();
      }
      digitalWrite(CV_PIN, LOW);
      break;
    }
  case MODE_CV:
    {
      if (mode != MODE_CV)
      {
        dac = 4095;
        setDAC();
      }
      digitalWrite(CV_PIN, HIGH);
      break;
    }
  case MODE_CP:
    {
      if (mode == MODE_CV)
      {
        dac = 0;
        setDAC();
      }
      digitalWrite(CV_PIN, LOW);
      break;
    }
  case MODE_CR:
    {
      if (mode == MODE_CV)
      {
        dac = 0;
        setDAC();
      }
      digitalWrite(CV_PIN, LOW);
      break;
    }  
  case MODE_CVIP:
    {
      if (mode == MODE_CV)
      {
        dac = 0;
        setDAC();
      }
      digitalWrite(CV_PIN, LOW);
      break;
    }
    case MODE_MPPT:
    {
      if (mode == MODE_CV)
      {
        dac = 0;
        setDAC();
      }
      digitalWrite(CV_PIN, LOW);
      break;
    }
  }
  mode = newMode;
}

void setLoad(uint8_t id) // procedure called when there is a set (write to Arduino) data command
{  
  switch (id)
  {
  case MODE_CC:
    {
      setMode(MODE_CC);
      uint16_t b0 = serialData[0];
      uint16_t b1 = serialData[1];
      setI((b0 << 8) | b1);
      break;
    }
  case MODE_CV:
    {
      setMode(MODE_CV);
      uint16_t b0 = serialData[0];
      uint16_t b1 = serialData[1];
      setV((b0 << 8) | b1);
      break;
    }
  case MODE_CP:
    {
      setMode(MODE_CP);
      uint32_t b0 = serialData[0];
      uint32_t b1 = serialData[1];
      uint32_t b2 = serialData[2];
      setPower = (b0 << 16) | (b1 << 8) | b2;
      break;
    }
  case MODE_CR:
    {
      setMode(MODE_CR);
      uint32_t b0 = serialData[0];
      uint32_t b1 = serialData[1];
      uint32_t b2 = serialData[2];
      setResistance = (b0 << 16) | (b1 << 8) | b2;      
      break;
    }        
  case MODE_CVIP:
    {
      setMode(MODE_CVIP);
      uint16_t b0 = serialData[0];
      uint16_t b1 = serialData[1];
      setVoltage = (b0 << 8) | b1;
      break;
    }   
  case MODE_MPPT:
    {
      setMode(MODE_MPPT);
      uint16_t b0 = serialData[0];
      uint16_t b1 = serialData[1];
      setI((b0 << 8) | b1);
      break;
    } 
  case REMOTE_ID:
   {
     setRemote(serialData[0]);
     break;
   }
  case SERIES_RESISTANCE_ID:
    {
      uint16_t b0 = serialData[0];
      uint16_t b1 = serialData[1];
      seriesResistance = (b0 << 8) | b1;
      break;
    }
  }
}

void setRemote(uint8_t value)
{
  if (value == 0) // local
  {
      digitalWrite(REMOTE_PIN, LOW);
      remoteStatus = 0;
  }
  else // remote
  {
      digitalWrite(REMOTE_PIN, HIGH);
      remoteStatus = 1;
  }
}

void sendMessage(uint8_t command) // procedure called when there is a send (read from Arduino) data command; MSB first
{  
  switch (command)
  {
  case IDN:
    {
      SerialPort.println("MightyWatt");
      break;
    }
  case QDC:
    {      
      SerialPort.println(FW_VERSION);
      SerialPort.println(BOARD_REVISION);            
      SerialPort.println(MAX_IDAC);
      SerialPort.println(MAX_IADC);
      SerialPort.println(MAX_VDAC);
      SerialPort.println(MAX_VADC);
      SerialPort.println(MAX_POWER);
      SerialPort.println(DVM_INPUT_RESISTANCE);
      SerialPort.println(TEMPERATURE_THRESHOLD);           
      break;
    }
  case SERIES_RESISTANCE_ID:
    {
      SerialPort.println(seriesResistance);
      break;
    }
  default:
    {
      serialData[0] = current >> 8;
      serialData[1] = current & 0xFF;
      serialData[2] = voltage >> 8;
      serialData[3] = voltage & 0xFF;
      serialData[4] = temperature;
      serialData[5] = remoteStatus;
      serialData[6] = loadStatus;
      SerialPort.write(serialData, 7);
      loadStatus = READY;
      break;
    }
  }
}

void setVrange(uint8_t newRange)
{  
  vRange = newRange; 
  if (newRange == 0)
  {
    digitalWrite(V_GAIN_PIN, LOW); // range 0
    if (mode == MODE_CV)
    {
      setV(setVoltage);
    }
  }
  else
  {    
    digitalWrite(V_GAIN_PIN, HIGH); // range 1
    if (mode == MODE_CV)
    {
      setV(setVoltage);
    }    
  }
}

void setStatus(uint8_t code)
{
  loadStatus |= code;
  if (code)
  {
    setMode(MODE_CC);
    setI(0);
  }
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

uint16_t readADC12bit(int16_t channel) // oversamples ADC to 12 bit (UNO) or averages 16 samples (DUE)
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
