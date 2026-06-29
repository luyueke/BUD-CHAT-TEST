// @Author: YangJie
// @Description:
// @Date:  2023/08/17
// @Modify:

using GameData.BaseInfo;
using System.Collections.Generic;

namespace GameData.Base
{
    public class UgcBaseInfo
    {
        public string id;
        public string name;
        public string desc;
        /// <summary>封面图Url</summary>
        public string cover;
        /// <summary>基础元数据Url</summary>
        public string metaDataUrl;
        /// <summary>创作者id</summary>
        public string creator;
        public long createTime;
        /// <summary> 最后修改时间</summary>
        public long updateTime;
        /// <summary>模板ID</summary>
        public virtual string templateId { get; set; }
        /// <summary> 草稿版本</summary>
        public int draftVersion;

        /// <summary>
        /// UGC贴图zip地址
        /// </summary>
        public string textureUrl;
        // public int coverAutoSaved = 0;

        /// <summary>
        /// 业务字段
        /// </summary>
        public bool isLocal;

        /// <summary>编辑时长</summary>
        public int editTime;

        public AuditStatus auditInfo;
        //设计码
        public string designCode;

        public CoverSaveStatus coverAutoSaved = CoverSaveStatus.NoSaved;

        public int forceUpdate = 0; // ForceUpdate

        public int migrateData; //是否原海外服 迁移数据 0 否，1 是

        public int gameType;  //0，普通地图，1 AI game类地图

        public GameSetting gameSetting = new();

        public List<SectionItemData> sectionInfo = new();
    }

    public class SectionItemData
    {
        public string sectionId;
        public string name;
    }

    public enum ForceUpdate {
        Default = 0,
        HotUpdate = 1,
        AppUpdate = 2,
        NeedUpdateFeature = 3,
        CustomUpdate = 4,
    }

    public class AuditStatus
    {
        /// <summary>
        /// 不通过原因
        /// </summary>
        public string rejectReason;
        public int auditResult;
    }

    public enum AuditResult {
        ErrAuditResult = 0,
        PendingToAudit = 1, //待审核
        Rejected = 2,  //不通过
        Passed = 3, //通过

        Appealing = 4, // 申诉中


    }

    public enum CoverSaveStatus {
        NoSaved = 0,
        AutoSaved = 1,
        ManualSaved = 2,
    }

}
