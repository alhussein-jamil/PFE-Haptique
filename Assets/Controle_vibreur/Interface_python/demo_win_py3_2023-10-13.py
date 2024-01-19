import serial
import serial.tools.list_ports

import numpy as np
import struct
from threading import Thread
# import pandas as pd
import matplotlib.pyplot as plt
import pyqtgraph as pg
import PyQt5.QtCore as QtCore
import PyQt5.QtGui as QtGui
import PyQt5.QtWidgets as QtWidgets
import pyqtgraph.dockarea as dockArea
import os
import sys
import time
import datetime
import pickle as pk
from scipy.io.wavfile import write as writeWave
from scipy.io.wavfile import read as readWave	

import socket

from WavyUDPClient import WavyUDPClient
import collections

def lPorts():
	l=serial.tools.list_ports.comports()
	for li in l:
		print("%s -- %s"%(li.device, li.description))

#lPorts() #-> liste les devices COM

#!-------------------------!
#!!!! ---- DEFINES ---- !!!!
#!-------------------------!
#-> 
BAUDRATE= 115200 #-> Peu import, ce n'est pas pris en compte
PORT	= '/dev/ACM0'
#<- Variables de serial

myConfigFile='./config.txt'
LOAD_CONFIG=True

USEnCHANNELS = 28

AFFnCHANNELS = 11
channelNames={}
for idx in range(AFFnCHANNELS):  channelNames[idx]='Channel %d'%(idx)
channelNames[0]='Pouce'
channelNames[1]='PaumeExtH'
channelNames[2]='PaumeExtB'
channelNames[3]='PaumeInt'
channelNames[4]='PaumeAnnulaire'
channelNames[6]='PaumeIndex'

#-> 
MAX_SIZE=64 #-> C'est la valeur maximale possible, il est inutile de la baisser
SFRAME_NOSPLIT=b'$A' #-> Entête signifiant nouvelle donnée contenue dans 1 frame de 64 Bytes max
SFRAME_SPLIT=b'$B' #-> Entête signifiant nouvelle donnée contenue dans plusieurs frames
#<- Variables de configuration USB

#->
DEFINE_N_CHANNELS=USEnCHANNELS
if(DEFINE_N_CHANNELS==28):
	maxPWM=511 #-> Pour l'instant 185 équivaut à un signal d'amplitude maximale
	fe=4e3 #-> Fréquence d'échantillonnage du stream		//4e3
	LEN_USBBUFF=32 #-> On envoie 5 samples à la suite (#define dans STM32)	//8
	TRAME_SIZE=28 #-> On envoie les signaux pour 28 actionneurs (#define dans STM32)		//28
if(DEFINE_N_CHANNELS==2):
	maxPWM=255 #-> Pour l'instant 185 équivaut à un signal d'amplitude maximale
	fe=4e3 #-> Fréquence d'échantillonnage du stream		//4e3
	LEN_USBBUFF=8 #-> On envoie 5 samples à la suite (#define dans STM32)	//8
	TRAME_SIZE=2 #-> On envoie les signaux pour 28 actionneurs (#define dans STM32)		//28

LEN_USBBUFF=8
TRAME_SIZE=8

nbBits='USE16BITS'
# nbBits='USE8BITS'
TRAME_HEAD_16bits=b'C'
TRAME_HEAD_8bits=b'c'
# COEFF_TIMING=1.0
COEFF_TIMING=1.0
GAIN_Correction=0.0005 #*10
correctionTiming=0 #0-100: réduction du temps; 100-200: augmentation du temps
#<- Paramètres de stream


#->
MARGE_QUEUE=64 #-> à 4kHz correspond à une lattence de 16ms
GAIN_QUEUE_python=1.0 #-> pour convertir en uint8_t
#-- Côté UNITY:
#-- GAIN_QUEUE_unity=0.5
#-- dt=(time.perf_counter()-t0<(1/fe)*LEN_USBBUFF*(1.0+GAIN_QUEUE_unity*((correctionTimingUDP-100)/GAIN_QUEUE_python)/MARGE_QUEUE))
#-- --> donc si queue est 2x plus long que la marge, dt est multiplié par 1.5
#-- --> et si queue a une longueur de presque 0, dt est multiplié par 0.5
#<- Paramètres de stream UDP


#->
#-Sinus
default_freq=100
default_amp=0.1
default_burst=1.000
#-Impulsion
default_tau=2.000
#-Glissement
default_dt=2.000
default_discrdt=2.000
default_logcoef=0.5
default_continu=False
default_AR=False
#-Wav
default_wav='Sin1'
#<- Paramètres de signaux


#->
N_GLISSEMENT=2
N_STEP_GLISSEMENT=5
listeGlissPropagationTypes=['Linéaire', 'Log', 'Discret']
#<- Paramètres d'affichage


print('Start')

#!------------------------!
#!!!! ---- WINDOW ---- !!!!
#!------------------------!
class stateBtn():
	def __init__(self, win, nom, listTxt=[("","")],check=False,controlChange=False,size=1):
		self.nom=nom
		self.lenTxt=len(listTxt)
		self.state=0
		self.listTxt=listTxt
		self.btn=QtWidgets.QPushButton(listTxt[self.state][0])
		if size>1:
			self.btn.setSizePolicy(QtWidgets.QSizePolicy.Expanding,QtWidgets.QSizePolicy.Expanding)
			font=self.btn.font()
			s=10
			if size==1.5: s=15
			if size==2: s=25
			font.setPointSize(s)
			font.setBold(True)		 
			self.btn.setFont(font)
			self.setTextToColor('blue')
			
			
		self.btn.clicked.connect(win.commandeAcqEvent)
		self.checkable=check
		self.controlChange=controlChange
		if check:
			self.btn.setCheckable(True)
	
	def changeState(self):
		if not self.controlChange:
			self.state+=1
			if (self.state==self.lenTxt): self.state=0
			self.btn.setText(self.listTxt[self.state][0])
	def setState(self, state):
		self.state=state
		self.btn.setText(self.listTxt[self.state][0])
		if self.checkable:
			if self.state==0:
				self.btn.setChecked(False)
			else:
				self.btn.setChecked(True)
	def setTextToColor(self,color):
			palette = QtGui.QPalette(self.btn.palette()) # make a copy of the palette
			palette.setColor(QtGui.QPalette.ButtonText, QtGui.QColor(color))
			self.btn.setPalette(palette) # assign new palette						


