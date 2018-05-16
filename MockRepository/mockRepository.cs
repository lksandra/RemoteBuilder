///////////////////////////////////////////////////////////////////////////////
///  mockRepository.cs - This package demonstrates the functionality of 
///                      mockRepository as part of federation of servers    ///
///  ver 1.0                                                                ///
///  Language:     C#                                                       ///
///  Platform:     windows10. toshiba satellite                             ///
///  Application:  Demonstrating Project 3 and To be used in project 4 of   ///
///                SMA CSE 681                                              ///
///                                                                         ///
///  Author:       Lakshmi kanth sandra, 229653990                          ///
///  Reference:    Prof Jim Fawcett                                         ///
///////////////////////////////////////////////////////////////////////////////
//Module Operations:
//==================
//this module helps the client to create Build Request and forward it to the
//motherBuilder. It also sends the files, as requested by the childBuilder, 
//to the childBuilder. It receives log files from the childBuilder and 
//childTestHarness. In addition to that it forwards the quit message that was
//sent by client.
///////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1. IMPCommService.cs
//2. MPCommService.cs
//3. BlockingQueue.cs
//4. xml.cs
//5. mockRepository.cs
//6. fileManager.cs
////////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
//public class mockRepository:
//
//this is the main class which handles all the operations of the mockRepository. 
//------------------------------------------------------------------------------
// public mockRepository():
//
//constructor initialises the WCF communication needed by the mockRepository object
//and all the othe fields of the class.
//------------------------------------------------------------------------------
//public List<List<string>> clientsBuildContent_:
//
//this property sets the data that is needed to generate XML build request.
//------------------------------------------------------------------------------
//public void sendXmlBuildRequestStringToMotherBuilder():
//
//this function is evoked when build request xml need to be generated and sent to 
//motherBuilder via wcf. prior to calling this the clientsBuildContent_ needs 
//to be set with data. XML content is sent as string to the mother builder.
//--------------------------------------------------------------------------------
//public void sendXmlBuildRequestStringToMotherBuilder(string XmlFilePath):
//
//this function is evoked in order to send an already existing XML file to the
//mother builder. It takes the path of that XML file. The build request sent
//to the mother builder will be a XML content converted to string.
//--------------------------------------------------------------------------------
//public void sendQuitMessageToMotherBuilder():
//thisfunction sends quit message to the mother builder which makes the motherbuilder
//quit the child builders and also self quit.
//---------------------------------------------------------------------------------
//public void dequeueMsgFromWCF():
//
//this function continuously receives the messages sent to its listener port and hands 
//the message to appropriate handler. listener address of the mockRepository is
//http://localhost:9007/MessagePassingComm.Receiver
//----------------------------------------------------------------------------------
////////////////////////////////////////////////////////////////////////////////////
//maintenance history: V1.0, 29/10/17.
//===================================
//NIL
///////////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using MessagePassingComm;
using XMLHandler;
using System.Xml.Linq;
using System.IO;
using fileHandler;


namespace MockRepository
{
    public class mockRepository
    {
        private int _motherBuilderPortAddress = 9009;
        private Comm wcfMediator = null;
        private int _mockRepoPortAddress = 9007;
        private int _motherTestHarnessPortAddress = 9019;
        private string _machineName = "http://localhost";
        private string _mockRepoFileStorage = "../../mockRepoStorage";
        private List<List<string>> clientsBuildContent;
        public List<List<string>> clientsBuildContent_ { set { clientsBuildContent = value; } }
        private XMLGenerator xmlGenerator = null;
        private string _nameOfBuildRequestToBeCreated;
        private CommMessage recievedMsg = null;
        private Dictionary<string, Action<CommMessage>> listOfProcessingFunctions;

        //initialises the comm object of WCF and other variables. 
        public mockRepository()
        {
            Console.WriteLine("mockRepository initialising comm object with listener port address: {0}", _mockRepoPortAddress);
            wcfMediator = new Comm(_machineName, _mockRepoPortAddress);
            clientsBuildContent = new List<List<string>>();
            _nameOfBuildRequestToBeCreated = null;

            listOfProcessingFunctions = new Dictionary<string, Action<CommMessage>>();
            addFunctionsToTheList();
            Console.WriteLine("service environment of the mockRepository is set to: ../../ServiceFileStore");
            Console.WriteLine("childBuilder sends build log files to the above folder");
            ServiceEnvironment.fileStorage = @"../../ServiceFileStore";
        }

