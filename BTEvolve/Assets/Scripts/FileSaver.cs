using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class FileSaver {

    private string directoryPath;

    private static FileSaver instance;

    private FileSaver()
    {
        directoryPath = "./SavedTrees";
    }

    public static FileSaver GetInstance()
    {
        if (instance == null)
            instance = new FileSaver();

        return instance;
    }

	public void SaveTree (N_Root tree, string fileName)
    {
        string filePath = directoryPath + "/" + fileName + ".tree";
        if (!File.Exists(filePath))
        {
            Directory.CreateDirectory(directoryPath);
            FileStream newFile = File.Create(filePath);
            newFile.Close();
        }

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(filePath, FileMode.Create);

        bf.Serialize(file, tree);
        file.Close();
        Debug.Log("Successfully saved " + filePath);
    }

    public N_Root LoadTree (string fileName)
    {
        string filePath = directoryPath + "/" + fileName + ".tree";
        if (File.Exists(filePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(filePath, FileMode.Open);
            N_Root data = (N_Root)bf.Deserialize(file);
            file.Close();

            Debug.Log("Successfully loaded " + filePath);
            return data;
        }
        else
        {
            Debug.Log("No savefile found, when trying to load.");
            return null;
        }
    }
}