class MainWindow(QtWidgets.QMainWindow):
	def __init__(self):
		QtWidgets.QMainWindow.__init__(self)
		self.hasQuitt=False
		self.SetupUI(self)

		self.quitAction =  QtWidgets.QAction("Quit", self)
		self.quitAction.triggered.connect(self.quitt)

	def SetupUI(self, MainWindow):
		MainWindow.setWindowTitle("Contrôle Vibreurs")

		area = dockArea.DockArea()	  
		self.setCentralWidget(area)		

		def chColor(lab,color):			
			palette = QtGui.QPalette(lab.palette())
			myColor=QtGui.QColor(color) if type(color) is str else QtGui.QColor(*color)
			palette.setColor(QtGui.QPalette.WindowText, myColor)
			lab.setPalette(palette)
		def setSize(lab,size):
			font=lab.font()			
			font.setPointSize(size)
			font.setBold(True)		 
			lab.setFont(font)   



		dContr = pg.dockarea.Dock("Contrôle", size=(50,50))		 
		area.addDock(dContr, 'right') 
		setSize(dContr.label, 10)

		V=QtWidgets.QVBoxLayout()
		self.LeditCOM=QtWidgets.QLineEdit(PORT)
		V.addWidget(self.LeditCOM)			

		self.BtnListPorts=stateBtn(self, "listPorts", [("List Ports", "cmdPorts")], check=False)
		V.addWidget(self.BtnListPorts.btn)	

		self.BtnLoadConfig=stateBtn(self, "loadConfig", [("Load Config", "cmdLConf")], check=False)
		V.addWidget(self.BtnLoadConfig.btn)	

		self.BtnConnectSTM=stateBtn(self, "connectSTM", [("Connect STM", "cmdConnect"),("Disconnect STM", "cmdDisconnect")], check=True)
		V.addWidget(self.BtnConnectSTM.btn)			
		self.BtnConnectSTM.setState(stmConnected)

		self.BtnPlaySin=stateBtn(self, "playSin", [("PlaySin", "cmdPlaySin"), ("PlaySin", "cmdStopSin")], check=True)
		V.addWidget(self.BtnPlaySin.btn)	
		self.BtnBurstSin=stateBtn(self, "burstSin", [("BurstSin", "cmdBurstSin")], check=False)
		V.addWidget(self.BtnBurstSin.btn)	

		self.BtnTrigGliss=stateBtn(self, "trigGliss", [("TrigGliss", "cmdTrigGlis")], check=False)
		V.addWidget(self.BtnTrigGliss.btn)

		self.BtnTrigImp=stateBtn(self, "trigImp", [("TrigImp", "cmdTrigImp")], check=False)
		V.addWidget(self.BtnTrigImp.btn)	

		self.BtnTrigWav=stateBtn(self, "trigWav", [("TrigWav", "cmdTrigWav"), ("TrigWav", "cmdStopWav")], check=True)
		V.addWidget(self.BtnTrigWav.btn)

		self.BtnStopVib=stateBtn(self, "stopVib", [("StopVib", "cmdStop")], check=False)
		V.addWidget(self.BtnStopVib.btn)			

		self.BtnPlotGliss=stateBtn(self, "plotGliss", [("Plot Gliss", "cmdPlotGliss")], check=False)
		V.addWidget(self.BtnPlotGliss.btn)	

		self.BtnPlayUDP=stateBtn(self, "playUdp", [("PlayUdp", "cmdPlayUdp"), ("PlayUdp", "cmdStopUdp")], check=True)
		V.addWidget(self.BtnPlayUDP.btn)	

		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dContr.addWidget(gBox) 

		dSin = pg.dockarea.Dock("Sinus", size=(50,50))		 
		area.addDock(dSin, 'left') 
		setSize(dSin.label, 10)

		V=QtWidgets.QVBoxLayout()
		lab=QtWidgets.QLabel('Sinus')
		setSize(lab, 15)
		chColor(lab, (230, 145, 48))
		V.addWidget(lab)	
		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dSin.addWidget(gBox) 

		V=QtWidgets.QVBoxLayout()
		H=QtWidgets.QHBoxLayout()
		lab=QtWidgets.QLabel('name')
		lab.setFixedSize(55,15)
		lab.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(lab)	
		freq=QtWidgets.QLabel('freq')
		freq.setAlignment(QtCore.Qt.AlignCenter)
		freq.setFixedSize(55,15)
		freq.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(freq)		
		amp=QtWidgets.QLabel('amp')
		amp.setAlignment(QtCore.Qt.AlignCenter)
		amp.setFixedSize(55,15)
		amp.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(amp)	
		burst=QtWidgets.QLabel('burst')
		burst.setAlignment(QtCore.Qt.AlignCenter)
		burst.setFixedSize(53,15)
		burst.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(burst)					
		active=QtWidgets.QLabel('ON')
		active.setAlignment(QtCore.Qt.AlignCenter)
		active.setFixedSize(20,15)
		active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(active)			
		V.addLayout(H)

		H=QtWidgets.QHBoxLayout()
		lab=QtWidgets.QLabel('')
		lab.setFixedSize(55,23)
		lab.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(lab)		
		freq=stateBtn(self, "resetFreq", [("reset", "sinResetFreq")], check=False)
		freq.btn.setFixedSize(55,23)
		freq.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(freq.btn)	
		self.boxResetFreq=freq;
		amp=stateBtn(self, "resetAmp", [("reset", "sinResetAmp")], check=False)
		amp.btn.setFixedSize(55,23)
		amp.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		H.addWidget(amp.btn)	
		self.boxResetAmp=amp;
		burst=stateBtn(self, "resetBurst", [("reset", "sinResetBurst")], check=False)
		burst.btn.setFixedSize(55,23)
		burst.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		H.addWidget(burst.btn)	
		self.boxResetBurst=burst;		
		active=QtWidgets.QCheckBox()
		active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		active.setFixedSize(20,23)
		active.stateChanged.connect(self.commandeAcqEvent)	
		H.addWidget(active)	
		self.boxResetActive=active;
		V.addLayout(H)	

		self.listBoxFreq=[]
		self.listBoxAmp=[]
		self.listBoxBurst=[]
		self.listBoxActive=[]
		for idx in range(AFFnCHANNELS):
			H=QtWidgets.QHBoxLayout()
			name=listChannels[idx]
			lab=QtWidgets.QLabel(name)
			H.addWidget(lab)
			freq=QtWidgets.QDoubleSpinBox()
			freq.setValue(default_freq)
			freq.setRange(10,600); freq.setDecimals(1); freq.setSingleStep(10)		
			freq.valueChanged.connect(self.commandeAcqEvent)	
			H.addWidget(freq)
			self.listBoxFreq+=[freq]
			amp=QtWidgets.QDoubleSpinBox()
			amp.setValue(default_amp)
			amp.setRange(0,0.95); amp.setDecimals(4); amp.setSingleStep(0.05)
			amp.valueChanged.connect(self.commandeAcqEvent)	
			H.addWidget(amp)
			self.listBoxAmp+=[amp]
			burst=QtWidgets.QDoubleSpinBox()
			burst.setValue(default_burst)
			burst.setRange(0,99); burst.setDecimals(4); burst.setSingleStep(0.1)
			burst.valueChanged.connect(self.commandeAcqEvent)	
			H.addWidget(burst)
			self.listBoxBurst+=[burst]			
			active=QtWidgets.QCheckBox()
			active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
			active.setFixedSize(20,23)
			active.stateChanged.connect(self.commandeAcqEvent)	
			H.addWidget(active)							
			self.listBoxActive+=[active]
			V.addLayout(H)
		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dSin.addWidget(gBox) 		
				
		dImp = pg.dockarea.Dock("Impulsion", size=(100,100))		 
		area.addDock(dImp, 'below', relativeTo=dSin) 
		setSize(dImp.label, 10)

		V=QtWidgets.QVBoxLayout()
		lab=QtWidgets.QLabel('Impulsion')
		setSize(lab, 15)
		chColor(lab, (250, 67, 174))
		V.addWidget(lab)	
		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dImp.addWidget(gBox) 

		V=QtWidgets.QVBoxLayout()
		H=QtWidgets.QHBoxLayout()
		lab=QtWidgets.QLabel('name')
		lab.setFixedSize(55,15)
		lab.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(lab)	
		freq=QtWidgets.QLabel('freq')
		freq.setAlignment(QtCore.Qt.AlignCenter)
		freq.setFixedSize(55,15)
		freq.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(freq)		
		amp=QtWidgets.QLabel('amp')
		amp.setAlignment(QtCore.Qt.AlignCenter)
		amp.setFixedSize(55,15)
		amp.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(amp)	
		burst=QtWidgets.QLabel('t5%')
		burst.setAlignment(QtCore.Qt.AlignCenter)
		burst.setFixedSize(53,15)
		burst.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(burst)					
		active=QtWidgets.QLabel('ON')
		active.setAlignment(QtCore.Qt.AlignCenter)
		active.setFixedSize(20,15)
		active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(active)			
		V.addLayout(H)

		H=QtWidgets.QHBoxLayout()
		lab=QtWidgets.QLabel('')
		lab.setFixedSize(55,23)
		lab.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(lab)		
		freq=stateBtn(self, "copyImpFreq", [("copy", "impCopyFreq")], check=False)
		freq.btn.setFixedSize(55,23)
		freq.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(freq.btn)	
		self.boxCopyImpFreq=freq;
		amp=stateBtn(self, "copyImpAmp", [("copy", "impCopyAmp")], check=False)
		amp.btn.setFixedSize(55,23)
		amp.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		H.addWidget(amp.btn)	
		self.boxCopyImpAmp=amp;
		tau=stateBtn(self, "resetTau", [("reset", "impResetTau")], check=False)
		tau.btn.setFixedSize(55,23)
		tau.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		H.addWidget(tau.btn)	
		self.boxResetImpTau=tau;		
		active=QtWidgets.QCheckBox()
		active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		active.setFixedSize(20,23)
		active.stateChanged.connect(self.commandeAcqEvent)	
		H.addWidget(active)	
		self.boxResetImpActive=active;
		V.addLayout(H)	

		self.listBoxImpFreq=[]
		self.listBoxImpAmp=[]
		self.listBoxImpTau=[]
		self.listBoxImpActive=[]
		for idx in range(AFFnCHANNELS):
			H=QtWidgets.QHBoxLayout()
			name=listChannels[idx]
			lab=QtWidgets.QLabel(name)
			H.addWidget(lab)
			freq=QtWidgets.QDoubleSpinBox()
			freq.setValue(default_freq)
			freq.setRange(10,600); freq.setDecimals(1); freq.setSingleStep(10)		
			freq.valueChanged.connect(self.commandeAcqEvent)	
			H.addWidget(freq)
			self.listBoxImpFreq+=[freq]
			amp=QtWidgets.QDoubleSpinBox()
			amp.setValue(default_amp)
			amp.setRange(0,0.95); amp.setDecimals(4); amp.setSingleStep(0.05)
			amp.valueChanged.connect(self.commandeAcqEvent)	
			H.addWidget(amp)
			self.listBoxImpAmp+=[amp]
			tau=QtWidgets.QDoubleSpinBox()
			tau.setValue(default_tau)
			tau.setRange(0.05,99); tau.setDecimals(3); tau.setSingleStep(0.1)
			tau.valueChanged.connect(self.commandeAcqEvent)	
			H.addWidget(tau)
			self.listBoxImpTau+=[tau]			
			active=QtWidgets.QCheckBox()
			active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
			active.setFixedSize(20,23)
			active.stateChanged.connect(self.commandeAcqEvent)	
			H.addWidget(active)							
			self.listBoxImpActive+=[active]
			V.addLayout(H)

		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dImp.addWidget(gBox) 

		dGli = pg.dockarea.Dock("Glissement", size=(100,100))		 
		area.addDock(dGli, 'below', relativeTo=dSin) 
		setSize(dGli.label, 10)

		V=QtWidgets.QVBoxLayout()
		lab=QtWidgets.QLabel('Glissement')
		setSize(lab, 15)
		chColor(lab, (90, 163, 29))
		V.addWidget(lab)	
		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dGli.addWidget(gBox) 

		widg=QtWidgets.QWidget()
		lay=QtWidgets.QVBoxLayout(widg)
		widg.setLayout(lay)
		widg.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Expanding)	
		scroll=QtWidgets.QScrollArea(widg)
		scroll.setWidgetResizable(True)
		lay.addWidget(scroll)
		scrollContent=QtWidgets.QWidget(scroll)
		scrollLayout=QtWidgets.QVBoxLayout(scrollContent)
		scrollContent.setLayout(scrollLayout)

		self.listBoxGlissNames={}
		self.listBoxGlissFreq={}
		self.listBoxGlissAmp={}
		self.listBoxGlissDt={}
		self.listBoxGlissCopy={}
		self.listBoxGlissProp=[]
		self.listBoxGlissCont=[]
		self.listBoxGlissAR=[]
		self.listBoxGlissDiscrDT=[]
		self.listLabGlissDiscrDT=[]
		self.listBoxGliss=[]

		self.glistBoxGlissNames=[]
		self.glistBoxGlissFreq=[]
		self.glistBoxGlissAmp=[]
		self.glistBoxGlissDt=[]	
		for nGli in range(N_GLISSEMENT):
			V=QtWidgets.QVBoxLayout()
			H=QtWidgets.QHBoxLayout()
			lid=QtWidgets.QLabel('id')
			lid.setFixedSize(10,15)
			lid.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
			H.addWidget(lid)		
			lab=QtWidgets.QLabel('name')
			lab.setFixedSize(130,15)
			lab.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
			H.addWidget(lab)	
			freq=QtWidgets.QLabel('freq')
			freq.setAlignment(QtCore.Qt.AlignCenter)
			freq.setFixedSize(55,15)
			freq.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
			H.addWidget(freq)		
			amp=QtWidgets.QLabel('amp')
			amp.setAlignment(QtCore.Qt.AlignCenter)
			amp.setFixedSize(55,15)
			amp.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
			H.addWidget(amp)	
			v=QtWidgets.QLabel('')
			v.setFixedSize(55,15)
			v.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
			H.addWidget(v)			
			dt=QtWidgets.QLabel('dt')
			dt.setAlignment(QtCore.Qt.AlignCenter)
			dt.setFixedSize(55,15)
			dt.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
			H.addWidget(dt)						
			V.addLayout(H)

			self.listBoxGlissNames[nGli]=[]
			self.listBoxGlissFreq[nGli]=[]
			self.listBoxGlissAmp[nGli]=[]
			self.listBoxGlissDt[nGli]=[]
			self.listBoxGlissCopy[nGli]=[]
			for step in range(N_STEP_GLISSEMENT):
				H=QtWidgets.QHBoxLayout()
				lid=QtWidgets.QLabel('%d'%(step+1))
				lid.setFixedSize(10,20)
				lid.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
				H.addWidget(lid)				
				cbox=QtWidgets.QComboBox()
				cbox.addItem('---------')
				for idx in range(AFFnCHANNELS):
					cbox.addItem(channelNames[idx] if idx in channelNames.keys() else 'Channel %d'%(idx))
				cbox.currentIndexChanged.connect(self.commandeAcqEvent)	
				cbox.setFixedSize(130,20)
				cbox.name='boxGlissNames_%d_%d'%(nGli, step)
				H.addWidget(cbox)	
				freq=QtWidgets.QDoubleSpinBox()
				freq.setValue(default_freq)
				freq.setRange(10,600); freq.setDecimals(1); freq.setSingleStep(10)		
				freq.valueChanged.connect(self.commandeAcqEvent)	
				freq.name='boxGlissFreq_%d_%d'%(nGli, step)
				H.addWidget(freq)	
				amp=QtWidgets.QDoubleSpinBox()
				amp.setValue(default_amp)
				amp.setRange(0,0.95); amp.setDecimals(2); amp.setSingleStep(0.05)
				amp.valueChanged.connect(self.commandeAcqEvent)	
				amp.name='boxGlissAmp_%d_%d'%(nGli, step)
				H.addWidget(amp)
				cpy=stateBtn(self, "copyGli_%d_%d"%(nGli, step), [("copy", "gliCopy_%d_%d"%(nGli, step))], check=False)
				cpy.btn.setFixedSize(55,23)
				cpy.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
				H.addWidget(cpy.btn)	
				dt=QtWidgets.QDoubleSpinBox()
				dt.setValue(default_dt)
				dt.setRange(0.05,99); dt.setDecimals(3); dt.setSingleStep(0.1)
				dt.valueChanged.connect(self.commandeAcqEvent)	
				dt.name='boxGlissDt_%d_%d'%(nGli, step)
				H.addWidget(dt)							
				V.addLayout(H)
				self.listBoxGlissNames[nGli]+=[cbox]
				self.listBoxGlissFreq[nGli]+=[freq]
				self.listBoxGlissAmp[nGli]+=[amp]
				self.listBoxGlissDt[nGli]+=[dt]
				self.listBoxGlissCopy[nGli]+=[cpy]

				self.glistBoxGlissNames+=[cbox]
				self.glistBoxGlissFreq+=[freq]
				self.glistBoxGlissAmp+=[amp]
				self.glistBoxGlissDt+=[dt]
			H=QtWidgets.QHBoxLayout()
			propGli=QtWidgets.QLabel('Propagation')
			propGli.setAlignment(QtCore.Qt.AlignCenter)
			H.addWidget(propGli)
			cbox=QtWidgets.QComboBox()
			for it in listeGlissPropagationTypes:
				cbox.addItem(it)
			cbox.currentIndexChanged.connect(self.commandeAcqEvent)	
			H.addWidget(cbox)
			ldiscrdt=QtWidgets.QLabel('dtBurstDiscret')
			ldiscrdt.setAlignment(QtCore.Qt.AlignCenter)
			ldiscrdt.setEnabled(cbox.currentText=='Discret')
			H.addWidget(ldiscrdt)			
			discrdt=QtWidgets.QDoubleSpinBox()
			discrdt.setValue(default_discrdt)
			discrdt.setRange(-10,99); discrdt.setDecimals(3); discrdt.setSingleStep(0.1)
			discrdt.valueChanged.connect(self.commandeAcqEvent)			
			discrdt.setEnabled(cbox.currentText=='Discret')
			H.addWidget(discrdt)			
			cont=QtWidgets.QCheckBox('Continu')
			cont.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
			cont.setChecked(default_continu)
			cont.stateChanged.connect(self.commandeAcqEvent)	
			H.addWidget(cont)		
			ar=QtWidgets.QCheckBox('Aller-Retour')
			ar.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
			ar.setChecked(default_AR)
			ar.stateChanged.connect(self.commandeAcqEvent)	
			H.addWidget(ar)					
			# dtGli=QtWidgets.QDoubleSpinBox()
			# dtGli.setValue(default_dtGli)
			# dtGli.setRange(0.05,99); dtGli.setDecimals(3); dtGli.setSingleStep(0.1)
			# dtGli.valueChanged.connect(self.commandeAcqEvent)	
			# H.addWidget(dtGli)									
			V.addLayout(H)
			self.listBoxGlissProp+=[cbox]
			self.listBoxGlissCont+=[cont]
			self.listBoxGlissAR+=[ar]
			self.listBoxGlissDiscrDT+=[discrdt]
			self.listLabGlissDiscrDT+=[ldiscrdt]


			gBox=QtWidgets.QGroupBox() 
			gBox.setLayout(V)
			gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
			gBox.setTitle('Glissement %d'%(nGli+1))
			gBox.setCheckable(True)
			gBox.setChecked(False)
			gBox.clicked.connect(self.commandeAcqEvent)	
			self.listBoxGliss+=[gBox]
			scrollLayout.addWidget(gBox)
		scroll.setWidget(scrollContent)
		dGli.addWidget(widg) 

		dWav = pg.dockarea.Dock("WAV File", size=(100,100))		 
		area.addDock(dWav, 'below', relativeTo=dImp) 
		setSize(dWav.label, 10)

		V=QtWidgets.QVBoxLayout()
		lab=QtWidgets.QLabel('WAV File')
		setSize(lab, 15)
		chColor(lab, (37, 144, 232))
		V.addWidget(lab)	
		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dWav.addWidget(gBox) 

		V=QtWidgets.QVBoxLayout()
		H=QtWidgets.QHBoxLayout()
		lab=QtWidgets.QLabel('name')
		lab.setFixedSize(55,15)
		lab.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(lab)	
		wav=QtWidgets.QLabel('file')
		wav.setAlignment(QtCore.Qt.AlignCenter)
		wav.setFixedSize(75,15)
		wav.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(wav)		
		dure=QtWidgets.QLabel('durée')
		dure.setAlignment(QtCore.Qt.AlignCenter)
		dure.setFixedSize(55,15)
		dure.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(dure)	
		volume=QtWidgets.QLabel('volume')
		volume.setAlignment(QtCore.Qt.AlignCenter)
		volume.setFixedSize(55,15)
		volume.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(volume)			
		loop=QtWidgets.QLabel('loop')
		loop.setAlignment(QtCore.Qt.AlignCenter)
		loop.setFixedSize(53,15)
		loop.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(loop)					
		active=QtWidgets.QLabel('ON')
		active.setAlignment(QtCore.Qt.AlignCenter)
		active.setFixedSize(20,15)
		active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(active)			
		V.addLayout(H)

		H=QtWidgets.QHBoxLayout()
		lab=QtWidgets.QLabel('')
		lab.setFixedSize(85,23)
		lab.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(lab)		
		wav=stateBtn(self, "loadWav", [("load", "wavLoad")], check=False)
		wav.btn.setFixedSize(75,23)
		wav.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
		H.addWidget(wav.btn)	
		self.boxLoadWav=wav;
		dure=stateBtn(self, "", [(" ", " ")], check=False)
		dure.btn.setFixedSize(55,23)
		dure.btn.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dure.btn.setEnabled(False)
		H.addWidget(dure.btn)	
		volume=QtWidgets.QDoubleSpinBox()
		volume.setValue(0)
		volume.setRange(0,1); volume.setDecimals(2); volume.setSingleStep(0.05)
		volume.setEnabled(True)
		volume.setFixedSize(55,23)
		H.addWidget(volume)
		volume.valueChanged.connect(self.commandeAcqEvent)	
		self.boxWavVolume=volume		
		loop=QtWidgets.QCheckBox()
		loop.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		loop.setFixedSize(20,23)
		loop.stateChanged.connect(self.commandeAcqEvent)	
		H.addWidget(loop)		
		self.boxResetWavLoop=loop;
		active=QtWidgets.QCheckBox()
		active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		active.setFixedSize(20,23)
		active.stateChanged.connect(self.commandeAcqEvent)	
		H.addWidget(active)	
		self.boxResetWavActive=active;
		V.addLayout(H)	

		self.listBoxWavName=[]
		self.listBoxWavDure=[]
		self.listBoxWavVolume=[]
		self.listBoxWavLoop=[]
		self.listBoxWavActive=[]
		for idx in range(AFFnCHANNELS):
			H=QtWidgets.QHBoxLayout()
			name=listChannels[idx]
			lab=QtWidgets.QLabel(name)
			H.addWidget(lab)
			wav=QtWidgets.QLineEdit()
			wav.setFixedSize(75,23)
			wav.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)		
			wav.setText(default_wav)
			wav.textEdited.connect(self.commandeAcqEvent)	
			H.addWidget(wav)
			self.listBoxWavName+=[wav]
			dure=QtWidgets.QDoubleSpinBox()
			dure.setValue(0)
			dure.setRange(0,99); dure.setDecimals(2); dure.setSingleStep(0.05)
			dure.setEnabled(False)
			dure.setFixedSize(55,23)
			H.addWidget(dure)
			self.listBoxWavDure+=[dure]
			volume=QtWidgets.QDoubleSpinBox()
			volume.setValue(0)
			volume.setRange(0,1); volume.setDecimals(2); volume.setSingleStep(0.05)
			volume.setEnabled(True)
			volume.setFixedSize(55,23)
			H.addWidget(volume)
			volume.valueChanged.connect(self.commandeAcqEvent)	
			self.listBoxWavVolume+=[volume]			
			loop=QtWidgets.QCheckBox()
			loop.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
			loop.setFixedSize(20,23)
			loop.stateChanged.connect(self.commandeAcqEvent)	
			H.addWidget(loop)
			self.listBoxWavLoop+=[loop]			
			active=QtWidgets.QCheckBox()
			active.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
			active.setFixedSize(20,23)
			active.stateChanged.connect(self.commandeAcqEvent)	
			H.addWidget(active)							
			self.listBoxWavActive+=[active]
			V.addLayout(H)

		gBox=QtWidgets.QGroupBox() 
		gBox.setLayout(V)
		gBox.setSizePolicy(QtWidgets.QSizePolicy.Fixed,QtWidgets.QSizePolicy.Fixed)
		dWav.addWidget(gBox) 




		dSin.raiseDock()
		dImp.raiseDock()
		dGli.raiseDock()
		dWav.raiseDock()

	def quitt(self, event):
		self.hasQuitt=True
		print('QUITT')
		

	def closeEvent(self, event):
		print('CLOSED')
		quitHandler()

	def commandeAcqEvent(self,message):
		send=self.sender()
		arg=[]
		
		listStateBtn=[self.BtnListPorts, self.BtnLoadConfig, self.BtnConnectSTM, 
						self.BtnPlaySin, self.BtnBurstSin, self.BtnTrigImp, self.BtnTrigWav, self.BtnTrigGliss, self.BtnStopVib, self.BtnPlayUDP]
		listStateBtn+=[self.boxResetFreq, self.boxResetAmp, self.boxResetBurst]
		listStateBtn+=[self.boxCopyImpFreq, self.boxCopyImpAmp, self.boxResetImpTau]
		listStateBtn+=[self.boxLoadWav]
		listStateBtn+=[self.BtnPlotGliss]
		for i in range(N_GLISSEMENT): listStateBtn+=self.listBoxGlissCopy[i]
		if send in [i.btn for i in listStateBtn]:
			lBtn=[i.btn for i in listStateBtn]
			instStateBtn=listStateBtn[lBtn.index(send)]
			arg=[instStateBtn.listTxt[instStateBtn.state][1]]					  
			instStateBtn.changeState()
		elif send in self.listBoxFreq: arg=['sinSetFreq', self.listBoxFreq.index(send), send.value()]
		elif send in self.listBoxAmp: arg=['sinSetAmp', self.listBoxAmp.index(send), send.value()]
		elif send in self.listBoxBurst: arg=['sinSetBurst', self.listBoxBurst.index(send), send.value()]
		elif send in self.listBoxActive: arg=['sinSetActive', self.listBoxActive.index(send), send.isChecked()]
		elif send is self.boxResetActive: arg=['sinResetActive', send.isChecked()]

		elif send in self.listBoxImpFreq: arg=['impSetFreq', self.listBoxImpFreq.index(send), send.value()]
		elif send in self.listBoxImpAmp: arg=['impSetAmp', self.listBoxImpAmp.index(send), send.value()]
		elif send in self.listBoxImpTau: arg=['impSetTau', self.listBoxImpTau.index(send), send.value()]
		elif send in self.listBoxImpActive: arg=['impSetActive', self.listBoxImpActive.index(send), send.isChecked()]		
		elif send is self.boxResetImpActive: arg=['impResetActive', send.isChecked()]

		elif send in self.glistBoxGlissNames: arg=['gliSetName', int(send.name.split('_')[1]), int(send.name.split('_')[2]), send.currentIndex(), send.currentText()]		
		elif send in self.glistBoxGlissFreq: arg=['gliSetFreq', int(send.name.split('_')[1]), int(send.name.split('_')[2]), send.value()]		
		elif send in self.glistBoxGlissAmp: arg=['gliSetAmp', int(send.name.split('_')[1]), int(send.name.split('_')[2]), send.value()]		
		elif send in self.glistBoxGlissDt: arg=['gliSetDt', int(send.name.split('_')[1]), int(send.name.split('_')[2]), send.value()]		
		
		elif send in self.listBoxGlissProp: arg=['gliSetProp', self.listBoxGlissProp.index(send), send.currentIndex(), send.currentText()]
		elif send in self.listBoxGlissCont: arg=['gliSetCont', self.listBoxGlissCont.index(send), send.isChecked()]
		elif send in self.listBoxGlissAR: arg=['gliSetAR', self.listBoxGlissAR.index(send), send.isChecked()]
		elif send in self.listBoxGlissDiscrDT: arg=['gliSetDiscrDT', self.listBoxGlissDiscrDT.index(send), send.value()]
		elif send in self.listBoxGliss: arg=['gliSetChk', self.listBoxGliss.index(send), send.isChecked()]

		elif send in self.listBoxWavName: arg=['wavChangedName', self.listBoxWavName.index(send), send.text()]
		elif send in self.listBoxWavVolume: arg=['wavChangedVolume', self.listBoxWavVolume.index(send), send.value()]
		elif send is self.boxWavVolume: arg=['wavChangedVolume_all', send.value()]
		elif send is self.boxResetWavLoop: arg=['wavResetLoop', send.isChecked()]
		elif send is self.boxResetWavActive: arg=['wavResetActive', send.isChecked()]

		elif send in self.listBoxWavLoop: arg=['wavSetLoop', self.listBoxWavLoop.index(send), send.isChecked()]
		elif send in self.listBoxWavActive: arg=['wavSetActive', self.listBoxWavActive.index(send), send.isChecked()]

		if (arg!= []):
			gestionSig.transfertSig("commandeAcqEvent", arg) 

		# self.listBoxGlissProp={}
		# self.listBoxGlissCont={}
		# self.listBoxGlissAR={}



