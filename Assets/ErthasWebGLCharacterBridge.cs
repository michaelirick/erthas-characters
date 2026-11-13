using System;
using UnityEngine;
using UnityEngine.Scripting;

/// <summary>
/// Receives character JSON from the Next.js host through Unity's WebGL
/// SendMessage API. The creator can subscribe to CharacterDataReceived when
/// the Next app begins sending its saved character format.
/// </summary>
[Preserve]
public sealed class ErthasWebGLCharacterBridge : MonoBehaviour
{
    public static event Action<string> CharacterDataReceived;

    public string LatestCharacterJson { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBridge()
    {
        if (GameObject.Find(nameof(ErthasWebGLCharacterBridge)) != null) return;
        var bridge = new GameObject(nameof(ErthasWebGLCharacterBridge));
        bridge.AddComponent<ErthasWebGLCharacterBridge>();
        DontDestroyOnLoad(bridge);
    }

    // Called by createUnityInstance(...).SendMessage(...) in the Next host.
    [Preserve]
    public void ReceiveNextAppData(string json)
    {
        LatestCharacterJson = json;
        CharacterDataReceived?.Invoke(json);
        Debug.Log("Received character data from the Next.js host.");
    }
}
