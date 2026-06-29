using EasySpreadsheet;
using UnityEngine;


public interface IEsDataLoader
{
    EsRowDataTable Load(string sheetClassName);
}
	
/// <summary>
/// Load generated data from Resources.
/// </summary>
public class EsDataLoader : IEsDataLoader
{
    public EsRowDataTable Load(string sheetClassName)
    {
        var filePath = EsSettings.Instance.generatedAssetPath+$"/{sheetClassName}.asset" ;
        EsRowDataTable table = null;
        if (!Application.isEditor)
        {
            var tableWrap = Loader.Load<EsRowDataTable>(filePath);
            table = tableWrap.RetainAsset();
        }
        else
        {
#if UNITY_EDITOR
            table = UnityEditor.AssetDatabase.LoadAssetAtPath<EsRowDataTable>(filePath);
#endif

        }
        

#if UNITY_EDITOR
        if (table == null)
        {
            EsLog.Error("Can not load file " + filePath + ".asset");
        }
#endif
        return table;
    }
}
