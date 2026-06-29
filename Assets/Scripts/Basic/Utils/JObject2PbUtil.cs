using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;
namespace Basic.Utils
{
public static class JObject2PbUtil
{
    /// <summary>
    /// 清理 JObject 使其字段与 proto message 匹配，缺失字段补默认值，多余字段删除
    /// </summary>
    /// <param name="jObject">原始 JObject</param>
    /// <param name="message">proto 消息实例（用于类型推断）</param>
    /// <returns>可安全 ParseJson 的 JObject</returns>
    public static JObject CleanUnknownFields(JObject jObject, IMessage message)
    {
        if (jObject == null || message == null) return null;
        var cleanedObject = new JObject();
        var descriptor = message.Descriptor;
        var fields = descriptor.Fields.InDeclarationOrder();
        foreach (var field in fields)
        {
            var fieldName = field.Name;
            JToken value = null;
            if (jObject.TryGetValue(fieldName, out value))
            {
                try
                {
                    // 处理 null 值或 Repeated 字段的特殊情况
                    if (value == null || value.Type == JTokenType.Null)
                    {
                        cleanedObject[fieldName] = GetDefaultValueForField(field);
                        continue;
                    }
                    
                    // 嵌套 message
                    if (field.FieldType == FieldType.Message && value is JObject nestedObj)
                    {
                        var nestedMsg = GetDefaultMessageInstance(field.MessageType);
                        cleanedObject[fieldName] = CleanUnknownFields(nestedObj, nestedMsg);
                    }
                    // repeated 嵌套 message
                    else if (field.IsRepeated && value is JArray arr && field.FieldType == FieldType.Message && field.MessageType != null)
                    {
                        var arrClean = new JArray();
                        foreach (var item in arr)
                        {
                            if (item == null)
                            {
                                // 跳过 null 元素，或者添加默认值
                                continue;
                            }
                            else if (item is JObject itemObj)
                            {
                                var nestedMsg = GetDefaultMessageInstance(field.MessageType);
                                var cleanedItem = CleanUnknownFields(itemObj, nestedMsg);
                                if (cleanedItem != null)
                                {
                                    arrClean.Add(cleanedItem);
                                }
                            }
                            else
                            {
                                // 验证并转换简单类型
                                var validatedItem = ValidateAndConvertValue(item, field);
                                if (validatedItem != null)
                                {
                                    arrClean.Add(validatedItem);
                                }
                            }
                        }
                        cleanedObject[fieldName] = arrClean;
                    }
                    // repeated 简单类型
                    else if (field.IsRepeated && value is JArray arrSimple)
                    {
                        var arrClean = new JArray();
                        foreach (var item in arrSimple)
                        {
                            if (item == null)
                            {
                                // 跳过 null 元素
                                continue;
                            }
                            var validatedItem = ValidateAndConvertValue(item, field);
                            if (validatedItem != null)
                            {
                                arrClean.Add(validatedItem);
                            }
                        }
                        cleanedObject[fieldName] = arrClean;
                    }
                    // repeated 字段但 JSON 中不是数组（类型不匹配）
                    else if (field.IsRepeated)
                    {
                        // 如果 JSON 中 repeated 字段不是数组，尝试将单个值转换为数组
                        var arrClean = new JArray();
                        if (value != null && value.Type != JTokenType.Null)
                        {
                            // 尝试将单个值添加到数组中
                            var validatedItem = ValidateAndConvertValue(value, field);
                            if (validatedItem != null)
                            {
                                arrClean.Add(validatedItem);
                            }
                        }
                        cleanedObject[fieldName] = arrClean;
                    }
                    else
                    {
                        cleanedObject[fieldName] = ValidateAndConvertValue(value, field);
                    }
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"处理字段 {fieldName} 时出错: {ex.Message}, 字段类型: {field.FieldType}, 值类型: {value?.Type}");
                    // 出错时使用默认值
                    cleanedObject[fieldName] = GetDefaultValueForField(field);
                }
            }
            else
            {
                cleanedObject[fieldName] = GetDefaultValueForField(field);
            }
        }
        return cleanedObject;
    }

    private static JToken GetDefaultValueForField(FieldDescriptor field)
    {
        // 对于 Repeated 字段，返回空数组
        if (field.IsRepeated)
        {
            return new JArray();
        }
        
        switch (field.FieldType)
        {
            case FieldType.String:
                return "";
            case FieldType.Int32:
            case FieldType.Int64:
            case FieldType.UInt32:
            case FieldType.UInt64:
            case FieldType.SInt32:
            case FieldType.SInt64:
            case FieldType.Fixed32:
            case FieldType.Fixed64:
            case FieldType.SFixed32:
            case FieldType.SFixed64:
                return 0;
            case FieldType.Float:
            case FieldType.Double:
                return 0.0;
            case FieldType.Bool:
                return false;
            case FieldType.Bytes:
                return "";
            case FieldType.Enum:
                return 0;
            case FieldType.Message:
                return new JObject();
            default:
                return null;
        }
    }

    private static JToken ValidateAndConvertValue(JToken value, FieldDescriptor field)
    {
        try
        {
            switch (field.FieldType)
            {
                case FieldType.String:
                    return value?.ToString() ?? "";
                case FieldType.Int32:
                case FieldType.Int64:
                case FieldType.UInt32:
                case FieldType.UInt64:
                case FieldType.SInt32:
                case FieldType.SInt64:
                case FieldType.Fixed32:
                case FieldType.Fixed64:
                case FieldType.SFixed32:
                case FieldType.SFixed64:
                    if (value.Type == JTokenType.Integer || value.Type == JTokenType.String)
                        return value;
                    return 0;
                case FieldType.Float:
                case FieldType.Double:
                    if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer || value.Type == JTokenType.String)
                        return value;
                    return 0.0;
                case FieldType.Bool:
                    if (value.Type == JTokenType.Boolean || value.Type == JTokenType.String)
                        return value;
                    return false;
                case FieldType.Bytes:
                    return value?.ToString() ?? "";
                case FieldType.Enum:
                    if (value.Type == JTokenType.Integer || value.Type == JTokenType.String)
                        return value;
                    return 0;
                default:
                    return value ?? null;
            }
        }
        catch
        {
            return GetDefaultValueForField(field);
        }
    }

    private static IMessage GetDefaultMessageInstance(MessageDescriptor messageDescriptor)
    {
        // 对于嵌套消息，我们返回一个空的 JObject 作为占位符
        // 这样可以避免 null 引用异常，同时保持类型安全
        try
        {
            // 尝试通过反射创建默认实例
            var messageType = messageDescriptor.ClrType;
            if (messageType != null)
            {
                return (IMessage)System.Activator.CreateInstance(messageType);
            }
        }
        catch (System.Exception ex)
        {
            // 如果反射创建失败，记录错误并返回 null
            UnityEngine.Debug.LogError($"创建消息实例失败: {ex.Message}, 消息类型: {messageDescriptor?.Name}");
        }
        return null;
    }
    }
}