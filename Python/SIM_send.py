import math
import socket
import struct
import time
from enum import IntEnum
import numpy as np

IP = '127.0.0.1'
PORT = 11000

# Enum for Degrees of Actuation
class DOAs(IntEnum):
    th_flex = 0
    th_rot = 1
    index = 2
    middle = 3
    ring = 4
    little = 5
    wr_flex_ext = 6
    wr_uln_rad = 7
    wr_sup_pron = 8

# DataType definitions
class DataType:
    EMG_RAW = bytes([0, 0])
    EMG_ARV = bytes([0, 1])
    CTRL = lambda doa: bytes([1, doa])
    CTRL2 = lambda doa: bytes([3, doa])
    FMG_RAW = bytes([2, 0])
    FMG_ARV = bytes([2, 1])

class UDPStreamlinedSender:
    def __init__(self, ):
        self.udp_counter = 0
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

        self.sock.connect((IP, PORT))

    def on_new_input(self,predictions:list):
        for i in range(len(predictions)):
            num_count = 1
            data_type = DataType.CTRL(i)
            data_bytes = struct.pack("d", predictions[i])

            self.send_udp_message(num_count, float, data_type, data_bytes)

    def get_byte_for_type(self, py_type):
        floating_types = {float}
        signed_types = {int}
        unsigned_types = {bytes}

        if py_type not in floating_types | signed_types | unsigned_types:
            raise Exception(f"Unsupported data type: {py_type}")

        type_info = 0b00000000
        if py_type in floating_types:
            type_info |= 0b00100000
        elif py_type in signed_types:
            type_info |= 0b00010000

        type_info += struct.calcsize('d' if py_type is float else 'i')
        return type_info

    def send_udp_message(self, num_count, val_type, data_type, data):
        message = bytearray()
        message.append(num_count)
        message.append(self.get_byte_for_type(val_type))
        message.extend(data_type)
        message.extend(struct.pack("d", time.time()))
        message.extend(data)
        message.extend(struct.pack("H", self.udp_counter))

        self.udp_counter = (self.udp_counter + 1) % 65536
        self.sock.send(message)



newSender = UDPStreamlinedSender()

rate_hz = 20
interval = 1.0 / rate_hz  # 0.05 seconds (50 ms)
t = 0.0
step = interval
a = 50

while True:
    # Generate 4 sine values, each with its own frequency or phase
    values = [
        a * math.sin(2 * math.pi * 1 * t),     # 1 Hz sine
        a *math.sin(2 * math.pi * 0.5 * t),   # 0.5 Hz sine
        a *math.sin(2 * math.pi * 2 * t),     # 2 Hz sine
        a *math.sin(2 * math.pi * 1 * t),  # 1 Hz sine, phase shifted
        a *math.sin(2 * math.pi * 1 * t),     # 1 Hz sine
        a *math.sin(2 * math.pi * 0.5 * t),   # 0.5 Hz sine
        a *math.sin(2 * math.pi * 2 * t),     # 2 Hz sine
        a *math.sin(2 * math.pi * 1 * t),  # 1 Hz sine, phase shifted
        a * math.sin(2 * math.pi * 1 * t),     # 1 Hz sine
        a *math.sin(2 * math.pi * 0.5 * t),   # 0.5 Hz sine
        a *math.sin(2 * math.pi * 2 * t),     # 2 Hz sine
        a *math.sin(2 * math.pi * 1 * t)  # 1 Hz sine, phase shifted
    ]

    newSender.on_new_input(values)

    time.sleep(interval)
    t += step