class signalUpdater_C():
	def __init__(self):
		self.signal_update_thread_running=True
		self.signal_update_thread = Thread(target=self.update)
		self.signal_update_thread.daemon = True
		self.signal_update_thread.start() # Start the thread to query sensor data 
		self.ended=False
		self.sUsed=False

	def update(self):
		global playAsBurst, lastPlayAsBurst, correctionTiming
		listBurstDone=[]
		
		tCorrectionTimingUDP=time.perf_counter()
		while self.signal_update_thread_running:
			# if not stmConnected: time.sleep(0.1); continue
			t0=time.perf_counter() #-> Marqueur de temps pour la synchronisation de l'envoi à fe/LEN_USBBUFF
			i=0
			while(((LEN_USBBUFF>>(i+1))&0b1)!=1): i+=1 ##-- LEN_USBBUFF=0b1<<(((data>>5)+1)&0b111)
			data=bytes([(i<<5)|(TRAME_SIZE&0b11111)])
			#print(LEN_USBBUFF, TRAME_SIZE, i, TRAME_SIZE&0b11111, data, int.from_bytes(data[0], 'big'))
			srt_list = []
			if udpPlay:
				if len(myUpdQueue)>=LEN_USBBUFF:
					dataOk=True
					for n in range(LEN_USBBUFF):
						di=createData(True)
						if not di is None : 
							data+=di
						else :
							dataOk = False
					if dataOk: srt_list=writeUSB(data)
			else:
				for n in range(LEN_USBBUFF):
					data+=createData()	
				srt_list=writeUSB(data)
			data=b''		
			ok=0
			if stmConnected: 
				self.sUsed=True
				for i,srt in enumerate(srt_list): ok=s.write(srt) #-> Ici les frames USB sont envoyées, il y a un timeout sur le write qui doit être suffisant (100ms au moins je pense ?)
				self.sUsed=False
			whileOnce=1
			while(((time.perf_counter()-t0<(1/fe)*LEN_USBBUFF*(COEFF_TIMING+GAIN_Correction*correctionTiming)) & (ok!=0))|whileOnce): 	#1.08	
				if stmConnected: 
					self.sUsed=True
					if(s.inWaiting()!=0):
						while(s.inWaiting()!=0): res=s.read(1)
						correctionTiming+=(int.from_bytes(res,'big')-100)
						if correctionTiming<-100: correctionTiming=-100
						if correctionTiming>100: correctionTiming=100
						# print(correctionTiming)
						# correctionTiming=0
					self.sUsed=False
				if udpPlay:
					if (time.perf_counter()-tCorrectionTimingUDP>0.01):
						tCorrectionTimingUDP=time.perf_counter()
						deltaQueue=len(myUpdQueue)
						correctionTimingUDP=int((deltaQueue-MARGE_QUEUE)*GAIN_QUEUE_python)
						#print("correctionTimingUDP" + str(correctionTimingUDP))
						# if correctionTimingUDP<-60:
						# 	print("UDP queue very small :" + str(deltaQueue))
						if correctionTimingUDP<-100: correctionTimingUDP=-100
						if correctionTimingUDP>100:
							# print("UDP queue very large :" + str(deltaQueue))
							correctionTimingUDP=100

						UDPCli.sendMessage(b'$d'+bytes([(correctionTimingUDP+100) & 0xff]))
				if(playAsBurst & (not lastPlayAsBurst)): 
					lastPlayAsBurst=True; timePlayAsBurst=time.perf_counter()
					if typBurst=='sin':
						listBurstDone=np.array([not ex.listBoxActive[i].isChecked() for i in range(AFFnCHANNELS)])
						ex.BtnBurstSin.setTextToColor('red')
					elif typBurst=='imp':
						listBurstDone=np.array([not ex.listBoxImpActive[i].isChecked() for i in range(AFFnCHANNELS)])
						ex.BtnTrigImp.setTextToColor('red')		
					elif typBurst=='wav':
						listBurstDone=np.array([not listWav[i]['active'] for i in range(AFFnCHANNELS)])
						ex.BtnTrigWav.setTextToColor('red')											
				if(playAsBurst):
					for i in range(AFFnCHANNELS):
						if listBurstDone[i]: continue
						useMaxT=True
						if typBurst=='sin':
							maxT=listBurst[i]
						elif typBurst=='imp':
							maxT=1.5*listTau[i]
						elif typBurst=='wav':
							maxT=listWav[i]['duree']	
							useMaxT=not listWav[i]['loop']
						if useMaxT:			
							if(time.perf_counter()-timePlayAsBurst>maxT): 
								if typBurst=='sin':
									updateSignalsSin([i], False)
								elif typBurst=='imp':
									updateSignalsImp([i], False)
								elif typBurst=='wav':
									updateSignalsWav([i], False)								
								listBurstDone[i]=1
					if np.prod(listBurstDone): 
						playAsBurst=False; lastPlayAsBurst=False; 
						if typBurst=='sin':
							ex.BtnBurstSin.setTextToColor('black')
						elif typBurst=='imp':
							ex.BtnTrigImp.setTextToColor('black')
						elif typBurst=='wav':
							ex.BtnTrigWav.setTextToColor('black')	
							ex.BtnTrigWav.btn.setChecked(False);
							ex.BtnTrigWav.setState(0);						
						print('DONE')
				elif(playAsGliss):
					updateGlissVol()
					
				whileOnce=0
				pass #-> Attente avant envoi de la prochaine trame
		self.ended=True
        
