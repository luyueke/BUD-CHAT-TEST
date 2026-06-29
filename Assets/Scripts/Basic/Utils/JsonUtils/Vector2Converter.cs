using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Basic.Utils {
    public class Vector2Converter: JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Vector2 vector = (Vector2)value;
            writer.WriteValue($"{vector.x.ToString("f4", CultureInfo.InvariantCulture)},{vector.y.ToString("f4", CultureInfo.InvariantCulture)}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {

            try {
                if (reader.TokenType == JsonToken.String) {
                    return DataUtil.DeSerializeVector2(reader.Value.ToString());
                } else {
                    JObject jo = JObject.Load(reader);
                    return new Vector2((float)jo["x"], (float)jo["y"]);
                }


            } catch (Exception e) {
                Debug.LogException(e);
                return Vector3.zero;
            }


        }
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Vector2);
        }
    }
}