        //helper function to add the delegates to the listOfProcessingFunctions 
        private void helperToAddFunctionsToTheList(string command, Action<CommMessage> action)
        {
            listOfProcessingFunctions.Add(command, action);
        }

        //hashmaps functions for handling requests from client and childBuilder
        private void addFunctionsToTheList()
        {
            helperToAddFunctionsToTheList("sendBuildFiles", sendFilesToChildBuilder);
            helperToAddFunctionsToTheList("buildRequest", handleXmlGenerationRequestFromClient);
            helperToAddFunctionsToTheList("quit", handleQuitMessageFromClient);
            helperToAddFunctionsToTheList("FileList", handleFileListRequestFromClient);
            helperToAddFunctionsToTheList("XMLFileList", handleXmlBuildReqListFromClient);
            helperToAddFunctionsToTheList("forwardBuildRequest", handleXmlForwardRequestFromClient);
        }





        //this function first creates a directory with the name author and then a subdirectory
        //with the name author+firstTestName. then it generates the XML build request by aggregating XMLGenerator.
        //it returns the relative path of the XML build request generated.
        private string generateXMLBuildRequest()
        {
            Console.WriteLine("author is: {0}", clientsBuildContent[0][0]);
            string author = clientsBuildContent[0][0];
            string authorDirectory = _mockRepoFileStorage + "/" + author;
            string testNameDirectory = authorDirectory + "/" + author + clientsBuildContent[1][0];
            Console.WriteLine("Client selected testdriver and teste files are being copied from \n ../../mockRepoStorage to" +
                "\n {0}", testNameDirectory);
            Console.WriteLine("files to the child builders will be sent upon request from the above path");
            getNameOfBuildRequestToBeCreated();

            if (!Directory.Exists(_mockRepoFileStorage + "/" + author))
            {

                Directory.CreateDirectory(authorDirectory);
                Directory.CreateDirectory(testNameDirectory);
                string fullNameOfBuildRequestToBeCreated = testNameDirectory + "/" + _nameOfBuildRequestToBeCreated.ToString();
                xmlGenerator = new XMLGenerator("", fullNameOfBuildRequestToBeCreated, "", clientsBuildContent, null, 0);
                copyFilesFromMockRepoStorageToAuthorTestNameFolder(testNameDirectory);
                return fullNameOfBuildRequestToBeCreated;
            }
            else if (!Directory.Exists(testNameDirectory))
            {
                Directory.CreateDirectory(testNameDirectory);
                string fullNameOfBuildRequestToBeCreated = testNameDirectory + "/" + _nameOfBuildRequestToBeCreated.ToString();
                xmlGenerator = new XMLGenerator("", fullNameOfBuildRequestToBeCreated, "", clientsBuildContent, null, 0);
                copyFilesFromMockRepoStorageToAuthorTestNameFolder(testNameDirectory);
                return fullNameOfBuildRequestToBeCreated;
            }
            else
            {
                string fullNameOfBuildRequestToBeCreated = testNameDirectory + "/" + _nameOfBuildRequestToBeCreated.ToString();
                xmlGenerator = new XMLGenerator("", fullNameOfBuildRequestToBeCreated, "", clientsBuildContent, null, 0);
                copyFilesFromMockRepoStorageToAuthorTestNameFolder(testNameDirectory);
                return fullNameOfBuildRequestToBeCreated;
            }
        }

        //this helper function copies required files from the mockrepoStorage to the newly created folder as per client's XML structure.
        private void copyFilesFromMockRepoStorageToAuthorTestNameFolder(string testNameDirectory)
        {
            int numberOfTestsCount = clientsBuildContent.Count();
            if (numberOfTestsCount > 1)
            {
                fileManager fm = new fileManager();
                for (int i = 1; i < numberOfTestsCount; i++)
                {
                    int numberOfCsharpFiles = clientsBuildContent[i].Count();
                    for (int j = 1; j < numberOfCsharpFiles; j++)
                    {
                        var fileName = clientsBuildContent[i][j];
                        var fullFileName = Path.Combine(_mockRepoFileStorage, fileName);
                        fm.sendFileFromSourceToDestination(fullFileName, testNameDirectory);
                    }
                }
            }
        }


