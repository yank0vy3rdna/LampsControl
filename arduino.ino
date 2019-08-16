/*
  Скетч для взаимодействия c Arduino
 */


#include <SPI.h>// Без этого не работает, не убирать!
#include <Ethernet.h>//Еthernet библиотека
#include <EEPROM.h>//Библиотека для работы с памятью
#include <EthernetUdp.h>//Библиотека UDP 

#define UDP_RX_PACKET_MAX_SIZE_MY 30 //размер буфера приема
#define UDP_TX_PACKET_MAX_SIZE_MY 30 //размер буфера передачи
//#define LED_PIN 13 //Пин лампы
#define LED_PIN 4 //Пин лампы
//#define PORT 8888
byte mac[] = {0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED }; //mac - адрес ethernet shielda
byte ip[] = {10, 200, 0, 237 };        // ip адрес ethernet shielda
byte subnet[] = {255, 255, 128, 0 }; //маска подсети
byte gateway[] = {10, 200, 0, 1 }; //маска подсети
byte server[] = {10,200,80,8};
unsigned int PORT = 8888;      // local port to listen on
unsigned int PORTtx = 6000;      // local port to listen on
//unsigned int PORTtx = 8888;      // local port to listen on

byte priem_name = 255;//Имя приемника

//byte признак перенастройки;
// - 0 ничего не перенастраивать
// - 0xA0 (1010 0000) - включить лампу
// - 0x50 (0101 0000) - вЫключить лампу
// - (хххх 1111) перенастроить имя приемника, MAC, IP, MASK, GW
byte priem_priznak = 0;

// ubrat -- IPAddress remotest(10, 200, 1, 88);

//статус состояния лампы
//0 - горит
//не 0 - не горит
byte status_lamp = 0;

int U; //переменная куда считываю значение напряжения с аналогового входа А0
int I; //переменная куда считываю значение тока с аналогового входа А1

EthernetUDP Udp;// Инициализация экземпляра класса EthernetUDP

//=======================================================
//===              ПЕРЕЗАГРУЗКА ARDUINO               === 
//=======================================================
void(* resetFunc) (void) = 0; // Функция для перезагрузки

//================================================================================
//===                       ЗАПИСЬ ДАННЫХ В EEPROM                             ===
//===--------------------------------------------------------------------------===
//===       IP - 1-4 байты                                                     ===
//===       MAC - 5-10 байты                                                   ===
//===       Номер - 11 байт                                                    ===
//===       Шлюз - 12-15 байты                                                 ===
//===       Маска - 16-19 байты                                                ===
//================================================================================
void writeaddresses(byte ips[], byte macs[],int nmr, byte gateways[], byte masks[])
{
Serial.println();
Serial.println("Zapis v EEPROM");
Serial.print("IP = ");
  for (byte i = 1; i < 5; i++) //Запись IP адреса
  {
    EEPROM.write(i, ips[i-1]);
    Serial.print(ips[i-1], DEC);
      if (i < 4)
      {
        Serial.print(".");
      }
  }
Serial.println();

 

Serial.print("MAC = ");
  for (byte i = 5; i < 11; i++) {//Запись MAC адреса
    EEPROM.write(i, macs[i - 5]);
    Serial.print(macs[i - 5], HEX);
      if (i < 9)
      {
        Serial.print("-");
      }

  }
Serial.println();


  EEPROM.write(11, nmr);//Запись номера
Serial.print("Name (number) = ");
Serial.print(nmr, DEC);
Serial.println();

Serial.print("GW = ");
  for (byte i = 12; i < 16; i++) //Запись адреса шлюза
  {
    EEPROM.write(i, gateways[i-12]);
    Serial.print(gateways[i-12], DEC);
      if (i < 15)
      {
        Serial.print(".");
      }
  }
Serial.println();

Serial.print("MASK = ");
  for (byte i = 16; i < 20; i++) //Запись маски
  {
    EEPROM.write(i, masks[i-16]);
    Serial.print(masks[i-16], DEC);
      if (i < 19)
      {
        Serial.print(".");
      }
  }
Serial.println();
Serial.println();

  delay(5000);
}

