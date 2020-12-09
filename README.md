# LampsControl
Управление уличным освещением

## Controller arduino+ethernet+relay+sensors:

![](https://i.ibb.co/f256Qfj/image.png)

`arduino\arduino.ino`

## Python backend

Опрашивает с заданной периодичностью все устройства. Записывает данные о токе, напряжении, мощности на каждой лампе в базу Clickhouse. Реализует API для фронтенда. Работает в Docker

- [Python backend](https://github.com/yank0vy3rdna/web-lamps)

## React frontend

Реализует интерфейс для управления системой. Позволяет кликом по карте включить или выключить лампу, а также включить сразу все лампы. Реализована авторизация. Работает в Docker

![](https://i.imgur.com/rcfkdDg.png)

- [React frontend](https://github.com/yank0vy3rdna/web_front_lamps)

## Grafana+Clickhouse

### Clickhouse

Я использую базу Clickhouse, так как она хорошо подходит для подобного рода задач, ведь я храню в ней записи о моментальных значениях показаний датчиков.

### Grafana

Позволяет строить графики на основе данных из Clickhouse

![](https://i.imgur.com/ZFcMKw5.png)

## nginx

Все сервисы работают через reverse proxy nginx
