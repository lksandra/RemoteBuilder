///////////////////////////////////////////////////////////////////////////////
///  Program.cs - module demonstrates mockTestHarness functionality as part of
///               a federation of servers                                   ///
///  ver 1.0                                                                ///
///  Language:     C#                                                       ///
///  Platform:     windows10. toshiba satellite                             ///
///  Application:  demonstrating project4 of SMA                            ///
///  Author:       Lakshmi kanth sandra, 229653990   
///  Reference:    Jim Fawcett, Ammar Salman                                ///
///                                                                         ///
///////////////////////////////////////////////////////////////////////////////
//Module Operations: 
//==================
//this module dynamically loads the dlls built by the builder and executes them.
//logs the results/warnings of the tests and sends the logs to mockReposotory: 
//@"../../../MockRepository/ServiceFileStore"
////////////////////////////////////////////////////////////////////////////////
//Required file:
//==============
//1. ExtensionMethods.extensions.cs
//2. IMPCommService.cs
//3. MPCommService.cs
//4. BlockingQueue.cs
//5. xml.cs
////////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
// public class mockTestHarness{}:
//-------------------------------
//this class is the main class which dynamically loads the dlls built by the
//builder and executes them.
//-----------------------------------------------------------------------------
//public mockTestHarness(string testRequestXml):
//----------------------------------------------
//this constructor initialises the comm channel for child testHarness and sends
//readMessage to the motherTestHarness. motherTestHarness in turn sends the 
//xml testRequest if aavailable to it. Upon receival the childTstHarness requests
//dll files from the childBuilder and then exexutes the tests. logs are sent to 
//the repo.
//
//-----------------------------------------------------------------------------

///////////////////////////////////////////////////////////////////////////////
//maintenance history: V1.0, 12/06/17.
//====================================
//NIL
//
///////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using ExtensionMethods;
using System.Threading;
using System.Diagnostics;
using MessagePassingComm;
using System.Xml.Linq;
using XMLHandler;

namespace TestBuild
{
   

    public class mockTestHarness
    {
        private int _motherTestHarnessPortAddress;
        public int motherTestHarnessPortAddress { set { _motherTestHarnessPortAddress = value; } }
        private int _childTestHarnessPortAddress;
        private int _mockRepoPortAddress = 9007;
        private int mockRepoPortAddress { get { return _mockRepoPortAddress; } set { _mockRepoPortAddress = value; } }
        private string _fullMockRepoAddress = "";
        private string fullMockrepoAddress { get { if (_fullMockRepoAddress != "") return _fullMockRepoAddress; return _fullMockRepoAddress= machineName + ":" + _mockRepoPortAddress + "/" + "MessagePassingComm.Receiver"; } }
        private string machineName = "http://localhost";
        private static CommMessage _readyMessage = null;
        private CommMessage readyMessage
        {
            get
            {
                if (_readyMessage != null)
                {
                    return _readyMessage;
                }
                else
                {
                    this.createReadyMessage();
                    return _readyMessage;
                }

            }

        }

        public Comm wcfMediator = null;
        private string _childTestHarnessStoragePath = @"mockTestHarnessStorage";
        private string _author;
        private string _testDirectory;
        private string _fullPathToTestDirectory;
        private string _fileLoggerPath;
        private StringWriter _LogBuilder;
        public string Log { get { return _LogBuilder.ToString(); } }
        DirectoryInfo dir = new DirectoryInfo("mockTestHarnessStorage");
        private Dictionary<string, Action<CommMessage>> _listOfProcessingFunctions = null;

        //this function hashmaps functions to the _listOfProcessingFunctions.
        private void addFunctionsToTheList(string command, Action<CommMessage> function)
        {
            _listOfProcessingFunctions.Add(command, function);
        }


        //functions for handling requests from mothertestHarness and childBuilder.
        private void addFunctionHelper()
        {
            addFunctionsToTheList("testRequest", processTestRequestMsg);
            addFunctionsToTheList("sendTestLogsToRepo", sendLogsToRepo);
            addFunctionsToTheList("sentDllFiles", executeTests);
        }

        //this function creates a readyMessage template to be used to send to motherTestHarness for conveying readyness of the testHarness.
        private void createReadyMessage()
        {
            _readyMessage = new CommMessage(CommMessage.MessageType.request);
            _readyMessage.command = "readyMessage";
            _readyMessage.from = machineName + ":" + _childTestHarnessPortAddress + "/" + "MessagePassingComm.Receiver";
            _readyMessage.to = machineName + ":" + _motherTestHarnessPortAddress + "/" + "MessagePassingComm.Receiver";

        }

