///////////////////////////////////////////////////////////////////////////////
///  childBuilder.cs -  this package demonstrates building of csharp files
///                   and coomunicating with mockRepository, Mother Builder,
///                   motherTestHarness and childTestHarness                ///
///  ver 1.0                                                                ///
///  Language:     C#                                                       ///
///  Platform:     windows10. toshiba satellite                             ///
///  Application:  Demonstrating Project 4                                  ///
///                SMA CSE 681.                                             ///
///                                                                         ///
///  Author:       Lakshmi kanth sandra, 229653990                          ///
///  Reference:    Prof Jim Fawcett                                         ///
///////////////////////////////////////////////////////////////////////////////
//Module Operations:
//==================
//This modules provide one class "childBuilder" which has the functionalities for:
//1.communicating with mother builder via WCF to get build request XML.
//2.communicating with mockRepository to get files to build
//3.communicating with mockRepository to send build logs.
//4.communicate with motherTestHarness to send testRequestXMl
//5.communicate with childTestHarness to send dll files.
///////////////////////////////////////////////////////////////////////////////
////Required files:
//===============
//1. IMPCommService.cs
//2. MPCommService.cs
//3. BlockingQueue.cs
//4. xml.cs
//5. childBuider.cs
///////////////////////////////////////////////////////////////////////////////
////public interface: 
//=================
//public class childBuilder:
//
//main class which contains all the fucntionalities described above.
//------------------------------------------------------------------------------
//public childBuilder(int motherBuilderPortAddress, int childBuilderPortAddress):
//
//constructor takes motherbuilder port address and child builder port address and
//initialises the communication setup necessary for the childBuilder.
//-------------------------------------------------------------------------------
//public int motherBuilderPortAddress :
//
//this proerty sets the motherbuilder port address.
//-------------------------------------------------------------------------------
////////////////////////////////////////////////////////////////////////////////////
//maintenance history: V1.0, 29/10/17.
//===================================
//V 1.0 released
///////////////////////////////////////////////////////////////////////////////////




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using MessagePassingComm;
using System.Xml.Linq;
using XMLHandler;
using System.IO;
using fileHandler;


namespace childBuilder
{
    public class childBuilder
    {
        private int _motherBuilderPortAddress;
        public int motherBuilderPortAddress { set { _motherBuilderPortAddress = value; } }
        private int _childBuilderPortAddress;
        private int _motherTestHarnessPortAddress = 9019;
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
        private static CommMessage _testRequestXmlMsg = null;
        private CommMessage testRequestXmlMsg {
            get
            {
                
                
                
                    this.create_testRequestXmlMsg();
                    return _testRequestXmlMsg;
                

            }
        }
        
        private Comm wcfMediator = null;
        private string _childBuilderStoragePath = "serviceFileStore";
        private string _author;
        private string _testDirectory;
        private string _fullPathToTestDirectory;
        private string _fileLoggerPath;
        private string xmlBuildReqString;
        private string _testRequestXMLToBeGenerated;
        private Dictionary<String, HashSet<string>> _testNames = null;
        private Dictionary<string, HashSet<string>> _filesToBeBuilt = null;
        private List<string> commandArguments_ = null;

        private Dictionary<string, Action<CommMessage>> _listOfProcessingFunctions = null;

        //this function adds function the _listOfProcessingFunctions.
        private void addFunctionsToTheList(string command, Action<CommMessage> function)
        {
            _listOfProcessingFunctions.Add(command, function);
        }

        //this function adds the functionalities for processing build request from mother builder ,
        //building files from the mockRepository and sending dll files to the child testHarness.
        private void addFunctionHelper()
        {
            addFunctionsToTheList("buildRequest", processingBuildRequest);
            addFunctionsToTheList("sentBuildFiles", buildingFiles);
            addFunctionsToTheList("sendDLLFiles", sendDllToChildTestHarness);
        }

        //this function initialises the _readyMessage field by creating a readyMessage which will be
        //used by the childBuilder to send to the motherBuilder every time.
        private void createReadyMessage()
        {
            _readyMessage = new CommMessage(CommMessage.MessageType.request);
            _readyMessage.command = "readyMessage";
            _readyMessage.from = machineName + ":" + _childBuilderPortAddress + "/" + "MessagePassingComm.Receiver";
            _readyMessage.to = machineName + ":" + _motherBuilderPortAddress + "/" + "MessagePassingComm.Receiver";

        }

