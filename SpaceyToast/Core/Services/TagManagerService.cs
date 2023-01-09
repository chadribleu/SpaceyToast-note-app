using Newtonsoft.Json;
using SpaceyToast.Source.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace SpaceyToast.Core.Services
{
    public class TagManagerService
    {
        private static object Padlock { get; set; } = new object();
        private bool HasBeenUpdated { get; set; }
        private List<string> DeserializedTags { get; set; }

        private static TagManagerService _instance;
        public static TagManagerService Instance
        {
            get
            {
                lock (Padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new TagManagerService();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Initializes the TagManagerService.
        /// </summary>
        private TagManagerService()
        {
            HasBeenUpdated = true;
            DeserializedTags = new List<string>();
        }

        /// <summary>
        /// Get the list of global tags.
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetGlobalTags()
        {
            if (!HasBeenUpdated)
            {
                return DeserializedTags;
            }

            StorageFile tagFile = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync("tags.json");
            if (tagFile == null)
            {
                tagFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("tags.json");
                await FileIO.WriteTextAsync(tagFile, "[]");
            }

            DeserializedTags = JsonConvert.DeserializeObject<List<string>>(await FileIO.ReadTextAsync(tagFile));

            HasBeenUpdated = false;

            return DeserializedTags;
        }

        /// <summary>
        /// Updates the list of global tags within the app.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public async Task UpdateGlobalTags(List<string> tags)
        {
            StorageFile tagFile = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync("tags.json");
            if (tagFile == null)
            {
                tagFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("tags.json");
                await FileIO.WriteTextAsync(tagFile, "[]");
                DeserializedTags = new List<string>();
                HasBeenUpdated = false;

                return;
            }

            await FileIO.WriteTextAsync(tagFile, JsonConvert.SerializeObject(tags, Formatting.Indented));

            StorageFile tagManifestFile = await GetTagsManifestFile();
            List<TagManifestData> manifestDataList = JsonConvert.DeserializeObject<List<TagManifestData>>(await FileIO.ReadTextAsync(tagManifestFile));

            for (int i = 0; i < manifestDataList.Count; i++)
            {
                manifestDataList[i].Tags = manifestDataList[i].Tags.Where(t =>
                {
                    if (!tags.Contains(t))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }).ToList();
            }

            await FileIO.WriteTextAsync(tagManifestFile, JsonConvert.SerializeObject(manifestDataList));

            HasBeenUpdated = true;
        }

        /// <summary>
        /// Get or create the tags manifest file.
        /// </summary>
        /// <returns></returns>
        private async Task<StorageFile> GetTagsManifestFile()
        {
            StorageFile manifest = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync("manifest.json");
            if (manifest == null)
            {
                manifest = await ApplicationData.Current.LocalFolder.CreateFileAsync("manifest.json");
                await FileIO.WriteTextAsync(manifest, "[]");
            }

            return manifest;
        }
    }
}
