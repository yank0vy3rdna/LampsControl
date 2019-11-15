# LampsControl
Managing street lights

## Controller with arduino+ethernet+relay:

![](https://i.ibb.co/f256Qfj/image.png)

`arduino\arduino.ino`

## Python main app

requests all controllers to get amperage/voltage data and record it to MySQL or send on/off commands from WebUI or time of sunsets and sunrises for each lamp controller.

`main\main.py`

## Python CGI for WebUI + IIS + JavaScript frontend

you can on/off every lamp, on/off all lamps by the only one click and see sunset/sunrise times by that UI

![](https://i.ibb.co/XZyyS5g/image.png)

`cgi\`