        //sends testRequestXml as a string to the motherTstHarness.
        private void create_testRequestXmlMsg()
        {
            _testRequestXmlMsg = new CommMessage(CommMessage.MessageType.request);
            _testRequestXmlMsg.command = "testRequest";
            _testRequestXmlMsg.from = machineName + ":" + _childBuilderPortAddress + "/" + "MessagePassingComm.Receiver";
            _testRequestXmlMsg.to = machineName + ":" + _motherTestHarnessPortAddress + "/" + "MessagePassingComm.Receiver";
            Console.WriteLine("_testRequestXMLToBeGenerated inside create_testRequestXmlMsg is:{0} ", _testRequestXMLToBeGenerated);
            _testRequestXmlMsg.arguments.Add(XDocument.Load(_testRequestXMLToBeGenerated).ToString());
        }

        //initialises the comm object, sends readyMessage to the motherBuilder and calls the dequeueMsgFromRcvQ
        public childBuilder(int motherBuilderPortAddress, int childBuilderPortAddress)
        {
            this._childBuilderPortAddress = childBuilderPortAddress;
            this.motherBuilderPortAddress = motherBuilderPortAddress;
            Console.WriteLine("\n REQUIREMENT 2: initialising comm object with childbuilder portaddress: {0}", childBuilderPortAddress);
            wcfMediator = new Comm(machineName, childBuilderPortAddress);
            _listOfProcessingFunctions = new Dictionary<string, Action<CommMessage>>();
            addFunctionHelper();
            commandArguments_ = new List<string>();
            Console.WriteLine("\n REQUIREMENT 6: readyMessage being sent to mother builder");
            Console.WriteLine("readyMessage.from:{0}", readyMessage.from);
            Console.WriteLine("readyMessage.to: {0}", readyMessage.to);
            wcfMediator.postMessage(readyMessage);
            Console.WriteLine("wcfmediator.postmessage returned posting readyMsg");
            dequeueMsgFromRcvQ();
        }

        //calls appropriate processing function based on CommMessage.command
        private void processMsg(CommMessage receivedMsg)
        {
            ///childbuilder receives msg from motherbuilder, repo, 
            if (receivedMsg.command != null)
            {
                Console.WriteLine("\n REQUIREMENT 6: command received is : {0} and recieved from: {1}",receivedMsg.command, receivedMsg.from);
                _listOfProcessingFunctions[receivedMsg.command].Invoke(receivedMsg);
            }
            else
            {
                Console.WriteLine("empty command in the message received from: {0}", receivedMsg.from);
                Console.WriteLine("receivedMsg.type is: {0}", receivedMsg.type);
            }

        }

        //processes the build request string obtained from the mother builder. sends files request message
        //to the mockRepository.
        private void processingBuildRequest(CommMessage msg)
        {
            xmlBuildReqString = msg.arguments[0];
            Console.WriteLine("\n REQUIREMENT 3: xmlbuildreqstring received from mother builder is: {0}", xmlBuildReqString);
            XMLParser xmlParser = new XMLParser(xmlBuildReqString);
            xmlParser.parseXml(0);
            _author = xmlParser.getMetaDataInRequestXML["author"].ElementAt(0);
            _testDirectory = _author + xmlParser.getTestNamesinXMlRequest["testName"].ElementAt(0);
            _filesToBeBuilt = xmlParser.getFileNamesInXMLRequest;
            _testNames = xmlParser.getTestNamesinXMlRequest;
            foreach(var eachFile in _filesToBeBuilt)
            { 
                 string testDriver = eachFile.Key;
                Console.WriteLine("\n REQUIREMENT 3 & 7: expected test driver file to be rcvd from mockRepo: {0}", testDriver);
                if (testDriver != "")
                {
                    foreach (var eachTestedFile in eachFile.Value)
                    {
                        Console.WriteLine("\n REQUIREMENT 3 & 7: expected testedFile file to be rcvd from mockRepo: {0}", eachTestedFile);
                    }
                }
            }
            foreach(var eachTestName in _testNames["testName"])
            {
                Console.WriteLine("\n REQUIREMENT 3 & 7: expected testName.DLL to be built: {0}", eachTestName);
            }
            CommMessage msgToRepo = new CommMessage(CommMessage.MessageType.request);
            msgToRepo.to = null;
            msgToRepo.to = msg.from;
            msgToRepo.from = null;
            msgToRepo.from = machineName + ":" + _childBuilderPortAddress + "/MessagePassingComm.Receiver";
            msgToRepo.command = null;
            msgToRepo.command = "sendBuildFiles";
            msgToRepo.author = null;
            msgToRepo.author = _author;
            msgToRepo.infoAboutContent = null;
            msgToRepo.infoAboutContent = _testDirectory;
            _fullPathToTestDirectory = _childBuilderStoragePath + "/" + _author + "/" + _testDirectory;
            _fileLoggerPath = _fullPathToTestDirectory + "\\" + _testDirectory + "BuildLogger.txt";
            _testRequestXMLToBeGenerated = _fullPathToTestDirectory + "/" + _testDirectory + "TestRequest.xml";
            ServiceEnvironment.fileStorage = _fullPathToTestDirectory;
            ClientEnvironment.fileStorage = _fullPathToTestDirectory;
            Directory.CreateDirectory(_childBuilderStoragePath + "/" + _author);
            Directory.CreateDirectory(_fullPathToTestDirectory);
            Console.WriteLine("build files request message with command: {0} sent to repo at:{1} ", msgToRepo.command, msgToRepo.to);
            wcfMediator.postMessage(msgToRepo);
            Console.WriteLine("\n REQUIREMENT 3 & 7: child builder waiting for the repo to send the build files in to folder(ServiceEnvironment.fileStorage): \n {0}", Path.GetFullPath( ServiceEnvironment.fileStorage));
        }


