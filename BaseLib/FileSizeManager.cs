/***************************************************************************************************************************
Copyright: Juerg Peter Huber, Horgen, 2016
This code is contributed to the Public Domain. It may be freely used by anyone for any purpose, commercial or non-commercial. 
The software is provided "as-is." The author gives no warranty of any kind that the code is free of defects, merchantable, 
fit for a particular purpose or non-infringing. Use this code only if you agree with this conditions. The entire risk of 
using it is with you :-)
***************************************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;


namespace ACoreLib {


  #region FileParameterStruct
  //      ===================

  /// <summary>
  /// Stores file parameters like file name and location, size limitations, etc.
  /// </summary>
  public struct FileParameterStruct {

    #region Properties
    //      ----------

    /// <summary>
    /// The Directory Path where the FileName will be created 
    /// </summary>
    public string DirectoryPath { get{return directoryPath;}}
    string directoryPath;

    /// <summary>
    /// File Name (Exclude the extension name eg: testfile)
    /// </summary>
    public readonly string FileName;

    /// <summary>
    /// The extension of the file. It cannot have a '.'.
    /// </summary>
    public readonly string FileExtension;

    /// <summary>
    /// The Max size of the File in bytes. If 0, file size is not limitted.
    /// </summary>
    public long MaxFileByteCount { get { return maxFileByteCount; } }
    long maxFileByteCount;

    /// <summary>
    /// Number of files to stored before it stops writing
    /// </summary>
    public int MaxFileCount { get { return maxFileCount; } }
    int maxFileCount;
    #endregion


    #region Constructor
    //      -----------

    //constructor
    public FileParameterStruct(
      string newDirectoryPath,
      string newFileName,
      string newFileExtension,
      long newMaxFileByteCount,
      int newMaxFileCount) //
    {
      directoryPath = newDirectoryPath;
      FileName = newFileName;
      FileExtension = newFileExtension;
      maxFileByteCount = newMaxFileByteCount;
      maxFileCount = newMaxFileCount;
    }
    #endregion


    #region Methods
    //      -------

    /// <summary>
    /// Checks if the provided parameters are valid for the FileSizeManager constructor. This
    /// is useful to check the parameters immediately when the user changes them, even
    /// they might be used only once the application restarts.
    /// If isFileWritingTest is true, a file is written to see if everything is ok.
    /// Problem contains an explanation if anything is wrong.
    /// </summary>
    public bool ValidateConstructorParameters(
      bool isFileWritingTest,
      out string problem) //
    {
      if (DirectoryPath==null) {
        problem = "Path missing.";
        return false;
      }
      if (FileName==null || FileName.Length<1) {
        problem = "Filename name missing.";
        return false;
      }
      if (FileName.Contains(".")) {
        problem = "File name '" + FileName + "' should not contain '.'.";
        return false;
      }
      if (FileExtension==null || FileExtension.Length<1) {
        problem = "File extension missing.";
        return false;
      }
      if (FileExtension.Contains(".")) {
        problem = "File extension '" + FileExtension + "' should not contain '.'.";
        return false;
      }
      if (maxFileByteCount<0) {
        problem = "File Size must be greater than or equal 0. File Size: " + maxFileByteCount.ToString();
        return false;
      }
      if (maxFileCount<1) {
        problem = "Number of files to keep must be greater 0. MaxFilesCount: " + maxFileCount.ToString();
        return false;
      }
      if (maxFileByteCount==0) {
        if (maxFileCount!=1) {
          problem = "Number of Files To Store must be 1 if max file size is 0. Max Store: " + 
            maxFileCount.ToString();
          return false;
        }
      }
      if (isFileWritingTest) {
        //try if file can be created. This is the best test to find out, if the parameters are legal
        try {
          DirectoryInfo directoryInfo = Directory.CreateDirectory(DirectoryPath);
          FileInfo[] foundFiles = directoryInfo.GetFiles(GetFileSearchPattern());
          if (foundFiles.Length==0) {
            FileInfo newFileInfo = new FileInfo(GetNewFileName(0));
            using (FileStream fileStream = newFileInfo.Create()) {
              //open and close file
            };
            newFileInfo.Delete();
          }
        } catch (Exception ex) {
          problem = ex.Message;
          return false;
        }
      }
      problem = "";
      return true;
    }


    /// <summary>
    /// Overwrite DirectoryPath. This is usful for overwriting a relative directory path
    /// by the absolute directory path
    /// </summary>
    /// <param name="newDirectoryPath"></param>
    internal void UpdateDirectoryPath(string newDirectoryPath) {
      directoryPath = newDirectoryPath;
    }


    /// <summary>
    /// Supports the changing of file size and max number of files
    /// </summary>
    internal void ChangeProperties(long newMaxFileByteCount, int newMaxFileCount) {
      maxFileByteCount = newMaxFileByteCount;
      maxFileCount = newMaxFileCount;
    }

    
    /// <summary>
    /// Get a search string which will find the files indicated by
    /// FileName, any running number and the file extension
    /// </summary>
    public string GetFileSearchPattern() {
      return FileName + "*." + FileExtension;
    }


    /// Get the complete file name based on
    /// the file name, running number and the file extension
    public string GetNewFileName(int fileNumber) {
      return FileName + fileNumber + "." + FileExtension;
    }


    /// <summary>
    /// Pack the property values in a string
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return
        "DirectoryPath: " + DirectoryPath + "; " +
        "FileName: " + FileName + "; " +
        "FileExtension: " + FileExtension + "; " +
        "MaxFileByteCount: " + MaxFileByteCount.ToString() + "; " +
        "MaxFileCount: " + MaxFileCount.ToString() + ";";
    }
    #endregion
  }
  #endregion


  #region FileSizeManager
  //      ===============

  public class FileSizeManager {
    
    #region Properties
    //      ----------

    /// <summary>
    /// File parameters like file name and location, size limitations, etc.
    /// </summary>
    public FileParameterStruct FileParameter { get { return fileParameter; } }
    FileParameterStruct fileParameter;

    /// <summary>
    /// Full Path of the current active file
    /// </summary>
    public string FullName { get { return currentFileInfo.FullName; } }

    /// <summary>
    /// Number of files being presently managed
    /// </summary>
    public int ActualFileCount {
      get {
        return fileList.Count;
      }
    }
    #endregion


    #region Constructor
    //      -----------

    private int currentFileNumber;
    private List<FileInfoNumberStruct> fileList;
    private DirectoryInfo directoryInfo;
    private FileInfo currentFileInfo;

    /// <summary>
    /// Constructor
    /// 
    /// Creates and opens/closes a file
    /// 
    /// will throw an exception if file cannot be created
    /// </summary>
    public FileSizeManager(FileParameterStruct newParameter) {
      //ensure that parameters are valid.
      string problem = null;
      if (!newParameter.ValidateConstructorParameters(false, out problem)) {
        throw new Exception("Cannot create '" + newParameter.ToString() + "'." +  
          (problem==null ? "" : " The following problem occured: " + Environment.NewLine + problem));
      }

      //create directory if necessary and prepare filelist
      directoryInfo = Directory.CreateDirectory(newParameter.DirectoryPath);
      newParameter.UpdateDirectoryPath(directoryInfo.FullName);
      fileParameter = newParameter;
      fileList = new List<FileInfoNumberStruct>();

      //Read fileinfos with file numbers into sorted fileList
      FileInfo[] fileInfos =
          directoryInfo.GetFiles(fileParameter.GetFileSearchPattern(), SearchOption.TopDirectoryOnly);

      int fileNumber = 1;
      currentFileNumber = 1;
      //TestFileName123.tst ==> 123
      //             TestFileName
      int startPos = fileParameter.FileName.Length;
      //                    .   tst
      int extensionLength = 1 + fileParameter.FileExtension.Length;
      foreach (FileInfo fileInfo in fileInfos) {
        string fileName = fileInfo.Name;
        if (int.TryParse(fileName.Substring(startPos, fileName.Length-startPos-extensionLength), out fileNumber)) {
          fileList.Add(new FileInfoNumberStruct(fileNumber, fileInfo));

          if (fileNumber>currentFileNumber) {
            currentFileNumber = fileNumber;
          }
        }
      }
      fileList.Sort();

      //make sure that the correct number of files exist, 
      if (fileList.Count==0) {
        createNewFile();
      }else{
        enforceFileCountMax();
        currentFileInfo = fileList[fileList.Count-1].FileInfo;
      }
    }
    #endregion


    #region Methods
    //      -------

    /// <summary>
    /// Returns the full path and name of the current file to use. If the existing file on the
    /// hard disk is too big, a new file is created. If there are too many files, the oldest file gets deleted.
    /// </summary>
    public string GetFileToUse() {
      if (fileParameter.MaxFileByteCount>0) {
        //Check if a new backup file should be created
        FileInfo newFile = new FileInfo(FullName);
        if (!newFile.Exists) {
          //create file with same file number
          createNewFile();
        } else if (newFile.Length>=fileParameter.MaxFileByteCount) {
          //limit file size, create file with incremented file numner
          currentFileNumber++;
          createNewFile();
        }
      }
      return FullName;
    }



    /// <summary>
    /// Returns the full path and name of the next, empty file. If there are too many files, the oldest file gets deleted.
    /// </summary>
    public string GetNextFile() {
      currentFileNumber++;
      createNewFile();
      return FullName;
    }


    private void createNewFile() {
      currentFileInfo = new FileInfo(fileParameter.DirectoryPath + @"\" + fileParameter.GetNewFileName(currentFileNumber));
      fileList.Add(new FileInfoNumberStruct(currentFileNumber, currentFileInfo));
      enforceFileCountMax();
    }


    private void enforceFileCountMax() {
      //make sure that not more than MaxFilesCount of files exist, 
      int fileCountDifference = fileList.Count - fileParameter.MaxFileCount;
      if (fileCountDifference>0) {
        //there are too many files, delete the ones with the lower numbers
        for (int fileIndex = 0; fileIndex<fileCountDifference; fileIndex++) {
          try {
            fileList[0].FileInfo.Delete();
            fileList.Remove(fileList[0]);
          } catch (Exception ex) {
            Tracer.ShowExceptionInDebugger(ex);
            Tracer.TraceException(ex, "FilesSizeManager.EnforceFileCountMax(): Cannot delete file '" + fileList[0].FileInfo.FullName + "'.");
          }
        }
      }
    }
    #endregion


    #region File Information Structure
    //      --------------------------

    private struct FileInfoNumberStruct: IComparable<FileInfoNumberStruct> {
      public int FileNumber;
      public FileInfo FileInfo;


      public FileInfoNumberStruct(int newFileNumber, FileInfo newFileInfo) {
        FileNumber = newFileNumber;
        FileInfo = newFileInfo;
      }

      public int CompareTo(FileInfoNumberStruct other) {
        return this.FileNumber.CompareTo(other.FileNumber);
      }

      public override string ToString() {
        return "FileNumber: " + FileNumber.ToString() + " ; Path: " + FileInfo.FullName;
        ;
      }
    }
    #endregion

    /// <summary>
    /// Supports the changing of file size and max number of files
    /// </summary>
    internal void ChangeProperties(long newMaxFileByteCount, int newMaxFileCount) {
      fileParameter.ChangeProperties(newMaxFileByteCount, newMaxFileCount);
    }
  }
  #endregion
}
