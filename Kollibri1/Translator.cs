using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;

namespace Nokk
{
    static class Translator
    {
        private static readonly string YANDEX_TRANSLATOR_API_KEY_PATH = $"{Environment.CurrentDirectory}\\YandexTranslatorAPIKey.txt";
        private static readonly string ApiKey = GetAPIKey();
        private static string GetAPIKey()
        {
            return File.ReadAllText(YANDEX_TRANSLATOR_API_KEY_PATH);
        }

        public static string IdentifyLanguage(string row)
        {
            WebRequest req = WebRequest.Create("https://translate.yandex.net/api/v1.5/tr.json/detect?"
                                  + "key=" + ApiKey
                                  + "&text=" + row
                                  + "&hint=en,ru");
            string str = "";
            Language language;

            WebResponse response = req.GetResponse();
            using (Stream s = response.GetResponseStream())
            {
                using (StreamReader r = new StreamReader(s))
                {
                    str = r.ReadToEnd();
                }
            }
            response.Close();
            language = JsonConvert.DeserializeObject<Language>(str);
            return language.lang;

        }
        public static string Translate(string s)
        {
                string lang;
                if (s.Length > 0)
                {
                    switch (IdentifyLanguage(s))
                    {
                        case "ru":
                            lang = "ru-en";
                            break;
                        case "en":
                            lang = "en-ru";
                            break;
                        default:
                            lang = "en-ru";
                            break;
                    }
                    WebRequest request = WebRequest.Create("https://translate.yandex.net/api/v1.5/tr.json/translate?"
                        + "key=" + GetAPIKey()
                        + "&text=" + s
                        + "&lang=" + lang);

                    WebResponse response = request.GetResponse();

                    using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                    {
                        string line;

                        if ((line = stream.ReadLine()) != null)
                        {
                            Translation translation = JsonConvert.DeserializeObject<Translation>(line);

                            s = "";

                            foreach (string str in translation.text)
                            {
                                s += str;
                            }
                        }
                    }

                    return s;
                }
                else
                    return "";
        }
        class Translation
        {
            public string code { get; set; }
            public string lang { get; set; }
            public string[] text { get; set; }
        }
        class Language
        {
            public string lang { get; set; }
        }
    }
}
