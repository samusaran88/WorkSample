using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using System.Xml;
using System.IO;
using System.Linq;
using static EnemySpawner;
public class StageManager : MonoBehaviour
{
    [SerializeField] string xmlDir;
    [SerializeField] List<SpawnEnemyTime> listSpawnEnemies = new List<SpawnEnemyTime>();
    [SerializeField] GameObject ring;
    [SerializeField] TMP_Text timer;
    [SerializeField] int stageLevel;
    //[SerializeField] float maxTime = 600.0f;
    [SerializeField] float minSpeed = 50.0f;
    [SerializeField] float addSpeedPerSec = 1.0f / 600.0f;
    int enemySpawnMult = 1;
    public bool isStart = false;
    public bool bossStart = false;
    public bool testMode = false;
    Queue<SpawnEnemyTime> queueSpawnEnemies = new Queue<SpawnEnemyTime>();
    int index = 0;
    public TimerUI timerUI;
    private void OnEnable()
    {
        isStart = false;
    }
    void Start()
    {
#if UNITY_EDITOR_WIN
        Application.targetFrameRate = 60;

        if (XmlRead() == false) XmlWrite();
        if (stageLevel == 1) DataManager.I.saveData.arraySpawnEnemies001 = listSpawnEnemies.ToArray();
        if (stageLevel == 2) DataManager.I.saveData.arraySpawnEnemies002 = listSpawnEnemies.ToArray();
        if (stageLevel == 3) DataManager.I.saveData.arraySpawnEnemies003 = listSpawnEnemies.ToArray();
        DataManager.I.SaveData();
#endif
        //DataManager.I.LoadData();
        listSpawnEnemies.Clear();
        if (stageLevel == 1) listSpawnEnemies = DataManager.I.saveData.arraySpawnEnemies001.ToList();
        if (stageLevel == 2) listSpawnEnemies = DataManager.I.saveData.arraySpawnEnemies002.ToList();
        if (stageLevel == 3) listSpawnEnemies = DataManager.I.saveData.arraySpawnEnemies003.ToList();

        TimeManager.I.Setup();

        TimerManager.I.SetTimer("EnemySpawnTime", 60.0f);
        TimerManager.I.SetTimer("EnemySpawnMultiplier", 60.0f);
        TimerManager.I.SetTimer("EnemySpawnTime_Short", 5.0f);
        TimerManager.I.AddTimer("StageClear", 1.0f);
        TimerManager.I.SetOnFinishedCallback("StageClear", PopUpManager.I.GameClear);

        testMode = DataManager.I.isTestMode;
        DataManager.I.stageLevel = stageLevel;
    }
    void Update()
    { 
        if (TimeManager.I.GetCurrentTime() >= TimeManager.I.GetMaxTime() && TimerManager.I.IsActive("StageClear") == false)
        {
            TimeManager.I.SetMaxTime();
            TimerManager.I.ResetTimer("StageClear");
        }
        int minute = (int)(TimeManager.I.GetMaxTime() - TimeManager.I.GetCurrentTime()) / 60 % 60;
        int second = (int)(TimeManager.I.GetMaxTime() - TimeManager.I.GetCurrentTime()) % 60;
        //timer.text = (minute).ToString("D2") + ":" + (second).ToString("D2");
        timerUI.SetTimeTextImage(minute, second);
        //minute정렬 :정렬 sceond정렬

        if (testMode == false)
        {
            if (index < listSpawnEnemies.Count)
            {
                if (listSpawnEnemies[index].spawnTime < TimeManager.I.GetCurrentTime())
                {
                    EnemySpawner.I.SpawnEnemy(
                        listSpawnEnemies[index].name,
                        listSpawnEnemies[index].level,
                        listSpawnEnemies[index].life,
                        listSpawnEnemies[index].damage,
                        minSpeed + addSpeedPerSec * TimeManager.I.GetCurrentTime(),
                        listSpawnEnemies[index].count,
                        listSpawnEnemies[index].spawnType);
                    index++;
                } 
            }
        }


        //if (bossStart == false && currentTime > maxTime - 0.1f)
        //{
        //    Vector3 p = PlayerManager.I.player.transform.position + PlayerManager.I.player.transform.forward * 100.0f;
        //    ring.transform.position = new Vector3(p.x, TerrainManager.I.GetTerrainHeight(p.x, p.z) - 250, p.z);
        //    ring.SetActive(true);
        //    EnemySpawner.I.SpawnBoss(eEnemyType.eTypeBoss001, PlayerManager.I.player.transform.position + PlayerManager.I.player.transform.forward * 100.0f - Vector3.up * 10.0f);
        //    bossStart = true;
        //}  

        //if (TimerManager.I.IsFinished("EnemySpawnTime_Short"))
        //{
        //    EnemySpawner.I.SpawnEnemy("Lizard_Blue", 1, 100, 1.0f, 10.0f, 5, eSpawnType.eRandom);
        //    //EnemySpawner.I.SpawnEnemy("Spider_Black", 1, 10, 1.0f, 5 * enemySpawnMult, eSpawnType.eRandom);
        //    //EnemySpawner.I.SpawnEnemy("Plant_Pink", 1, 200, 1.0f, 0.0f, 5 * enemySpawnMult, eSpawnType.ePack);
        //    //EnemySpawner.I.SpawnEnemy("Crystal_Keeper_Blue", 1, 200, 1.0f, 0.0f, 5, eSpawnType.eRandom);
        //    //EnemySpawner.I.SpawnEnemy("Spider_Purple", 1, 500, 0.1f, 5.0f, 5, eSpawnType.ePack);
        //    TimerManager.I.ResetTimer("EnemySpawnTime_Short");
        //}


        //if (TimerManager.I.IsFinished("EnemySpawnTime_Short"))
        //{
        //    EnemySpawner.I.SpawnEnemy("Plant_Pink", 1, 10.0f, 1.0f, 5 * enemySpawnMult, eSpawnType.eRandom);
        //    TimerManager.I.ResetTimer("EnemySpawnTime");
        //}
        //if (TimerManager.I.IsFinished("EnemySpawnMultiplier"))
        //{
        //    enemySpawnMult++;
        //    TimerManager.I.ResetTimer("EnemySpawnMultiplier");
        //}
        if (Input.GetKeyDown(KeyCode.Z))
        {
            //EnemySpawner.I.SpawnEnemyInstant("CarBoss", 100, 10000, 1.0f, 10.0f, 1, eSpawnType.eBehind);
            //EnemySpawner.I.SpawnEnemyInstant("ChestMonster", 100, 1000, 10.0f, 10.0f, 1, eSpawnType.ePack);
            //EnemySpawner.I.SpawnEnemyInstant("Plant_Red", 1, 5, 7.0f, 10.0f, 5, eSpawnType.eRandom); 
            //EnemySpawner.I.SpawnEnemy("Plant_Red", 1, 5, 7.0f, 10.0f, 50, eSpawnType.eRandom);
            //EnemySpawner.I.SpawnEnemy("Cobra_Green", 1, 100, 1.0f, 10.0f, 50, eSpawnType.eRandom);
            //EnemySpawner.I.SpawnEnemy("Griffin_Blue", 1, 100, 1.0f, 10.0f, 50, eSpawnType.eRandom);
            //EnemySpawner.I.SpawnEnemy("Gorgon_Green", 1, 100, 1.0f, 10.0f, 50, eSpawnType.eRandom);
            EnemySpawner.I.SpawnEnemy("Scorpion_Blue", 1, 100, 1.0f, 10.0f, 50, eSpawnType.eRandom); 
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            //EnemySpawner.I.SpawnEnemy("Spider_Black", 1, 10.0f, 1.0f, 5 * enemySpawnMult, eSpawnType.eRandom);
            //EnemySpawner.I.SpawnEnemy("Spider_Black", 5 * enemySpawnMult, eSpawnType.eRandom);
            //ObjectManager.I.ActivateChest(PlayerManager.I.player.transform.position + new Vector3(UnityEngine.Random.Range(-10.0f, 10.0f),0, UnityEngine.Random.Range(-10.0f, 10.0f)));
            //WeaponManager.I.ActivateWeapon("Lightning");
            //WeaponManager.I.ActivateWeapon("Flame");
            //WeaponManager.I.ActivateWeapon("Rocket");
            //WeaponManager.I.ActivateWeapon("DroneA");
            WeaponManager.I.ActivateWeapon("Landmine");
        }
    }
    public void XmlWrite()
    {
        string filestreamPath = Application.streamingAssetsPath + "/SerializedData/SpawnData/stage001_ES.xml";
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", "yes"));

        XmlElement SpawnListElement = xmlDoc.CreateElement("SpawnList");
        xmlDoc.AppendChild(SpawnListElement);

        for (int i = 0; i < listSpawnEnemies.Count; i++)
        {
            XmlElement SpawnElement = xmlDoc.CreateElement("Enemy");
            SpawnElement.SetAttribute("Name", listSpawnEnemies[i].name);
            SpawnElement.SetAttribute("Level", listSpawnEnemies[i].level.ToString());
            SpawnElement.SetAttribute("Life", listSpawnEnemies[i].life.ToString());
            SpawnElement.SetAttribute("Damage", listSpawnEnemies[i].damage.ToString());
            SpawnElement.SetAttribute("Count", listSpawnEnemies[i].count.ToString());
            SpawnElement.SetAttribute("SpawnTime", listSpawnEnemies[i].spawnTime.ToString());
            SpawnElement.SetAttribute("SpawnType", ((int)listSpawnEnemies[i].spawnType).ToString());
            SpawnListElement.AppendChild(SpawnElement);
        }
        xmlDoc.Save(filestreamPath);
    }
    public bool XmlRead()
    {
        string filestreamPath = Application.streamingAssetsPath + xmlDir;

        FileInfo info = new FileInfo(filestreamPath);
        if (!info.Exists) return false;

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.Load(filestreamPath);

        XmlElement SpawnListElement = xmlDoc["SpawnList"];
        listSpawnEnemies.Clear();

        foreach (XmlElement SpawnElement in SpawnListElement.ChildNodes)
        {
            SpawnEnemyTime Item = new SpawnEnemyTime();
            Item.name = SpawnElement.GetAttribute("Name");
            Item.level = System.Convert.ToInt32(SpawnElement.GetAttribute("Level"));
            Item.life = System.Convert.ToSingle(SpawnElement.GetAttribute("Life"));
            Item.damage = System.Convert.ToSingle(SpawnElement.GetAttribute("Damage"));
            Item.count = System.Convert.ToInt32(SpawnElement.GetAttribute("Count"));
            Item.spawnTime = System.Convert.ToSingle(SpawnElement.GetAttribute("SpawnTime"));
            Item.spawnType = (eSpawnType)System.Convert.ToInt32(SpawnElement.GetAttribute("SpawnType")); 
            listSpawnEnemies.Add(Item);
        }
        return true;
    }

}
[Serializable]
public class SpawnEnemyTime
{
    public string name;
    public int level;
    public float life;
    public float damage;
    public int count;
    public float spawnTime; 
    public eSpawnType spawnType;
}