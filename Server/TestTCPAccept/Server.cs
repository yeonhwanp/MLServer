﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;

// Notes: Abstract the protobuf method to <T>
// Notes: Two separate streams for UDP and TCP.

namespace Servers
{
    /// <summary>
    /// Object to store stuff for TCPAsyncListener
    /// </summary>
    /// 

    public class StartBoth
    {

        // Ports to be used
        private const int TCPInPort = 15000;
        private const int UDPInPort = 8888;
        private const int UDPOutPort = 9999;

        // TCP network stuff 
        static IPAddress local = IPAddress.Parse("127.0.0.1");
        static IPEndPoint TCPEndPoint = new IPEndPoint(local, TCPInPort);
        static Socket TCPlistener = new Socket(local.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        // UDP network stuff
        public static UdpClient UDPlistener = new UdpClient(UDPInPort);

        public static bool _TCPlistening = false;
        public static bool _UDPlistening = false;


        public static void StartListening()
        {

            TCPlistener.Bind(TCPEndPoint);

            Console.WriteLine("Waiting for connections...");

            while (true)
            {
                try
                {
                    // Don't know what this code is for but it was there before
                    TCPlistener.Listen(100);

                    // Only activate the listeners if they're not already launched. If they are, just skips right over.
                    if (!_TCPlistening)
                    {
                        _TCPlistening = true;
                        TCPlistener.BeginAccept(new AsyncCallback(TCPAsyncListener.AcceptCallBack), TCPlistener);
                    }   
                    if (!_UDPlistening)
                    {
                        _UDPlistening = true;
                        UDPlistener.BeginReceive(new AsyncCallback(UDPAsyncListener.ReadCallBack), UDPlistener);
                    }
                }

                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }

        }
    }

    public class StateObject
    {
        public Socket workSocket = null;
        public const int Buffersize = 1024;
        public byte[] buffer = new byte[Buffersize];
        public StringBuilder sb = new StringBuilder();
    }

    /// <summary>
    /// Methods needed to receive/sendback data.
    /// Port: 15000
    /// </summary>
    public class TCPAsyncListener
    {
        /// <summary>
        /// Here we go after we get a signal.
        /// </summary>
        public static void AcceptCallBack(IAsyncResult ar)
        { 

            Console.WriteLine("TCP Connection made. Waiting for information...");

            // Socket that was plugged in
            Socket listener = (Socket)ar.AsyncState;
            // Completed the server thing -- now this socket contains all of the information
            Socket handler = listener.EndAccept(ar);

            // Create a stateobject to hold the socket
            StateObject state = new StateObject();
            state.workSocket = handler;
            
            // There's a buffer, buffersize... etc. Passing in state.
            handler.BeginReceive(state.buffer, 0, StateObject.Buffersize, 0, new AsyncCallback(ReadCallBack), state);
        }

        public static void ReadCallBack(IAsyncResult ar)
        {
            String content = String.Empty;

            // There's an asynchronous state object -- get the state object and handler object
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Endreceive stores number of bytes received.
            int bytesRead = handler.EndReceive(ar);

            // Append to the stringbuilder -> print out the result
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
            content = state.sb.ToString();
            Console.WriteLine("\nThe client sent this: {0}\n", content);
            Send(handler, content);
        }

        public static void Send(Socket handler, String data)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallBack), handler);
        }

        private static void SendCallBack(IAsyncResult ar)
        {
            Socket handler = (Socket)ar.AsyncState;

            int bytesSent = handler.EndSend(ar);
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

            StartBoth._TCPlistening = false;
        }
    }

    /// <summary>
    /// A class to represent all of the work done through a UDP connection.
    /// UDPAsyncListener Port: 8888
    /// </summary>
    public class UDPAsyncListener
    {
        private const int listenPort = 8888;
        private const int replyPort = 9999;

        /// <summary>
        /// Goes here after receiving a signal
        /// </summary>
        public static void ReadCallBack(IAsyncResult res)
        {
            UdpClient client = (UdpClient)res.AsyncState;
            IPEndPoint RemoteIPEndPoint = new IPEndPoint(IPAddress.Any, listenPort);

            Console.WriteLine("Receiving data...");
            byte[] received = client.EndReceive(res, ref RemoteIPEndPoint);
            string message = Encoding.ASCII.GetString(received, 0, received.Length);

            Console.WriteLine("Data received: \n {0} \n", message);
            Console.WriteLine("Waiting for sendback request...");

            byte[] toSendBack = client.Receive(ref RemoteIPEndPoint);
            string theReply = Encoding.ASCII.GetString(toSendBack, 0, toSendBack.Length);

            if (theReply.ToUpper() == "SENDBACK")
            {
                Console.WriteLine("Request for sendback received, sending back...");
                SendBack(RemoteIPEndPoint.Address, received, replyPort);
            }
            else
            {
                StartBoth._UDPlistening = false;
            }
            StartBoth._UDPlistening = false;
        }

        public static void SendBack(IPAddress otherIP, byte[] data, int replyPort)
        {
            Socket thisSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint replyEndPoint = new IPEndPoint(otherIP, replyPort);

            thisSocket.SendTo(data, replyEndPoint);
            Console.WriteLine("Data sent back.");
            Console.WriteLine();
        }
    }
}