        //function that evokes appropriate method.
        private void processMsg(CommMessage receivedMsg)
        {
             
            if (receivedMsg.command != null)
            {
                Console.WriteLine("command received is : {0} and recieved from: {1}", receivedMsg.command, receivedMsg.from);
                _listOfProcessingFunctions[receivedMsg.command].Invoke(receivedMsg);
            }
            else
            {
                Console.WriteLine("empty command in the message received from: {0}", receivedMsg.from);
                Console.WriteLine("receivedMsg.type is: {0}", receivedMsg.type);
            }

        }


        //this method dequeues from the blockingqueue and hansover to the appropriate processing fucntion. Upon close request the function kills the server.
        private void dequeueMsgFromRcvQ()
        {
            while (true)
            {

                Console.WriteLine("\n deququeMsgFromRcvQ entrered L316");
                var temp = wcfMediator.getMessage();
                Console.WriteLine("\n wcf.getmsg returned from dequeueMsgFromRcvQ L318");
                if (temp.type != CommMessage.MessageType.closeReceiver)
                {
                    Console.WriteLine("\n command received in dequeueMsgFromRcvq L321 is: {0}", temp.command);
                    processMsg(temp);
                    Console.WriteLine("\n returned after processMsg in dequeueMsgFromRcvQ, L323");
                }
                else
                {
                    Console.WriteLine("\n REQUIREMENT 7: command received from mother testHarness to: {0} ", temp.command);
                    Process.GetCurrentProcess().Kill();

                    break;
                }


            }
        }


        //this fucntion sets the ground for executing tests. it requests the concenred childBuilder to send dll files.
        private void processTestRequestMsg(CommMessage msg)
        {
            string xmlTestReqString = msg.arguments[0];
            Console.WriteLine("\n REQUIREMENT 8: xmlTestReqString received from motherTesHarness is: {0}", xmlTestReqString);
            XMLParser xmlParser = new XMLParser(xmlTestReqString);
            xmlParser.parseXml(1);
            _author = xmlParser.getMetaDataInRequestXML["author"].ElementAt(0);
            _testDirectory = _author + xmlParser.getTestNamesinXMlRequest["testNameDll"].ElementAt(0).getFolderName();
            _fullPathToTestDirectory = _childTestHarnessStoragePath + "/" + _author + "/" + _testDirectory;
            _fileLoggerPath = _fullPathToTestDirectory + "\\" + _testDirectory + "TestLogger.txt";
            Console.WriteLine("\n DLLs need to be received from the mockRepo into the folder: {0}",_fullPathToTestDirectory);
            Directory.CreateDirectory(_childTestHarnessStoragePath + "/" + _author);
            Directory.CreateDirectory(_fullPathToTestDirectory);
            ServiceEnvironment.fileStorage = _fullPathToTestDirectory;
            Console.WriteLine("\n childTestHarness requests dlls from the child Builder with address: {0}", msg.from);
            sendDllFileRequestMsgToChildBuilder(msg);
            Console.WriteLine("\n childHarness sucessfully posted message to the child builder with address: {0}", msg.from);
          
        }

        //function to send request message to childBuilder for sending dll files.
        private void sendDllFileRequestMsgToChildBuilder(CommMessage msgToChildBuilder)
        {
            try
            {
                dir.Refresh();
                string addressOfChildBuilder = msgToChildBuilder.from;
                string addressOfchildTestHarness = msgToChildBuilder.to;
                msgToChildBuilder.from = addressOfchildTestHarness;
                msgToChildBuilder.to = addressOfChildBuilder;
                msgToChildBuilder.command = "sendDLLFiles";
                msgToChildBuilder.infoAboutContent = _testDirectory;
                Console.WriteLine("msgToChildBuilder.infoAboutContent:{0} ", msgToChildBuilder.infoAboutContent);
                Console.WriteLine("\n REQUIREMENT 8: DLL files request message being sent to childBuilder with address: {0}", addressOfChildBuilder);
                msgToChildBuilder.author = _author;
                wcfMediator.postMessage(msgToChildBuilder);
            }
            catch(Exception ex)
            {
                Console.WriteLine("\n error occured while sedning DLLrequest msg to childbuilder: {0}", ex.Message);
            }
        }


        //sends build logs to mockRepository.
        private void sendLogsToRepo(CommMessage msgFromChildBuilder)
        {
            string logFileName = Path.GetFileName(_fileLoggerPath);
            ClientEnvironment.fileStorage = _fullPathToTestDirectory;

            Console.WriteLine("\n REQUIREMENT 9:  sending log file of project: --{0}-- to mockRepository with address: {1} ", _testDirectory, fullMockrepoAddress);
            Console.WriteLine("\n full path of the log file being sent is: {0}", Path.GetFullPath(Path.Combine(ClientEnvironment.fileStorage, logFileName)));
            wcfMediator.postFile(logFileName, fullMockrepoAddress);
            Console.WriteLine("\n post File returned");

        }

