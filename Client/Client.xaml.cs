///////////////////////////////////////////////////////////////////////////////
///  Client.xaml.cs - this module demonstrates client operations in a 
///                   federation of servers                                 ///
///  ver 2.0                                                                ///
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
//this module is the GUI prototype for the entire project 3. It has the ability
//to startup mothr builder, mockRepository and specifc number of child Builders.
//It can enable the user to generate a XML build request by selecting required
//files. It can also quit the mockRepository, mother builder and childbuilders.
///////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1. IMPCommService.cs
//2. MPCommService.cs
//3. BlockingQueue.cs
//4. CLient.xaml.cs
//5. App.xaml.cs
//6. TestUtilities.cs
///////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
//class ClientGUI:
//
//This is the only class in this module and it contains all the fucntionalities
//as described above.
///////////////////////////////////////////////////////////////////////////////
//maintenance history: V1.0, 29/10/17.
//===================================
//V 2.0. Added remote file browsing and building files functionality
//V 1.0 1st release.
///////////////////////////////////////////////////////////////////////////////////



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ServiceModel;
using MessagePassingComm;
using Microsoft.Win32;
using System.IO;
using System.Threading;
using System.Diagnostics;



namespace Client
{

    public partial class ClientGUI : Window
    {
        private Comm wcfMediator;
        private string _machineName = "http://localhost";
        private int _clientPortAddress = 9006;
        private int _motherBuilderPortAddress = 9009;
        private int _mockRepositoryPortAddress = 9007;
        private int _motherTestHarnessPortAddress = 9019;
        private string _clientFullAddress;
        private List<List<string>> _clientsBuildContent;
        string initDirectory = @"../../../MockRepository/mockRepoStorage";
        private StringBuilder _authorName;
        private string _testNameProvided;
        private string _testDriverForEachTest;
        private List<string> _testedFilesForEachTest = new List<string>();
        private Dictionary<string, Action<CommMessage>> listOfProcessingFunctions;
        private CommMessage recievedMsg = null;
        private CommMessage _fileListMsg = null;
        private CommMessage fileListMsg
        {
            get
            {
                if (_fileListMsg != null)
                {
                    return _fileListMsg;
                }
                else
                {
                    this.createFileListMsgToRepo();
                    return _fileListMsg;
                }
            }
        }
        private CommMessage _xmlReqMsgToRepo = null;
        private CommMessage xmlReqMsgToRepo
        {
            get
            {
                
                this.createXMLfileListMsgToRepo();
                return _xmlReqMsgToRepo;
            }
        }
        private Stack<string> prevDirectory;
        private Stack<string> prevDirectory_TestedFiles;
        private List<string> listOfFilesFromRepo;
        private List<string> iistOfDirsFromRepo;
        private bool quitMsgSent = false;




        //this initialises the comm object and also calls a method for demonstrating to TAs
        public ClientGUI()
        {
            Console.WriteLine("REQUIREMENT 10: initiating coomchanel for client with portaddress: {0}", _clientPortAddress);
            wcfMediator = new Comm(_machineName, _clientPortAddress);
            _clientFullAddress = _machineName + ":" + _clientPortAddress + "/MessagePassingComm.Receiver";
            _clientsBuildContent = new List<List<string>>();
            InitializeComponent();
            _authorName = new StringBuilder();
            listOfProcessingFunctions = new Dictionary<string, Action<CommMessage>>();
            listOfFilesFromRepo = new List<string>();
            iistOfDirsFromRepo = new List<string>();
            prevDirectory = new Stack<string>();
            prevDirectory_TestedFiles = new Stack<string>();
            addFunctionsToTheList();
            dequeueMsgFromWCF();
            automatedTestForDemonstration();
            Console.WriteLine("last line of client constructor");


        }

        //fileList message template to request list of files from the repo.
        private void createFileListMsgToRepo()
        {
            Console.WriteLine("createFileListMsgToRepo entered");
            _fileListMsg = new CommMessage(CommMessage.MessageType.request);
            _fileListMsg.from = _clientFullAddress;
            _fileListMsg.to = _machineName + ":" + _mockRepositoryPortAddress + "/MessagePassingComm.Receiver";
            _fileListMsg.command = "FileList";
            _fileListMsg.infoAboutContent = "";
            Console.WriteLine("createFileListMsgToRepo done");



        }

