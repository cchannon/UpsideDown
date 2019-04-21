#include <SPI.h>
#include <WiFi101.h>
#include "arduino_secrets.h" 
#include <Adafruit_NeoPixel.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define LED_PIN    9
#define LED_COUNT 96
#define OLED_RESET 3

Adafruit_SSD1306 display(128, 32, &Wire, OLED_RESET);
Adafruit_NeoPixel strip(LED_COUNT, LED_PIN, NEO_GRB + NEO_KHZ800);

char ssid[] = SECRET_SSID;        // your network SSID (name)
char pass[] = SECRET_PASS;    // your network password (use for WPA, or use as key for WEP)
int keyIndex = 0;                 // your network key Index number (needed only for WEP)
const int buttonPin = 2;
int status = WL_IDLE_STATUS;
WiFiServer server(80);

void setup() {
  WiFi.setPins(8,7,4,2);
  Serial.begin(115200);      // initialize serial communication
  pinMode(13, OUTPUT);      // set the LED pin mode
  pinMode(buttonPin, INPUT_PULLUP);
  display.begin(SSD1306_SWITCHCAPVCC, 0x3C);
  display.display();
  delay(1500);
  display.clearDisplay();
  
  // check for the presence of the shield:
  if (WiFi.status() == WL_NO_SHIELD) {
    Serial.println("WiFi shield not present");
    while (true);       // don't continue
  }
    
  Serial.print("Connecting to: ");
  Serial.println(ssid);   
  // attempt to connect to WiFi network:
  while ( status != WL_CONNECTED) {
    Serial.print("Connecting to: ");
    Serial.println(ssid);                   // print the network name (SSID);

    // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
    status = WiFi.begin(ssid, pass);
    // wait 10 seconds for connection:
    delay(10000);
  }
  server.begin();                           // start the web server on port 80
  printWiFiStatus();                        // you're connected now, so print out the status
  
  strip.begin();                            // INITIALIZE NeoPixel strip object (REQUIRED)
  strip.show();                             // Turn OFF all pixels ASAP
  strip.setBrightness(50);                  // Set BRIGHTNESS to about 1/5 (max = 255)
}

void loop() {
  WiFiClient client = server.available();   // listen for incoming clients
  if (client) {                             // if you get a client,
    Serial.println("new client");           // print a message out the serial port
    String currentLine = "";                // make a String to hold incoming data from the client
    while (client.connected()) {            // loop while the client's connected
      if (client.available()) {             // if there's bytes to read from the client,
        char c = client.read();             // read a byte, then
        Serial.write(c);                    // print it out the serial monitor
        if (c == '\n') {                    // if the byte is a newline character

          // if the current line is blank, you got two newline characters in a row.
          // that's the end of the client HTTP request, so send a response:
          if (currentLine.length() == 0) {
            // HTTP headers always start with a response code (e.g. HTTP/1.1 200 OK)
            // and a content-type so the client knows what's coming, then a blank line:
            client.println("HTTP/1.1 200 OK");
            client.println("Content-type:text/html");
            client.println();

            // The HTTP response ends with another blank line:
            client.println();
            // break out of the while loop:
            break;
          }
          else {      // if you got a newline, then clear currentLine:
            currentLine = "";
          }
        }
        else if (c != '\r') {    // if you got anything else but a carriage return character,
          currentLine += c;      // add it to the end of the currentLine
        }
        else if (currentLine.startsWith("GET /")) {
          currentLine.remove(0,currentLine.indexOf("/") + 1);
          currentLine.remove(currentLine.lastIndexOf(" "), currentLine.length());
          Serial.println("\n");
          Serial.println(currentLine);
          int numChar = currentLine.length();
          
          char reply[30];
          currentLine.toCharArray(reply, 30);

          sparkle(60);
          colorWipe(strip.Color(0, 0, 0), 25);
          
          for (int i = 0; i < numChar; i++){
            Serial.println(letterAddress(currentLine[i]));
            if(letterAddress(currentLine[i]) != 0){
            strip.setPixelColor(letterAddress(currentLine[i]), strip.Color(127, 127, 127));
            strip.show();
            delay(500);
            strip.setPixelColor(letterAddress(currentLine[i]), strip.Color(0, 0, 0));
            strip.show();
            delay(250);
            }
            else {
              delay(750);
            }
          }
        }
      }
    }
    // close the connection:
    client.stop();
    Serial.println("client disonnected");
  }
}

