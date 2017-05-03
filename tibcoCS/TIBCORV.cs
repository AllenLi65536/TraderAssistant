using System;
using TIBCO.Rendezvous;

namespace TIBCORV
{
    public delegate void ListenerFunc(object listener , MessageReceivedEventArgs messageReceivedEventArgs);

    class TIBCORVListener
    {
        int N = 0;
        private Transport[] transport;

        public TIBCORVListener(string[] service , string[] network , string[] daemon) {

            N = service.Length;
            transport = new Transport[N];
            try {
                /* Create internal TIB/Rendezvous machinery */
                if (TIBCO.Rendezvous.Environment.IsIPM()) {
                    TIBCO.Rendezvous.Environment.Open(".\\tibrvipm.cfg");
                } else {
                    TIBCO.Rendezvous.Environment.Open();
                }
            } catch (RendezvousException exception) {
                Console.Error.WriteLine("Failed to open Rendezvous Environment: {0}" , exception.Message);
                Console.Error.WriteLine(exception.StackTrace);
                Console.ReadKey();
                System.Environment.Exit(1);
            }
            Console.WriteLine("Open Environment"); //
            for (int i = 0; i < N; i++) {
                // Create Network transport
                try {
                    transport[i] = new NetTransport(service[i] , network[i] , daemon[i]);
                    Console.WriteLine("Create Net Transport"); //
                } catch (RendezvousException exception) {
                    Console.Error.WriteLine("Failed to create NetTransport");
                    Console.Error.WriteLine(exception.StackTrace);
                    Console.ReadKey();
                    System.Environment.Exit(1);
                }
            }
        }

        public void Listen(string[] topic , ListenerFunc[] CallBack) {
            Listener[] listeners = new Listener[N];
            for (int i = 0; i < N; i++) {
                // Create listeners for specified subjects                
                try {
                    listeners[i] = new Listener(Queue.Default , transport[i] , topic[i] , null);
                    listeners[i].MessageReceived += new MessageReceivedEventHandler(CallBack[i]);
                    Console.Error.WriteLine("Listening on: " + topic[i]);
                } catch (RendezvousException exception) {
                    Console.Error.WriteLine("Failed to create listener:");
                    Console.Error.WriteLine(exception.StackTrace);
                    System.Environment.Exit(1);
                }
            }

            // dispatch Rendezvous events
            while (true) {
                try {
                    //Console.WriteLine(Queue.Default.Count);//
                    Queue.Default.Dispatch();
                } catch (RendezvousException exception) {
                    Console.Error.WriteLine("Exception dispatching default queue:");
                    Console.Error.WriteLine(exception.StackTrace);
                    break;
                }
            }

            // Force optimizer to keep alive listeners up to this point.
            GC.KeepAlive(listeners);

            TIBCO.Rendezvous.Environment.Close();

        }
    }

    class TIBCORVSender
    {
        private Transport transport = null;

        public TIBCORVSender(string service , string network , string daemon) {
            try {
                /* Create internal TIB/Rendezvous machinery */
                if (TIBCO.Rendezvous.Environment.IsIPM()) {
                    TIBCO.Rendezvous.Environment.Open(".\\tibrvipm.cfg");
                } else {
                    TIBCO.Rendezvous.Environment.Open();
                }
            } catch (RendezvousException exception) {
                Console.Error.WriteLine("Failed to open Rendezvous Environment: {0}" , exception.Message);
                Console.Error.WriteLine(exception.StackTrace);
                Console.Error.WriteLine("Press any key to exit.");
                Console.ReadKey();
                System.Environment.Exit(1);
            }

            // Create Network transport            
            try {
                transport = new NetTransport(service , network , daemon);
            } catch (RendezvousException exception) {
                Console.Error.WriteLine("Failed to create NetTransport:");
                Console.Error.WriteLine(exception.StackTrace);
                Console.Error.WriteLine("Press any key to exit.");
                Console.ReadKey();
                System.Environment.Exit(1);
            }


        }

        public void Send(Message message , string topic) {
            // Create the message
            //Message message = new Message();

            // Set send subject into the message
            try {
                message.SendSubject = topic;
            } catch (RendezvousException exception) {
                Console.Error.WriteLine("Failed to set send subject:");
                Console.Error.WriteLine(exception.StackTrace);
                Console.Error.WriteLine("Press any key to exit.");
                Console.ReadKey();
                System.Environment.Exit(1);
            }

            try {
                //message.AddField("DATA" , Msg , 0);
                transport.Send(message);
            } catch (RendezvousException exception) {
                Console.Error.WriteLine("Error sending a message:");
                Console.Error.WriteLine(exception.StackTrace);
                Console.Error.WriteLine("Press any key to exit.");
                Console.ReadKey();
                System.Environment.Exit(1);
            }

        }
    }
}
