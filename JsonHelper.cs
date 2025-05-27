using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public static class JsonHelper
{
    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }

    [Serializable]
    private class Entry<T>
    {
        public string key;
        public T[] value;
    }

    [Serializable]
    private class WrapperDict<T>
    {
        public List<Entry<T>> entries;

        public Dictionary<string, T[]> ToDictionary()
        {
            return entries.ToDictionary(e => e.key, e => e.value);
        }
    }

    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(newJson).array;
    }

    public static T[] FromJsonWithKey<T>(string json, string key)
    {
        string arrayContent = ExtractJsonArray(json, key);
        if (string.IsNullOrEmpty(arrayContent))
        {
            Debug.LogError($"⚠️ Chiave '{key}' non trovata o array malformato.");
            return null;
        }

        string newJson = "{ \"array\": " + arrayContent + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    public static string ToJson<T>(T[] array, bool prettyPrint = false)
    {
        Wrapper<T> wrapper = new Wrapper<T> { array = array };
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    public static string ToJsonWithKey<T>(T[] array, string key, bool prettyPrint = false)
    {
        return JsonUtility.ToJson(new Wrapper<T> { array = array }, prettyPrint).Replace("array", key);
    }

    public static Dictionary<string, T[]> FromJsonDict<T>(string json)
    {
        try
        {
            var dict = new Dictionary<string, T[]>();
            var topLevel = Regex.Matches(json, "\\\"(.*?)\\\":\\[.*?\\](?=,\\\"|}$)", RegexOptions.Singleline);

            foreach (Match match in topLevel)
            {
                string key = match.Groups[1].Value;
                string arrayJson = match.Value.Substring(match.Value.IndexOf('['));
                var array = FromJson<T>(arrayJson);
                dict.Add(key, array);
            }

            return dict;
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Errore nel parsing del dizionario JSON: " + e.Message);
            return null;
        }
    }

    private static string ExtractJsonArray(string json, string key)
    {
        int keyIndex = json.IndexOf($"\"{key}\"");
        if (keyIndex == -1)
        {
            Debug.LogError($"Chiave '{key}' non trovata nel JSON.");
            return null;
        }

        int startIndex = json.IndexOf('[', keyIndex);
        int endIndex = json.IndexOf(']', startIndex);
        if (startIndex == -1 || endIndex == -1)
        {
            Debug.LogError($"Array non valido nella chiave '{key}'.");
            return null;
        }

        return json.Substring(startIndex, endIndex - startIndex + 1);
    }
}