class udpUpdater_C():    
	def __init__(self):
		try:
			self.UDP_PORT=30000
			self.UDP_IP='0.0.0.0'
			self.s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, 0)
			self.s.bind((self.UDP_IP, self.UDP_PORT))
		#except:
		#	print("EXCEPTION IN UDP UPDATER")    
		#	return
		finally:
			pass
		self.updateRate=0.001
		self.signal_update_thread_running=True
		self.signal_update_thread = Thread(target=self.update)
		self.signal_update_thread.daemon = True
		self.signal_update_thread.start() # Start the thread to query sensor data 
		self.ended=False
		self.sUsed=False
			
	def update(self):
		print("update")
		while self.signal_update_thread_running:
			data, address = self.s.recvfrom(4096)
			print("\n\n 2. Client received : ", data.decode('utf-8'), "\n\n")
			data=data.decode('utf-8').split(';')+['u']
			#processSignal(data)
			gestionSig.transfertSig("commandeAcqEvent", data)
			time.sleep(self.updateRate)
		self.ended=True

#!---------------------------!
#!!!! ---- FONCTIONS ---- !!!!
#!---------------------------!
def connectSTM(connect=True):
	global s, stmConnected
	
	if connect:
		try:
			PORT=ex.LeditCOM.text()
			s=serial.Serial(PORT,BAUDRATE,timeout=100,write_timeout=200)	
			if s.isOpen(): print('%s Connected'%PORT)
		except:
			print('Can\'t connect %s'%PORT)
			stmConnected=False
		stmConnected=True
	else:
		if not s is None: 
			stmConnected=False
			while signalUpdater.sUsed: pass
			s.close()
			print('%s Disconnected'%s.port)
		s=None

