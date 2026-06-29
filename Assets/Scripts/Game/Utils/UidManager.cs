using Pb.Map;

/// <summary>
/// Author:Shaocheng
/// Description: 场景节点的UID管理
/// Date: 2022-3-30 19:43:08
/// </summary>
public class UidManager : GameInstance<UidManager>
{
    public uint curMaxUid = 0;

    public uint GetUid(uint uid = 0)
    {
        uint newUid;
        if (uid == 0)
        {
            newUid = ++curMaxUid;
        }
        else
        {
            newUid = uid;
        }

        if (newUid > curMaxUid)
        {
            curMaxUid = newUid;
        }

        return newUid;
    }

    public uint GetUid(PNodeData data)
    {
        uint newUid;

        if (data == null)
        {
            //prop edit mode, not set uid
            newUid = 0;
        }
        else if (data.Uid == 0)
        {
            newUid = UidManager.Inst.GetUid(0);
        }
        else
        {
            newUid = data.Uid;
            if (newUid > curMaxUid)
            {
                curMaxUid = newUid;
            }
        }

        return newUid;
    }

}