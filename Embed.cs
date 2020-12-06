using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsTeamsBot
{
    [JsonObject]
    public class Embed
    {
        public Author author;
        public string title;
        public string url;
        public string description;
        public UInt32 color;
        public List<Field> fields = new List<Field>();
        public Thumbnail thumbnail;
        public Image image;
        public Footer footer;
        public DateTime timestamp;
    }
    [JsonObject]
    public class Author
    {
        public string name;
        public string url;
        public string icon_url;
    }

    [JsonObject]
    public class Field
    {
        public string name;
        public string value;
        public bool inline;
    }
    [JsonObject]
    public class Thumbnail
    {
        public string url;
    }
    [JsonObject]
    public class Image
    {
        public string url;
    }
    [JsonObject]
    public class Footer
    {
        public string text;
        public string icon_url;
    }
}
