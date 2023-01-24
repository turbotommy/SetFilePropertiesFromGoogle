using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Threading.Tasks;

namespace SetFilePropertiesFromGoogle
{
    class FileInfo
    {
        public DateTime createDate;
        public string fileName;
        public string path;
        public string SourceFile
        {
            get
            {
                return Path.Combine(path, fileName);
            }
        }
          
        MediaType fileMediaType;     
        internal enum MediaType { Photo, Gif, Video, Other };
        internal MediaType GetMediaType(string extension)
        {
            string photo = ".jpg.jpeg.png.dng";
            string gif = ".gif";
            string video = ".mp4.mov.m4v.3gp";

            var lowerExt = extension.ToLower();
            if (photo.Contains(lowerExt))
                return MediaType.Photo;
            else if (gif.Contains(lowerExt))
                return MediaType.Gif;
            else if (video.Contains(lowerExt))
                return MediaType.Video;
            else
                return MediaType.Other;
        }
        internal FileInfo(MediaItem item)
        {
            fileName = item.filename;
            //Set createdDate
            var googleCreationDate=item.mediaMetadata.creationTime.ToLocalTime();
            if (googleCreationDate == null)
                Console.WriteLine($"CreatedDate null for {fileName}");
            else
            {
                createDate = googleCreationDate;
            }
        }

        internal FileInfo(string file)
        {
            fileName = Path.GetFileName(file);
            path = Path.GetDirectoryName(file);
        }

        internal void AddLocalFileAttributes(ShellFile localFile)
        {
            //Add local source path
            path = localFile.Path;
        }

        internal void AddGoogleTime(DateTime googleDateTime)
        {
            createDate = googleDateTime;
        }
        internal void Move(string destination)
        {
            
            var destFile = Path.Combine(destination,fileName);
            try
            {
                File.Move(SourceFile, destFile);
                //Set ItemDateCreated

                var shellFile = ShellFile.FromFilePath(destFile);
                
                SetCreatedFileDate(createDate, ref shellFile);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error during filemove to " + destFile + ", " + e.Message);
            }
        }
        internal void SetCreatedFileDate(DateTime creationDate, ref ShellFile localFile)
        {
            
            try
            {
                var localExt = localFile.Properties.System.FileExtension.Value;
                var mediaType = GetMediaType(localExt);
                switch (mediaType)
                {
                    case MediaType.Photo:
                        if (localFile.Properties.System.Photo.DateTaken.Value != creationDate)
                        {
                            var photo = localFile.Properties.System.Photo;
                            localFile.Properties.System.Photo.DateTaken.Value = creationDate;
                        }
                        break;
                    case MediaType.Video:
                        if (localFile.Properties.System.Media.DateEncoded.Value != creationDate)
                            localFile.Properties.System.Media.DateEncoded.Value = creationDate;
                        break;
                    case MediaType.Gif:
                        break;
                    case MediaType.Other:
                    default:
                        throw new AccessViolationException("Unknown extension for " + localFile.Properties.System.FileName.Value);

                }
                File.SetCreationTime(localFile.Path,creationDate);
            }
            catch (Exception e)
            {

                //Not a photo, try video
                Console.WriteLine("Error occurred during propertyset:" + e.Message);

            }
        }
    }
    class DirInfo
    {
        ShellProperties shellProperties;
        public string baseDir;
        public string processedDir;
        long nrOfFiles=0;
        public Dictionary<string, FileInfo> pictures { get; set; }

        public DirInfo(string baseDir)
        {
            this.baseDir = baseDir + "\\Google Foto";
            processedDir = baseDir + "\\Processed";
            pictures = new Dictionary<string, FileInfo>();
        }
        public Dictionary<string, FileInfo> PopulateDictionary()
        {
            return PopulateDictionary(baseDir);
        }
        public Dictionary<string,FileInfo> PopulateDictionary(string startDir)
        {
            var dirs = Directory.GetDirectories(startDir);

            foreach(var dir in dirs)
            {
                PopulateDictionary(dir);
            }
            var files = Directory.GetFiles(startDir);
            if(files == null)
            {
                if (startDir != baseDir)
                {
                    Console.WriteLine("Removing empty " + startDir);
                    Directory.Delete(startDir);
                }
            } 
            else
            {
                foreach(var file in files)
                {
                    if (file.EndsWith("json"))
                    {
                        //Only file is json. Remove both file and folder
                        Console.WriteLine("Removing " + file);
                        File.Delete(file);
                        if (files.Length == 1)
                        {
                            Console.WriteLine("Removing empty " + startDir);
                            Directory.Delete(startDir);
                        }
                    }
                    else
                    {
                        nrOfFiles++;

                        var fileInfo = new FileInfo(file);
                        //Console.WriteLine(fileInfo.Name);
                        //var month = string.Format("{0:yyyy-MM}", fileInfo.createDate);

                        if (!pictures.TryAdd(fileInfo.fileName, fileInfo))
                        {
                            var dupFile = pictures[fileInfo.fileName];
                            Console.WriteLine($"{fileInfo.SourceFile} duplicate for {dupFile.SourceFile}");
                        }
                    }
                }
            }
            return pictures;
        }

        public Dictionary<ShellProperty<DateTime?>, List<ShellProperties>> DateSort(Dictionary<ShellProperty<DateTime?>, ShellProperties> pictures)
        {
            Dictionary<ShellProperty<DateTime?>, List<ShellProperties>> dateSorted = new Dictionary<ShellProperty<DateTime?>, List<ShellProperties>>();
            foreach(var picture in pictures)
            {
                var picValue = picture.Value;

                if (dateSorted.TryGetValue(picValue.System.DateCreated, out var piclist))
                {
                    piclist.Add(picValue);
                    dateSorted[picValue.System.DateCreated] = piclist;
                }
                else
                {
                    var picList = new List<ShellProperties>();
                    piclist.Add(picValue);
                    dateSorted.Add(picValue.System.DateCreated, picList);
                }
            }
            return dateSorted;
        }

        internal void AddSortedPic(ShellProperties localPic)
        {

            throw new NotImplementedException();
        }

        internal async Task MoveFiles(Dictionary<string, List<FileInfo>> monthsToMove)
        {
            //string processedDir = baseDir + "\\Processed";
            foreach (var month in monthsToMove)
            {
                //Move to YYYY-MM folder
                //DateTime dateCreated = (DateTime)fileToMove.Key;
                //var month = dateCreated.ToString("yyyy-MM");

                var monthPath = processedDir + @"\" + month.Key;
                Directory.CreateDirectory(monthPath);
                foreach (var fileToMove in month.Value)
                {
                    fileToMove.Move(monthPath);
                }
            }
        }
    }
}
