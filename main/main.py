getDataRequest = [ 1, 00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ];
lampONRequest = [ 1, 160, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ];
lampOFFRequest = [ 1, 80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ];

ku = 0.306
ki = 0.0245

logtype = 2

lamps = {}
import json
import socket
import time
from threading import Thread
import requests
from apscheduler.schedulers.background import BackgroundScheduler
import mysql.connector
def log(text):
	if logtype == 1:
		with open('log.txt','a') as file:
			file.write(text)
	else:
		print(text)
		
with open('lamps.json', 'r') as file:
	lamps = json.loads(file.read())
	
print('Данные о лампах загружены')

def getData(lampid):
	try:
		#log('getData('+str(lampid)+')')
		sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
		sock.settimeout(1)
		sock.sendto(bytes(getDataRequest),0,('10.200.120.' + str(lampid), 8888))
		data = list(sock.recv(1024))
		#print('client addr: ', list(data))
		highByte = data[28] << 8
		lowByte = data[27] & 0x00ff
		amperage = highByte | lowByte
		highByte = data[20] << 8
		lowByte = data[21] & 0x00ff
		enable = 0
		voltage = highByte | lowByte
		voltage = ku * voltage
		if amperage - 512 < 0:
			amperage = (512 - amperage) * ki
		else:
			amperage = (amperage - 512) * ki

		if data[29] == 0: 
			enable = True
		else:
			enable = False
		lamps[str(lampid)].update({'amperage':amperage,'voltage':voltage,'enable':enable,'connected':1,'id':lampid})
		cnx = mysql.connector.connect(user='user', password='password', host='10.230.0.161', database='arduinodatabase')
		cursor = cnx.cursor()
		query = "INSERT INTO valuesofconsumption(consumper_id, capasitor_id, capasity,voltage,amperage) VALUES(" + str(lampid) + ", 1, " + str(amperage * voltage) + ", " + str(voltage) + ", " + str(amperage) + "); "
		cursor.execute(query)
		cnx.commit()
		cursor.close()
		cnx.close()
	except socket.timeout:
		lamps[str(lampid)].update({'connected':0,'id':lampid})
		
def lampOn(lampid):
	#log('lampOn('+str(lampid)+')')
	sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
	sock.settimeout(1)
	sock.sendto(bytes(lampONRequest),0,('10.200.120.' + str(lampid), 8888))

def lampOff(lampid):
	#log('lampOff('+str(lampid)+')')
	sock = socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
	sock.settimeout(1)
	sock.sendto(bytes(lampOFFRequest),0,('10.200.120.' + str(lampid), 8888))

def allOff():
	#print(datetime.datetime.now(), ' выключение ламп')
	for lamp in lamps.keys():
		if lamps[lamp]['type'] == 'lamp':
			lampOff(lamp)
			
def allOn():
	for lamp in lamps.keys():
		if lamps[lamp]['type'] == 'lamp':
			lampOn(lamp)
			
def getDataAsync(lampid):
	while True:
		getData(lampid)
		time.sleep(3)

for lamp in lamps.keys():
	lampThread = Thread(target = getDataAsync, args= (int(lamp),))
	lampThread.start()
def sunrisef():
	allOff()
	time.sleep(60)
	allOff()
	
def sunsetf():
	allOn()
	time.sleep(60)
	allOn()
print('Опрос устройств начат')
sunrise = ''
sched = BackgroundScheduler()
sched.start()
sunset = ''
def gettingsuntime():
	print('Время восхода и захода солнца получено')
	global sunrise
	global sunset
	global sched
	sunrisejob = None
	sunsetjob = None
	while True:
		headers = {'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10.9; rv:45.0) Gecko/20100101 Firefox/45.0'}
		r = requests.get('https://time.is/Dubna#time_zone',headers=headers).text
		sunrise = r[r.index("<li>Восход: ") + 12:r.index("<li>Восход: ") + 12 + 5]
		sunset= r[r.index("<li>Закат: ") + 11:r.index("<li>Закат: ") + 11 + 5]
		if sunrisejob == None or sunsetjob == None:
			sunrisejob = sched.add_job(sunrisef, 'cron', hour=sunrise.split(':')[0],minute=sunrise.split(':')[1])
			sunsetjob = sched.add_job(sunsetf, 'cron', hour=sunset.split(':')[0],minute=sunset.split(':')[1])
		else:
			try:
				sunrisejob.reschedule(trigger='cron',hour=sunrise.split(':')[0],minute=sunrise.split(':')[1])
				sunsetjob.reschedule(trigger='cron',hour=sunset.split(':')[0],minute=sunset.split(':')[1])
			except Exception as e: 
				print(e)
		time.sleep(3600*24)
		 

suntimeThread = Thread(target = gettingsuntime)

suntimeThread.start()
time.sleep(1)

def listening():
	sock = socket.socket()
	sock.bind(('', 9090))
	while True:
		sock.listen(1)
		conn, addr = sock.accept()
		data = conn.recv(1024).decode('utf-8')
		if(data == 'getInfo'):
			conn.send(bytes(json.dumps(lamps), encoding = 'utf-8'))
		if(data[:7]=='lampOff'):
			print(data)
			lampOff(int(data.split(' ')[1]))
		if(data[:6]=='lampOn'):
			print(data)
			lampOn(int(data.split(' ')[1]))
		if(data=='allOff'):
			print(data)
			allOff()
		if(data=='allOn'):
			print(data)
			allOn()
		if(data=='getSunsetTime'):
			conn.send(bytes(sunset, encoding = 'utf-8'))
			
		if(data=='getSunriseTime'):
			conn.send(bytes(sunrise, encoding = 'utf-8'))
		conn.close()
		
listeningThread = Thread(target = listening)

listeningThread.start()
