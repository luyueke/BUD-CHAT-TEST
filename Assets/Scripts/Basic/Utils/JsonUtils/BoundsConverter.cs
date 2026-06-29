using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Basic.Utils {
    public class BoundsConverter : JsonConverter {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            Bounds bounds = (Bounds)value;
            JObject jo = new JObject {
                { "center", JsonConvert.SerializeObject(bounds.center) },
                { "size", JsonConvert.SerializeObject(bounds.size) }
            };
            jo.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer) {
            try {
                JObject jo = JObject.Load(reader);

                Vector3 center = JsonConvert.DeserializeObject<Vector3>((string)jo["center"] ?? string.Empty);
                Vector3 size = JsonConvert.DeserializeObject<Vector3>((string)jo["size"] ?? string.Empty);
                Bounds bounds = new Bounds(center, size);
                return bounds;
            } catch (Exception e) {
                Debug.LogException(e);
                return new Bounds();
            }

        }

        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Bounds);
        }
    }
}
