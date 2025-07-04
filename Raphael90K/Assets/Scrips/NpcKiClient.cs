using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class NpcKIClient : MonoBehaviour
{
    [Header("KI Server Einstellungen")]
    public string apiUrl = "http://136.199.51.131:1234/v1/chat/completions"; // Optional: https verwenden
    public string modelName = "deepseek-coder-v2-lite-instruct";

    public Action<string, string, string> OnKIResponse; // action, direction, emotion

    public void RequestKIResponse(string prompt)
    {
        StartCoroutine(SendPrompt(prompt));
    }

    IEnumerator SendPrompt(string userPrompt)
    {
        string systemPrompt =
            "Du bist ein Computergegner in einem Videospiel.\n" +
            "Du erhälst Anweisungen und Statusinformationen.\n" +
            "Antwortformat (nur das, nichts anderes):\n" +
            "action: walk\n" +
            "direction: east\n" +
            "emotion: scared\n" +
            "Gültige Aktionen: idle, walk, crouch, wink\n" +
            "Gültige Richtungen: north, south, east, west\n" +
            "Gültige Emotionen: scared, angry, happy, calm";

        string jsonBody = BuildJsonRequest(systemPrompt, userPrompt);
        Debug.Log("Gesendetes JSON:\n" + jsonBody);

        using UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer lm-studio"); // Entferne diese Zeile, wenn dein Server keinen Token verlangt

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Fehler bei KI-Anfrage: " + request.error);
            yield break;
        }

        string rawJson = request.downloadHandler.text;
        string content = TryExtractContent(rawJson);

        if (string.IsNullOrEmpty(content))
        {
            Debug.LogWarning("Antwort enthält kein lesbares Content-Feld.");
            yield break;
        }

        var result = ParseFlexible(content);
        Debug.Log($"KI → action: {result.action}, direction: {result.direction}, emotion: {result.emotion}");

        OnKIResponse?.Invoke(result.action, result.direction, result.emotion);
    }

    string BuildJsonRequest(string systemPrompt, string userPrompt)
    {
        string sys = EscapeJson(systemPrompt);
        string usr = EscapeJson(userPrompt);

        return $@"{{
  ""model"": ""{modelName}"",
  ""messages"": [
    {{""role"": ""system"", ""content"": ""{sys}""}},
    {{""role"": ""user"", ""content"": ""{usr}""}}
  ],
  ""temperature"": 0.7
}}";
    }

    string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", ""); // Falls CR vorhanden ist
    }

    string TryExtractContent(string json)
    {
        try
        {
            Match match = Regex.Match(json, @"""content"":\s*""(.*?)""", RegexOptions.Singleline);
            if (match.Success)
            {
                string content = match.Groups[1].Value;
                return content.Replace("\\n", "\n").Replace("\\\"", "\"").Trim();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Fehler beim Parsen der Antwort: " + e.Message);
        }

        return null;
    }

    (string action, string direction, string emotion) ParseFlexible(string response)
    {
        string action = TryFindValue(response, "action", new[] { "idle", "walk", "crouch", "wink" });
        string direction = TryFindValue(response, "direction", new[] { "north", "south", "east", "west" });
        string emotion = TryFindValue(response, "emotion", new[] { "scared", "angry", "happy", "calm" });

        return (action, direction, emotion);
    }

    string TryFindValue(string text, string key, string[] allowedValues)
    {
        Match match = Regex.Match(text, $"{key}\\s*[:\\-]?\\s*(\\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string value = match.Groups[1].Value.ToLower();
            foreach (var valid in allowedValues)
            {
                if (value.Contains(valid)) return valid;
            }
        }

        // Fallback-Werte
        return key switch
        {
            "action" => "idle",
            "direction" => "north",
            "emotion" => "calm",
            _ => ""
        };
    }
}
