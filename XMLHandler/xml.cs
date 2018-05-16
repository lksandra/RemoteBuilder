///////////////////////////////////////////////////////////////////////////////
///  xml.cs -     module that provides methods to manipulate/create XML files//
///  ver 1.0                                                                ///
///  Language:     C#                                                       ///
///  Platform:     windows10. toshiba satellite                             ///
///  Application:  To be used in project 2 of SMA CSE 681                   ///
///                                                                         ///
///  Author:       Lakshmi kanth sandra, 229653990                          ///
///                                                                         ///
///////////////////////////////////////////////////////////////////////////////
//Module Operations:
//==================
//this module provides management operations like creating XML build request, 
//XML test request, and parsing the above said requests. 
////////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1. XMLHandler.xml.cs
////////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
//public class XMLParser{}:
// this class provides operations for parsing the test and build requests
//-----------------------------------------------------------------------------
//public XMLParser(string xmlFile):
//---------------------------------
//this constructor initialises the object by taking a path to the xml file that
//is to be parsed.
//-----------------------------------------------------------------------------
//public void parseXml(int category):
//-----------------------------------
//this function is a warapper parser function which calls the specific functions
//for buld requst(caetgory=0) or for test request(category=1).
//-----------------------------------------------------------------------------
//public Dictionary<String, HashSet<string>> getFileNamesInXMLRequest:
//--------------------------------------------------------------------
//this property is used to obtain the file names present in the XML after parsing
//=-----------------------------------------------------------------------------
//public Dictionary<String, HashSet<string>> getTestNamesinXMlRequest:
//--------------------------------------------------------------------
//this property is used to obtain the test names present in the XML
//-----------------------------------------------------------------------------
//public Dictionary<String, HashSet<string>> getMetaDataInRequestXML:
//-------------------------------------------------------------------
//this peroperty is used to obtain the metadata that is present in the xml.
//author name will be present in this as first element.
//-----------------------------------------------------------------------------
//public class XMLGenerator{}:
//----------------------------
// this class provides mechanism to create a XML, given the content.
//the generic form of the Build request XML is :
//<?xml version="1.0" encoding="utf-8" standalone="yes"?>
//<buildRequest>
//  <author> author name </author>
//  <test>
//    <testName>firstTest</testName>
//    <testDriver>TestDriver.cs</testDriver>
//    <testedFile>TestedOne.cs</testedFile>
//    <testedFile>TestedTwo.cs</testedFile>
//  </test>
//</buildRequest>
//
//The test request XML format is:
//<?xml version="1.0" encoding="utf-8" standalone="yes"?>
//<testRequest>
//  <author>sandra</author>
//  <testNameDll>testName.dll</testNameDll>
//</testRequest>
//-------------------------------------------------------------------------------
//public XMLGenerator(string receievedBuildRequestXml = "", 
//string nameOfXmlBuildRequestToBeCreated = "", 
//string nameOfXmlTestRequestToBeCreated = "", 
//List<List<string>> contentForBuildRequst = null, 
//List<List<string>> contentForTestRequest = null, int category = 3):
//-------------------------------------------------------------------
//this constructor initialises the XMLGenerator object and generates needed XML 
//request. parameters are given default values. However, the user can specifiy 
//them too.names of the requests to be egenrated should be full file names of 
//the XML files. content should be of the form List<List<string>>. default value 
//is null. category=0 for buld request. 1 for test request.
//-------------------------------------------------------------------------------
//public string receivedXmlFilePath:
//----------------------------------
//to set or get the XML file full name. this is the build request file name that is
//used to create the test request XML.
//-------------------------------------------------------------------------------
//public string generatedXmlBuildRequestFile:
//-------------------------------------------
//this gets the created build request file.
//-------------------------------------------------------------------------------
//public string generatedXmlTestRequestFile:
//------------------------------------------
//this gets the generated test request file.
//------------------------------------------------------------------------------
//public List<List<string>> contentForXMLBuildRequest:
//sets/gets the content for the build request generation purpose.
//------------------------------------------------------------------------------
//public List<List<string>> contentForXMLTestRequest:
//---------------------------------------------------
//sets/gets the content for the test request generation purpose.
//////////////////////////////////////////////////////////////////////////////////
//maintenance history: V1.0, 13/9/17.
//===================================
//V 1.0 rleased
//////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using System.IO;

namespace XMLHandler
{