//----------------------nov--------------------------------------------
//---------------------------------------------------------------------
void ReadAddressEeprom()//Функция чтения данных из EEPROM
{
  //IP - 1-4 байты
  //MAC - 5-10 байты
  //Номер - 11 байт
  //Шлюз - 12-15 байты
  //Маска - 16-19 байты
 byte ips[] = {0, 0, 0, 0};
 byte macs[] = {0, 0, 0, 0, 0, 0};
 int nmr = 0;
 byte gateways[] = {0, 0, 0, 0};
 byte masks[] = {0, 0, 0, 0};
 
 
  Serial.println();
  Serial.println("read and print from EEPROM");
  Serial.print("IP address = ");
 for (byte i = 1; i < 5; i++) //Запись IP адреса
  {
    ips[i-1] = EEPROM.read(i);
    Serial.print(ips[i-1], DEC);
      if (i < 4)
      {
        Serial.print(".");
      }
 }

  Serial.println();
  Serial.println();

  Serial.print("MAC address = ");
  for (byte i = 5; i < 11; i++) {//Запись MAC адреса
    macs[i - 5] = EEPROM.read(i);
    Serial.print(macs[i - 5], HEX);
      if (i < 10)
      {
        Serial.print("-");
      }
  }

  Serial.println();
  Serial.println();

  Serial.print("NAME = ");
   nmr = EEPROM.read(11);//Запись номера
   Serial.print(nmr, DEC);

  Serial.println();
  Serial.println();

  Serial.print("GW = ");
  for (byte i = 12; i < 16; i++) //Запись адреса шлюза
  {
    gateways[i-12] = EEPROM.read(i);
    Serial.print(gateways[i-12]);
      if (i < 15)
      {
        Serial.print(".");
      }
  }

  Serial.println();
  Serial.println();

  Serial.print("MASK = ");
  for (byte i = 16; i < 20; i++) //Запись маски
  {
   masks[i-16] = EEPROM.read(i);
   Serial.print(masks[i-16], DEC);
      if (i < 19)
      {
        Serial.print(".");
      }
  }
  Serial.println();
  Serial.println();
}


//----------------------nov--------------------------------------------
//---------------------------------------------------------------------
void PrintAddressTEK()//Функция печати текущих настроек
{
  //IP - 1-4 байты
  //MAC - 5-10 байты
  //Номер - 11 байт
  //Шлюз - 12-15 байты
  //Маска - 16-19 байты
// byte ips[] = {0, 0, 0, 0};
// byte macs[] = {0, 0, 0, 0, 0, 0};
// int nmr = 0;
// byte gateways[] = {0, 0, 0, 0};
// byte masks[] = {0, 0, 0, 0};
 
 
  Serial.println();
  Serial.println("Print tek nastroek");
  Serial.print("IP address = ");
 for (byte i = 0; i < 4; i++) //Запись IP адреса
  {
    Serial.print(ip[i], DEC);
      if (i < 3)
      {
        Serial.print(".");
      }
 }

  Serial.println();
  Serial.println();

  Serial.print("MAC address = ");
  for (byte i = 0; i < 6; i++) {//Запись MAC адреса
    Serial.print(mac[i], HEX);
      if (i < 5)
      {
        Serial.print("-");
      }
  }

  Serial.println();
  Serial.println();

  Serial.print("NAME = ");
   Serial.print(priem_name, DEC);

  Serial.println();
  Serial.println();

  Serial.print("GW = ");
  for (byte i = 0; i < 4; i++) //Запись адреса шлюза
  {
    Serial.print(gateway[i]);
      if (i < 3)
      {
        Serial.print(".");
      }
  }

  Serial.println();
  Serial.println();

  Serial.print("MASK = ");
  for (byte i = 0; i < 4; i++) //Запись маски
  {
   Serial.print(subnet[i], DEC);
      if (i < 3)
      {
        Serial.print(".");
      }
  }
  Serial.println();
  Serial.println();
}



//----------------------nov-----------------------------------------------------
//------------------------------------------------------------------------------

int getaddresses()//Чтение IP адреса
{
  priem_name = EEPROM.read(11);//Чтение номера устройства
  if (priem_name != 255) //с
  { //значит уже какой-то адрес запрограмирован и его нужно считать
    //считываем IP адрес
    for (byte i = 1; i < 5; i++) 
    {
      ip[i-1]=EEPROM.read(i);
    }
    //считываем MAC адрес
    for (byte i = 5; i < 11; i++) 
    {
      mac[i - 5]=EEPROM.read(i);
    }
    //счмтываем адрес шлюза
    for (byte i = 12; i < 16; i++) 
    {
      gateway[i-12]=EEPROM.read(i);
    }  
    //считываем маску
    for (byte i = 16; i < 20; i++) 
    {
       subnet[i-16]=EEPROM.read(i);
    }
  } 
  //ИНАЧЕ (priem_name==255) - в EEPROM ничего не было записанно
  //и считывать настройки не нужно
return priem_name;
}


