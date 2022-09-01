using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace SetFilePropertiesFromGoogle
{
    class DirInfo
    {
        ShellProperties.PropertySystem shellProperties;
        Dictionary<string, ShellProperties.PropertySystem> pictures=new Dictionary<string, ShellProperties.PropertySystem>();
        public void PopulateDictionary(string startDir)
        {
            var dirs = Directory.GetDirectories(startDir);

            foreach(var dir in dirs)
            {
                PopulateDictionary(dir);
            }
            var files = Directory.GetFiles(startDir);
            
            foreach(var file in files)
            {
                if(!file.EndsWith("json"))
                {
                    var fileInfo = ShellFile.FromFilePath(file);
                    Console.WriteLine(fileInfo.Name);
                    if (!pictures.TryAdd(fileInfo.Name, fileInfo.Properties.System))
                    {
                        Console.WriteLine($"{fileInfo.Name} duplicate for {pictures[fileInfo.Name]}");

                    }
                }
            }
        }
    }
}
