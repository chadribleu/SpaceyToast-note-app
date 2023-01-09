using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace SpaceyToast.Source
{
    public class SaveDataManager<T> where T : new()
    {
        private string _fileName;
        private static object _padlock = new object();
        private static SaveDataManager<T> _instance;
        private SaveDataManager() { }

        public T SaveData { get; set; }

        public static async Task<T> GetCurrent(string filename)
        {
            Instance._fileName = filename;
            StorageFile configFile = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync(filename);
            if (configFile == null)
            {
                T saveData = new T();
                configFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(configFile, JsonConvert.SerializeObject(saveData, Formatting.None));
                return saveData;
            }
            return JsonConvert.DeserializeObject<T>(await FileIO.ReadTextAsync(configFile));
        }

        public async Task UpdateLocalFile()
        {
            StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(_fileName);
            if (file == null) return;
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(SaveData, Formatting.None));
        }

        public static SaveDataManager<T> Instance
        {
            get
            {
                lock (_padlock)
                {
                    if (_instance == null)
                    {
                        _instance = new SaveDataManager<T>();
                    }
                    return _instance;
                }
            }
        }
    }
}