void takingaddress()
{
Serial.println("test point takingaddress 1 ");
  int packetSize = 0;//ожидание пакета

PrintAddressTEK();

  Ethernet.begin(mac, ip, subnet, gateway);
  Udp.begin(PORT);

  byte packetBuffer[UDP_RX_PACKET_MAX_SIZE_MY];// Буфер приемника - куда будет принят пакет UDP

  byte ReplyBuffer[UDP_TX_PACKET_MAX_SIZE_MY]; // Буфер передатчика - данные которые отправятся в пакет UDP
  
  ReplyBuffer[0] = priem_name;//Формирование пакета

  ReplyBuffer[1] = 0x0F;

  ReplyBuffer[2] = mac[0];
  ReplyBuffer[3] = mac[1];
  ReplyBuffer[4] = mac[2];
  ReplyBuffer[5] = mac[3];
  ReplyBuffer[6] = mac[4];
  ReplyBuffer[7] = mac[5];

  ReplyBuffer[8] = ip[0];
  ReplyBuffer[9] = ip[1];
  ReplyBuffer[10] = ip[2];
  ReplyBuffer[11] = ip[3];

  ReplyBuffer[12] = subnet[0];
  ReplyBuffer[13] = subnet[1];
  ReplyBuffer[14] = subnet[2];
  ReplyBuffer[15] = subnet[3];

  ReplyBuffer[16] = gateway[0];
  ReplyBuffer[17] = gateway[1];
  ReplyBuffer[18] = gateway[2];
  ReplyBuffer[19] = gateway[3];

  U=analogRead(0);

  ReplyBuffer[21] = lowByte(U);
  ReplyBuffer[20] = highByte(U);

    I=izmer_I();
//    I=analogRead(A1);
  ReplyBuffer[27] = lowByte(I);
  ReplyBuffer[28] = highByte(I);

  ReplyBuffer[29] = status_lamp;


Serial.println("test point takingaddress 2 ");

//  Udp.beginPacket(server, PORT);//отправка
  Udp.beginPacket(server, PORTtx);//отправка
  Udp.write(ReplyBuffer,UDP_TX_PACKET_MAX_SIZE_MY);
  Udp.endPacket();
  

Serial.println("test point takingaddress 3 ");
  
  while(!packetSize){
    packetSize = Udp.parsePacket();
  }

Serial.println("test point takingaddress 4 ");


  if(packetSize)//если пришел пакет, читаю его
  {
    Serial.print("Received packet of size ");
    Serial.println(packetSize);
    Serial.print("From ");
    IPAddress RIP = Udp.remoteIP();
    int RPort = Udp.remotePort();
    for (int i =0; i < 4; i++)
    {
      Serial.print(RIP[i], DEC);
      if (i < 3)
      {
        Serial.print(".");
      }
    }
    Serial.print(", port ");
    Serial.println(RPort);

    Udp.read(packetBuffer,UDP_TX_PACKET_MAX_SIZE_MY);  

Serial.println("test point takingaddress 5 ");
delay (3000);

    if(packetBuffer[1] == 15){//0F - перезапись IP настроек

Serial.println("test point takingaddress 6 ");
delay (5000);
    
      // ДА - получены новые настройки IP
      //считываю из пакета данные
      // IP
      byte ipsave[4]={packetBuffer[8],packetBuffer[9],packetBuffer[10],packetBuffer[11]};
      // MAC
      byte macsave[6]={packetBuffer[2],packetBuffer[3],packetBuffer[4],packetBuffer[5],packetBuffer[6],packetBuffer[7]};
      // номер устройства (его имя)
      byte nmrsave = packetBuffer[0];
      // IP servera
      byte snsave[4] = {packetBuffer[12],packetBuffer[13],packetBuffer[14],packetBuffer[15]};
      // GW
      byte gwsave[4] = {packetBuffer[16],packetBuffer[17],packetBuffer[18],packetBuffer[19]};
        
      //запись в EEPROM память ip,mac,nmr,gateway и subnet
      writeaddresses(ipsave,macsave,nmrsave,gwsave,snsave);

Serial.println("test point takingaddress 6.1 ");
      
      //перезагрузка
      resetFunc();
    } 
    else{ // НЕТ - принятый пакет не содержит новых настроек IP
      
Serial.println("test point takingaddress 7 ");
delay (5000); 

      //перезагрузка
      resetFunc();
    }
  }
  
}




