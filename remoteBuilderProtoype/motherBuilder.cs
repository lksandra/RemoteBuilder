///////////////////////////////////////////////////////////////////////////////
///  motherBuilder.cs - This package demonstrates the functionality of mother//
///                     builder. communicates to MockRepository, ChildBuilder//
///                     and client via WCF.                                 ///
///  ver 1.0                                                                ///
///  Language:     C#                                                       ///
///  Platform:     windows10. toshiba satellite                             ///
///  Application:  Demonstrating Project 4                                  ///
///                SMA CSE 681                                              ///
///                                                                         ///
///  Author:       Lakshmi kanth sandra, 229653990                          ///
///  Reference:    Prof Jim Fawcett                                         ///
///////////////////////////////////////////////////////////////////////////////
//Module Operations:
//==================
//this module helps in building of the csharp files by handing over the build 
//requests, obtained from mockRepository, to the available child builders. 
//It spawns child builders based on the number required by the client. Mother
//builder also quits the child processes upon request from the client.
///////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1. IMPCommService.cs
//2. MPCommService.cs
//3. BlockingQueue.cs
//4. motherBuilder.cs
//////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
//public class motherBuilder{}:
//
//this is the main class of the motherBuilder.cs which provides all the 
//operations.
//---------------------------------------------------------------------------
//public motherBuilder(int motherPort):
//
//constructor takes an int parameter which is the port address for the mother-
//builder. Default value of the port is set to 9009. the constructor
//sets up the WCF communication for motherBuilder. Listener address of the 
//motherBuilder will be of the form http://localhost:motherPort/MessagePassingComm.Receiver
//---------------------------------------------------------------------------
//public void receiveMessageFromWCF():
//
//this function, when evoked, will be able to receive a message from other
//packages and hands its over to the necessary processing functions.
//----------------------------------------------------------------------------
////maintenance history: V1.0, 29/10/17.
//===================================
//V1.0 released
///////////////////////////////////////////////////////////////////////////////////



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;
using SWTools;
using MessagePassingComm;


namespace remoteBuilderProtoype
{
   public class motherBuilder
    {
        private int _numberOfChildBuilders = 2;
        public int numberOfChildBuilders { set { _numberOfChildBuilders = value; ; } }
        private HashSet<string> _addressesOfChildBuilders;
        private int _portOfMotherBuilder = 9009;
        private string _IPAddress = "http://localhost";
        public int portOfMotherBuilder { set { _portOfMotherBuilder = value; } }
        private BlockingQueue<CommMessage> readyMessageQ ;
        private BlockingQueue<CommMessage> xmlRequestQ ;
        private MessagePassingComm.Comm wcfMediator = null;
        private CommMessage quitMessageTochildBuilder;
        private Dictionary<string, Action<CommMessage>> listOfProcessingFunctions;

        //this function enables adding functions to the field: listOfProcessingFunctions
        private void helperFunctionToAddToListOfFunctions(string command, Action<CommMessage> action)
        {
            listOfProcessingFunctions.Add(command, action);
        }
        
        //this function cals the helperFunctionToAddToListOfFunctions to add necessary functions to the list.
        private void addFunctionsToTheList()
        {
            helperFunctionToAddToListOfFunctions("buildRequest", handleBuildRequestFromRepo);
            helperFunctionToAddToListOfFunctions("readyMessage", handleReadyMessagefromChildBuilder);
            helperFunctionToAddToListOfFunctions("numberOfProcesses", handleSpawningOfChildBuilders);
            helperFunctionToAddToListOfFunctions("quit", handleQuitMessage);
        }

        //this function enques XMLstring, into the xmlRequestQ, obtained from the mockRepository and calls for further processing 
        private void handleBuildRequestFromRepo(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 3: XML buildRequest msg rcvd from mockRepo: {0}", tempCommMessage.from);
            xmlRequestQ.enQ(tempCommMessage);
            processMsg();
        }

        //this functions enqueus ReadyMessage in to the readyMessageQ and calls for further processing
        private void handleReadyMessagefromChildBuilder(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 6: readyMessage received from childBuilder: {0}", tempCommMessage.from);
            _addressesOfChildBuilders.Add(tempCommMessage.from);
            readyMessageQ.enQ(tempCommMessage);
            Console.WriteLine(tempCommMessage.from);
            processMsg();
        }

        //this function spawns the child builders as demkanded by the client via WCF
        private void handleSpawningOfChildBuilders(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 5: start childbuilders request message received from the client with address: {0}", tempCommMessage.from);
            int.TryParse(tempCommMessage.arguments.ElementAt(0), out _numberOfChildBuilders);
            spawnProcesses();
        }

