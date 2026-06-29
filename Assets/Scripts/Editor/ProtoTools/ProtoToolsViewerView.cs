using UnityEngine;
using common.editor;
using GameData.MapData;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Pb.Map;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;

namespace proto.editor
{
    public enum ParseDataEnum
    {
        [LabelText("地图")]
        Map,
        [LabelText("素材")]
        UGItem,
    }

    public class ProtoToolsViewerView : BaseToolView
    {
        private JsonFormatter jsonFormatter;

        [LabelText("PB数据类型")]
        [LabelWidth(110)]
        public ParseDataEnum dataEnum;

        [LabelText("PB地址(本地/远程)")]
        [LabelWidth(110)]
        public string protoRemoteUrl = "";

        [HideLabel]
        [TextArea(10, 50)]
        public string jsonTextArea = "";

        [Button("解析", ButtonSizes.Medium)]
        [PropertySpace(SpaceBefore = 10)]
        void OnParsePb()
        {
            protoRemoteUrl = jsonTextArea;
            jsonTextArea = "";
            if (string.IsNullOrEmpty(protoRemoteUrl))
            {
                Debug.Log("PB工具：Url不能为空");
                return;
            }

            if (protoRemoteUrl.Contains("https://"))
            {
                new AssetLoader().DownloadAsset(protoRemoteUrl.Replace('\\', '/'), (byte[] metaDataBytes) =>
                {
                    LoadMetaData(metaDataBytes);
                }, errorCode =>
                {
                    Debug.LogError(errorCode);
                });
            } else {
                byte[] metaDataBytes = File.ReadAllBytes(protoRemoteUrl);
                LoadMetaData(metaDataBytes);
            }
        }

        [Button("保存", ButtonSizes.Medium)]
        [PropertySpace(SpaceBefore = 10)]
        void SavePB()
        {
            var data = PMapData.Parser.ParseJson(jsonTextArea);
        }

        void LoadMetaData(byte[] metaDataBytes)
        {
            IMessage msg = null;
            PMapData mapData = null; 
            if (dataEnum == ParseDataEnum.Map) {
                msg = MapPbDataTool.ParseMapPb(metaDataBytes);
                mapData = msg as PMapData;
            } else if (dataEnum == ParseDataEnum.UGItem) {
                msg = PNodeData.Parser.ParseFrom(metaDataBytes);
            }
            if (msg != null)
            {
                var formatStr = jsonFormatter.Format(mapData);
                var formatMapData = JsonConvert.DeserializeObject(formatStr);
                jsonTextArea = JsonConvert.SerializeObject(formatMapData, Formatting.Indented);
            } else {
                EditorUtility.DisplayDialog("提示", "解析失败", "确定");
            }
        }

        public ProtoToolsViewerView(OdinMenuEditorWindow window) : base(window)
        {

        }

        [OnInspectorInit]
        void InitData()
        {
            var descriptorList = GameComponentDataReflection.Descriptor.MessageTypes.ToList();
            var settingDescriptorList = GameSettingDataReflection.Descriptor.MessageTypes;
            descriptorList.AddRange(settingDescriptorList);
            var typeRegistry = TypeRegistry.FromMessages(descriptorList);
            var setting = new JsonFormatter.Settings(true, typeRegistry);
            jsonFormatter = new JsonFormatter(setting);
        }
    }
}