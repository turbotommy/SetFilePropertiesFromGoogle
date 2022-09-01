using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

using System.IO;
using System.Net;
using System.Threading;
using System.Net.Http;

namespace SetFilePropertiesFromGoogle
{
    class GooglePhoto
    {
        UserCredential credential;
        string baseGooglePhotoURL = "https://photoslibrary.googleapis.com/v1/mediaItems/";
        HttpClientHandler handler;
        HttpClient httpClient;

        public GooglePhoto()
        {
            //Create Http client
            handler = new HttpClientHandler();
            httpClient = new HttpClient(handler);
            
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
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    UserName,
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }
        }
      
        public HttpWebResponse HttpCall(string method, string query)
        {
            if (credential.Token.IsExpired(Google.Apis.Util.SystemClock.Default))
            {
                var refreshResult = credential.RefreshTokenAsync(CancellationToken.None).Result;
            }

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(query);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Headers.Add("Authorization:" + credential.Token.TokenType + " " + credential.Token.AccessToken);

            httpWebRequest.Method = method;
            if (httpWebRequest.Method == "PATCH")
            {
                
            }
            HttpWebResponse response = httpWebRequest.GetResponse() as HttpWebResponse;

            return response;
        }

        public void SetDescription(string id, string description)
        {
            string query = "https://photoslibrary.googleapis.com/v1/mediaItems/"+id+ "?description";
            HttpCall("PATCH", query);
        }
        public Dictionary<string, string> GetPhotos()
        {
            Dictionary<string, string> pictures = new Dictionary<string, string>();
            string nextPage = "-";
            string baseAddress = "https://photoslibrary.googleapis.com/v1/mediaItems?pageSize=100";
            string query = "";
            
            clsResponseRootObject responseObject = new clsResponseRootObject();

            try
            {
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
                    var response = HttpCall("GET", query);

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);

                        responseObject = JsonConvert.DeserializeObject<clsResponseRootObject>(reader.ReadToEnd());

                        if (responseObject != null)
                        {
                            nextPage = responseObject.nextPageToken;
                            if (responseObject.mediaItems != null && responseObject.mediaItems.Count > 0)
                            {
                                Console.WriteLine("------------------------Retrieving media files--------------------------------");
                                foreach (var item in responseObject.mediaItems)
                                {
                                    response = HttpCall("GET", "https://photoslibrary.googleapis.com/v1/mediaItems/" + item.id);
                                    using (Stream contentStream = response.GetResponseStream())
                                    {
                                        StreamReader contentReader = new StreamReader(contentStream, Encoding.UTF8);

                                        var mediaContent = JsonConvert.DeserializeObject(contentReader.ReadToEnd());
                                    }
                                        SetDescription(item.id, "Duplicate");
                                    if(!pictures.TryAdd(item.filename, item.id)) {
                                        Console.WriteLine($"{item.filename} already exists!");
                                    }
                                    //Console.WriteLine(string.Format("ID:{0}, Filename:{1}, MimeType:{2}, Created:{3}", item.id, item.filename, item.mimeType, item.mediaMetadata.creationTime));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured: " + ex.Message);
            }


            return pictures;
        }
    }
}