        //builds files and sends log to the mockRepository
        private void buildingFiles(CommMessage rcvdMsgRromRepo)
        {
            try
            {
                Thread.Sleep(1000);
                getCommandArguments("csc");
                buildDlls();
                sendLogsToRepo(rcvdMsgRromRepo);
                sendXmlTestRequestToMotherTestHarness();

            }
            catch (Exception e)
            {
                Console.WriteLine("exception thrown in buildingFiles function. details: {0}", e);
            }

            Console.WriteLine("\n \n REQUIREMENT 8: childBuilder waiting for the childTestHarness to request dll files");


        }

        //gets commandline arguments as per the programming language of the file to be tested.
        private void getCommandArguments(string language)
        {
            //call a function or toolchain class function for scanning and returning file extensions.
            switch (language)
            {
                case "csc":
                    getCommandArgumentsForCSharp();
                    break;
                default:
                    return;
            }

        }

        //provides commandline arguments for building csharp files.
        private void getCommandArgumentsForCSharp()
        {
            string[] arrayOfTestNames = new string[_testNames["testName"].Count];
            _testNames["testName"].CopyTo(arrayOfTestNames);
            int i = 0;
            StringBuilder sb = new StringBuilder();

            foreach (var item in _filesToBeBuilt)
            {
                sb.Append(" /nologo /t:library /out:");
                sb.Append(arrayOfTestNames[i] + ".DLL");
                i += 1;
                string testDriver = item.Key;

                sb.Append((" " + testDriver).ToString());
                foreach (string testedFile in _filesToBeBuilt[testDriver])
                {
                    sb.Append((" " + testedFile).ToString());
                }

                commandArguments_.Add(sb.ToString());
                sb.Clear();
                
            }




        }

        //main builder function which calls the csc compiler and builds
        private void buildDlls()
        {
            ProcessStartInfo cmdProcess = new ProcessStartInfo("csc");
            cmdProcess.WorkingDirectory = _fullPathToTestDirectory;
            Process p = new Process();
            foreach (String each in commandArguments_)
            {
                Console.WriteLine("\n REQUIREMENT 7: files being built are: {0}", each);
                cmdProcess.Arguments = each;
                cmdProcess.RedirectStandardOutput = true;
                cmdProcess.UseShellExecute = false;
                p.StartInfo = cmdProcess;
                p.Start();
                string s = p.StandardOutput.ReadToEnd();
                if (s.Length > 0)
                {
                    File.AppendAllText(_fileLoggerPath, s);
                    Console.WriteLine("\n REQUIREMENT 7:Build failed. check logger at {0} for further details", _fullPathToTestDirectory);

                    return;
                }
                else
                {
                    Console.WriteLine("\n REQUIREMENT 7: files built successfuly and the dlls are stored at: \n {0} \n logging results into logPath: \n {1}", _fullPathToTestDirectory, _fileLoggerPath);
                    File.AppendAllText(_fileLoggerPath, "build success" + Environment.NewLine);
                }
            }
            commandArguments_.Clear();
        }

        //sends build logs to mockRepository.
        private void sendLogsToRepo(CommMessage rcvdMsgRromRepo)
        {
            string logFileName = Path.GetFileName(_fileLoggerPath);
            Console.WriteLine("\n REQUIREMENT 7 & 8: sending log file of project: --{0}-- to mockRepository with address: {1} ", _testDirectory, rcvdMsgRromRepo.from);
            Console.WriteLine("\n full path of the log file being sent is: {0}\n ", Path.GetFullPath(ClientEnvironment.fileStorage));
            wcfMediator.postFile(logFileName, rcvdMsgRromRepo.from);
        }