        //XMLfileList message template to request xml build request from repo that was created on client's request and persisted in the repo.
        private void createXMLfileListMsgToRepo()
        {
            Console.WriteLine("creating template for xmlFileRequest msg to repo");
            _xmlReqMsgToRepo = new CommMessage(CommMessage.MessageType.request);
            _xmlReqMsgToRepo.from = _clientFullAddress;
            _xmlReqMsgToRepo.to = _machineName + ":" + _mockRepositoryPortAddress + "/MessagePassingComm.Receiver";
            _xmlReqMsgToRepo.command = "XMLFileList";
            _xmlReqMsgToRepo.author = _authorName.ToString();
            Console.WriteLine("createXMLfileListMsgToRepo done");

        }

        //helper function to add the delegates to the listOfProcessingFunctions 
        private void helperToAddFunctionsToTheList(string command, Action<CommMessage> action)
        {
            listOfProcessingFunctions.Add(command, action);
        }

        //adds functions for handling requests from mockRepo
        private void addFunctionsToTheList()
        {
            helperToAddFunctionsToTheList("FileList", displayFileAndDirectoriesFromRepo);
            helperToAddFunctionsToTheList("XMLFileList", populateXmlFileListBoxFromRepo);
        }


        //this fucntion dequeues the messages obtained via comm object.
        public void dequeueMsgFromWCF()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    recievedMsg = wcfMediator.getMessage();

