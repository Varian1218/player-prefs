using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class PlayerPrefsEditorWindow : EditorWindow
    {
        private string[] _keys;
        private string _value;

        private static string[] GetKeys(string name)
        {
            using var rootKey = Registry.CurrentUser.OpenSubKey(name);
            if (rootKey == null) return Array.Empty<string>();
            var ansi = Encoding.GetEncoding(1252);
            var names = rootKey.GetValueNames();
            var utf8 = Encoding.UTF8;
            rootKey.Close();
            return names
                .Select(v => utf8.GetString(ansi.GetBytes(v[..v.LastIndexOf("_h", StringComparison.Ordinal)])))
                .Where(v => !v.StartsWith("unity.") && !v.StartsWith("UnityGraphicsQuality"))
                .ToArray();
        }

        private static object GetValue(string key)
        {
            const string error = "error";
            var sValue = PlayerPrefs.GetString(key, error);
            if (sValue != error) return sValue;
            var fValue = PlayerPrefs.GetFloat(key, float.NaN);
            if (!float.IsNaN(fValue)) return fValue;
            var iValue = PlayerPrefs.GetInt(key, int.MinValue);
            if (iValue != int.MinValue) return iValue;
            throw new ArgumentException();
        }

        private void OnEnable()
        {
            _keys = GetKeys(@$"SOFTWARE\Unity\UnityEditor\{PlayerSettings.companyName}\{PlayerSettings.productName}");
            _value = JsonConvert.SerializeObject(_keys.ToDictionary(it => it, GetValue), Formatting.Indented);
        }

        private void OnGUI()
        {
            _value = EditorGUILayout.TextArea(_value);
            if (GUILayout.Button("Save"))
            {
                var value = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(_value);
                foreach (var (key, v) in value)
                {
                    switch (v.Type)
                    {
                        case JTokenType.Integer:
                            PlayerPrefs.SetInt(key, v.ToObject<int>());
                            break;
                        case JTokenType.Float:
                            PlayerPrefs.SetFloat(key, v.ToObject<float>());
                            break;
                        case JTokenType.String:
                            PlayerPrefs.SetString(key, v.ToObject<string>());
                            break;
                        case JTokenType.None:
                        case JTokenType.Object:
                        case JTokenType.Array:
                        case JTokenType.Constructor:
                        case JTokenType.Property:
                        case JTokenType.Comment:
                        case JTokenType.Boolean:
                        case JTokenType.Null:
                        case JTokenType.Undefined:
                        case JTokenType.Date:
                        case JTokenType.Raw:
                        case JTokenType.Bytes:
                        case JTokenType.Guid:
                        case JTokenType.Uri:
                        case JTokenType.TimeSpan:
                            PlayerPrefs.SetString(key, v.ToString());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                foreach (var key in _keys.Where(it => !value.ContainsKey(it)))
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }
        }

        [MenuItem("Player Prefs/Edit")]
        public static void OpenWindow()
        {
            var window = (PlayerPrefsEditorWindow)GetWindow(typeof(PlayerPrefsEditorWindow));
            window.titleContent = new GUIContent("Player Prefs");
            window.Show();
        }
    }
}