        //generates testRequest xml.
        private void generateXMLTestRequest()
        {
            Console.WriteLine("\n REQUIREMENT 8: generating a TEST request XML");
            List<List<string>> temp = new List<List<string>>();
            List<string> temp2 = new List<string>();
            temp2.AddRange(_testNames["testName"]);
            temp.Add(temp2);
            string _xmlReceived = _fullPathToTestDirectory + "/" + _testDirectory + "BuildRequest.xml";
            XDocument.Parse(xmlBuildReqString).Save(_xmlReceived);
            
            try
            {
                XMLGenerator xmlGenerator = new XMLGenerator(xmlBuildReqString, "", _testRequestXMLToBeGenerated, null, temp, 1);
            }
            catch (Exception ex)
            {
                File.AppendAllText(_fileLoggerPath.ToString(), ex.Source);
                File.AppendAllText(_fileLoggerPath.ToString(), ex.Message);
                Console.WriteLine("\n failure to generate XML Test request. check logger at {0} for further details", _fullPathToTestDirectory);

            }
        }

        //sends DLL files to childTestHarness upon request from the child TestHarness.
        private void sendDllToChildTestHarness(CommMessage msgRcvdFromChildTestHarness)
        {
            Console.WriteLine("sendDllToChildTestHarness entered" );
            string fullPathToLibraryFiles = _childBuilderStoragePath + "/" +  msgRcvdFromChildTestHarness.author + "/"+ msgRcvdFromChildTestHarness.infoAboutContent;
            fileManager fm = new fileManager();
            HashSet<string> allLibraryFiles = new HashSet<string>();
            allLibraryFiles = fm.getAllFilesFromAGivenPath(Path.GetFullPath(fullPathToLibraryFiles), "*.DLL");
            Console.WriteLine("size of allLibraryFiles present in the project folder: {0}  : {1}",fullPathToLibraryFiles ,allLibraryFiles.Count);
            ClientEnvironment.fileStorage = fullPathToLibraryFiles;
            foreach(var eachLibrary in allLibraryFiles)
            {
                try
                {
                    Console.WriteLine("\n \n REQUIREMENT 8: fileName about to be sent: {0}", eachLibrary);
                    wcfMediator.postFile(Path.GetFileName(eachLibrary), msgRcvdFromChildTestHarness.from);
                    Console.WriteLine("\n PostFile returned after sending, {0}", eachLibrary);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("\n error occured with details:\n {0}", ex.Message);
                    _fileLoggerPath = fullPathToLibraryFiles + "logger.txt";
                    File.AppendAllText(_fileLoggerPath, ex.Message);
                }

            }
            //sending acknowledgement message that dll files have been sent to childTstHarness
            sendConfirmationMsgToChildTestHarness(msgRcvdFromChildTestHarness);
            //sending readymsg to the mother builder after the child builder finishes sending dlls to the child testharness.

            Console.WriteLine("\n childBuilder finished building files, sending test request xml to child testHarness, sending DLL files to ch.test harness");
            Console.WriteLine("\n sending ready message to the motherBuilder");
            wcfMediator.postMessage(readyMessage);

        }

        //sends the generated xmlTestReq to mother TestHarness.
        private void sendXmlTestRequestToMotherTestHarness()
        {
            generateXMLTestRequest();
            try
            {
                wcfMediator.postMessage(testRequestXmlMsg);
                Console.WriteLine("\n REQUIREMENT 8: testRequest xml string SENT to the motherTestHarness", testRequestXmlMsg.arguments[0]);
            }
            catch(Exception ex)
            {
                Console.WriteLine("error occured in sending xml test request message to mother testharness with details: \n {0}", ex.Message);
            }
        }


        //acknowledgement messsage sent to childTestHarness once DLL files have been sent to it.
        private void sendConfirmationMsgToChildTestHarness(CommMessage msgRcvdFromChildTestHarness)
        {
            Console.WriteLine("\n REQUIREMENT 8: DLL files sent acknowledgement gesture for childTestHarness is about to be made");
            CommMessage msgToChTestHarness = new CommMessage(CommMessage.MessageType.request);
            msgToChTestHarness.to = msgRcvdFromChildTestHarness.from;
            msgToChTestHarness.command = "sentDllFiles";
            msgToChTestHarness.from = msgRcvdFromChildTestHarness.to;
            Console.WriteLine("\n msgToChTestHarness with address: {0}   is being sent", msgToChTestHarness.to);
            Console.WriteLine("\n childbuilder's address sending the msgToAddressTestHarness: {0}", msgToChTestHarness.from);
            wcfMediator.postMessage(msgToChTestHarness);
            Console.WriteLine("\n DLL files sent acknowledgement gesture made to childTestHarness ");
        }



        //dequeues message from the recieve queue of the childbuilder and hands it over to processing
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
                    Console.WriteLine("\n REQUIREMENT 7: command received from mother builder to: {0} ", temp.command);
                    Process.GetCurrentProcess().Kill();

                    break;
                }


            }
        }
    }

    class entryPoint
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n REQUIRMENT 5: CHILD BUILDER process spawned");
            childBuilder ch1 = new childBuilder(int.Parse(args[0]), int.Parse(args[1]));
            Console.WriteLine("Press Enter Key To Exit");
            Console.ReadLine();


        }
    }
}
