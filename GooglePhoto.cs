using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetFilePropertiesFromGoogle
{
    class GooglePhoto
    {
        Dictionary<string, string> pictures = new Dictionary<string, string>();
        UserCredential credential;
        Uri baseGooglePhotoURL = new Uri("https://photoslibrary.googleapis.com/v1/mediaItems/");
        HttpClientHandler handler;
        HttpClient httpClient;

        public GooglePhoto()
        {
            //Create Http client
            handler = new HttpClientHandler();
            httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

            httpClient.BaseAddress = baseGooglePhotoURL;
        }

        public object GetGooglePhotoJSON(string jsonFile)
        {
            
            FileStream fs = new FileStream(jsonFile,FileMode.Open);

            StreamReader reader = new StreamReader(fs, Encoding.UTF8);

            var responseObject = JsonConvert.DeserializeObject<GooglePhotoMetadata>(reader.ReadToEnd());

            return responseObject;
        }
        public void Authenticate(string UserName, string credentialsFile)
        {
            string credPath = @"C:\Temp\";
            
            string[] scopes = {
                "https://www.googleapis.com/auth/photoslibrary.sharing",
                "https://www.googleapis.com/auth/photoslibrary.readonly",
                "https://www.googleapis.com/auth/photoslibrary.edit.appcreateddata"            
            };

            using (var stream = new FileStream(credentialsFile, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    UserName,
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }
        }
      
        public HttpResponseMessage HttpCall(HttpMethod method, string query)
        {
            if (credential.Token.IsExpired(Google.Apis.Util.SystemClock.Default))
            {
                var refreshResult = credential.RefreshTokenAsync(CancellationToken.None).Result;
            }
            
            var authorization = AuthenticationHeaderValue.Parse(credential.Token.TokenType + " " + credential.Token.AccessToken);
            httpClient.DefaultRequestHeaders.Authorization = authorization;

            Uri uri = new Uri(query);
            
            HttpRequestMessage request = new HttpRequestMessage(method,uri);
            request.Headers.Add("Authorization", credential.Token.TokenType + " " + credential.Token.AccessToken);

            var task = httpClient.GetAsync(uri);
            task.Wait();

            var response = task.Result;
            
            return response;
        }

        public void SetDescription(string id, string description)
        {
            string query = "https://photoslibrary.googleapis.com/v1/mediaItems/"+id+ "?description";
            //HttpCall("PATCH", query);
        }

        /// <summary>
        ///  Check photos in Google Photos against local photo collection
        /// </summary>
        /// <param name="localPhotos"></param>
        /// <returns>Collection with found local photos</returns>
        public Dictionary<DateTime, FileInfo> CheckPhotos(DirInfo dirInfo)
        {
            Dictionary<DateTime, FileInfo> foundPics = new Dictionary<DateTime, FileInfo>();
            string nextPage = "-";
            string baseAddress = "https://photoslibrary.googleapis.com/v1/mediaItems?pageSize=100";
            string query = "";
            
            clsResponseRootObject responseObject = new clsResponseRootObject();
            Dictionary<string, List<FileInfo>> dateSortedPics;
            try
            {
                Task fileMoveTask = null;
                while (nextPage?.Length > 0)
                {
                    if (nextPage.Length == 1)
                    {
                        query = baseAddress;
                    }
                    else
                    {
                        query = baseAddress + "&pageToken=" + nextPage;
                    }
                    var response = HttpCall(new HttpMethod("get"), query);
                    var task = response.Content.ReadAsStringAsync();
                    if (fileMoveTask != null)
                    {
                        fileMoveTask.Wait();
                        if (fileMoveTask.IsFaulted)
                        {
                            Console.WriteLine("Error occurred in filemove: " + fileMoveTask.Exception.Message);
                        }
                    }
                    task.Wait();
                    responseObject = JsonConvert.DeserializeObject<clsResponseRootObject>(task.Result);
                    if (responseObject != null)
                    {
                        nextPage = responseObject.nextPageToken;
                        if (responseObject.mediaItems != null && responseObject.mediaItems.Count > 0)
                        {
                            Console.WriteLine("------------------------Retrieving media files--------------------------------");
                            dateSortedPics = new Dictionary<string, List<FileInfo>>();
                            var localPhotos = dirInfo.pictures;
                            foreach (var item in responseObject.mediaItems)
                            {
                                //Create file object
                                FileInfo file = new FileInfo(item);
                                var googleCreationDate = file.createDate;
                                
                                /*
                                if (item.filename.StartsWith("IMAG00"))
                                    Console.WriteLine(item.filename);
                                */
                                if(localPhotos.TryGetValue(item.filename,out var localPic))
                                {
                                    
                                    localPic.AddGoogleTime(googleCreationDate);
                                    /*
                                    ShellPropertyWriter propertyWriter = localPic.GetPropertyWriter();
                                    propertyWriter.WriteProperty(SystemProperties.System.Author, new string[] { "Author" });
                                    propertyWriter.Close();
                                    if (Math.Abs(DateTime.Compare((DateTime)localCreationDate,googleCreationDate))>2)
                                    {
                                        Console.WriteLine($"Date, Google:{googleCreationDate} local:{localCreationDate}");
                                        localPic.System.Photo.DateTaken.Value = (DateTime?)googleCreationDate;
                                    }
                                    */
                                    
                                    
                                    
                                    var month = string.Format("{0:yyyy-MM}", googleCreationDate);
                                    Console.WriteLine($"Date for {localPic.SourceFile}: {localPic.createDate}, on google:{googleCreationDate}");

                                    //Add to SortedDate
                                    if (dateSortedPics.TryGetValue(month, out var picList))
                                    {
                                        if (picList.Contains(localPic))
                                        {
                                            Console.WriteLine($"Weird - {localPic.SourceFile} already in list");
                                        }
                                        else
                                        {
                                            picList.Add(localPic);

                                        }
                                        dateSortedPics[month] = picList;
                                    }
                                    else
                                    {
                                        picList = new List<FileInfo>();
                                        picList.Add(localPic);
                                        dateSortedPics.Add(month, picList);
                                    }
                                }
                                /*
                                response = HttpCall(new HttpMethod("GET"), "https://photoslibrary.googleapis.com/v1/mediaItems/" + item.id);

                                using (Stream contentStream = new MemoryStream())
                                {
                                    StreamReader contentReader = new StreamReader(contentStream, Encoding.UTF8);

                                    var mediaContent = JsonConvert.DeserializeObject(contentReader.ReadToEnd());
                                }

                                SetDescription(item.id, "Duplicate");

                                if (!pictures.TryAdd(item.filename, item.id))
                                {
                                    Console.WriteLine($"{item.filename} already exists!");
                                }
                                */
                                //Console.WriteLine(string.Format("ID:{0}, Filename:{1}, MimeType:{2}, Created:{3}", item.id, item.filename, item.mimeType, item.mediaMetadata.creationTime));
                            }
                            if(dateSortedPics.Count>0)
                                fileMoveTask = dirInfo.MoveFiles(dateSortedPics);
                        }
                        responseObject = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured: " + ex.Message);
            }

            return foundPics;
        }

    }
}

