using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SpaceyToast.Source.User
{
    public class TagManifestData
    {
        [JsonProperty("guid")]
        public string Id { get; set; }

        [JsonProperty("datetime")]
        public DateTime DateTime { get; set; }

        [JsonProperty("board_path")]
        public string BoardPath { get; set; }

        [JsonProperty("thumbnail_path")]
        public string ThumbnailPath { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        public TagManifestData(string guid, DateTime dateTime, string path, string thumbnailUri, List<string> tags)
        {
            Id = guid;
            DateTime = dateTime;
            BoardPath = path;
            ThumbnailPath = thumbnailUri;
            Tags = tags;
        }
    }
}
