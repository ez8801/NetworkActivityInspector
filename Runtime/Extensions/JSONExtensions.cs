using System.IO;
using Newtonsoft.Json;

namespace EZ.Json.Extensions
{
    public static class JSONExtensions
    {
        public static string ToPrettyJSONify(this string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                string indented = string.Empty;
                using (var reader = new StringReader(json))
                {
                    using (var writter = new StringWriter())
                    {
                        var jsonReader = new JsonTextReader(reader);
                        var jsonWritter = new JsonTextWriter(writter)
                        {
                            Formatting = Formatting.Indented
                        };
                        jsonWritter.WriteToken(jsonReader);
                        indented = writter.ToString();
                    }
                }
                return indented;
            }
            return string.Empty;
        }
    }
}