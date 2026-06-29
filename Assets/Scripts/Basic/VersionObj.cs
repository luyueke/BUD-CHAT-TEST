using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VersionObj
{
    public string master = "";
    public string alpha = "";
    public string prod = "";
}

public class VersionInfo
{
    public VersionObj trunk;
    public VersionObj submodules;

    public VersionInfo()
    {
        trunk = new VersionObj();
        submodules = new VersionObj();
    }
}