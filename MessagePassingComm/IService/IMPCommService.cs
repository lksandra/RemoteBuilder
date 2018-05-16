///////////////////////////////////////////////////////////////////////////////
///  IMPCommService.cs - this package describes the interface needed for the///
///                     WCF communication.                                  ///
///  ver 3.0                                                                ///
///  Language:     C#                                                       ///
///  Platform:     windows10. toshiba satellite                             ///
///  Application:  Demonstrating Project 3 and To be used in project 4 of   ///
///                SMA CSE 681,   2017. client, mockRepository, motherBuilder
///                references this interface.                               ///
///                                                                         ///
///  Author:       Lakshmi kanth sandra, 229653990                          ///
///  Reference:    Prof Jim Fawcett                                         ///
///////////////////////////////////////////////////////////////////////////////
//Module Operations:
//==================
//this module provides interface for establishing the service contract,
//and the data contract class for WCF.
///////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1.IPCommService.cs
///////////////////////////////////////////////////////////////////////////////
////public interface: 
//=================
//public struct ClientEnvironment:
//
//ClientEnvironment provides setter and getters for customising the client 
//environment.
//-----------------------------------------------------------------------------
//public static string fileStorage:
//
//this sets the sender's location dirctory.
//-----------------------------------------------------------------------------
//public const long blockSize:
//
//this sets the size of the block to be read from the file while read/write 
//operations.
//-----------------------------------------------------------------------------
//public struct ServiceEnvironment:
//
//this struct sets the service side environment.
//-----------------------------------------------------------------------------
//public static string fileStorage:
//
//sets the filestorage for the server.
//-----------------------------------------------------------------------------
//public interface IMessagePassingComm:
//
//this interface is the service contract for the implementing fucntions to 
//implement.
//-----------------------------------------------------------------------------
//void postMessage(CommMessage msg):
//
//this operation contract function declaration is used to send messages. It 
//takes the CommMessage object as parameter.
//-----------------------------------------------------------------------------
//CommMessage getMessage():
//
//this operation contract function declaration is used to receive messages.
//----------------------------------------------------------------------------
//bool openFileForWrite(string name):
//
//this operation contract function declaration is used to open file for writing 
//on the server side.
//----------------------------------------------------------------------------
//bool writeFileBlock(byte[] block):
//
//this operation contract function declaration is used to write file on the 
//server side. Takes byte array as argument.
//-----------------------------------------------------------------------------
//void closeFile():
//
//this operation contract function declaration is used to close the file that was
//opened to write on the server side.
//-----------------------------------------------------------------------------
//public class CommMessage:
//
//this class declares and defines the data contract for the service to use.
//-----------------------------------------------------------------------------
//public enum MessageType:
//
//this enum contains the keywords that can be used in coomunicating.
//-----------------------------------------------------------------------------
//public CommMessage(MessageType mt)
//
//constructor instantiates the variables of this class. it takes MessageType enum
//as parameter.
//------------------------------------------------------------------------------
//public MessageType type:
//
//indicates the type of the Message that is beng sent.
//-----------------------------------------------------------------------------
//public string to:
//indicates the address of the receipient.
//-----------------------------------------------------------------------------
//public string from:
//indicates the sender's address
//-----------------------------------------------------------------------------
//public string author:
//indicates author instantiating the message communication.
//-----------------------------------------------------------------------------
//public Command command:
//this indicates what the sender expects from the receiver to do.
//-----------------------------------------------------------------------------
//public List<Argument> arguments:
//this can be used to set arguments for the message.
//-----------------------------------------------------------------------------
//public string infoAboutContent:
//this indicates what content is present in the message.
//-----------------------------------------------------------------------------
//public List<List<string>> xmlBuildContent:
//this indicates if the content being sent is the arguments for the XML
//XML build request generation.
//-----------------------------------------------------------------------------
//public void show():
//this function displays the CommMessage object on the console.



/*
 * 
 * Maintenance History:
 * --------------------
 * ver 3.0 : 29 Oct 2017
 * - added infoAboutContent, xmlBuildContent to the data contract.
 * - removed baseaddress in both service and client environment.
 * - modified the fileStorage from const to static.
 * ver 2.0 : 19 Oct 2017
 * - renamed namespace and ClientEnvironment
 * - added verbose property to ClientEnvironment
 * ver 1.0 : 15 Jun 2017
 * - first release
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Threading;

namespace MessagePassingComm
{
  using Command = String;             // Command is key for message dispatching, e.g., Dictionary<Command, Func<bool>>
  using EndPoint = String;            // string is (ip address or machine name):(port number)
  using Argument = String;      
  using ErrorMessage = String;

    public struct ClientEnvironment
    {
        public static string fileStorage; 
        public const long blockSize = 1024;
        public static bool verbose { get; set; }
  }

  public struct ServiceEnvironment
  {
    public static string fileStorage ;
  }

  [ServiceContract(Namespace = "MessagePassingComm")]
  public interface IMessagePassingComm
  {
    /*----< support for message passing >--------------------------*/

    [OperationContract(IsOneWay = true)]
    void postMessage(CommMessage msg);

    // private to receiver so not an OperationContract
    CommMessage getMessage();

    /*----< support for sending file in blocks >-------------------*/

    [OperationContract]
    bool openFileForWrite(string name);

    [OperationContract]
    bool writeFileBlock(byte[] block);

    [OperationContract(IsOneWay = true)]
    void closeFile();
  }

  [DataContract]
  public class CommMessage
  {
    public enum MessageType
    {
      [EnumMember]
      connect,           // initial message sent on successfully connecting
      [EnumMember]
      request,           // request for action from receiver
      [EnumMember]
      reply,             // response to a request
      [EnumMember]
      closeSender,       // close down client
      [EnumMember]
      closeReceiver      // close down server for graceful termination
      
      
        }

    /*----< constructor requires message type >--------------------*/

    public CommMessage(MessageType mt)
    {
      type = mt;
    }
    /*----< data members - all serializable public properties >----*/

    [DataMember]
    public MessageType type { get; set; } = MessageType.connect;

    [DataMember]
    public string to { get; set; }

    [DataMember]
    public string from { get; set; }

    [DataMember]
    public string author { get; set; }

    [DataMember]
    public Command command { get; set; }

    [DataMember]
    public List<Argument> arguments { get; set; } = new List<Argument>();

    [DataMember]
    public int threadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

    [DataMember]
    public ErrorMessage errorMsg { get; set; } = "no error";

    [DataMember]
     public string infoAboutContent { get; set; }

    [DataMember]
    public List<List<string>> xmlBuildContent { get; set; } = new List<List<string>>();

        public void show()
    {
      Console.Write("\n  CommMessage:");
      Console.Write("\n    MessageType : {0}", type.ToString());
      Console.Write("\n    to          : {0}", to);
      Console.Write("\n    from        : {0}", from);
      Console.Write("\n    author      : {0}", author);
      Console.Write("\n    command     : {0}", command);
      Console.Write("\n    arguments   :");
      if (arguments.Count > 0)
        Console.Write("\n      ");
      foreach (string arg in arguments)
        Console.Write("{0} ", arg);
      Console.Write("\n    ThreadId    : {0}", threadId);
      Console.Write("\n    errorMsg    : {0}\n", errorMsg);
    }
  }
}
