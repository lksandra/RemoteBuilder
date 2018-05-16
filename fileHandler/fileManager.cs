///////////////////////////////////////////////////////////////////////////////
///  Program.cs - module that provides methods to file management           ///
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
//this module provides generic file management operations
////////////////////////////////////////////////////////////////////////////////
//Required files:
//===============
//1. fileHandler.fileManager.cs
////////////////////////////////////////////////////////////////////////////////
//public interface: 
//=================
//class fileManager{}:
//--------------------
//this class contains all the file management operations
//------------------------------------------------------------------------------
//public HashSet<string> getAllFilesFromAGivenPath(String path, String pattern):
//-----------------------------------------------------------------------------
//this function gets all the files from the given path for a given Pattern.
//-----------------------------------------------------------------------------
// public bool sendFileFromSourceToDestination(string fullFileName, 
//String destinationPath):
//-----------------------------------------------------------------
//this function sends one file, given its fully qualified name nor relative path 
//name to the destination path, which is either relative or absolute
//-----------------------------------------------------------------------------
// public bool sendFilesFromSourceToDestination(HashSet<string> fullFileNames,
//String destinationPath):
//-----------------------------------------------------------------------------
//this function sends given HashSet of files, absolute or relative, to the 
//destination. absolute or relative.
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
using System.IO;

namespace fileHandler
{
    public class fileManager
    {
        private HashSet<string> setOfFiles_ = new HashSet<string>();
        public HashSet<string> setOfFiles { get { return setOfFiles_; } }
        //this function gets all the files from the given path for a given Pattern.
        public HashSet<string> getAllFilesFromAGivenPath(String path, String pattern)
        {
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path, pattern);
                setOfFiles_.UnionWith(files);
                string[] subDirs = Directory.GetDirectories(path);
                foreach (string eachSubDir in subDirs)
                {
                    getAllFilesFromAGivenPath(eachSubDir, pattern);
                }
            }
            else
            {
                Console.WriteLine("Path doesnt exist:{0} ", path);
            }
            return setOfFiles;
        }
        //this function sends one file, given its fully qualified name nor relative path 
        //name to the destination path, which is either relative or absolute
        public bool sendFileFromSourceToDestination(string fullFileName, String destinationPath)
        {
            string finalFileName = Path.Combine(destinationPath, Path.GetFileName(fullFileName));
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            File.Copy(fullFileName, finalFileName, true);
            return true;
        }
        //this function sends given HashSet of files, absolute or relative, to the 
        //destination. absolute or relative.
        public bool sendFilesFromSourceToDestination(HashSet<string> fullFileNames, String destinationPath)
        {
            try
            {
                foreach (string eachFullFileName in fullFileNames)
                {
                    sendFileFromSourceToDestination(eachFullFileName, destinationPath);

                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

        }
        static void Main(string[] args)
        {
            fileManager f = new fileManager();
            f.getAllFilesFromAGivenPath(@"../../../mockRepo/repoStorage", "*.cs");
            var xyz = f.setOfFiles;
            f.sendFilesFromSourceToDestination(xyz, @"../../../builder/builderStorage");
            Console.ReadLine();
            Console.ReadLine();



        }
    }
}