//================================================================================
//===    ВКЛЮЧЕНИЕ ЛАМП с зедержкой зависимой от номера (имени) устройства     ===
//================================================================================


//Задержка для избежания моментального повышения нагрузки на питание ламп

void randomLamp()
{
  int vr;
  // - считываем имя устройства
  vr = getaddresses();
Serial.print("name ustroystva =  ");
Serial.println(vr);

//      while (vr > 10) {
//        vr = vr / 10;
//    }

  vr = vr % 10;

Serial.print("name menshe 10 = ");
Serial.println(vr);

  vr = vr * 1000;

Serial.print("zadervka v mc = ");
Serial.println(vr);

  delay(vr);

Serial.print("status LAMP do = ");
Serial.println(status_lamp);

  digitalWrite(LED_PIN, LOW);
  status_lamp = 0; //не горит

Serial.print("status LAMP posle = ");
Serial.println(status_lamp);

}
//
//
//
int izmer_I()//Чтение IP адреса
{
  int I_temp = 0;
  I_temp = analogRead(A1);
  delay(50);
  I_temp = I_temp + analogRead(A1);
  delay(50);
  I_temp = I_temp + analogRead(A1);
  delay(50);
  I_temp = I_temp + analogRead(A1);
  delay(50);

  I_temp = I_temp >> 2;

  return I_temp;
}




void setup() {
  //Первым делом - гашу лампу
    pinMode(LED_PIN, OUTPUT);
    digitalWrite(LED_PIN, HIGH); //погасить лампу
    status_lamp = 1; //не горит
    delay(2000);
  //Инициализация последовательного порта
    Serial.begin(9600); 
    while (!Serial) {
    }

Serial.println("VKL Lamp");
    //включение лампы с задержкой зависимой от номера (имени) устройства
    randomLamp();
    
    delay(2000);

//Serial.println("test point 1 ");
//PrintAddressTEK();
//ReadAddressEeprom();

  //Считываю из EEPRM номер устройства и если он не равен 255, то
  //считываю и остальные настройки IP
  I = getaddresses();

//Serial.print("i = ");
//Serial.println(I, DEC);
//Serial.println("test point 2 ");
  
  if(I==255) //ЕСЛИ номер устройства равен 255,
  { //ТО устройство нужно инициализировать
//Serial.println("test point 3 ");
    takingaddress();//Обращение к программе инициализации
//Serial.println("test point 3_1 ");
//PrintAddressTEK();
//ReadAddressEeprom();
  }
  else //ИНАЧЕ - устройство уже инициализированно 
  {    //- производим его загрузку
//Serial.println("test point 4 ");

    Ethernet.begin(mac, ip, subnet, gateway);//инициализация сети
    Udp.begin(PORT);

//Serial.println("test point 5 - END SETUP ");

  }
}

