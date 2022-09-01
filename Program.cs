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
            var googlePhoto=new GooglePhoto();
            
            googlePhoto.Authenticate("tommy.ekh@google.com", @"C:\LabDev\SetFilePropertiesFromGoogle\credentials.json");

            var photos = googlePhoto.GetPhotos();

            var dirInfo = new DirInfo();
        
            dirInfo.PopulateDictionary(@"C:\Temp\Takeout\Google Foto");
            //dirInfo.PopulateDictionary(@"V:\Tmp\Takeout\Google Foto");

            string filePath = @"C:\Temp\Takeout\Google Foto\2008-04-26\IMAG0095.jpg";
            string metadatafile= @"C:\Temp\Takeout\Google Foto\2008-04-26\IMAG0095.jpg.json";


            var metadata = googlePhoto.GetGooglePhotoJSON(metadatafile);
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
