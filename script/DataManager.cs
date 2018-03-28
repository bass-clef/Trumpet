using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class DataManager<Type> {
	string fullPath, filePath;

	public DataManager(string fileName) {
		filePath = Application.persistentDataPath + "/{0}.json";
		fullPath = string.Format(filePath, fileName);
	}

	public string loadBinary() {
		BinaryFormatter bf = new BinaryFormatter();
		FileStream fs = File.Open(fullPath, FileMode.Open);
		string json = (string)bf.Deserialize(fs);
		fs.Close();
		return json;
	}
	public void saveBinary(string json) {
		BinaryFormatter bf = new BinaryFormatter();
		FileStream fs = File.Create(fullPath);
		bf.Serialize(fs, json);
		fs.Close();
	}

	public Type load() {
		string json = loadBinary();
		return JsonUtility.FromJson<Type>(json);
	}
	public void save(Type obj) {
		string json = JsonUtility.ToJson(obj);
		saveBinary(json);
	}

	public void delete() {
		File.Delete(fullPath);
	}

	public bool exist() {
		return File.Exists(fullPath);
	}
}
