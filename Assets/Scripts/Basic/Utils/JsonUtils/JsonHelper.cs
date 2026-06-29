using Newtonsoft.Json;

namespace Basic.Utils {
    public class JsonHelper {
        public static void Init() {
            JsonConvert.DefaultSettings = GetSetting;
        }

        private static JsonSerializerSettings GetSetting() {
            var settings = new JsonSerializerSettings {
                Converters = new JsonConverter[] {
                    new Vector3Converter(),
                    new Vector2Converter(),
                    new BoundsConverter(),
                },
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            return settings;
        }
    }
}
