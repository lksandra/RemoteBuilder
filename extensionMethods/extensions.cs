///////////////////////////////////////////////////////////////////////////////
///  Program.cs - module that provides extension methods to strings         ///
///  ver 1.0                                                                ///
///  Language:     C#                                                       ///
///  Platform:     windows10. toshiba satellite                             ///
///  Application:  to demonstrate core builder. To be used in project 2 of  ///
///                SMA                                                      ///
///  Author:       Lakshmi kanth sandra, 229653990                          ///
///                                                                         ///
///////////////////////////////////////////////////////////////////////////////
//Module Operations:
//==================
//this module provides string operations
////////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1. ExtensionMethods.extensions.cs
////////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
//class stringExtensions{}:
//-------------------------
//this static class provides some static extension methods to be sued by strings
//-----------------------------------------------------------------------------
//public static string getFolderName(this string x):
//--------------------------------------------------
//this static method enables obtaining the substring, which happens to be folder 
//name here.
//------------------------------------------------------------------------------
//public static string getTypeOfProgrammingLanguage(this string x):
//-----------------------------------------------------------------
//this function retuns the programming language iin which the file was written.
////////////////////////////////////////////////////////////////////////////////
//maintenance history: V1.0, 13/9/17.
//===================================
//NIL
///////////////////////////////////////////////////////////////////////////////



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ExtensionMethods
{
    public static class stringExtensions{
        public static string getFolderName(this string x)
        {
            string subString;
            int index = x.IndexOf(@"BuildRequest");
            if (index > 0)
            {
                
                subString = x.Remove(index);
              //  Console.WriteLine(subString);
                return subString;
            }
            else if(x.IndexOf(@"TestRequest") > 0)
            {
                index = x.IndexOf(@"TestRequest");
                subString = x.Remove(index);
              //  Console.WriteLine(subString);
                return subString;
            }
            else
            {
                index = x.IndexOf(@".dll");
                subString = x.Remove(index);
                return subString;
            }
            
        }

        public static string getTypeOfProgrammingLanguage(this string x)
        {
            if (x.Contains(".cs"))
            {
                return "csc";
            }
            else
            {
                return "java";
            }
        }
    
    }
    class Program
    {
        

        static void Main(string[] args)
        {
            string y = "sandraFirstTestBuildRequest";
            var xyz = y.getFolderName();
            string z = "sandraFirstTestTestRequest";
            var zyx = z.getFolderName();
            Console.ReadLine();


        }
    }
}
