using Newtonsoft.Json;
using SpaceyToast.Source.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace SpaceyToast.Core.Services
{
    public class TagManifestManager
    {
        private static object Padlock { get; set; } = new object();
        private bool HasBeenUpdated { get; set; }
        private List<TagManifestData> DeserializedData { get; set; }

        private static TagManifestManager _instance;
        public static TagManifestManager Instance
        {
            get
            {
                lock (Padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new TagManifestManager();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Initializes the TagManifestManager.
        /// </summary>
        private TagManifestManager()
        {
            HasBeenUpdated = true;
            DeserializedData = new List<TagManifestData>();
        }


        /// <summary>
        /// Get manifest data list.
        /// </summary>
        /// <returns></returns>
        public async Task<List<TagManifestData>> Get()
        {
            if (!HasBeenUpdated)
            {
                return DeserializedData;
            }

            StorageFile manifestFile = await GetTagsManifestFile();

            DeserializedData = JsonConvert.DeserializeObject<List<TagManifestData>>(await FileIO.ReadTextAsync(manifestFile));

            HasBeenUpdated = false;

            return DeserializedData;
        }

        /// <summary>
        /// Get manifest data from Guid.
        /// </summary>
        /// <returns></returns>
        public async Task<TagManifestData> GetFromGuid(string guid)
        {
            try
            {
                if (guid == null)
                {
                    return null;
                }

                if (!HasBeenUpdated)
                {
                    return DeserializedData.Where(d => d.Id == guid).First();
                }

                StorageFile manifestFile = await GetTagsManifestFile();

                DeserializedData = JsonConvert.DeserializeObject<List<TagManifestData>>(await FileIO.ReadTextAsync(manifestFile));

                HasBeenUpdated = false;

                return DeserializedData.Where(d => d.Id == guid).First();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Updates the list of global tags within the app.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns></returns>
        public async Task Update(List<TagManifestData> data)
        {
            StorageFile tagFile = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync("manifest.json");
            if (tagFile == null)
            {
                tagFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("manifest.json");
                await FileIO.WriteTextAsync(tagFile, "[]");
                DeserializedData = new List<TagManifestData>();
                HasBeenUpdated = false;

                return;
            }

            await FileIO.WriteTextAsync(tagFile, JsonConvert.SerializeObject(data, Formatting.Indented));

            HasBeenUpdated = true;
        }

        public async Task UpdateTagsFromGuid(List<string> newData, string guid)
        {
            StorageFile tagFile = await GetTagsManifestFile();
            
            var list = await Get();
            int index = list.FindIndex(t => t.Id == guid);
            
            if (index == -1) throw new KeyNotFoundException();
            list[index].Tags = newData;

            await FileIO.WriteTextAsync(tagFile, JsonConvert.SerializeObject(list, Formatting.Indented));
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
