# LampsControl
Managing street lights

## Controller with arduino+ethernet+relay:

![](https://i.ibb.co/f256Qfj/image.png)

`arduino\arduino.ino`

## Python main app

requests all controllers to get amperage/voltage data and record it to MySQL or send on/off commands from WebUI or time of sunsets and sunrises for each lamp controller. Dockerized

- [Python main app](https://github.com/yank0vy3rdna/main-lamps)

## Python CGI for WebUI. Python backend + JavaScript frontend

you can on/off every lamp, on/off all lamps by the only one click and see sunset/sunrise times by that UI. Dockerized

![](https://i.imgur.com/Qflli9J.png)

- [Python web backend](https://github.com/yank0vy3rdna/web-lamps)