    public class XMLParser
    {
        public Dictionary<String, HashSet<string>> getFileNamesInXMLRequest { get { return FileNamesInXMLRequest_; } }
        public Dictionary<String, HashSet<string>> getTestNamesinXMlRequest { get { return testNames_; } }
        public Dictionary<String, HashSet<string>> getMetaDataInRequestXML { get { return metaDataInRequestXML_; } }
        //   public Dictionary<String, HashSet<string>> testNames { get { return testNames_; } }
        public XMLParser(string xmlFile) { xmlFile_ = xmlFile; }
        private String xmlFile_;
        private Dictionary<String, HashSet<string>> FileNamesInXMLRequest_ = new Dictionary<String, HashSet<string>>();
        private Dictionary<String, HashSet<string>> metaDataInRequestXML_ = new Dictionary<String, HashSet<string>>();
        private Dictionary<String, HashSet<string>> testNames_ = new Dictionary<String, HashSet<string>>();
        //parseXml calls parseTestRequestXML() when category =1 and parseBuildRequestXML() when category =0
        public void parseXml(int category)
        {
            switch (category)
            {
                case 0:
                    parseBuildRequestXml(); break;
                case 1:
                    parseTestRequestXML(); break;

            }

        }


        private void parseTestRequestXML()
        {
            try
            {
                XDocument x = XDocument.Parse(xmlFile_);
                HashSet<string> tempSet = new HashSet<string>();
                tempSet.Add(x.Descendants("author").ElementAt(0).Value);
                metaDataInRequestXML_.Add("author", tempSet);
                HashSet<string> tempTestNameDllSet = new HashSet<string>();
                XElement[] arrayOfTestNameDlls = x.Descendants("testNameDll").ToArray();
                foreach (string eachDLl in arrayOfTestNameDlls)
                {
                    tempTestNameDllSet.Add(eachDLl);
                    Console.WriteLine("eachDLL to be received [in xml.cs Line 155]: {0}", eachDLl);
                }
                testNames_.Add("testNameDll", tempTestNameDllSet);
            } catch(Exception ex)
            {
                Console.WriteLine("build failed due to improper BuildRequest XML format in {0}", xmlFile_);
                Console.WriteLine(ex.Message);
            }
        }

        private void parseBuildRequestXml()
        {
            XDocument x = XDocument.Parse(xmlFile_);
            // tags in the xml: test, testName, testDriver, testedFile, author.
            XElement[] arrayOfTestElements = x.Descendants("test").ToArray();
            foreach (var eachTest in arrayOfTestElements)
            {
                string tempTestName = eachTest.Element("testName").Value;
                if (testNames_.ContainsKey("testName"))
                {
                    testNames_["testName"].Add(tempTestName);
                }
                else
                {
                    HashSet<string> testName = new HashSet<string>();
                    testName.Add(tempTestName);
                    testNames_.Add("testName", testName);
                }
                string temp = eachTest.Element("testDriver").Value;
                foreach (var temp1 in eachTest.Elements("testedFile"))
                {
                    if (FileNamesInXMLRequest_.ContainsKey(temp))
                    {
                        FileNamesInXMLRequest_[temp].Add(temp1.Value);
                    }
                    else
                    {
                        HashSet<string> tempSet1 = new HashSet<string>();
                        tempSet1.Add(temp1.Value);
                        FileNamesInXMLRequest_.Add(temp, tempSet1);
                    }
                }
            }
            string authorInfo = x.Descendants("author").ElementAt(0).Value;
            HashSet<string> tempSet2 = new HashSet<string>();
            tempSet2.Add(authorInfo);
            metaDataInRequestXML_.Add("author", tempSet2);
        }
    }

    public class XMLGenerator
    {
        public XMLGenerator(string receievedBuildRequestXmlString = "", string nameOfXmlBuildRequestToBeCreated = "", string nameOfXmlTestRequestToBeCreated = "", List<List<string>> contentForBuildRequst = null, List<List<string>> contentForTestRequest = null, int category = 3)
        {
            receivedXmlString = receievedBuildRequestXmlString;
            generatedXmlBuildRequestFile_ = nameOfXmlBuildRequestToBeCreated;
            generatedXmlTestRequestFile_ = nameOfXmlTestRequestToBeCreated;
            contentForXMLBuildRequest = contentForBuildRequst;
            contentForXMLTestRequest = contentForTestRequest;
            generateXML(category);
        }

        

        private void generateXML(int category)
        {
            switch (category)
            {
                case 0:
                    generateBuildRequestXML(); break;
                case 1:
                    generateTestRequestXML(); break;
                default:
                    break;
            }
        }

