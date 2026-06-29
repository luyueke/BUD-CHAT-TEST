using System;

namespace Game.COSXML.Transfer
{
    public class COSXMLUploadInfo
    {
        public int SerialId
        {
            get;
            internal set;
        }

        public string Bucket
        {
            get;
            internal set;
        }

        public string SrcPath
        {
            get;
            internal set;
        }

        public string UploadUri
        {
            get;
            internal set;
        }

        public CosXmlUploadManager.OnUploadSuccess CompleteCallBack
        {
            get;
            internal set;
        }

        public CosXmlUploadManager.OnUploadProgress ProgressCallBack
        {
            get;
            internal set;
        }

        public COSXMLUploadTask Task
        {
            get;
            internal set;
        }

        internal string Error;

        internal float Progress;
    }
}
