using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text.RegularExpressions;

public class NpcKIClient : MonoBehaviour
{
    [Header("KI Server Einstellungen")] public string apiUrl = "\"http://136.199.51.131:1234/v1/chat/completions";
    public string modelName = "deepseek-coder-v2-lite-instruct";

    public Action<string, string, string> OnKIResponse; // action, direction, emotion

    public void RequestKIResponse(string prompt)
    {
        StartCoroutine(SendPrompt(prompt));
    }

    IEnumerator SendPrompt(string userPrompt)
    {
        string systemPrompt = @"Du bist ein Computergegner in einem Videospiel. Du erhälts immer Anweisungen
                und Statusinformationen. Deine Antwort soll NUR folgendes Format haben (Beispiel) action run
                direction - east; emotion scared. Es stehen folgende Aktionen zur Verfügung: idle, walk, crouch, wink
                Es gibt folgende Richtungen north, south, west, east; Es gibt folgende Emotionen: scared, happy, calm, angry
                ";
        
        string jsonBody = 
            $@"
                {{
                  ""model"": ""{modelName}"",
                  ""messages"": [
                    {{""role"": ""system"", ""content"": ""{EscapeJson(systemPrompt)}""}},
                    {{""role"": ""user"", ""content"": ""{EscapeJson(userPrompt)}""}}
                  ],
                  ""temperature"": 0.7
                }}";
        using UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer lm-studio");
        
        Debug.Log("Gesendetes JSON:\n" + jsonBody);

        
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

        // Robust analysieren
        var result = ParseFlexible(content);
        Debug.Log($"KI → action: {result.action}, direction: {result.direction}, emotion: {result.emotion}");

        OnKIResponse?.Invoke(result.action, result.direction, result.emotion);
    }

    // Robust extrahieren aus JSON-Antwort
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
        catch
        {
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
        // Suche Zeile mit z. B. "action: crawl"
        Match match = Regex.Match(text, $"{key}\\s*:\\s*(\\w+)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string value = match.Groups[1].Value.ToLower();
            foreach (var valid in allowedValues)
            {
                if (value.Contains(valid)) return valid;
            }
        }

        // Fallback-Werte
        switch (key)
        {
            case "action": return "idle";
            case "direction": return "north";
            case "emotion": return "calm";
            default: return "";
        }
    }

    string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}