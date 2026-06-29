// @Author: YangJie
// @Description:
// @Date:  2023/07/14
// @Modify:

namespace UGCAsset.Draft
{
    public enum UploadStatus
    {

        //上传状态 0:未上传 1:上传中 2:上传成功 3:上传失败 4:已经保存到后端 5: 后端审核
        NotUpload = 0,
        Uploading = 1,
        UploadSuccess = 2,
        UploadFail = 3,
        SavedDraft = 4,
    }
}
