using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _STR
{
    public const string DTABLE_INFO = "table/SkillInfo";
    public const string DTABLE_WAVE = "table/Waves";
}

public class CAsset
{
    public int m_id = 0;                // id
}

public class AssetSkillInfo : CAsset
{
    public string name = "";
    public string[] description = new string[9];
}

public class AssetWaves : CAsset
{
    public float time;
    public int level;
    public float min;
    public float max;
}


/*
 *  어셋 정보 관리자
 */
public class AssetMgr 
{
    static AssetMgr instance = null;
    public static AssetMgr Inst
    {
        get
        {
            if (instance == null)
                instance = new AssetMgr();
            return instance;
        }
    }

    private AssetMgr()
    {
        IsInstalled = false;
    }

    //----------------------------------------------------------

    public bool IsInstalled { get; set; }
    public List<AssetSkillInfo> m_AssSkillInfo = new List<AssetSkillInfo>();
    public List<AssetWaves> m_AssWaves = new List<AssetWaves>();

    public void Initialize()
    {
        Initialzie_SkillInfo(_STR.DTABLE_INFO);
        Initialzie_Waves(_STR.DTABLE_WAVE);
        IsInstalled = true;
    }

    public void Initialzie_SkillInfo(string pathName)
    {
        List<string[]> kDatas = CSVParser.Load(pathName);
        if (kDatas == null)
            return;

        m_AssSkillInfo.Clear();

        for (int i = 1; i < kDatas.Count; i++)
        {
            string[] aStr = kDatas[i];
            AssetSkillInfo kAss = new AssetSkillInfo();
            int index = 0;

            kAss.m_id = int.Parse(aStr[index++]);
            kAss.name = aStr[index++];

            for (int j = 0; j < 9; j++)
                kAss.description[j] = aStr[index++];

            m_AssSkillInfo.Add(kAss);
        }
        kDatas.Clear();
    }

    public void Initialzie_Waves(string pathName)
    {
        List<string[]> kDatas = CSVParser.Load(pathName);
        if (kDatas == null)
            return;

        m_AssWaves.Clear();

        for (int i = 1; i < kDatas.Count; i++)
        {
            string[] aStr = kDatas[i];
            AssetWaves kAss = new AssetWaves();
            int index = 0;

            kAss.m_id = int.Parse(aStr[index++]);

            kAss.time = float.Parse(aStr[index++]);
            kAss.level = int.Parse(aStr[index++]);
            kAss.min = float.Parse(aStr[index++]);
            kAss.max = float.Parse(aStr[index++]);

            m_AssWaves.Add(kAss);
        }
        kDatas.Clear();
    }
    //------------------------------------------------------------------
}
