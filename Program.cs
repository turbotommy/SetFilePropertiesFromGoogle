using System;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
 
namespace SetFilePropertiesFromGoogle
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string filePath = @"C:empexample.docx";
            var file = ShellFile.FromFilePath(filePath);

            // Read and Write:

            string[] oldAuthors = file.Properties.System.Author.Value;
            string oldTitle = file.Properties.System.Title.Value;

            file.Properties.System.Author.Value = new string[] { "Author #1", "Author #2" };
            file.Properties.System.Title.Value = "Example Title";

            // Alternate way to Write:

            ShellPropertyWriter propertyWriter = file.Properties.GetPropertyWriter();
            propertyWriter.WriteProperty(SystemProperties.System.Author, new string[] { "Author" });
            propertyWriter.Close();
        }
    }
}
