using Godot;
using System;
using System.IO;
using System.Text;

namespace SFS
{
    public class DataSaver
    {
        static private readonly string kDataPath = "My Games/SFS/";

        public static void Save(Variant data, string fileName)
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(path, kDataPath);
            path = Path.Combine(path, fileName + ".sav");

            string jsonText = Json.Stringify(data);
            byte[] jsonByte = Encoding.ASCII.GetBytes(jsonText);

            //Create Directory if it does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));

            Debug.Print(path);

            try
            {
                File.WriteAllBytes(path, jsonByte);
                Debug.Print("Saved data to: " + path.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Debug.PrintWarning("Failed To save data to: " + path.Replace("/", "\\"));
                Debug.PrintWarning("Error: " + e.Message);
            }
        }

        public static Variant Load(string fileName)
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            path = Path.Combine(path, kDataPath);
            path = Path.Combine(path, fileName + ".sav");

            //Exit if Directory or File does not exist
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Debug.PrintWarning("Load directory does not exist");
                return default;
            }

            if (!File.Exists(path))
            {
                Debug.Print("Load file does not exist");
                return default;
            }

            //Load saved Json
            byte[] jsonByte = null;
            try
            {
                jsonByte = File.ReadAllBytes(path);
                Debug.Print("Loaded data from: " + path.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Debug.PrintWarning("Failed To load data from: " + path.Replace("/", "\\"));
                Debug.PrintWarning("Error: " + e.Message);
            }

            string jsonData = Encoding.ASCII.GetString(jsonByte);

            Variant result = Json.FromNative(jsonData);

            return result;
        }

    }
}