        //this function requests the childBuilders to quit and then quits the mother builder.
        private void handleQuitMessage(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 7: quit command recieved from client with address: {0}", tempCommMessage.from);
            //this is to enable the GUI to command mother builder to shutdown its processes.
            foreach (string childBuilder in _addressesOfChildBuilders)
            {
                Console.WriteLine("\n REQUIREMENT 7: quit command sending to child builder");
                quitMessageTochildBuilder.to = childBuilder;
                quitMessageTochildBuilder.command = "quit";
                wcfMediator.postMessage(quitMessageTochildBuilder);
                Console.WriteLine("quit message sent to child builder");
                Thread.Sleep(250);
            }
            Console.WriteLine("mother builder quits itself after asking the child builders to close. Press Enter to exit");
            Process.GetCurrentProcess().Kill();
        }




        //this function makes a blocking call on the receive blocking queue and hands over the messages to the necessary processing.
        public void receiveMessageFromWCF()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var tempCommMessage = wcfMediator.getMessage();
                    Console.WriteLine("command to mother builder is: {0}", tempCommMessage.command);
                    if (tempCommMessage.command != null)
                    {
                        if (listOfProcessingFunctions.ContainsKey(tempCommMessage.command))
                        {
                            listOfProcessingFunctions[tempCommMessage.command].Invoke(tempCommMessage);
                        }
                        else
                        {
                            Console.WriteLine("messagetype received by the mother builder is: {3} and \n " +
                            "the tempCommMessage.command is: {0} and " +
                            "tempCPmmMessage.infoAboutcontent is: {1} " +
                            "and tempCommMessage.type is: {2}", tempCommMessage.command, 
                            tempCommMessage.infoAboutContent, tempCommMessage.type, tempCommMessage.type);
                        }
                    }
                    
                }

            });
            
        }

        //this function forwards the XML request string obtianed from the mockRepository to the available childbuider
        private void processMsg()
        {
                if (readyMessageQ.size() >= 1 && xmlRequestQ.size() >= 1)
                {
                Console.WriteLine("\n processMsg enetered after both queues are >= 1");
                    String childBuilderAddressInfo = readyMessageQ.deQ().from;  
                    CommMessage changingMsg_To_field = xmlRequestQ.deQ();
                    lock (this)
                    {
                        changingMsg_To_field.to = childBuilderAddressInfo;
                    changingMsg_To_field.command = "buildRequest";
                    Console.WriteLine("REQUIREMENT 3: xml request is forwarded to available child builder with address: {0}", changingMsg_To_field.to);
                    Console.WriteLine("message command is: {0}", changingMsg_To_field.command);
                    Console.WriteLine("xmlString being sent as argument is: {0}", changingMsg_To_field.arguments[0].ToString());
                        wcfMediator.postMessage(changingMsg_To_field);
                    }
                }
        }

        //constructor initialises all the fields in the class.
        public motherBuilder(int motherPort = 9009)
        {
            Console.WriteLine("\n REQUIREMENT 2: initialising comm object");
            _portOfMotherBuilder = motherPort;
            wcfMediator = new Comm(_IPAddress, (_portOfMotherBuilder));
            _addressesOfChildBuilders = new HashSet<string>();
            readyMessageQ = new SWTools.BlockingQueue<CommMessage>();
            xmlRequestQ = new BlockingQueue<CommMessage>();
            quitMessageTochildBuilder = new CommMessage(CommMessage.MessageType.closeReceiver);
            listOfProcessingFunctions = new Dictionary<string, Action<CommMessage>>();
            addFunctionsToTheList();
        }


        //this core function spwans child builders.
        public void spawnProcesses()
        {
            for(int i = 1; i<= _numberOfChildBuilders; i++)
            {
                
                Process proc = new Process();
                string fullPath = Path.GetFullPath(@"../../../childBuilder/bin/Debug/childBuilder.exe");

                ProcessStartInfo pstartinfo = new ProcessStartInfo(fullPath, (_portOfMotherBuilder).ToString() + " " + (_portOfMotherBuilder + i).ToString());
                pstartinfo.UseShellExecute = true;
                pstartinfo.CreateNoWindow = false;
                pstartinfo.WorkingDirectory = @"../../../childBuilder";
                
                Process.Start(pstartinfo);

                
            }
            
        }


    }
        class entryPoint
        { 

            static void Main(string[] args)
            {

            motherBuilder m1 = new motherBuilder(9009);
            m1.receiveMessageFromWCF();
           
            Console.ReadLine();
            

            }
    }
}