#->
def strBool(myStr): 
	if myStr in ['False', 'True']: return [False, True][['False', 'True'].index(myStr)]
	else: return
#<- Utilitaire


#->
def writeUSB(data):
	L=len(data)
	srt_list=[]
	if L<MAX_SIZE-2:
		srt_list+=[SFRAME_NOSPLIT+data]
	else:			
		srt_list+=[SFRAME_SPLIT+bytes([((L>>8)&0xff),L&0xff])+data[:MAX_SIZE-4]]
		for fi in range((L-(MAX_SIZE-4))//MAX_SIZE+1):
			srt_list+=[data[(MAX_SIZE-4)+MAX_SIZE*fi:(MAX_SIZE-4)+MAX_SIZE*(fi+1)]]
	return srt_list
#<- Génération des frames USB

#->
def updateSignalsSin(listIdxSig=[], play=True):
	global listPWM, listCurrIdx, listPlaying
	for idx in listIdxSig:
		listCurrIdx[idx]=0
		ti=np.linspace(0,1/listFreq[idx]-1/fe,int(fe/listFreq[idx]))
		si=listAmp[idx]*np.sin(2*np.pi*listFreq[idx]*ti)*play
		listPWM[idx]=(si*maxPWM/2+maxPWM/2).astype(int)
		listPlaying[idx]=play

def updateUdpBuffer(data):
	if udpPlay:
		for x in data:
			myUpdQueue.append(x)

def updateSignalsUdp(play=True):
	global listPWM, listCurrIdx, listPlaying, udpPlay
	if play:
		udpPlay=True
		for idx in range(TRAME_SIZE):
			listPlaying[idx]=True
		for idx in range(TRAME_SIZE, USEnCHANNELS):
			listPlaying[idx]=False
	else:
		udpPlay=False
		for idx in range(TRAME_SIZE):
			listPWM[idx]=(np.ones(1)*maxPWM/2).astype(int)
			listPlaying[idx]=False
	

def updateSignalsImp(listIdxSig=[], play=False):
	global listPWM, listCurrIdx, listPlaying
	for idx in listIdxSig:
		listCurrIdx[idx]=0
		ti=np.linspace(0,2*listTau[idx],int(fe*2*listTau[idx]))
		si=listAmp[idx]*np.exp(-ti/(listTau[idx]/3))*np.sin(2*np.pi*listFreq[idx]*ti)*play
		listPWM[idx]=(si*maxPWM/2+maxPWM/2).astype(int)
		listPlaying[idx]=play

def updateSignalsWav(listIdxSig=[], play=True):
	global listPWM, listCurrIdx, listPlaying
	for idx in listIdxSig:
		listCurrIdx[idx]=0
		if not listWav[idx]['active']: continue
		if listWav[idx]['data'] is None: continue
		si=listWav[idx]['data']*listWav[idx]['volume']*play
		listPWM[idx]=(si*maxPWM/2+maxPWM/2).astype(int)
		listPlaying[idx]=play

def updateGlissVol(init=False):
	global playAsGliss, timePlayAsGliss, currentGliss, currentStepGliss, currentAR, listVolume
	if init:
		stopVib()
		if np.sum(listActiveGliss)==0:	return
		currentGliss=np.where(listActiveGliss==1)[0][0]
		d=dictGliss['gliss%d'%(currentGliss+1)]
		firstVib=d['listVib'][0]
		if firstVib['id'] is None: currentGliss=None; return
		secVib=d['listVib'][1]
		if (d['prop']!='Discret') & (secVib['id'] is None): currentGliss=None; return
		for vib in d['listVib']:
			if vib['id'] in d['usedVib']:
				updateSinParams(vib['id'], 'freq', vib['freq'], updateSignals=False)
				updateSinParams(vib['id'], 'amp', vib['amp'], updateSignals=False)
		updateSignalsSin(d['usedVib'],play=True)		
		currentStepGliss=0; currentAR=1
		listVolume=np.zeros(USEnCHANNELS)
		listVolume[firstVib['id']]=1.0
		playAsGliss=True; timePlayAsGliss=time.perf_counter()
	else:
		d=dictGliss['gliss%d'%(currentGliss+1)]
		if (d['prop']!='Discret'):
			print(currentStepGliss)
			firstVib=d['listVib'][currentStepGliss]
			secVib=d['listVib'][currentStepGliss+currentAR]
			if d['prop']=='Linéaire':
				vol=((time.perf_counter()-timePlayAsGliss)/firstVib['dt'])
			elif d['prop']=='Log':
				vol=np.log(1+(time.perf_counter()-timePlayAsGliss)*d['logCoef'])/np.log(1+firstVib['dt']*d['logCoef'])
			if ((time.perf_counter()-timePlayAsGliss)>firstVib['dt']) | (secVib['id'] is None): 
				timePlayAsGliss=time.perf_counter(); 
				if not (secVib['id'] is None): currentStepGliss+=currentAR; 
				if (currentStepGliss+currentAR>=N_STEP_GLISSEMENT) | (currentStepGliss+currentAR<0) | (secVib['id'] is None): 
					if d['AR']: 
						if (currentStepGliss+currentAR<0) & (not d['continu']): stopVib(d['usedVib']); playAsGliss=False; print('stop'); return
						currentAR*=-1; 
					if (d['continu']) & (not d['AR']): print('Next'); currentStepGliss=0; return
					elif not d['AR']: stopVib(d['usedVib']); playAsGliss=False; print('currentStepGliss'); return
			if not (secVib['id'] is None):
				listVolume[firstVib['id']]=1-vol
				listVolume[secVib['id']]=vol
		elif d['prop']=='Discret':
			print(currentStepGliss)
			firstVib=d['listVib'][currentStepGliss]
			secVib=d['listVib'][currentStepGliss+currentAR]
			if (time.perf_counter()-timePlayAsGliss<=firstVib['dt']):
				vol=1.0
			elif ((time.perf_counter()-timePlayAsGliss>firstVib['dt']) & (time.perf_counter()-timePlayAsGliss<firstVib['dt']+d['dtBurst'])):
				vol=0.0
			else:
				vol=0.0
				print('--------------')
				timePlayAsGliss=time.perf_counter(); 
				if not (secVib['id'] is None): currentStepGliss+=currentAR; 
				elif (currentStepGliss==0) & (not d['continu']): stopVib(d['usedVib']); playAsGliss=False; print('stop'); return
				if (currentStepGliss+currentAR>=N_STEP_GLISSEMENT) | (currentStepGliss+currentAR<-1) | (secVib['id'] is None): 
					if d['AR']: 
						if (currentStepGliss+currentAR<0) & (not d['continu']): stopVib(d['usedVib']); playAsGliss=False; print('stop'); return
						currentAR*=-1; 
						if (secVib['id'] is None): currentStepGliss+=currentAR
					if (d['continu']) & (not d['AR']): print('Next'); currentStepGliss=0; return
					elif not d['AR']: stopVib(d['usedVib']); playAsGliss=False; print('currentStepGliss'); return
			listVolume[firstVib['id']]=vol


def stopVib(listIdxSig=range(USEnCHANNELS)):
	for idx in listIdxSig:
		si=np.zeros(1)
		listPWM[idx]=(si*maxPWM/2+maxPWM/2).astype(int)

map16bits=lambda x: [(x>>8)&0xff, x&0xff]

def createData(isUdp = False):
	global pwm1, idx_sig1, idx_sig2, startCnt, timeCnt, listPlaying
	srt=b''
	currPWM=[]
	if udpPlay and isUdp:
		if len(myUpdQueue)>0:
			currPWM=myUpdQueue.popleft()
		else: return None		
	else:
		for i in range(TRAME_SIZE):
			listCurrIdx[i]+=1
			if listCurrIdx[i]>=len(listPWM[i]): 
				listCurrIdx[i]=0
				if listPlayOnce[i]: listPWM[i]=(np.zeros(1)+maxPWM/2).astype(int)
			# print(listCurrIdx[i])
			if playAsGliss: currPWM+=[(int((listPWM[i][listCurrIdx[i]]-maxPWM/2)*listVolume[i]+maxPWM/2))]
			else: currPWM+=[int(listPWM[i][listCurrIdx[i]])]
	# print(currPWM)
	if nbBits=='USE8BITS':
		srt=TRAME_HEAD_8bits+bytes([listCurrIdx[0]==0]+[currPWM[i] for i in range(TRAME_SIZE)])
	if nbBits=='USE16BITS':
		srt=TRAME_HEAD_16bits+bytes([int(listCurrIdx[0]==0)])
		for i in range(TRAME_SIZE): srt+=bytes(map16bits(currPWM[i]))
	return srt
#<- Génération d'un signal en temps réel

#->
def updateSinParams(idx, param, value, updateSignals=True):
	global listAmp, listFreq, listBurst
	if param=='amp':
		listAmp[idx]=value
	elif param=='freq':
		listFreq[idx]=value		
	elif param=='burst':
		listBurst[idx]=value		
	if updateSignals:
		updateSignalsSin([idx], play=ex.BtnPlaySin.btn.isChecked())	

def updateImpParams(idx, param, value, updateSignals=True):
	global listAmp, listFreq, listTau
	if param=='amp':
		listAmp[idx]=value
	elif param=='freq':
		listFreq[idx]=value		
	elif param=='tau':
		listTau[idx]=value		
	if updateSignals:
		updateSignalsImp([idx], play=False)		

def updateGliParams(idxG, idxS, param, value):
	global dictGliss
	if param in ['prop', 'dtBurst', 'logCoef', 'continu', 'AR']:
		dictGliss['gliss%d'%(idxG+1)][param]=value
	if param in ['freq', 'amp', 'dt']:
		dictGliss['gliss%d'%(idxG+1)]['listVib'][idxS][param]=value
	if param=='name':
		dictGliss['gliss%d'%(idxG+1)]['listVib'][idxS]['name']=value
		if value!='---------':
			dictGliss['gliss%d'%(idxG+1)]['listVib'][idxS]['id']=listChannels.index(value)
		else:
			dictGliss['gliss%d'%(idxG+1)]['listVib'][idxS]['id']=None
		dictGliss['gliss%d'%(idxG+1)]['usedVib']=[di['id'] for di in list(dictGliss['gliss%d'%(idxG+1)]['listVib']) if di['id'] is not None]

def updateWavParams(idx, param, value):
	global listWav, myVar
	if param in ['loop', 'active', 'volume']:
		listWav[idx][param]=value
	if param == 'volume': print('ch %d'%idx, listWav[idx][param])
	if param in ['name']:
		ldir=os.listdir('./WAV/')
		if value+'.wav' in ldir:
			ex.listBoxWavName[idx].setStyleSheet("color: green;")		
		else:
			ex.listBoxWavName[idx].setStyleSheet("color: red;")
			listWav[idx]['data']=None
			listWav[idx]['duree']=0.0
			return
		w_fe, w_data=readWave('./WAV/'+value+'.wav')
		if w_fe!=fe: 
			print('%s File should have %dHz sample rate (it has %f)'%(value+'.wav', fe, w_fe))
			if 0:
				ex.listBoxWavName[idx].setStyleSheet("color: orange;")
				listWav[idx]['data']=None
				listWav[idx]['duree']=0.0
				return
			w_data=w_data[::int(w_fe/fe)]
			w_fe=fe
		if(w_data.dtype!=np.float32):
			listWav[idx]['data']=w_data/np.iinfo(w_data.dtype).max
		else:
			listWav[idx]['data']=w_data.copy()
		listWav[idx]['data'][-1]=0.0
		myVar=listWav[idx]['data']
		listWav[idx]['duree']=len(w_data)/w_fe
		ex.listBoxWavDure[idx].setValue(listWav[idx]['duree'])

def loadConfig():
	global listChangedVars
	if not myConfigFile.split('/')[-1] in os.listdir('/'.join(myConfigFile.split('/')[:-1])): print('No Config file %s'%myConfigFile); return
	f=open(myConfigFile, 'r')
	varName=None
	listChangedVars=[]
	for l in f:
		l=l.rstrip('\n\r')
		if len(l)==0: continue
		if l[0]=='#': 
			if len(l)==1: continue
			if l[1]=='>': 
				varName=l[2:].split(':')[0]
				if not varName in globals(): print('%s not defined'%varName); varName=None; continue
				listChangedVars+=[varName]
			elif l[1]=='<': varName=None
			elif varName is None: 
				varName=l[1:].split(':')[0]
				if len(l[1:].split(':'))!=2: print('PB in %s var %s'%(myConfigFile, varName)); varName=None; continue
				varValue=l[1:].split(':')[1].strip(' ')
				if '\'' in varValue: varValue=varValue[1:-1]
				elif '.' in varValue: varValue=float(varValue)
				elif varValue in ['True', 'False']: varValue=[False, True][varValue=='True']
				else: varValue=int(varValue)
				if not varName in globals(): print('%s not defined'%varName); varName=None; continue
				globals()[varName]=varValue
				listChangedVars+=[varName]
				varName=None
			else:
				idx=l[1:].split(':')[0]
				if len(l[1:].split(':'))!=2: print('PB in %s var %s idx %s'%(myConfigFile, varName, idx)); varName=None; continue
				if '\'' in idx: idx=idx[1:-1]
				elif '.' in idx: idx=float(idx)
				else: idx=int(idx)
				varValue=l[1:].split(':')[1].strip(' ')
				if '\'' in varValue: varValue=varValue[1:-1]
				elif '.' in varValue: varValue=float(varValue)
				elif varValue in ['True', 'False']: varValue=[False, True][varValue=='True']
				else: varValue=int(varValue)
				if type(globals()[varName]) is dict:
					# if not idx in list(globals()[varName]): print('%s not in %s'%(idx,varName)); continue
					globals()[varName][idx]=varValue
				if type(globals()[varName]) is list:
					if not type(idx) is int: print('index should be int (%s, %s)'%(str(idx), varName)); continue
					if idx>len(globals()[varName]): print('index out of range (%d, %s)'%(idx, varName))
					globals()[varName][idx]=varValue
	f.close()
#<- Gestion des parametres

#->
def makeWav():
	# from scipy.io.wavfile import write as writeWave
	# from scipy.io.wavfile import write as readWave	
	samplerate_wav=int(fe) #£ il faut fe >= 4kz
	amplitude_wav = np.iinfo(np.int16).max

	fSin=100 #Hz
	aSin=0.05
	t=2 #s
	t_sig=np.linspace(0,t,int(t*fe))
	s_sig=aSin*np.sin(2*np.pi*fSin*t_sig)
	s_wave=(amplitude_wav*s_sig).astype(np.int16)
	f=writeWave("./WAV/Sin1.wav", int(samplerate_wav), s_wave)
	plt.plot(t_sig, s_sig)

def plotGliss():
	if np.sum(listActiveGliss)==0:	return
	currentGliss=np.where(listActiveGliss==1)[0][0]
	d=dictGliss['gliss%d'%(currentGliss+1)]
	firstVib=d['listVib'][0]
	if firstVib['id'] is None: currentGliss=None; return
	secVib=d['listVib'][1]
	if (d['prop']!='Discret') & (secVib['id'] is None): currentGliss=None; return
	currentStepGliss=0; currentAR=1
	listVolume=np.zeros(USEnCHANNELS)
	listVolume[firstVib['id']]=1.0*firstVib['amp']

	timePlayAsGliss=0; t=0
	listT=[0]; listD=np.array([listVolume]); listIdx=[]
	done=False
	while not done:
		d=dictGliss['gliss%d'%(currentGliss+1)]
		firstVib=d['listVib'][currentStepGliss]
		secVib=d['listVib'][currentStepGliss+currentAR]
		listIdx+=[firstVib['id'], secVib['id']]

		if (d['prop']!='Discret'):
			if d['prop']=='Linéaire':
				vol=((t-timePlayAsGliss)/firstVib['dt'])
			elif d['prop']=='Log':
				vol=np.log(1+(t-timePlayAsGliss)*d['logCoef'])/np.log(1+secVib['dt']*d['logCoef'])
			if (t-timePlayAsGliss>firstVib['dt']) | (secVib['id'] is None): 
				timePlayAsGliss=t; 
				if not (secVib['id'] is None): currentStepGliss+=currentAR; 
				if (currentStepGliss+currentAR>=N_STEP_GLISSEMENT) | (currentStepGliss+currentAR<0) | (secVib['id'] is None): 
					if d['AR']: 
						if (currentStepGliss+currentAR<0): done=True
						currentAR*=-1; 
					elif not d['AR']: done=True
			if not (secVib['id'] is None):
				listVolume[firstVib['id']]=(1.0-vol)*firstVib['amp']
				listVolume[secVib['id']]=(vol)*secVib['amp']

		elif (d['prop']=='Discret'):
			if (t-timePlayAsGliss<=firstVib['dt']):
				vol=1.0
			elif ((t-timePlayAsGliss>firstVib['dt']) & (t-timePlayAsGliss<firstVib['dt']+d['dtBurst'])):
				vol=0.0
			else:
				vol=0.0
				timePlayAsGliss=t; 
				if not (secVib['id'] is None): currentStepGliss+=currentAR; 
				elif currentStepGliss==0: done=True
				if (currentStepGliss+currentAR>=N_STEP_GLISSEMENT) | (currentStepGliss+currentAR<-1) | (secVib['id'] is None): 
					if d['AR']: 
						if (currentStepGliss+currentAR<0): done=True
						currentAR*=-1; 
						if (secVib['id'] is None): currentStepGliss+=currentAR
					elif not d['AR']: done=True
			listVolume[firstVib['id']]=vol
		t+=firstVib['dt']/50		

		listT+=[t]
		listD=np.r_[listD,np.array([listVolume])]

	listIdx=list(set(listIdx))
	if None in listIdx: listIdx.pop(listIdx.index(None))
	listD2=listD[:,listIdx]
	listLabs=[listChannels[idx] for idx in listIdx]
	plt.figure()
	for i in range(len(listIdx)):
		plt.plot(listT, listD2[:,i], label=listLabs[i])
	plt.legend()
	plt.show()
#<- Visualisation signaux



if 1:
	#!---------------------------!
	#!!!! ---- PROGRAMME ---- !!!!
	#!---------------------------!	
	stmConnected=False
	s=None

	app=QtWidgets.QApplication(sys.argv) # Pour utiliser les singleShot 

	listChangedVars=[]
	loadConfig()

	udpPlay=False
	UDPCli = WavyUDPClient("127.0.0.1", 26000,26001, updateUdpBuffer, 0.1, False)
	myUpdQueue = collections.deque()
	
	def initVariables(step):
		global listAmp, listFreq, listBurst, listTau, listCurrIdx, listPWM, listPlayOnce, \
			listPlaying, listVolume, listActiveGliss, dictGliss, listWav, listChannels, \
			listSinActive, listSinInActive, listImpActive, listImpInActive
		
		if step==1:
			listChannels=[channelNames[idx] if idx in channelNames.keys() else 'Channel %d'%(idx) for idx in range(AFFnCHANNELS)]
			listAmp=np.zeros(USEnCHANNELS)
			listFreq=np.ones(USEnCHANNELS)*default_freq
			listBurst=np.ones(USEnCHANNELS)*default_burst
			listTau=np.ones(USEnCHANNELS)*default_tau
			listCurrIdx=np.zeros(USEnCHANNELS).astype(int)
			listPWM=[(np.zeros(1)*maxPWM/2+maxPWM/2).astype(int)]*USEnCHANNELS
			listPlayOnce=np.zeros(USEnCHANNELS).astype('uint8')
			listPlaying=np.zeros(USEnCHANNELS).astype('uint8')
			listVolume=np.zeros(USEnCHANNELS)

			listActiveGliss=np.zeros(N_GLISSEMENT).astype('uint8')
			default_dictVib={'name':'---------',
							'id':None,
							'freq':default_freq,
							'amp':default_amp,
							'dt':default_dt}
			dictGliss={}
			for i in range(N_GLISSEMENT): 
				dictGliss['gliss%d'%(i+1)]={'prop':listeGlissPropagationTypes[0],
											'dtBurst':default_discrdt,
											'logCoef': default_logcoef,
											'continu':default_continu,
											'AR':default_AR,
											'listVib':[],
											'usedVib':[]}
				for j in range(N_STEP_GLISSEMENT): 
					dictGliss['gliss%d'%(i+1)]['listVib']+=[default_dictVib.copy()]

			listWav=[]
			for i in range(USEnCHANNELS):
				listWav+=[{'name': default_wav,
	       				   'data':None,
	       				   'duree':0.0,
						   'volume':1.0,
						   'loop': False,
						   'active': False}]

		if step==2:
			updateSignalsSin(range(AFFnCHANNELS), play=False)
			for i in range(AFFnCHANNELS): updateSinParams(i, 'amp', default_amp)

			listSinActive=lambda: [i for i in range(AFFnCHANNELS) if ex.listBoxActive[i].isChecked()]
			listSinInActive=lambda: [i for i in range(AFFnCHANNELS) if not ex.listBoxActive[i].isChecked()]
			listImpActive=lambda: [i for i in range(AFFnCHANNELS) if ex.listBoxImpActive[i].isChecked()]
			listImpInActive=lambda: [i for i in range(AFFnCHANNELS) if not ex.listBoxImpActive[i].isChecked()]	

			for i in range(AFFnCHANNELS):
				listWav[i]['loop']=ex.listBoxWavLoop[i].isChecked()
				listWav[i]['active']=ex.listBoxWavActive[i].isChecked()

	initVariables(1)

	ex = MainWindow()

	initVariables(2)

	def resetEx():
		global ex
		initVariables(1)
		ex2 = MainWindow()
		ex = ex2
		initVariables(2)

		ex.show()

	playAsBurst=False; lastPlayAsBurst=False; timePlayAsBurst=0.0
	typBurst='sin' #'imp'
	playAsGliss=False; lastPlayAsGliss=False; timePlayAsGliss=0.0; currentGliss=None; currentStepGliss=0; currentAR=1
	playAsWav=False;

	ex.show()

	signalUpdater=signalUpdater_C()
	udpUpdater=udpUpdater_C()
	#app.exec_()
	#sys.exit(app.exec_())


	savedData=[]

	# r=updateWavParams(0, 'name', 'Sin1')

	#!-------------------------!
	#!!!! ---- PROCESS ---- !!!!
	#!-------------------------!	
	def processSignal(arg): ### *** Processs ***
		global currentSendMode, currentMode, timePlayAsBurst, playAsBurst, typBurst, playAsWav, savedData, playAsGliss
		for i in range(len(arg)): arg[i]=str(arg[i])
		print("processSignal arg:", ';'.join(arg))
		inp=arg[0]; #print("inp: "+inp)
		useUdp=False
		if arg[-1]=='u': useUdp=True
		if inp=="": print("SignalEmpty"); return   
		elif inp[:3]=='cmd':
			if inp[3:] =='Ports': lPorts()
			elif inp[3:] == 'LConf': loadConfig(); resetEx()
			elif inp[3:] in ['Disconnect', 'Connect']: connectSTM(['Disconnect','Connect'].index(inp[3:]))
			elif inp[3:] == 'PlaySin': 
				if useUdp: ex.BtnPlaySin.setState(1)
				for i in range(AFFnCHANNELS): listPlayOnce[i]=0
				[updateSinParams(i, 'freq', ex.listBoxFreq[i].value(), updateSignals=False) for i in range(AFFnCHANNELS)]
				[updateSinParams(i, 'amp', ex.listBoxAmp[i].value(), updateSignals=False) for i in range(AFFnCHANNELS)]
				updateSignalsSin(listSinActive(),play=True); print('PlaySin')
				stopVib(listSinInActive());
			elif inp[3:] == 'StopUdp': 
				if useUdp: ex.BtnPlayUDP.setState(0)
				updateSignalsUdp(play=False); stopVib(); ex.BtnPlayUDP.setState(0); playAsBurst=False; print('StopUdp')

			elif inp[3:] == 'PlayUdp': 
				if useUdp: ex.BtnPlayUDP.setState(1)
				for i in range(AFFnCHANNELS): listPlayOnce[i]=1
				updateSignalsUdp(play=True); print('PlayUdp')
						
			elif inp[3:]=='BurstSin': 
				for i in range(AFFnCHANNELS): listPlayOnce[i]=0				
				[updateSinParams(i, 'freq', ex.listBoxFreq[i].value(), updateSignals=False) for i in range(AFFnCHANNELS)]
				[updateSinParams(i, 'amp', ex.listBoxAmp[i].value(), updateSignals=False) for i in range(AFFnCHANNELS)]
				updateSignalsSin(listSinActive(),play=True); playAsBurst=True; typBurst='sin'; print('BurstSin')
				stopVib(listSinInActive());
			elif inp[3:] == 'TrigGlis': 
				for i in range(AFFnCHANNELS): listPlayOnce[i]=0
				updateGlissVol(init=True);
			elif inp[3:]=='TrigImp': 
				for i in range(AFFnCHANNELS): listPlayOnce[i]=1					
				[updateImpParams(i, 'freq', ex.listBoxImpFreq[i].value(), updateSignals=False) for i in range(AFFnCHANNELS)]
				[updateImpParams(i, 'amp', ex.listBoxImpAmp[i].value(), updateSignals=False) for i in range(AFFnCHANNELS)]
				updateSignalsImp(listImpActive(),play=True); playAsBurst=True; typBurst='imp'; print('BurstImp')	
				stopVib(listImpInActive());	
			elif inp[3:]=='TrigWav': 
				if useUdp: ex.BtnTrigWav.setState(1)
				stopVib();				
				updateSignalsWav(range(AFFnCHANNELS),play=True); playAsBurst=True; typBurst='wav';
			elif inp[3:]=='StopWav': 
				if useUdp: ex.BtnTrigWav.setState(0)
				stopVib(); ex.BtnTrigWav.setTextToColor('black'); playAsBurst=False; print('StopWav')
			elif inp[3:]=='StopSin': 
				if useUdp: ex.BtnPlaySin.setState(0)
				updateSignalsSin(listSinActive(),play=False); stopVib(); ex.BtnPlaySin.setState(0); playAsBurst=False; print('StopSin')
			elif inp[3:]=='Stop': stopVib(); ex.BtnPlaySin.setState(0); ex.BtnTrigWav.setState(0); ex.BtnTrigWav.setTextToColor('black'); playAsBurst=False; playAsGliss=False; print('Stop')
			elif inp[3:]=='PlotGliss': plotGliss()
			else: print('%s not processed' %arg[0])
		elif inp[:3]=='sin':
			if inp[3:]=='SetFreq': 
				if useUdp: ex.listBoxFreq[int(arg[1])].setValue(float(arg[2])); return
				updateSinParams(int(arg[1]), 'freq', float(arg[2])); #print(arg[1], arg[2])
			elif inp[3:]=='SetAmp': 
				if useUdp: ex.listBoxAmp[int(arg[1])].setValue(float(arg[2])); return
				updateSinParams(int(arg[1]), 'amp', float(arg[2])); #print(arg[1], arg[2])
			elif inp[3:]=='SetBurst': 
				if useUdp: ex.listBoxBurst[int(arg[1])].setValue(float(arg[2])); return
				updateSinParams(int(arg[1]), 'burst', float(arg[2])); #print(arg[1], arg[2])
			elif inp[3:]=='SetActive': 
				if useUdp: ex.listBoxActive[int(arg[1])].setChecked(strBool(arg[2])); return
				if strBool(arg[2]): updateSinParams(int(arg[1]), 'amp', ex.listBoxAmp[int(arg[1])].value());
				else: stopVib([int(arg[1])]);
			elif inp[3:]=='ResetFreq': [(ex.listBoxFreq[i].setValue(default_freq), updateSinParams(i, 'freq', default_freq)) for i in range(AFFnCHANNELS)]; print('ResetFreq')
			elif inp[3:]=='ResetAmp': [(ex.listBoxAmp[i].setValue(default_amp), updateSinParams(i, 'amp', default_amp)) for i in range(AFFnCHANNELS)]; print('ResetAmp')
			elif inp[3:]=='ResetBurst': [(ex.listBoxBurst[i].setValue(default_burst), updateSinParams(i, 'burst', default_burst)) for i in range(AFFnCHANNELS)]; print('ResetBurst')
			elif inp[3:]=='ResetActive': 
				if useUdp: ex.boxResetActive.setChecked(strBool(arg[1])); return
				[chk.setChecked(strBool(arg[1])) for chk in ex.listBoxActive]
			else: print('%s not processed' %arg[0])
		elif inp[:3]=='imp':
			if inp[3:]=='SetFreq': 
				if useUdp: ex.listBoxImpFreq[int(arg[1])].setValue(float(arg[2])); return
				updateImpParams(int(arg[1]), 'freq', float(arg[2])); #print(arg[1], arg[2])
			elif inp[3:]=='SetAmp': 
				if useUdp: ex.listBoxImpAmp[int(arg[1])].setValue(float(arg[2])); return
				updateImpParams(int(arg[1]), 'amp', float(arg[2])); #print(arg[1], arg[2])
			elif inp[3:]=='SetTau': 
				if useUdp: ex.listBoxImpTau[int(arg[1])].setValue(float(arg[2])); return
				updateImpParams(int(arg[1]), 'tau', float(arg[2])); #print(arg[1], arg[2])
			elif inp[3:]=='SetActive': 
				if useUdp: ex.listBoxImpActive[int(arg[1])].setChecked(strBool(arg[2])); return
				if strBool(arg[2]): updateImpParams(int(arg[1]), 'amp', ex.listBoxImpAmp[int(arg[1])].value());
				else: stopVib([int(arg[1])]);
			elif inp[3:]=='CopyFreq': [ex.listBoxImpFreq[i].setValue(ex.listBoxFreq[i].value()) for i in range(AFFnCHANNELS)]; print('CopyFreq')
			elif inp[3:]=='CopyAmp': [ex.listBoxImpAmp[i].setValue(ex.listBoxAmp[i].value()) for i in range(AFFnCHANNELS)]; print('CopyAmp')
			elif inp[3:]=='ResetTau': [ex.listBoxImpTau[i].setValue(default_tau) for i in range(AFFnCHANNELS)]; print('ResetTau')
			elif inp[3:]=='ResetActive': 
				if useUdp: ex.boxResetImpActive.setChecked(strBool(arg[1])); return
				[chk.setChecked(strBool(arg[1])) for chk in ex.listBoxImpActive]
			else: print('%s not processed' %arg[0])
		elif inp[:3]=='gli':
			if inp[3:]=='SetName': 
				# if useUdp: ex.glistBoxGlissNames[[c.name for c in ex.glistBoxGlissNames].index('boxGlissNames_%s_%s'%(arg[1],arg[2]))].setCurrentIndex(int(arg[3])); return
				if useUdp: ex.listBoxGlissNames[int(arg[1])][int(arg[2])].setCurrentIndex(int(arg[3])); return
				updateGliParams(int(arg[1]), int(arg[2]), 'name', str(arg[4])); #print('SetName', arg[1], arg[2], arg[3], arg[4])
			elif inp[3:]=='SetFreq': 
				if useUdp: ex.listBoxGlissFreq[int(arg[1])][int(arg[2])].setValue(float(arg[3])); return
				updateGliParams(int(arg[1]), int(arg[2]), 'freq', float(arg[3])); #print('SetFreq', arg[1], arg[2], arg[3]); 
			elif inp[3:]=='SetAmp': 
				if useUdp: ex.listBoxGlissAmp[int(arg[1])][int(arg[2])].setValue(float(arg[3])); return
				updateGliParams(int(arg[1]), int(arg[2]), 'amp', float(arg[3])); #print('SetAmp', arg[1], arg[2], arg[3])
			elif inp[3:]=='SetDt': 
				if useUdp: ex.listBoxGlissDt[int(arg[1])][int(arg[2])].setValue(float(arg[3])); return
				updateGliParams(int(arg[1]), int(arg[2]), 'dt', float(arg[3])); #print('SetDt', arg[1], arg[2], arg[3])
			elif inp[3:]=='SetDiscrDT': 
				if useUdp: ex.listBoxGlissDiscrDT[int(arg[1])].setValue(float(arg[2])); return
				if ex.listBoxGlissProp[int(arg[1])].currentText()=='Log':
					updateGliParams(int(arg[1]), None, 'logCoef', float(arg[2])); #print('SetDt', arg[1], arg[2], arg[3])
				elif ex.listBoxGlissProp[int(arg[1])].currentText()=='Discret':
					updateGliParams(int(arg[1]), None, 'dtBurst', float(arg[2])); #print('SetDt', arg[1], arg[2], arg[3])			
			elif inp[3:]=='SetProp': 
				if useUdp: ex.listBoxGlissProp[int(arg[1])].setCurrentIndex(int(arg[2])); return
				if str(arg[3])=='Discret':
					ex.listBoxGlissDiscrDT[int(arg[1])].setEnabled(1); 
					ex.listBoxGlissDiscrDT[int(arg[1])].setValue(dictGliss['gliss%d'%(int(arg[1])+1)]['dtBurst']); 
					ex.listLabGlissDiscrDT[int(arg[1])].setEnabled(1); 
					ex.listLabGlissDiscrDT[int(arg[1])].setText('dtBurstDiscret')
				elif str(arg[3])=='Log':
					ex.listBoxGlissDiscrDT[int(arg[1])].setEnabled(1); 
					ex.listBoxGlissDiscrDT[int(arg[1])].setValue(dictGliss['gliss%d'%(int(arg[1])+1)]['logCoef']); 
					ex.listLabGlissDiscrDT[int(arg[1])].setEnabled(1); 
					ex.listLabGlissDiscrDT[int(arg[1])].setText('logCoef')					
				else:
					ex.listBoxGlissDiscrDT[int(arg[1])].setEnabled(0); 
					ex.listLabGlissDiscrDT[int(arg[1])].setEnabled(0); 
				updateGliParams(int(arg[1]), None, 'prop', str(arg[3]));
				#print('SetProp', arg[1], arg[2], arg[3])
			elif inp[3:]=='SetCont': 
				if useUdp: ex.listBoxGlissCont[int(arg[1])].setChecked(strBool(arg[2])); return
				updateGliParams(int(arg[1]), None, 'continu', strBool(arg[2])); #print('SetCont', arg[1], arg[2])
			elif inp[3:]=='SetAR': 
				if useUdp: ex.listBoxGlissAR[int(arg[1])].setChecked(strBool(arg[2])); return
				updateGliParams(int(arg[1]), None, 'AR', strBool(arg[2])); #print('SetAR', arg[1], arg[2])
			elif inp[3:]=='SetChk': 
				if useUdp: ex.listBoxGliss[int(arg[1])].setChecked(strBool(arg[2])); return
				if strBool(arg[2]):
					listOff=list(range(len(listActiveGliss))); listOff.pop(int(arg[1]))
					for i in listOff: listActiveGliss[i]=0; ex.listBoxGliss[i].setChecked(False)
				listActiveGliss[int(arg[1])]=strBool(arg[2]); print('SetChk', int(arg[1]), strBool(arg[2]))
			elif inp[3:7]=='Copy': 
				currTxt=ex.listBoxGlissNames[int(inp.split('_')[1])][int(inp.split('_')[2])].currentText()
				if currTxt!='---------':
					valFreq=ex.listBoxFreq[listChannels.index(currTxt)].value()
					valAmp=ex.listBoxAmp[listChannels.index(currTxt)].value()
					idx=(int(inp.split('_')[1]),int(inp.split('_')[2]))
					ex.listBoxGlissFreq[idx[0]][idx[1]].setValue(valFreq)
					ex.listBoxGlissAmp[idx[0]][idx[1]].setValue(valAmp)
				print('Copy', int(inp.split('_')[1]), int(inp.split('_')[2]))
			else: print('%s not processed' %arg[0])
		elif inp[:3]=='wav':
			if inp[3:]=='Load': [updateWavParams(i, 'name', ex.listBoxWavName[i].text()) for i in range(AFFnCHANNELS)];
			elif inp[3:]=='SetLoop': 
				if useUdp: ex.listBoxWavLoop[int(arg[1])].setChecked(strBool(arg[2])); return
				updateWavParams(int(arg[1]), 'loop', strBool(arg[2]))	
			elif inp[3:]=='SetActive': 
				if useUdp: ex.listBoxWavActive[int(arg[1])].setChecked(strBool(arg[2])); return
				updateWavParams(int(arg[1]), 'active', strBool(arg[2]))
			elif inp[3:]=='ResetLoop': 
				if useUdp: ex.boxResetWavLoop.setChecked(strBool(arg[1])); return
				[chk.setChecked(strBool(arg[1])) for chk in ex.listBoxWavLoop]
			elif inp[3:]=='ResetActive': 
				if useUdp: ex.boxResetWavActive.setChecked(strBool(arg[1])); return
				[chk.setChecked(strBool(arg[1])) for chk in ex.listBoxWavActive]
			elif inp[3:]=='ChangedName': 
				if useUdp: ex.listBoxWavName[int(arg[1])].setText(arg[2]); return
				ex.listBoxWavName[int(arg[1])].setStyleSheet("color: black;")	
			elif inp[3:]=='ChangedVolume': 
				if useUdp: ex.listBoxWavVolume[int(arg[1])].setValue(float(arg[2])); return
				updateWavParams(int(arg[1]), 'volume', float(arg[2]))	
			elif inp[3:]=='ChangedVolume_all': 
				if useUdp: 
					[updateWavParams(i, 'volume', float(arg[1])) for i in range(AFFnCHANNELS)]
					[sb.setValue(float(arg[1])) for sb in ex.listBoxWavVolume]
					ex.boxWavVolume.setValue(float(arg[1])); return
				[updateWavParams(i, 'volume', float(arg[1])) for i in range(AFFnCHANNELS)]
				[sb.setValue(float(arg[1])) for sb in ex.listBoxWavVolume]
			else: print('%s not processed' %arg[0])
		else: print('%s not processed' %arg[0])

	def OnGUI(arg):
		pass

	class gestionSigC(QtCore.QObject):
		trigCommandeAcqEvent=QtCore.pyqtSignal(list)	
		trigOnGui=QtCore.pyqtSignal(list)
		connctionsDefined=False

		def transfertSig(self, nom, arg):
			if not self.connctionsDefined: return
			if nom=="commandeAcqEvent":
				self.trigCommandeAcqEvent.emit(arg)
			if nom=="onGui":
				self.trigOnGui.emit(arg)
		
		def defineConnections(self):
			self.trigCommandeAcqEvent.connect(processSignal)
			self.trigOnGui.connect(OnGUI)
			
			self.connctionsDefined=True
	gestionSig=gestionSigC()
	gestionSig.defineConnections()  	
	
	
	#!-------------------------!
	#!!!! ---- QUITTER ---- !!!!
	#!-------------------------!	
	import signal
	passedQuitHandler=False
	def quitHandler(sig=None, fr=None):
		global passedQuitHandler
		print ('\nKilling'+[""," Again"][passedQuitHandler])
		if not passedQuitHandler:
			passedQuitHandler=True
			#!!!!!!! Killer les threads
			connectSTM(False)
			udpUpdater.s.close()
			signalUpdater.signal_update_thread_running=False
			udpUpdater.signal_update_thread_running=False
			UDPCli.terminate()
			plt.close('all')
			while not signalUpdater.ended: pass
			#!!!!!!!
	#	   global finScript   #A commenter si app	
	#	   finScript=True   #A commenter si app 
			app.closeAllWindows() #A commenter si on n'a pas d'app ou si on veut pas fermer les fenetres
			app.exit()   #A commenter si on n'a pas d'app
	signal.signal(signal.SIGINT, quitHandler)
	
	"""Si app:"""
	#__________##
	def attends():
		QtCore.QTimer.singleShot(100,attends) 
	attends()
	app.exec_()
	quitHandler()   #A commenter si on ne veut pas fermer les threads apres fermeture fenetres
	#__________##
		
	
	

