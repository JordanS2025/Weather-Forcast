using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

public class DisplayGraph : MonoBehaviour
{
    public RawImage graphDisplay;
    // Update the filePath if you change the location; note the "file:///" prefix is needed for local files
    public string imagePath = "file:///" + Application.dataPath + "/GraphImages/Graph.png";

    void Start()
    {
        StartCoroutine(LoadGraphImage());
    }

    IEnumerator LoadGraphImage()
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imagePath);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            graphDisplay.texture = texture;
        }
        else
        {
            Debug.LogError("Failed to load image: " + www.error);
        }
    }
}
