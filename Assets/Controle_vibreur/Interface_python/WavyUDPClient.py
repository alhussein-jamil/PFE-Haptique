#!/usr/bin/env python
# coding: utf-8
# Basic Client UDP for wavy
# Version 1.0 


from threading import Thread
import socket
import time
import sys
import collections


SFRAME_HEADER=b'$D'
TRAME_HEAD_16bits=int.from_bytes(b'C')
TRAME_HEAD_8bits=int.from_bytes(b'c')
# ===============================================================
# ---------------------- class WavyUDPClient ------------------------
class WavyUDPClient(Thread):
    '''
        Class that represents a UDP socket Client.
    '''
    isVerbose = False

    def __init__(self, ip, portListener, portSender, onDataReceived, timeout=0.1, isVerbose = False):
        '''
        Creates a UDP Client Thread that listens on a certain IP. It runs in its own thread, so the
        constructor returns immediately. State changes invoke the callback
        onDataReceived(data).
        @param ip: the IP address of the host
        @param portSender: the IP port where to sender (0..65535)
        @param portListener: the IP port where to listen (0..65535)
        @param onDataReceived: the callback function to register
        @param isVerbose: if true, debug messages are written to System.out, default: False
        '''

        Thread.__init__(self)
        self.server_address_reader = (ip, portListener)
        self.server_address_sender = (ip, portSender)
        self.sockSender = None
        self.sockReader = None
        self.dataReceived = onDataReceived
        self.timeout = timeout
        WavyUDPClient.isVerbose = isVerbose
        self.running = False
        self.daemon = True
        self.parsingState = 0
        self.parsingSubState = 0
        self.is16bits = True
        self.SAMPLE_COUNT = 0
        self.CHANNEL_COUNT = 0
        self.lastTrigger = 0
        self.processingData = [] #list[[sample1 : ch1, ch2, ...] [sample2 : ch1, ch2, ....]]
        self.valuePart = b''
        WavyUDPClient.debug("Starting UDP Thread with parameter :R" +str(self.server_address_reader))
        self.start()

    def run(self):
        self.sockReader = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        bufSize = 2058
        try:
            self.sockReader.bind(self.server_address_reader)
        except socket.error :
            WavyUDPClient.debug("Fatal error while creating WavyUDPClient: Bind failed.")

        self.sockReader.settimeout(self.timeout)
        self.running = True
        while self.running:
            data = None
            try:
                data, adrs = self.sockReader.recvfrom(bufSize)
            except socket.timeout :  #timeout
                continue
            if data == None or len(data) == 0: # Client disconnected
                WavyUDPClient.debug("conn.recv() returned None")
                continue
            WavyUDPClient.debug("Received msg from("+str(adrs)+") : " + str(data) )
            self.parseFrame(data)
        if(self.sockReader != None):
            self.sockReader.close()

    def parseFrame(self, data):
        i = 0
        while i < len(data):
            x = data[i]
            if self.parsingState == 0: #Header 1/2
                i += 1
                if x == SFRAME_HEADER[0] :
                    self.parsingState += 1 #next step
            elif self.parsingState ==  1: #Header 2/2
                if x == SFRAME_HEADER[1] :
                    i += 1
                    self.parsingState += 1 #next step
                else :
                    self.parsingState = 0 #unknown
            elif self.parsingState ==  2 : #parametter
                    i += 1
                    param = x # get param of the frame
                    self.SAMPLE_COUNT = 0b1<<(((param>>5)+1)&0b111)
                    self.CHANNEL_COUNT = param&0b11111
                    self.parsingState += 1 #next step
                    self.parsingSubState = 0
                    self.processingData = []
            elif self.parsingState ==  3 : #parsing subframe 
                i += 1 
                if self.parsingSubState == 0: #BitSize 8/16bits
                    self.processingData.append([]) #add a array for the sample
                    self.is16bits =  2 if x==TRAME_HEAD_16bits else 1
                    self.parsingSubState += 1
                elif self.parsingSubState ==  1 :#trigger ignored
                    if(self.lastTrigger+1 != x and x!=0):
                        print("Miss")
                    self.lastTrigger = x
                    self.parsingSubState += 1
                    self.currentChannel = 0
                    self.valuePart = b''
                elif self.parsingSubState ==  2 : #data 
                    self.valuePart+=bytes([x])
                    if self.is16bits == len(self.valuePart) :
                        self.processingData[-1].append(int.from_bytes(self.valuePart))
                        self.valuePart = b''
                        if len(self.processingData[-1]) == self.CHANNEL_COUNT : #we have all the channel for this sample
                            if len(self.processingData) == self.SAMPLE_COUNT : #we have all the sample
                                #then restart parse a new frame
                                self.parsingSubState = 0
                                self.parsingState = 0
                                self.dataReceived(self.processingData) #Callback , new frame parsed
                            else : #start parse the next sample
                                self.parsingSubState = 0
        

    def terminate(self):
        '''
        Closes the connection and terminates the server thread.
        Releases the IP port.
        '''
        WavyUDPClient.debug("Calling terminate()")
        if not self.running:
            WavyUDPClient.debug("Reader not running")
            return
        self.running = False
        if(self.sockSender != None):
            self.sockSender.close()


    def sendMessage(self, msg):
        '''
        Sends the information msg to the server.  
        @param msg: the message to send
        '''
        if self.sockSender == None:
            self.sockSender = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        WavyUDPClient.debug("sendMessage() with msg = " + str(msg))
        try:
            self.sockSender.sendto(msg, self.server_address_sender)
        except:
            WavyUDPClient.debug("Exception in sendMessage()" + str(sys.exc_info()[0]) + " at line # " +  str(sys.exc_info()[-1].tb_lineno))

    def isRunning(self):
        return self.running

    @staticmethod
    def debug(msg):
        if WavyUDPClient.isVerbose:
            print("   WavyUDPClient-> " + msg)


##------------------Debugging ---------------------##

def printMS(ms):
    global dataQueue
    for x in ms:
        dataQueue.append(x)
    print(ms)

def useData():
    global dataQueue
    return dataQueue.popleft()

def main():
    
    UDPCli = WavyUDPClient("127.0.0.1", 26000,26001, printMS, 0.1 ,True)
    print("press enter to quit")
    re = input()
    UDPCli.sendMessage(b'Coucou')
    correctionTimingUDP = 64
    UDPCli.sendMessage(b'$d'+bytes([(correctionTimingUDP+100) & 0xff]))
    re = input()
    print("End")
    UDPCli.terminate()

if __name__ == '__main__':
    dataQueue = collections.deque()
    main()