        //generates internally the XML build request and then sends it to the mother builder as a string.
        public void sendXmlBuildRequestStringToMotherBuilder()
        {
            Console.WriteLine("\n Requirement 3: XML build request generation process started");
            var xmlPath = generateXMLBuildRequest();
            XDocument xml = XDocument.Load(generateXMLBuildRequest());
            List<string> xmlStringContent = new List<string>();
            Console.WriteLine("\n REQUIREMENT 3: XML string generatd is", xml.ToString());
            xmlStringContent.Add(xml.ToString());
            CommMessage xmlCommMsg = new CommMessage(CommMessage.MessageType.connect);
            xmlCommMsg.command = "buildRequest";
            xmlCommMsg.to = "http://localhost:" + _motherBuilderPortAddress + "/MessagePassingComm.Receiver";
            xmlCommMsg.arguments = xmlStringContent;
            xmlCommMsg.from = _machineName + ":" + _mockRepoPortAddress + "/MessagePassingComm.Receiver";

            wcfMediator.postMessage(xmlCommMsg);
            Console.WriteLine("\n REQUIREMENT 3: XML build request string sent to the mother builder. source xml file:{0} ", xmlPath);
        }

        //this converts the existing XML  build request file in to string and dispatches it to the mother builder
        //via WCF
        public void sendXmlBuildRequestStringToMotherBuilder(string XmlFilePath)
        {
            XDocument xml = XDocument.Load(XmlFilePath);
            List<string> xmlStringContent = new List<string>();

            xmlStringContent.Add(xml.ToString());
            CommMessage xmlCommMsg = new CommMessage(CommMessage.MessageType.reply);
            xmlCommMsg.command = "buildRequest";
            xmlCommMsg.to = _machineName + ":" + _motherBuilderPortAddress + "/MessagePassingComm.Receiver";
            xmlCommMsg.arguments = xmlStringContent;
            xmlCommMsg.from = _machineName + ":" + _mockRepoPortAddress + "/MessagePassingComm.Receiver";
            wcfMediator.postMessage(xmlCommMsg);
            Console.WriteLine("\n REQUIREMENT 13: XML build request string sent to the mother builder. source xml file:{0} ", XmlFilePath);

        }

        //this function generates the XML build request based on the build request structures sent by the client and persists locally.
        private void handleXmlGenerationRequestFromClient(CommMessage message)
        {
            clientsBuildContent = message.xmlBuildContent;
            Console.WriteLine("\n REQUIREMENT 4: build request content receieved from the client.");
            string tempAuthor = message.author;
            string fullFilePathOfGeneratedXml = generateXMLBuildRequest();
            Console.WriteLine("REQUIREMENT 4: xml generated and its full path is {0}", fullFilePathOfGeneratedXml);
            persistXmlBuildRequestToAuthorFolder(fullFilePathOfGeneratedXml, tempAuthor);
        }

        //this persists the xml build request that was created on client's demand.
        private void persistXmlBuildRequestToAuthorFolder(string generatedXmlFilePath, string authorName)
        {
            Console.WriteLine("REQUIREMENT 12: demonstrating persistence of xmlBuild request");
            if (File.Exists(generatedXmlFilePath))
            {
                Console.WriteLine("author name is: {0}", authorName);
                File.Copy(generatedXmlFilePath, Path.GetFullPath(_mockRepoFileStorage + "/" + authorName + "/" + Path.GetFileName(generatedXmlFilePath)), true);
                Console.WriteLine("file Path of persisted XML build request is:\n {0} ", Path.GetFullPath(_mockRepoFileStorage + "/" + authorName + "/" + Path.GetFileName(generatedXmlFilePath)));
            }
            else
            {
                Console.WriteLine("error occured while copying xml file to: ", _mockRepoFileStorage + "/" + authorName);
            }
        }

