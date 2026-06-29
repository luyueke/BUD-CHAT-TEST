using System;
using Newtonsoft.Json;

namespace Basic.Utils {
    public class StringObjectConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,//空值处理 忽略序列化
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            writer.WriteValue(JsonConvert.SerializeObject(value, settings));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            var saveDataObj = JsonConvert.DeserializeObject(reader.Value as string ?? string.Empty, objectType);
            return saveDataObj;
        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(string);
        }
    }
}
