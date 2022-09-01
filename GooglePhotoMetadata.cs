using System;
using System.Collections.Generic;
using System.Text;

namespace SetFilePropertiesFromGoogle
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class CreationTime
    {
        public string timestamp { get; set; }
        public string formatted { get; set; }
    }

    public class GeoData
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double altitude { get; set; }
        public double latitudeSpan { get; set; }
        public double longitudeSpan { get; set; }
    }

    public class GeoDataExif
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public double altitude { get; set; }
        public double latitudeSpan { get; set; }
        public double longitudeSpan { get; set; }
    }

    public class ModificationTime
    {
        public string timestamp { get; set; }
        public string formatted { get; set; }
    }

    public class PhotoTakenTime
    {
        public string timestamp { get; set; }
        public string formatted { get; set; }
    }

    public class GooglePhotoMetadata
    {
        public string title { get; set; }
        public string description { get; set; }
        public string imageViews { get; set; }
        public CreationTime creationTime { get; set; }
        public ModificationTime modificationTime { get; set; }
        public GeoData geoData { get; set; }
        public GeoDataExif geoDataExif { get; set; }
        public PhotoTakenTime photoTakenTime { get; set; }
    }


}
