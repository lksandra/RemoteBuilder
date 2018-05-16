///////////////////////////////////////////////////////////////////////////////
///  motherTestHarness.cs -This package demonstrates the functionality of mother//
///                     testHarness.acts as a mediator bewteen the childbuldrs 
///                     and childTestharnesses                              ///
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
//This module helps in executing the tests by handing over the testRequests 
//to available child testHarness. It is responsible for spawning the child
//tstHarnesses based on the inut from the cient. It also enables the shutdown 
//of child TestHarness servers upon command from the client. 
///////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1. IMPCommService.cs
//2. MPCommService.cs
//3. BlockingQueue.cs
//4. motherTestHarness.cs
//////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
// public motherTestHarness(int motherPort = 9019):
//-------------------------------------------------
//this constructor initialises the communication channel and other class variables.
//it takes the port number for motherTestHarness as input. 9009 is default.
///////////////////////////////////////////////////////////////////////////////
////maintenance history: V1.0, 06/12/17.
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

namespace MotherTestHarness
{
    class motherTestHarness
    {
        private int _numberOfTestHarnesses = 2;
        private int numberOfTestHarnesses { set { _numberOfTestHarnesses = value; ; } }
        private HashSet<string> _addressesOfTestHarnesses;
        private int _portOfMotherTestHarness = 9019;
        private string _IPAddress = "http://localhost";
        private int portOfMotherBuilder { set { _portOfMotherTestHarness = value; } }
        private BlockingQueue<CommMessage> readyMessageQ;
        private BlockingQueue<CommMessage> xmlRequestQ;
        private MessagePassingComm.Comm wcfMediator = null;
        private CommMessage quitMessageTochildTestHarness;
        private Dictionary<string, Action<CommMessage>> listOfProcessingFunctions;

        //hashmaps functions.
        private void helperFunctionToAddToListOfFunctions(string command, Action<CommMessage> action)
        {
            listOfProcessingFunctions.Add(command, action);
        }

        //adds functions that can handle requests from the clinet, childBUilder and child TestHarness.
        private void addFunctionsToTheList()
        {
            helperFunctionToAddToListOfFunctions("testRequest", handleTestRequestFromChildBuilder);
            helperFunctionToAddToListOfFunctions("readyMessage", handleReadyMessagefromChildTestHarness);
            helperFunctionToAddToListOfFunctions("numberOfProcesses", handleSpawningOfTestHarnesses);
            helperFunctionToAddToListOfFunctions("quit", handleQuitMessage);
        }

        //spawns the child Testharnesses upon request from the client.
        private void handleSpawningOfTestHarnesses(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 5: start childTestHarnesses request message received from the client with address: {0}", tempCommMessage.from);
            int.TryParse(tempCommMessage.arguments.ElementAt(0), out _numberOfTestHarnesses);
            spawnProcesses();
        }

        //receives the test request xml string msg from the childBuilder and enqueues it.
        private void handleTestRequestFromChildBuilder(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 3: XML TestRequest msg rcvd from mockRepo:\n {0}", tempCommMessage.from);
            Console.WriteLine("content of the testRequestmsg from childbuilder: \n {0}", tempCommMessage.arguments[0]);
            xmlRequestQ.enQ(tempCommMessage);
            processMsg();
        }

        //receives the ready messages from the child testHarnesses and enqueues them.
        private void handleReadyMessagefromChildTestHarness(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 6: readyMessage received from childTestHarness: {0}", tempCommMessage.from);
            _addressesOfTestHarnesses.Add(tempCommMessage.from);
            readyMessageQ.enQ(tempCommMessage);
            Console.WriteLine(tempCommMessage.from);
            processMsg();
        }

        //quit message handler which sends quit messages to all availabel child Testharnesses and also self kills.
        private void handleQuitMessage(CommMessage tempCommMessage)
        {
            Console.WriteLine("\n REQUIREMENT 7: quit command recieved from client with address: {0}", tempCommMessage.from);
            //this is to enable the GUI to command mother testHarness to shutdown its processes.
            foreach (string childTestHarness in _addressesOfTestHarnesses)
            {
                Console.WriteLine("\n REQUIREMENT 7: quit command sending to child TestHarness");
                quitMessageTochildTestHarness.to = childTestHarness;
                quitMessageTochildTestHarness.command = "quit";
                wcfMediator.postMessage(quitMessageTochildTestHarness);
                Console.WriteLine("quit message sent to child testHarness with address: {0}", childTestHarness);
                Thread.Sleep(250);
            }
            Console.WriteLine("mother testHarness quits itself after asking the child testHarnesses to close. Press Enter to exit");
            Process.GetCurrentProcess().Kill();
        }