int letterAddress(char letter) {
  switch (letter) {
  case 'A':
    return 74;
    break;
  case 'B':
    return 77;
    break;
  case 'C':
    return 80;
    break;
  case 'D':
    return 83;
    break;
  case 'E':
    return 86;
    break;
  case 'F':
    return 89;
    break;
  case 'G':
    return 92;
    break;
  case 'H':
    return 95;
    break;
  case 'I':
    return 64;
    break;
  case 'J':
    return 60;
    break;
  case 'K':
    return 57;
    break;
  case 'L':
    return 54;
    break;
  case 'M':
    return 50;
    break;
  case 'N':
    return 47;
    break;
  case 'O':
    return 44;
    break;
  case 'P':
    return 41;
    break;
  case 'Q':
    return 37;
    break;
  case 'R':
    return 1;
    break;
  case 'S':
    return 4;
    break;
  case 'T':
    return 7;
    break;
  case 'U':
    return 11;
    break;
  case 'V':
    return 14;
    break;
  case 'W':
    return 17;
    break;
  case 'X':
    return 21;
    break;
  case 'Y':
    return 24;
    break;
  case 'Z':
    return 27;
    break;
  default:
    return 0;
    break;
  }
}

void printWiFiStatus() {
  // print the SSID of the network you're attached to:
  Serial.print("SSID: ");
  Serial.println(WiFi.SSID());

  // print your WiFi shield's IP address:
  IPAddress ip = WiFi.localIP();
  Serial.print("IP Address: ");
  Serial.println(ip);

  // print the received signal strength:
  long rssi = WiFi.RSSI();
  Serial.print("signal strength (RSSI):");
  Serial.print(rssi);
  Serial.println(" dBm");

  display.setTextSize(1);
  display.setTextColor(WHITE);
  display.setCursor(0,0);
  display.print("Connected to:");
  display.println(WiFi.SSID());
  display.setCursor(0,16);
  display.print("IP address: ");
  display.println(ip);
  display.display();
  delay(2000);
}

void colorWipe(uint32_t color, int wait) {
  for(int i=0; i<strip.numPixels(); i++) { // For each pixel in strip...
    strip.setPixelColor(i, color);         //  Set pixel's color (in RAM)
    strip.show();                          //  Update strip to match
    delay(wait);                           //  Pause for a moment
  }
}

void run(){
  char thisWord[] = {'R','U','N'};
  for (int i=0;i<4;i++){
    int light = 500-100*i;
    int dark = 250-50*i;
    for (int j=0;j<3;j++){
      strip.setPixelColor(letterAddress(thisWord[j]), 255, 0, 0);
      strip.show();
      delay(light);
      strip.setPixelColor(letterAddress(thisWord[j]), 0, 0, 0);
      strip.show();
      delay(dark);
    }
  }
  char alphabet[] = {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'}
  for (int j=0;j<3;j++){
    for (int i=0;i<26;i++){
      strip.setPixelColor(letterAddress(alphabet[i]), 127, 0, 0);
    }
    strip.show();
    delay(250);
    for (int i=0;i<26;i++){
      strip.setPixelColor(letterAddress(alphabet[i]), 0, 0, 0);
    }
    strip.show();
    delay(250);
  }
}

void sparkle(int wait) {
  String sparkle = "OIAYTNLIAEBTQZXDJKFNKJBENYTOIEUWPCOGSEFZSELHTNGLKZ";
  char reply[50];
  sparkle.toCharArray(reply, 50);
  for(int i=0; i<50; i++) { // For each pixel in strip...
    strip.setPixelColor(letterAddress(sparkle[i]), strip.Color(random(127), random(127), random(127)));
    strip.show();
    delay(wait);
    strip.setPixelColor(letterAddress(sparkle[i]), strip.Color(0, 0, 0));
    strip.show();
  }
}