        //this gets the list of xml build requst files present in the requested author directory and sends the list of file names to client.
        private void handleXmlBuildReqListFromClient(CommMessage msgFromClient)
        {
            Console.WriteLine("REQUIREMENT 13: demonstrating that repo sends the below list of xml build request strings to client");
            string filePath = _mockRepoFileStorage + "/" + msgFromClient.author;
            fileManager fm = new fileManager();
            try
            {
                HashSet<string> xmlBuildReqFiles = fm.getAllFilesFromAGivenPath(Path.GetFullPath(filePath), "*.xml");
                msgFromClient.arguments.Clear();
                foreach (var eachFilePath in xmlBuildReqFiles)
                {
                    if (!msgFromClient.arguments.Contains(Path.GetFileName(eachFilePath)))
                    {
                        msgFromClient.arguments.Add(Path.GetFileName(eachFilePath));
                        Console.WriteLine("fileName added as argument to msg to client: {0}", Path.GetFileName(eachFilePath));

                    }
                }
                string clientAddress = msgFromClient.from;
                msgFromClient.from = msgFromClient.to;
                msgFromClient.to = clientAddress;
                Console.WriteLine("posting list of xml request file names to client: {0}", msgFromClient.to);
                wcfMediator.postMessage(msgFromClient);
                Console.WriteLine("posting sucessfull");

            }
            catch (Exception ex)
            {
                Console.WriteLine("error occured while sending xml file names to client. Details: \n {0}", ex.Message);
            }
        }

        //forwards the persisted build request to mother builder upon request from the client.
        private void handleXmlForwardRequestFromClient(CommMessage msgFromClient)
        {
            Console.WriteLine("REQUIREMENT 13: client requested xml file: -- {0} -- to be forwarded to buildserver", msgFromClient.arguments[0]);
            if (msgFromClient.arguments.Count > 0)
            {
                string fullPathToXmlBuildReqInRepo = Path.GetFullPath( _mockRepoFileStorage + "/" + msgFromClient.author + "/" + msgFromClient.arguments[0]);
                sendXmlBuildRequestStringToMotherBuilder(fullPathToXmlBuildReqInRepo);
            }
            else
            {
                Console.WriteLine("error in forwarding the build request to motherbuilder. No argument from the client");
            }


        }

        //upon request by the child builder the requested files are sent by the mockRepository to the respective
        //child builder.
        private void sendFilesToChildBuilder(CommMessage message)
        {
            Console.WriteLine("sendFilesToChildBuilder entered with message.command: {0}", message.command);
            string pathTo_author_firstTestName_directory = _mockRepoFileStorage + "/" + message.author + "/" + message.infoAboutContent;
            var tempFileStorage = Directory.GetFiles(pathTo_author_firstTestName_directory, "*.cs");
            List<string> fileNames = new List<string>();
            foreach (var each in tempFileStorage)
            {
                fileNames.Add(Path.GetFileName(each));
            }

            ClientEnvironment.fileStorage = pathTo_author_firstTestName_directory;
            Console.WriteLine("\n REQUIREMENT 3: repo sends files to childBuilder from folder: {0}  ", pathTo_author_firstTestName_directory);
            foreach (var each in fileNames)
            {
                Console.WriteLine("fileName about to be sent: {0}", each);
                wcfMediator.postFile(each, message.from);
                Console.WriteLine("PostFile returned after sending, {0}", each);
            }
            CommMessage msgToChildBuilder = new CommMessage(CommMessage.MessageType.reply);
            msgToChildBuilder.command = "sentBuildFiles";
            msgToChildBuilder.from = _machineName + ":" + _mockRepoPortAddress + "/MessagePassingComm.Receiver";
            msgToChildBuilder.to = message.from;
            Console.WriteLine("\n sending acknowledgement message with command -\"sentBuildFile\"-. address of childBuilder is: \n {0}", msgToChildBuilder.to);
            wcfMediator.postMessage(msgToChildBuilder);

        }

        //sends quit message to the mother builder
        public void sendQuitMessageToMotherBuilder()
        {
            Console.WriteLine("\n REQUIREMENT 7: repository requesting mother builer to close receiever and quit.");
            CommMessage quitmessagetomother = new CommMessage(CommMessage.MessageType.closeReceiver);
            quitmessagetomother.command = "quit";
            quitmessagetomother.to = "http://localhost:" + _motherBuilderPortAddress + "/MessagePassingComm.Receiver";
            quitmessagetomother.show();
            wcfMediator.postMessage(quitmessagetomother);
        }