void loop() {

  byte packetBuffer[UDP_RX_PACKET_MAX_SIZE_MY];// Буфер приемника - куда будет принят пакет UDP

  byte ReplyBuffer[UDP_TX_PACKET_MAX_SIZE_MY]; // Буфер передатчика - данные которые отправятся в пакет UDP
  int packetSize = Udp.parsePacket();

  if(packetSize)//если пришел пакет, читаю его
  {
 //----------------------nov-------------------
    Serial.print("Received packet of size ");
    Serial.println(packetSize);
    Serial.print("From ");
    IPAddress remote = Udp.remoteIP();
    for (int i =0; i < 4; i++)
    {
      Serial.print(remote[i], DEC);
      if (i < 3)
      {
        Serial.print(".");
      }
    }
    Serial.print(", port ");
    Serial.println(Udp.remotePort());
//----------------------nov-------------------
   
    Udp.read(packetBuffer,UDP_TX_PACKET_MAX_SIZE_MY);
    // Запись в буфер для отправки
//  if(ReplyBuffer[1] == 0x00){
  if(packetBuffer[1] == 0x00){

Serial.println("Received packet 00 - izmer");

    ReplyBuffer[0] = priem_name;

    ReplyBuffer[1] = 0x00;  

    ReplyBuffer[2] = mac[0];
    ReplyBuffer[3] = mac[1];
    ReplyBuffer[4] = mac[2];
    ReplyBuffer[5] = mac[3];
    ReplyBuffer[6] = mac[4];
    ReplyBuffer[7] = mac[5];

    ReplyBuffer[8] = ip[0];
    ReplyBuffer[9] = ip[1];
    ReplyBuffer[10] = ip[2];
    ReplyBuffer[11] = ip[3];

    ReplyBuffer[12] = subnet[0];
    ReplyBuffer[13] = subnet[1];
    ReplyBuffer[14] = subnet[2];
    ReplyBuffer[15] = subnet[3];

    ReplyBuffer[16] = gateway[0];
    ReplyBuffer[17] = gateway[1];
    ReplyBuffer[18] = gateway[2];
    ReplyBuffer[19] = gateway[3];

    U=analogRead(A0);
    ReplyBuffer[21] = lowByte(U);
    ReplyBuffer[20] = highByte(U);

    I=izmer_I();
//    I=analogRead(A1);
    ReplyBuffer[27] = lowByte(I);
    ReplyBuffer[28] = highByte(I);

    ReplyBuffer[29] = status_lamp;
  }
//  else if(ReplyBuffer[1] == 0xA0)
  else if(packetBuffer[1] == 0xA0)
  {
    
Serial.println("Received packet A0 - VKL lamp");

    ReplyBuffer[0] = priem_name;

    ReplyBuffer[1] = 0xA0;  

    ReplyBuffer[2] = mac[0];
    ReplyBuffer[3] = mac[1];
    ReplyBuffer[4] = mac[2];
    ReplyBuffer[5] = mac[3];
    ReplyBuffer[6] = mac[4];
    ReplyBuffer[7] = mac[5];

    ReplyBuffer[8] = ip[0];
    ReplyBuffer[9] = ip[1];
    ReplyBuffer[10] = ip[2];
    ReplyBuffer[11] = ip[3];

    ReplyBuffer[12] = subnet[0];
    ReplyBuffer[13] = subnet[1];
    ReplyBuffer[14] = subnet[2];
    ReplyBuffer[15] = subnet[3];

    ReplyBuffer[16] = gateway[0];
    ReplyBuffer[17] = gateway[1];
    ReplyBuffer[18] = gateway[2];
    ReplyBuffer[19] = gateway[3];
    
    status_lamp=0;
    digitalWrite(LED_PIN,LOW);
    
    U=analogRead(A0);
    ReplyBuffer[21] = lowByte(U);
    ReplyBuffer[20] = highByte(U);

    I=izmer_I();
//    I=analogRead(A1);
    ReplyBuffer[27] = lowByte(I);
    ReplyBuffer[28] = highByte(I);
    
    ReplyBuffer[29] = status_lamp;
  }
//  else if(ReplyBuffer[1] == 0x50)
  else if(packetBuffer[1] == 0x50)
  {

Serial.println("Received packet 50 - VyKL lamp");

    ReplyBuffer[0] = priem_name;

    ReplyBuffer[1] = 0x50;  

    ReplyBuffer[2] = mac[0];
    ReplyBuffer[3] = mac[1];
    ReplyBuffer[4] = mac[2];
    ReplyBuffer[5] = mac[3];
    ReplyBuffer[6] = mac[4];
    ReplyBuffer[7] = mac[5];

    ReplyBuffer[8] = ip[0];
    ReplyBuffer[9] = ip[1];
    ReplyBuffer[10] = ip[2];
    ReplyBuffer[11] = ip[3];

    ReplyBuffer[12] = subnet[0];
    ReplyBuffer[13] = subnet[1];
    ReplyBuffer[14] = subnet[2];
    ReplyBuffer[15] = subnet[3];

    ReplyBuffer[16] = gateway[0];
    ReplyBuffer[17] = gateway[1];
    ReplyBuffer[18] = gateway[2];
    ReplyBuffer[19] = gateway[3];
    
    status_lamp=1;
    digitalWrite(LED_PIN,HIGH);
    
    U=analogRead(A0);
    ReplyBuffer[21] = lowByte(U);
    ReplyBuffer[20] = highByte(U);

    I=izmer_I();
//    I=analogRead(A1);
    ReplyBuffer[27] = lowByte(I);
    ReplyBuffer[28] = highByte(I);
    
    ReplyBuffer[29] = status_lamp;
//  }else if(ReplyBuffer[1] == 0x0F)
  }

Serial.println("Send packet to Udp.remoteIP, Udp.remotePort");

    // send a reply, to the IP address and port that sent us the packet we received
    Udp.beginPacket(Udp.remoteIP(), Udp.remotePort());
    Udp.write(ReplyBuffer,UDP_TX_PACKET_MAX_SIZE_MY);
    Udp.endPacket();

Serial.println("----------------------");

  }
  delay(10);
}