        //aggregator method which executes tests, sends logs to repo and readyMsg to motherTestHarness.
        private void executeTests(CommMessage msgFromChildBuilder)
        {
            executeTestDrivers();
            _listOfProcessingFunctions["sendTestLogsToRepo"].Invoke(msgFromChildBuilder);
            Console.WriteLine("\n readymsg about to be sent to mother testHarness as test execution is completed");
            wcfMediator.postMessage(readyMessage);
            Console.WriteLine("\n REQUIREMENT 6: readyMsg sent to the mother testHarness");

        }

        //instanitates the class variables and communication channel for this server.
        public mockTestHarness(int motherTestHarnessPortAddress, int childTestHarnessPortAddress)
        {


            this.motherTestHarnessPortAddress = motherTestHarnessPortAddress;
            this._childTestHarnessPortAddress = childTestHarnessPortAddress;
            this._LogBuilder = new StringWriter();
            Console.WriteLine("\n REQUIREMENT 2: initialising comm object with childTestHarness portaddress: {0}", childTestHarnessPortAddress);
            wcfMediator = new Comm(machineName, _childTestHarnessPortAddress);
            _listOfProcessingFunctions = new Dictionary<string, Action<CommMessage>>();
            addFunctionHelper();
            Console.WriteLine("\n REQUIREMENT 6: readyMessage being sent to motherTestHarness");
            Console.WriteLine("readyMessage.from:{0}", readyMessage.from);
            Console.WriteLine("readyMessage.to: {0}", readyMessage.to);
            wcfMediator.postMessage(readyMessage);
            Console.WriteLine("wcfmediator.postmessage returned posting readyMsg");
            dequeueMsgFromRcvQ();

        }

        //main method that executes tests and logs results/wanrnings;
        private void executeTestDrivers()
        {
            bool flag = false;
            bool result = false;
            string[] Libraries = Directory.GetFiles(_fullPathToTestDirectory, "*.dll");
            if (Libraries.Count() <= 0)
            {
                Console.WriteLine("test Failed as there are no DLL files found at {0}", _fullPathToTestDirectory);
                File.AppendAllText(_fileLoggerPath, "test Failed as there are no DLL files found at path: /n" + _fullPathToTestDirectory);
            }
            else
            {
                foreach (var library in Libraries)
                {
                    TextWriter _old = Console.Out;
                    _LogBuilder.Flush();
                    _LogBuilder = new StringWriter();
                    Console.SetOut(_LogBuilder);
                    Console.WriteLine("\n \n REQUIREMENT 9: Executing library:-\n {0}", _fullPathToTestDirectory + "/" + Path.GetFileName(library));
                    var assem = Assembly.LoadFrom(library);
                    Type[] types = assem.GetExportedTypes();
                    foreach (Type t in types)
                    {
                        Type neededType = t.GetInterface("ITest", true);
                        Object zz = null;
                        if (neededType != null)
                        {
                            Console.WriteLine("Type with ITest interface found");
                            zz = assem.CreateInstance(t.ToString());
                            MethodInfo method = t.GetMethod("test");
                            if (method != null)
                            {
                                Console.WriteLine("methodName test found and being invoked");
                                result = (bool)method.Invoke(zz, new object[0]);
                                flag = true;
                            }
                        }
                        else { continue; }
                    }
                    if (flag){if (result){Console.WriteLine("\n test passed for the library: {0}", Path.GetFileName(library));}else{Console.WriteLine("\n test Failed for the library: {0}", Path.GetFileName(library));}   }
                    else{Console.WriteLine("\n error occured while testing {0}", _fullPathToTestDirectory + "/" + library); Console.WriteLine("\n no method with name test been provided as per protocol in the library: {0} ", _fullPathToTestDirectory + "/" + library); Console.WriteLine("============================================================================================");}
                    Console.SetOut(_old);
                    Console.WriteLine("\n contents of Log for project:- {0} {1}", _fullPathToTestDirectory, _LogBuilder.ToString());
                    File.AppendAllText(_fileLoggerPath, _LogBuilder.ToString());
                }
            }
        }
    }



    public class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("int.Parse(args[0]): int.Parse(args[1]):", int.Parse(args[0]), int.Parse(args[1]));
            mockTestHarness m = new mockTestHarness(int.Parse(args[0]), int.Parse(args[1])); ;
           
            Console.ReadLine();
        }
    }
}
