import threading
import asyncio
import io
import time
import sys
import os

from picamera import PiCamera

import tornado.httpserver
import tornado.ioloop
import tornado.web
import tornado.websocket
from tornado.options import define, options


is_service_stopped = False
wsVideoClients = []


image_stream = io.BytesIO()


camera = PiCamera()

camera.resolution = (320, 240)
#camera.resolution = (640, 480)
#camera.resolution = (1024, 768)

camera.framerate = 30
#camera.framerate = 60 # if enabled, then if use_video_port is True, then images are not in good color. use_video_port is for capturing quickly


def start_camera(loop):
    global wsVideoClients
    global is_service_stopped
    global image_stream
    global camera
    
    asyncio.set_event_loop(loop)
    
    while is_service_stopped == False:
        camera.capture(image_stream, format='jpeg', use_video_port = True, quality=100)
    
        if len(wsVideoClients) > 0:
            img = image_stream.getvalue()
            for wsClient in wsVideoClients:
                wsClient.write_message(img, binary=True)
            sleep(0.1)
            
        image_stream.truncate()
        image_stream.seek(0)
        
    print("camera stopped!")
    

def sleep(duration_in_ms):
    # The perf_counter() function always returns the float value of time in seconds
    duration_in_sec = duration_in_ms / 1000
    now = time.perf_counter()
    end = now + duration_in_sec
    while now < end:
        now = time.perf_counter()

        
define("port", default=8080, type=int)
class Application(tornado.web.Application):
    def __init__(self):
        handlers = [(r"/", MainHandler), (r"/wsVideo", WSVideoHandler)]
        super().__init__(handlers)
        
        
class MainHandler(tornado.web.RequestHandler):
    def get(self):
        global image_stream
        image = image_stream.getvalue()
        image_size = len(image)
        self.set_header('Content-type', 'image/jpeg')
        self.set_header('Content-length', image_size)
        print('image size: ' + str(image_size))
        self.write(image)
        

class WSVideoHandler(tornado.websocket.WebSocketHandler):
    def check_origin(self, origin):
        return True

    def open(self):
        global wsVideoClients
        wsVideoClients.append(self)
        print('client connected: ' + str(len(wsVideoClients)))
              
    def on_message(self, message):
        pass

    def on_close(self):
        global wsVideoClients
        wsVideoClients.remove(self)
        print('client disconnected: ' + str(len(wsVideoClients)))        
        print("close_code: " + str(self.close_code))    
        print("close_reason: " + str(self.close_reason))
        

if __name__ == '__main__':
    try:
        loop = asyncio.new_event_loop()
        captureThread = threading.Thread(target=start_camera, args=(loop,))
        captureThread.start()
        
        app = Application()
        httpServer = tornado.httpserver.HTTPServer(app)
        httpServer.listen(options.port)

        print("service started at port: " + str(options.port))

        tornado.ioloop.IOLoop.current().start()
        
    except KeyboardInterrupt:
        is_service_stopped = True
        time.sleep(0.5)
        camera.close()
        time.sleep(0.5)
        print("service stopped!")