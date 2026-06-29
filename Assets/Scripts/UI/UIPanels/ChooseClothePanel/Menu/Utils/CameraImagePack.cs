using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 相机图片包装类（相册/上传等统一使用该结构）
/// </summary>
[Serializable]
public class CameraImagePack
{
    public string name;      // 文件名（唯一键）
    public string albumId;   // 服务端相册 id（用于更新接口）
    public float size;       // 原始字节流大小（bytes.Length）
    public float height;
    public float width;
    public bool isCloud;     // 是否有云端
    public string url;       // 云端地址
    public Texture2D texture;// 解密后 Texture（可能为 null：云端但本地无图/未加载）
    public string uploader;  // 上传用户 uid（保存时也会写入）
    public long saveTime;    // 本地保存时间（秒）
    public long uploadTime;  // 云端上传时间（秒）

    // 内部字段：是否存在本地加密文件（给 UI/业务判定用）
    public bool hasLocal;

    // 媒体类型：0=照片，1=视频
    public int mediaType;
    // 媒体资源地址（照片可为空；视频建议放本地/远端视频地址）
    public string mediaUrl;
    // 展示缩略图地址（视频展示时优先使用）
    public string previewUrl;
    // 打卡点名称
    public string locationName;
    // 是否是打卡点打卡
    public bool isLandMark;
    // 地图ID
    public string mapId;
    // 是否公开在相册
    public bool isPublic;
    // @列表
    public List<atListItem> atList;
}