                    processMsg(recievedMsg);
                }
            });



        }



        //this function invokes the required function for procssing.
        private void processMsg(CommMessage message)
        {

            if (message.command != null)
            {
                Console.WriteLine("\n command received by the client: {0}", message.command);
                listOfProcessingFunctions[message.command].Invoke(message);
            }


        }


        //this function invokes the helper function so as to populate the listbox with xml build request files obtained from repo.

        private void populateXmlFileListBoxFromRepo(CommMessage msgFromRepo)
        {
            Console.WriteLine("populateXmlFileListBoxFromRepo entered");
            Dispatcher.Invoke(new Action<CommMessage>(populateXMlFileListBoxHelper),
                System.Windows.Threading.DispatcherPriority.Background, new CommMessage[] { msgFromRepo });
        }


        //this function populates the listbox with xml build request files obtained from repo.
        private void populateXMlFileListBoxHelper(CommMessage msg)
        {
            Console.WriteLine("populateXMlFileListBoxHelper entered");
            browseBuildReqXML.Items.Clear();
            if (msg.arguments.Count > 0)
            {
                foreach (var eachXml in msg.arguments)
                {
                    browseBuildReqXML.Items.Add(eachXml);
                }
            }
            else
            {
                Console.WriteLine("no xml files were found in the author:{0} directory", msg.author);
                MessageBox.Show("no xml files were found");
            }
        }

        //this function displays files and directories in the listbox. they are obtained from the repo.
        private void displayFileAndDirectoriesFromRepo(CommMessage msgFromRepo)
        {
            Console.WriteLine("displayFileAndDirectoriesFromRepo entered");

            Dispatcher.Invoke(new Action<CommMessage>(displayFilesHelper),
                System.Windows.Threading.DispatcherPriority.Background, new CommMessage[] { msgFromRepo });
        }

        //helper function to display files and directories in the listbox.
        private void displayFilesHelper(CommMessage msg)
        {
            Console.WriteLine("displayFilesHelper entered");
            testDriverFileList.Items.Clear();
            testDriverDirList.Items.Clear();
            testedFilesFileList.Items.Clear();
            testedFilesDirList.Items.Clear();

            if (msg.xmlBuildContent.Count >= 1)
            {
                if (msg.xmlBuildContent[0].Count >= 1)
                {
                    foreach (var eachFile in msg.xmlBuildContent[0])
                    {
                        Console.WriteLine("eachfileName sent by the repo is: {0}", eachFile);
                        testedFilesFileList.Items.Add(eachFile);
                        testDriverFileList.Items.Add(eachFile);
                    }
                }
                else
                {
                    Console.WriteLine("no files in this directory");
                }

                if (msg.xmlBuildContent[1].Count >= 1)
                {
                    foreach (var eachDir in msg.xmlBuildContent[1])
                    {
                        Console.WriteLine("eachDirName sent by the repo is: {0}", eachDir);

                        testedFilesDirList.Items.Add(eachDir);
                        testDriverDirList.Items.Add(eachDir);
                    }
                }
                else
                {
                    Console.WriteLine("no directories in this directory");
                }


            }

        }

        //this event handler is triggered when START button is clicked. starts up the motherBuilder,
        //mockRepository and specified number of childBuilders.
        private void numberOfChildBuilders(object sender, RoutedEventArgs e)
        {
            var numberEntered = (numberOfProcesses.Text);
            int y;
            bool isInteger = int.TryParse(numberEntered, out y);
            if (!isInteger)
            {
                MessageBox.Show("enter single digit integer only");
            }
            else
            {
                Console.WriteLine("\n RREQUIREMENT 5: demonstrating starting up process pool on command. \n {0} child Builders and child Test harnesses get spawned", y);
                startUpMotherBuilder();
                startUpMockRepository();
                startUpMotherTestHarness();
                Thread.Sleep(2000);
                startUpChildBuilder(numberEntered);
                startUpChildTestHarnesses(numberEntered);
                numberOfProcesses.Visibility = Visibility.Hidden;
                Start.Visibility = Visibility.Hidden;
                authorName.Visibility = Visibility.Visible;
                quit.Visibility = Visibility.Visible;
            }
        }

        //startingup motherBuilder.
        private void startUpMotherBuilder()
        {
            Process proc = new Process();
            string fullPath = System.IO.Path.GetFullPath(@"../../../remoteBuilderProtoype/bin/Debug/remoteBuilderProtoype.exe");

            ProcessStartInfo pstartinfo = new ProcessStartInfo(fullPath);
            pstartinfo.UseShellExecute = true;
            pstartinfo.CreateNoWindow = false;
            pstartinfo.WorkingDirectory = @"../../../remoteBuilderProtoype/bin/Debug";

            Process.Start(pstartinfo);
        }

        //startingUp mockRepository
        private void startUpMockRepository()
        {
            Process proc = new Process();
            string fullPath = System.IO.Path.GetFullPath(@"../../../MockRepository/bin/Debug/MockRepository.exe");

            ProcessStartInfo pstartinfo = new ProcessStartInfo(fullPath);
            pstartinfo.UseShellExecute = true;
            pstartinfo.CreateNoWindow = false;
            pstartinfo.WorkingDirectory = @"../../../MockRepository/bin/Debug";

            Process.Start(pstartinfo);
        }

        //startingup childBuilder by sending a message to the motherBuilder via comm object.
        private void startUpChildBuilder(string numberEntered)
        {
            CommMessage msgToMotherBuilder = new CommMessage(CommMessage.MessageType.request);
            msgToMotherBuilder.to = _machineName + ":" + _motherBuilderPortAddress + "/MessagePassingComm.Receiver";
            msgToMotherBuilder.from = _clientFullAddress;
            msgToMotherBuilder.command = "numberOfProcesses";
            msgToMotherBuilder.arguments = new List<string>();
            msgToMotherBuilder.arguments.Add(numberEntered);
            try
            {
                wcfMediator.postMessage(msgToMotherBuilder);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex.Message);
            }
        }

        //startup motherTestHarness
        private void startUpMotherTestHarness()
        {
            Process proc = new Process();
            string fullPath = System.IO.Path.GetFullPath(@"../../../MotherTestHarness/bin/Debug/MotherTestHarness.exe");

            ProcessStartInfo pstartinfo = new ProcessStartInfo(fullPath);
            pstartinfo.UseShellExecute = true;
            pstartinfo.CreateNoWindow = false;
            pstartinfo.WorkingDirectory = @"../../../MotherTestHarness/bin/Debug";

            Process.Start(pstartinfo);
        }

        //startup childTestHarness by sending a message to the mother TestHarness via comm object. number of childTestHarnesses spawned will be same as childbuilders.
        private void startUpChildTestHarnesses(string numberEntered)
        {
            CommMessage msgToMotherTestHarness = new CommMessage(CommMessage.MessageType.request);
            msgToMotherTestHarness.to = _machineName + ":" + _motherTestHarnessPortAddress + "/MessagePassingComm.Receiver";
            msgToMotherTestHarness.from = _clientFullAddress;
            msgToMotherTestHarness.command = "numberOfProcesses";
            msgToMotherTestHarness.arguments = new List<string>();
            msgToMotherTestHarness.arguments.Add(numberEntered);
            try
            {
                wcfMediator.postMessage(msgToMotherTestHarness);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}", ex.Message);
            }
        }

        //eventHandler for the button TestDriver. 
        private void testDriverSelected(object sender, RoutedEventArgs e)
        {
            //S: added in january 18
            _clientsBuildContent.Clear();
            var authorNameProvided = getAuthorName();
            if (authorNameProvided == false)
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }
            else if (testName.Text == "")
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }
            else
            {
                
                _testNameProvided = testName.Text;
                string selectedFileName;
                if (testDriverFileList.Items.Count > 0)
                {
                    if ((string)testDriverFileList.SelectedValue != "")
                    {
                        SelectedFiles.Text = "Test Driver:\n\t" + (string)testDriverFileList.SelectedValue + "\n";
                        selectedFileName = (string)testDriverFileList.SelectedValue;
                        Console.WriteLine("test driver selected: {0}", (string)testDriverFileList.SelectedValue);
                        _testDriverForEachTest = selectedFileName;
                    }
                }
            }

        }

        //eventHandler to capture the authorName entered.
        private void authorName_TextChanged(object sender, TextChangedEventArgs e)
        {
            _authorName.Clear();
            _authorName.Append(authorName.Text);
            Console.WriteLine("author name is: {0}", _authorName.ToString());

        }

        //eventhandler for TestedFile button. enables browsing and selecting multiple files.
        private void TestedFile_Click(object sender, RoutedEventArgs e)
        {
            if (authorName.Text == null)
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }
            else if(_testNameProvided=="")
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }else if (_testDriverForEachTest == null)
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }
            else
            {
                var testedFilesSelectedItems = testedFilesFileList.SelectedItems;
                Console.WriteLine("count of selected testedfiles: {0}", testedFilesSelectedItems.Count);
                foreach (Object x in testedFilesSelectedItems)
                {
                    Console.WriteLine("each testedfile selected:{0} ", (string)x);
                }

                if (testedFilesSelectedItems.Count > 0)
                {
                    StringBuilder filesToDisplay = new StringBuilder("\n TestedFiles: \n\t");
                    foreach (string each in testedFilesSelectedItems)
                    {

                        filesToDisplay.Append(each);
                        Console.WriteLine("testedFile added to _testedFilesForEachTest is {0}", each);
                        _testedFilesForEachTest.Add(each);
                        filesToDisplay.Append("\n");
                    }
                    SelectedFiles.AppendText(filesToDisplay.ToString());
                }
            }
        }




        //eventHandler for createXML. it sends the data to the mockRepository using WCF.
        //mockRepository generates the XML build reaquest.
        private void createXML_Click(object sender, RoutedEventArgs e)
        {
            CommMessage xmlStructureMsgToRepo = new CommMessage(CommMessage.MessageType.request);
            xmlStructureMsgToRepo.command = "buildRequest";
            xmlStructureMsgToRepo.to = _machineName + ":" + _mockRepositoryPortAddress + "/MessagePassingComm.Receiver";
            xmlStructureMsgToRepo.from = _clientFullAddress;
            getAuthorName();
            xmlStructureMsgToRepo.author = _authorName.ToString();
            xmlStructureMsgToRepo.xmlBuildContent = _clientsBuildContent;
            foreach (var eachTest in _clientsBuildContent)
            {
                foreach (var each in eachTest)
                {
                    Console.WriteLine("each element in _clientsBuildContent is:{0} ", each);
                }
            }
            wcfMediator.postMessage(xmlStructureMsgToRepo);
          //  Thread.Sleep(1300);
          //  _clientsBuildContent.Clear();
        }

        //getter function for authorName
        private bool getAuthorName()
        {
            if (authorName.Text != null)
            {
                if (_clientsBuildContent.Count() <= 0)
                {
                    List<string> auth = new List<string>();
                    auth.Add(_authorName.ToString());
                    _clientsBuildContent.Add(auth);
                    return true;
                }
                return true;

            }
            else
            {
                return false;
            }
        }



        //eventHandler for the Test. it adds the test structure to the field _clientsBuildContent
        private void Test_Click(object sender, RoutedEventArgs e)
        {
            if (authorName.Text == null)
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }
            else if (_testNameProvided == null)
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }
            else if (_testDriverForEachTest == null)
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;
            }else if(_testedFilesForEachTest.Count <=0)
            {
                MessageBox.Show("1. Enter Author Name \n2. then enter TestName  \n3. then select TestDriver[can double click in the directories box to fetch file list from repo] and click TestDriver \n4. Then select(multi) TestedFile and click TestedFile \n5. then click Add Test \n6. then click create XML \n7. then click Build. \n8. then double click xml file from list.");
                return;

            }
            else
            {
                _testedFilesForEachTest.Insert(0, _testNameProvided);
                _testedFilesForEachTest.Insert(1, _testDriverForEachTest);
                List<string> newList = new List<string>();
                foreach (var eachString in _testedFilesForEachTest)
                {
                    StringBuilder stringBuilder1 = new StringBuilder(eachString);
                    string string2 = stringBuilder1.ToString();
                    newList.Add(string2);
                }
                _clientsBuildContent.Add(newList);
                _testedFilesForEachTest = new List<string>();
                SelectedFiles.Text = null;
                _testDriverForEachTest = null; ;
                testName.Text = "";
                _testNameProvided = "";
            }

        }

        //eventHandler for QUITBUILDER. sends message tp mockrepository. eventually closes everyone except ClientGUI
        private void quit_Click(object sender, RoutedEventArgs e)
        {
            CommMessage quitMessageToAll = new CommMessage(CommMessage.MessageType.request);
            quitMessageToAll.command = "quit";
            quitMessageToAll.to = _machineName + ":" + _mockRepositoryPortAddress + "/MessagePassingComm.Receiver";
            quitMessageToAll.from = _clientFullAddress;
            wcfMediator.postMessage(quitMessageToAll);
            Thread.Sleep(2000);
            wcfMediator.closeCreatedChannel();
            numberOfProcesses.Visibility = Visibility.Visible;
            Start.Visibility = Visibility.Visible;
            quit.Visibility = Visibility.Hidden;
            quitMsgSent = true;
        }


        //this fucntion is to demonstrate to TAs the functionalities of the project.
        public void automatedTestForDemonstration()
        {

            List<string> author = new List<string>();
            author.Add("raju");
            _authorName.Clear();
            _authorName.Append("raju");
            List<string> content = new List<string>();
            content.Add("firstTest");
            content.Add("TestDriver.cs");
            content.Add("TestedOne.cs");
            content.Add("TestedTwo.cs");
            _clientsBuildContent.Add(author);
            _clientsBuildContent.Add(content);
            List<string> dataForSecondTest = new List<string>();
            dataForSecondTest.Add("secondTest");
            dataForSecondTest.Add("TestDriver2.cs");
            dataForSecondTest.Add("TestedOne2.cs");
            dataForSecondTest.Add("TestedTwo2.cs");
            _clientsBuildContent.Add(dataForSecondTest);
            MessagePassingComm.TestUtilities.title("REQUIREMENT 11:", '=');
            Object tempObject = new object();
            RoutedEventArgs tempRoutedEventArgs = new RoutedEventArgs();
            numberOfProcesses.Text = "2";
            numberOfChildBuilders(tempObject, tempRoutedEventArgs);
            MessagePassingComm.TestUtilities.title("\n Client sending build request content to mockRepository", '=');
            Console.WriteLine("\n TestDriver.cs, TestedOne.cs, TestedTwo.cs are the files for build request firstTest. " +
                "\n TestDriver2.cs, TestedOne2.cs, TestedTwo2.cs are the files for build request secondTest ");
            Console.WriteLine("they are located in the path, {0}", initDirectory);
            createXML_Click(tempObject, tempRoutedEventArgs);
            automatedTest2();
            

        }

        //this fucntion is to demonstrate to TAs the functionalities of the project.

        private void automatedTest2()
        {
            browseBuildXML_mousedoubleclickHelper("rajufirstTestBuildRequest.xml");
        }




        //event handler for obtaining files from the repo for any selected subdirectory.
        private void testDriverDirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            try
            {
                StringBuilder filePathToBeSentToRepo = new StringBuilder();
                Stack<string> tempStackOfDir = new Stack<string>();
                for (int i = 0; i < prevDirectory.Count; i++)
                {
                    tempStackOfDir.Push(prevDirectory.Pop());
                }
                if (tempStackOfDir.Count > 0)
                {
                    filePathToBeSentToRepo.Append("/");
                }
                for (int j = 0; j < tempStackOfDir.Count; j++)
                {
                    filePathToBeSentToRepo.Append(tempStackOfDir.Peek());
                    filePathToBeSentToRepo.Append("/");
                    prevDirectory.Push(tempStackOfDir.Pop());
                }
                prevDirectory.Push((string)testDriverDirList.SelectedValue);
                Console.WriteLine("value psuhed in to the prevdirectory is: {0}", (string)testDriverDirList.SelectedValue);
                filePathToBeSentToRepo.Append((string)testDriverDirList.SelectedValue);
                fileListMsg.infoAboutContent = filePathToBeSentToRepo.ToString();
                fileListMsg.from = _clientFullAddress;
                Console.WriteLine("posting message to mockRepo to send file list from the path:{0}", fileListMsg.infoAboutContent);

                wcfMediator.postMessage(fileListMsg);
                Console.WriteLine("GUI postmessage returnd after posting message to mockrepo with address: {0}", fileListMsg.to);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error occured while posting message to the mockrepository. testDriverDirList_MouseDoubleClick function. Details: \n {0}", ex.Message);
            }
        }


        //event handler for obtaining files from the repo for any selected subdirectory.
        private void testedFilesDirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                StringBuilder filePathToBeSentToRepo = new StringBuilder();
                Stack<string> tempStackOfDir = new Stack<string>();
                for (int i = 0; i < prevDirectory.Count; i++)
                {
                    tempStackOfDir.Push(prevDirectory.Pop());
                }
                if (tempStackOfDir.Count > 0)
                {
                    filePathToBeSentToRepo.Append("/");
                }
                for (int j = 0; j < tempStackOfDir.Count; j++)
                {
                    filePathToBeSentToRepo.Append(tempStackOfDir.Peek());
                    filePathToBeSentToRepo.Append("/");
                    prevDirectory.Push(tempStackOfDir.Pop());
                }
                prevDirectory.Push((string)testDriverDirList.SelectedValue);
                Console.WriteLine("value psuhed in to the prevdirectory is: {0}", (string)testDriverDirList.SelectedValue);
                filePathToBeSentToRepo.Append((string)testDriverDirList.SelectedValue);
                fileListMsg.infoAboutContent = filePathToBeSentToRepo.ToString();
                fileListMsg.from = _clientFullAddress;
                Console.WriteLine("posting message to mockRepo to send file list from the path:{0}", fileListMsg.infoAboutContent);

                wcfMediator.postMessage(fileListMsg);
                Console.WriteLine("GUI postmessage returnd after posting message to mockrepo with address: {0}", fileListMsg.to);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error occured while posting message to the mockrepository. testDriverDirList_MouseDoubleClick function. Details: \n {0}", ex.Message);
            }
        }

        //event handler for navigating to previoud directory in the repostorage
        private void Prev_Dir_TD_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder filePathToBeSentToRepo = new StringBuilder();
                Stack<string> tempStackOfDir = new Stack<string>();
                //removing the current subdir
                Console.WriteLine("prevDir count is: {0}", prevDirectory.Count);
                if (prevDirectory.Count > 0)
                {
                    prevDirectory.Pop();
                    for (int i = 0; i < prevDirectory.Count; i++)
                    {
                        tempStackOfDir.Push(prevDirectory.Pop());
                    }
                    for (int j = 0; j < tempStackOfDir.Count; j++)
                    {
                        filePathToBeSentToRepo.Append(tempStackOfDir.Peek());
                        filePathToBeSentToRepo.Append("/");
                        prevDirectory.Push(tempStackOfDir.Pop());
                    }
                    Console.WriteLine("Prev_Dir_TD_Click L572");
                    Console.WriteLine("Prev_Dir_TD_Click L574");
                    fileListMsg.from = _clientFullAddress;
                    fileListMsg.infoAboutContent = filePathToBeSentToRepo.ToString();
                    Console.WriteLine("posting message to mockRepo to send file list[prev] from the path:{0}", fileListMsg.infoAboutContent);
                    wcfMediator.postMessage(fileListMsg);
                    Console.WriteLine("GUI postmessage returnd after posting message to mockrepo with address: {0}", fileListMsg.to);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("error occured while posting message to the mockrepository. Prev_Dir_TD_Click function. Details: \n {0}", ex.Message);
            }
        }

        //event handler for navigating to previoud directory in the repostorage

        private void Prev_Dir_TF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StringBuilder filePathToBeSentToRepo = new StringBuilder();
                Stack<string> tempStackOfDir = new Stack<string>();
                Console.WriteLine("prevDir count is: {0}", prevDirectory.Count);
                if (prevDirectory.Count > 0)
                {
                    prevDirectory.Pop();
                    for (int i = 0; i < prevDirectory.Count; i++)
                    {
                        tempStackOfDir.Push(prevDirectory.Pop());
                    }
                    for (int j = 0; j < tempStackOfDir.Count; j++)
                    {
                        filePathToBeSentToRepo.Append(tempStackOfDir.Peek());
                        filePathToBeSentToRepo.Append("/");
                        prevDirectory.Push(tempStackOfDir.Pop());
                    }
                    Console.WriteLine("Prev_Dir_TD_Click L783");
                    Console.WriteLine("Prev_Dir_TD_Click L784");
                    fileListMsg.from = _clientFullAddress;
                    fileListMsg.infoAboutContent = filePathToBeSentToRepo.ToString();
                    Console.WriteLine("posting message to mockRepo to send file list[prev] from the path:{0}", fileListMsg.infoAboutContent);
                    wcfMediator.postMessage(fileListMsg);
                    Console.WriteLine("GUI postmessage returnd after posting message to mockrepo with address: {0}", fileListMsg.to);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("error occured while posting message to the mockrepository. Prev_Dir_TD_Click function. Details: \n {0}", ex.Message);
            }
        }

        //this gets XML build request that was just persisted in the mockRepository.
        private void build_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("REQUIREMENT 13: posting request message to mockrepo to send xml files list of author: {0}", xmlReqMsgToRepo.author);
            wcfMediator.postMessage(xmlReqMsgToRepo);
            Console.WriteLine("successfully posted xmlFileList message request to mockRepo");
        }




        //this essentially means client asking the repo to forward the selected xml build request to builder.
        private void browseBuildReqXML_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {


            browseBuildXML_mousedoubleclickHelper((string)browseBuildReqXML.SelectedValue);

        }

        //helper function for the repository XML file list browsing.
        private void browseBuildXML_mousedoubleclickHelper(string selectedXmlString)
        {
            Console.WriteLine("REQUIREMENT 13: client asks mockRepo to forward xml build request to builder: {0} ", selectedXmlString);

            CommMessage xmlFwdRequest = new CommMessage(CommMessage.MessageType.request);
            xmlFwdRequest.from = _clientFullAddress;
            xmlFwdRequest.to = _machineName + ":" + _mockRepositoryPortAddress + "/MessagePassingComm.Receiver";
            xmlFwdRequest.command = "forwardBuildRequest";
            xmlFwdRequest.author = _authorName.ToString();
            xmlFwdRequest.arguments.Add(selectedXmlString);
            Console.WriteLine("posting forwardBuildRequest command to mockRepo");
            wcfMediator.postMessage(xmlFwdRequest);
            Console.WriteLine("posted successfully");
        }

        //eventHandler for shutting down the GUI window.
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (quitMsgSent)
            {
                Console.WriteLine("all other servers have been sent quit msg before closing this window");
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                Object obj = new object();
                RoutedEventArgs rea = new RoutedEventArgs();
                quit_Click(obj, rea);
                Thread.Sleep(1000);
                Process.GetCurrentProcess().Kill();

            }

        }
    }


}