        //sends quit message to self.
        private void sendQuitMessageToSelf()
        {
            CommMessage selfQuitMsg = new CommMessage(CommMessage.MessageType.closeReceiver);
            selfQuitMsg.to = _machineName + ":" + _mockRepoPortAddress + "/MessagePassingComm.Receiver";
            Console.WriteLine("\n REQUIREMENT 7: sending message to self so as to close the receiever\n");
            wcfMediator.postMessage(selfQuitMsg);
            CommMessage selfSenderQuitmsg = new CommMessage(CommMessage.MessageType.closeSender);
            selfSenderQuitmsg.to = _machineName + ":" + _mockRepoPortAddress + "/MessagePassingComm.Receiver";
            Console.WriteLine("\n REQUIREMENT 7: sending message to self so as to close the sender");
            wcfMediator.postMessage(selfSenderQuitmsg);
            Process.GetCurrentProcess().Kill();
        }

        //sends quit message to motherTestHarness.
        private void sendQUitMsgToMotherTestHarness()
        {
            Console.WriteLine("\n REQUIREMENT 7: repository requesting mother testHarness to close receiever and quit.");
            CommMessage quitmessagetomother = new CommMessage(CommMessage.MessageType.closeReceiver);
            quitmessagetomother.command = "quit";
            quitmessagetomother.to = "http://localhost:" + _motherTestHarnessPortAddress + "/MessagePassingComm.Receiver";
            quitmessagetomother.show();
            wcfMediator.postMessage(quitmessagetomother);
        }

        //sends quit message to the mother builder upon request from the client. Then it self quits.
        private void handleQuitMessageFromClient(CommMessage message)
        {
            sendQuitMessageToMotherBuilder();
            sendQUitMsgToMotherTestHarness();
            Thread.Sleep(2000);
            Process.GetCurrentProcess().Kill();
        }


        //this function reads all the top level fileNames from the requested directory and serialises to the client via wcf. 
        //"../../mockRepoStorage" is the base address.
        private void handleFileListRequestFromClient(CommMessage msgFromClient)
        {
            try
            {
                Console.WriteLine("REQUIREMENT 11: demonstrating remote file browsing by the client and enaabled by the repository ");
                string filePath = msgFromClient.infoAboutContent;
                Console.WriteLine("client requested files from the path: {0}", filePath);
                filePath = _mockRepoFileStorage + @"/" + filePath;
                Console.WriteLine("combined path is: {0}", filePath);
                var dirs = Directory.GetDirectories(filePath);
                List<string> listOfDirs = new List<string>();
                foreach (string eachdir in dirs)
                {
                    listOfDirs.Add(Path.GetFileName(eachdir));
                    Console.WriteLine("directory name added to argument is: {0}", Path.GetFileName(eachdir));
                }
                var files = Directory.GetFiles(filePath);
                List<string> listOfFiles = new List<string>();
                foreach (var eachFile in files)
                {
                    listOfFiles.Add(Path.GetFileName(eachFile));
                    Console.WriteLine("file name added to argument is: {0}", Path.GetFileName(eachFile));
                }
                msgFromClient.to = msgFromClient.from;
                msgFromClient.xmlBuildContent = new List<List<string>>();
                msgFromClient.xmlBuildContent.Add(listOfFiles);
                msgFromClient.xmlBuildContent.Add(listOfDirs);
                Console.WriteLine("sending file and directories list to client: {0} ", msgFromClient.to);
                wcfMediator.postMessage(msgFromClient);

            }
            catch (Exception ex)
            {

            }
        }

        //this fucntion dequeues the messages obtained via comm object.
        public void dequeueMsgFromWCF()
        {
            while (true)
            {
                recievedMsg = wcfMediator.getMessage();

                processMsg(recievedMsg);
            }
        }

        //this function invokes the required function for procssing.
        private void processMsg(CommMessage message)
        {

            if (message.command != null)
            {
                Console.WriteLine("\n mockrepo received the command: {0}-- from: {0}-- ", message.command, message.from);
                listOfProcessingFunctions[message.command].Invoke(message);
            }


        }

        //this function generates the name that is to be used for generating XMLBuildRequest.
        private void getNameOfBuildRequestToBeCreated()
        {
            string author = clientsBuildContent[0][0];

            _nameOfBuildRequestToBeCreated = author + clientsBuildContent[1][0] + @"BuildRequest.xml";
        }




    }
    class entrypoint
    {
        static void Main(string[] args)
        {
            mockRepository mockRep = new mockRepository();
            
            mockRep.dequeueMsgFromWCF();
            Console.ReadLine();

        }
    }
}
