using System;
using System.Collections.Generic;

public class RedDotData
{
    public PGCRedDotData pgcReddot;
    public UGCRedDotData ugcReddot;
}

public class PGCRedDotData
{
    public List<string> idList;
}

public class UGCRedDotData
{
    public List<LocalRedDotData> list;
}

[Serializable]
public class LocalRedDotData
{
    public string id;
    public int type;
    public int redDotNum;

    public bool isPGC;
}