        private void generateTestRequestXML()
        {
            XDocument xml = new XDocument();
            xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
            XElement root = new XElement("testRequest");
            xml.Add(root);
            XElement author = new XElement("author", XDocument.Parse(receivedXmlString_).Descendants("author").ElementAt(0).Value);
            root.Add(author);
            XMLParser tempXmlParser = new XMLParser(receivedXmlString_);
            tempXmlParser.parseXml(0);
            var tempDictOfTestNames = tempXmlParser.getTestNamesinXMlRequest;
            foreach (string eachTestName in tempDictOfTestNames["testName"])
            {
                XElement testNameDll = new XElement("testNameDll", eachTestName + ".dll");
                root.Add(testNameDll);
            }
            xml.Save(generatedXmlTestRequestFile_);
        }

        private void generateBuildRequestXML()
        {

            XDocument xml = new XDocument();
            xml.Declaration = new XDeclaration("1.0", "utf-8", "yes");
            XElement root = new XElement("buildRequest");
            xml.Add(root);
            XElement author = new XElement("author", contentForXMLBuildRequest_.ElementAt(0).ElementAt(0)); // o is for author
            root.Add(author);
            for (int i = 1; i < contentForXMLBuildRequest_.Count(); i++)
            {
                var test = new XElement("test");
                root.Add(test);
                var testName = new XElement("testName", contentForXMLBuildRequest_.ElementAt(i).ElementAt(0)); // 0 is for testName
                test.Add(testName);
                var testDriver = new XElement("testDriver", contentForXMLBuildRequest_.ElementAt(i).ElementAt(1));
                test.Add(testDriver);
                for (int j = 2; j < contentForXMLBuildRequest_.ElementAt(i).Count(); j++)
                {
                    var testedFile = new XElement("testedFile", contentForXMLBuildRequest_.ElementAt(i).ElementAt(j));
                    test.Add(testedFile);
                }
            }
            xml.Save(generatedXmlBuildRequestFile_);


        }

        public string receivedXmlString { set { receivedXmlString_ = value; } }
        public List<List<string>> contentForXMLBuildRequest { set { contentForXMLBuildRequest_ = value; } }
        public List<List<string>> contentForXMLTestRequest { set { contentForXMLTestRequest_ = value; } }
        public string generatedXmlBuildRequestFile { get { return generatedXmlBuildRequestFile_; } }
        public string generatedXmlTestRequestFile { get { return generatedXmlTestRequestFile_; } }

        private List<List<string>> contentForXMLBuildRequest_;
        private List<List<string>> contentForXMLTestRequest_;
        private String generatedXmlBuildRequestFile_;
        private String generatedXmlTestRequestFile_;
        //build req that was sent by the repo. this will be used in creating testRequest.
        private string receivedXmlString_;

    }


    class xml
    {

        static void Main(string[] args)
        {
            //      XMLParser y = new XMLParser("TestRequest.xml");
            //    y.parseXml(0);
            List<List<string>> clientsBuildContent = new List<List<string>>();
            List<string> authorName = new List<string>() { "sandra" };
            List<string> test1 = new List<string>() { "firstTest", "testDriver1.cs", "testedFile1.cs", "testedFile2.cs" };
            List<string> test2 = new List<string>() { "secondTest", "testDriver2.cs", "testedFile2.cs", "testedFile3.cs" };
            clientsBuildContent.Add(authorName);
            clientsBuildContent.Add(test1);
            clientsBuildContent.Add(test2);

            XMLGenerator xmlGenerator = new XMLGenerator("", "buildRequest.xml", "", clientsBuildContent, null, 0);
            XDocument xml = XDocument.Load("buildRequest.xml");
            string xmlString = xml.ToString();
            XMLParser xmlParser = new XMLParser(xmlString);
            xmlParser.parseXml(0);
            var fileNames = xmlParser.getFileNamesInXMLRequest;
            var testNames = xmlParser.getTestNamesinXMlRequest;
            var metaData = xmlParser.getMetaDataInRequestXML;
            foreach (var eachTest in fileNames)
            {
                var testDriver = eachTest.Key;
                Console.WriteLine(testDriver);
                foreach (var eachTestedFile in fileNames[testDriver])
                {
                    Console.WriteLine(eachTestedFile);
                }
            }
            foreach (var eachTestName in testNames["testName"])
            {
                Console.WriteLine(eachTestName);
            }
            foreach (var auth in metaData["author"])
            {
                Console.WriteLine(auth);
            }

            XMLGenerator xmlGenerator2 = new XMLGenerator(xmlString,"" , "tstRequest.xml", null, null, 1);
            Console.WriteLine("=======================================================================================================");
            Console.WriteLine("check the {0} for locating the build and test requests in XML format", @"bin directory of XML Handler");

            Console.ReadLine();
            Console.ReadLine();
        }

    }
}