        //hands over the xml testrequest string to child TestHarness whenever its available.
        private void processMsg()
        {
            if (readyMessageQ.size() >= 1 && xmlRequestQ.size() >= 1)
            {
                Console.WriteLine("processMsg enetered after both queues are >= 1");
                String childTestHarnessAddressInfo = readyMessageQ.deQ().from;
                CommMessage changingMsg_To_field = xmlRequestQ.deQ();
                lock (this)
                {
                    changingMsg_To_field.to = childTestHarnessAddressInfo;
                    changingMsg_To_field.command = "testRequest";
                    Console.WriteLine("REQUIREMENT 3: xml test request is sent to child builder with address: {0}", changingMsg_To_field.to);
                    Console.WriteLine("message command is: {0}", changingMsg_To_field.command);
                    Console.WriteLine("xmlString being sent as argument is: {0}", changingMsg_To_field.arguments[0].ToString());
                    wcfMediator.postMessage(changingMsg_To_field);
                }
            }
        }

        //initialises the commchannel and other variables.
        public motherTestHarness(int motherPort = 9019)
        {
            Console.WriteLine("\n REQUIREMENT 2: initialising motherTestHArness comm object with port number: {0}", motherPort);
            _portOfMotherTestHarness = motherPort;
            wcfMediator = new Comm(_IPAddress, (_portOfMotherTestHarness));
            _addressesOfTestHarnesses = new HashSet<string>();
            readyMessageQ = new SWTools.BlockingQueue<CommMessage>();
            xmlRequestQ = new BlockingQueue<CommMessage>();
            quitMessageTochildTestHarness = new CommMessage(CommMessage.MessageType.closeReceiver);
            listOfProcessingFunctions = new Dictionary<string, Action<CommMessage>>();
            addFunctionsToTheList();
            receiveMessageFromWCF();
        }

        //method that spawns childTestHarnesses.
        public void spawnProcesses()
        {
            for (int i = 1; i <= _numberOfTestHarnesses; i++)
            {

                Process proc = new Process();
                string fullPath = Path.GetFullPath(@"../../../mockTestHarness/bin/Debug/mockTestHarness.exe");
                Console.WriteLine("childTestHarnessPortAddress is: {0}", (_portOfMotherTestHarness + i).ToString());
                ProcessStartInfo pstartinfo = new ProcessStartInfo(fullPath, (_portOfMotherTestHarness).ToString() + " " + (_portOfMotherTestHarness + i).ToString());
                pstartinfo.UseShellExecute = true;
                pstartinfo.CreateNoWindow = false;
                pstartinfo.WorkingDirectory = @"../../../mockTestHarness";

                Process.Start(pstartinfo);


            }

        }


        //main thread that makes a blocking call on receive queue.
        public void receiveMessageFromWCF()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    var tempCommMessage = wcfMediator.getMessage();
                    Console.WriteLine("\n command to mother tesHarness is: {0}", tempCommMessage.command);
                    if (tempCommMessage.command != null)
                    {
                        if (listOfProcessingFunctions.ContainsKey(tempCommMessage.command))
                        {
                            listOfProcessingFunctions[tempCommMessage.command].Invoke(tempCommMessage);
                        }
                        else
                        {
                            Console.WriteLine("messagetype received by the mother tesHarness is: {3} and \n " +
                            "the tempCommMessage.command is: {0} and " +
                            "tempCPmmMessage.infoAboutcontent is: {1} " +
                            "and tempCommMessage.type is: {2}", tempCommMessage.command,
                            tempCommMessage.infoAboutContent, tempCommMessage.type, tempCommMessage.type);
                        }
                    }

                }

            });

        }


        static void Main(string[] args)
        {
            motherTestHarness mTh = new motherTestHarness();
           
        }
    }
}
