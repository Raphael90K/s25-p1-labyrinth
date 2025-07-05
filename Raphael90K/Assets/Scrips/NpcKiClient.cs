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
            "You are an NPC in a video game.\n" +
            "You receive your status and the status of the player.\n" +
            "If the player reaches you, you lose.\n"+
            "Do Some wink and idle, even if the player is in sight.\n"+
            "You are gentle, but if the player approaches, you can get scared or angry.\n" +
            "Your Answer looks like:\n" +
            "action: ... " +
            "direction: ... " +
            "emotion: ... " +
            "You do not reply more than this!!!!!\n" +
            "valid actions: idle, walk, crouch, wink, run\n" +
            "valid directions: 0 - 360\n" +
            "valid emotions: scared, angry, happy, calm\n"+
            "if direction and angleToPlayer are similar, you run towards the Player. ";

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
        Debug.Log("Answer: " + rawJson);
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
        string action = TryFindValue(response, "action", new[] { "idle", "walk", "crouch", "wink", "run" });
        string direction = TryFindFloatBetween(response, "direction", 0f, 360f).ToString("F1"); 
        string emotion = TryFindValue(response, "emotion", new[] { "scared", "angry", "happy", "calm" });

        return (action, direction, emotion);
    }
    
    float TryFindFloatBetween(string text, string key, float min, float max)
    {
        // Beispiel: "direction: 123.45" oder "direction - 278"
        Match match = Regex.Match(text, $@"{key}\s*[:\-]?\s*([0-9]+(\.[0-9]+)?)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (float.TryParse(match.Groups[1].Value, out float result))
            {
                if (result >= min && result <= max)
                    return result;
            }
        }

        // Plan B: finde irgendeine gültige Zahl zwischen min und max
        MatchCollection numbers = Regex.Matches(text, @"([0-9]+(\.[0-9]+)?)");
        foreach (Match num in numbers)
        {
            if (float.TryParse(num.Value, out float value))
            {
                if (value >= min && value <= max)
                    return value;
            }
        }

        Debug.LogWarning($"Konnte keine gültige {key}-Gradzahl finden. Fallback auf 0.");
        return 0f; // fallback
    }
    
    string TryFindValue(string text, string key, string[] allowedValues)
    {
        // Alles klein für robusten Vergleich
        text = text.ToLower();

        foreach (string valid in allowedValues)
        {
            // Regex sucht nach: key gefolgt von beliebigen Zeichen, dann gültiger Wert
            // z.B. "action: run", "action - run!", "action is run?"
            string pattern = $@"{key}\s*[:\-]?\s*[^a-zA-Z0-9]?(?<{key}>{valid})\b";

            Match match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[key].Value;
            }
        }

        // Als Plan B: nach gültigem Wert im gesamten Text suchen
        foreach (string valid in allowedValues)
        {
            if (Regex.IsMatch(text, $@"\b{valid}\b", RegexOptions.IgnoreCase))
            {
                return valid;
            }
        }

        // Fallback
        return key switch
        {
            "action" => "idle",
            "direction" => "north",
            "emotion" => "calm",
            _ => ""
        };
    }
    
}
