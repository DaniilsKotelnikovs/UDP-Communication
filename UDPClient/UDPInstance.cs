using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UDPClient
{
    public class UdpInstance
    {
        // UDP client for sending and receiving data.
        private UdpClient udpClient;

        // Endpoint for the UDP connection.
        private IPEndPoint endPoint;

        // Queues for sending and receiving data.
        private ConcurrentQueue<byte[]> sendQueue;
        private ConcurrentQueue<byte[]> receiveQueue;

        // Events for signaling when data is added to the queues.
        private AutoResetEvent sendEvent;
        private AutoResetEvent receiveEvent;

        // Constructor.
        public UdpInstance(string ipAddress, int port)
        {
            // Initialize the UDP client and endpoint.
            udpClient = new UdpClient(port);
            endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);

            // Initialize the queues and events.
            sendQueue = new ConcurrentQueue<byte[]>();
            receiveQueue = new ConcurrentQueue<byte[]>();
            sendEvent = new AutoResetEvent(false);
            receiveEvent = new AutoResetEvent(false);
        }

        // Method for sending data.
        public void Send(byte[] data)
        {
            // Add the data to the send queue and signal the send event.
            sendQueue.Enqueue(data);
            sendEvent.Set();
        }

        // Method for starting the listening process.
        public void StartListening()
        {
            // Start a new task for receiving data.
            Task.Run(() =>
            {
                while (true)
                {
                    // Receive data and add it to the receive queue.
                    byte[] bytes = udpClient.Receive(ref endPoint);
                    receiveQueue.Enqueue(bytes);

                    // Signal the receive event.
                    receiveEvent.Set();
                }
            });

            // Start a new task for processing the send queue.
            Task.Run(() =>
            {
                while (true)
                {
                    // Wait for the send event to be signaled.
                    sendEvent.WaitOne();

                    // Process the send queue.
                    ProcessSendQueue();
                }
            });

            // Start a new task for processing the receive queue.
            Task.Run(() =>
            {
                while (true)
                {
                    // Wait for the receive event to be signaled.
                    receiveEvent.WaitOne();

                    // Process the receive queue.
                    ProcessReceiveQueue();
                }
            });
        }

        // Method for processing the send queue.
        private void ProcessSendQueue()
        {
            // Dequeue data from the send queue and send it.
            while (sendQueue.TryDequeue(out byte[] sendData))
            {
                udpClient.Send(sendData, sendData.Length, endPoint);
            }
        }

        // Method for processing the receive queue.
        private void ProcessReceiveQueue()
        {
            // Dequeue data from the receive queue and process it.
            while (receiveQueue.TryDequeue(out byte[] receivedData))
            {
                Console.WriteLine("Received: {0}", Encoding.ASCII.GetString(receivedData));
            }
        }
    }
}