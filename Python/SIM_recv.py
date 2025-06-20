import socket
import struct
import threading
import time
from collections import defaultdict

ENABLE_LOGGING = True
IP = '127.0.0.1'
PORT = 11000

# UdpObject: Interprets received UDP bytes
class UdpObject:
    def __init__(self, data_bytes):
        self.category = data_bytes[2]
        self.sub_category = data_bytes[3]
        self.dtype_code, self.dtype_size, self.dtype = self.parse_dtype(data_bytes[1])
        self.timestamp = struct.unpack('<d', data_bytes[4:12])[0]
        self.counter = struct.unpack('<H', data_bytes[-2:])[0]

        data_len = data_bytes[0] * self.dtype_size
        self.raw_data = data_bytes[12:12 + data_len]

    def parse_dtype(self, type_byte):
        is_float = (type_byte & 0b00100000) != 0
        is_signed = (type_byte & 0b00010000) != 0
        size = type_byte & 0b00001111

        if is_float:
            if size == 4:
                return 'f', 4, float
            elif size == 8:
                return 'd', 8, float
        else:
            if is_signed:
                return {1: 'b', 2: 'h', 4: 'i', 8: 'q'}[size], size, int
            else:
                return {1: 'B', 2: 'H', 4: 'I', 8: 'Q'}[size], size, int

        raise ValueError("Unsupported type byte")

    def get_data(self):
        count = len(self.raw_data) // self.dtype_size
        fmt = f"<{count}{self.dtype_code}"  # Little-endian
        try:
            return list(struct.unpack(fmt, self.raw_data))
        except Exception as e:
            raise ValueError(f"Failed to unpack data: {e}")


# ReceivingUdp: Handles continuous listening and storing
class ReceivingUdp:
    def __init__(self):
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind((IP, PORT))
        self.last_received_packets = defaultdict(list)
        self.lock = threading.Lock()
        self.running = True

    def start_udp_listener(self):
        try:
            thread = threading.Thread(target=self.recv_loop, daemon=True)
            thread.start()
        except Exception as e:
            print(f"Error starting UDP listener: {e}")

    def recv_loop(self):
        while self.running:
            try:
                data, addr = self.sock.recvfrom(4096)
                udp_obj = UdpObject(data)
                key = (udp_obj.category << 8) | udp_obj.sub_category

                with self.lock:
                    self.last_received_packets[key].append(udp_obj)

                    # keeps the last 100 samples for each key
                    if len(self.last_received_packets[key]) > 100:
                        self.last_received_packets[key].pop(0)

                # whatever one wants to do with the sample
                if ENABLE_LOGGING:
                    values_str = " ".join(str(x) for x in udp_obj.get_data())
                    print(f"[UDP] {time.time():.3f}, cat:{udp_obj.category}, sub:{udp_obj.sub_category}, "
                          f"counter:{udp_obj.counter}, data:[{values_str}]")

            except Exception as e:
                print(f"Error receiving data: {e}")

    def get_data(self, category, sub_category, samples_back=0):
        key = (category << 8) | sub_category
        with self.lock:
            data_list = self.last_received_packets.get(key)
            if not data_list:
                return None

            if samples_back > 0 and samples_back < len(data_list):
                return data_list[-samples_back].get_data()

            return data_list[-1].get_data()

    def stop(self):
        self.running = False
        self.sock.close()


if __name__ == "__main__":
    receiver = ReceivingUdp()
    receiver.start_udp_listener()

    try:
        while True:
            time.sleep(1)
            data = receiver.get_data(category=1, sub_category=2)
            if data:
                print("Latest data for (1,2):", data)
    except KeyboardInterrupt:
        receiver.stop()
        print("Receiver stopped.")