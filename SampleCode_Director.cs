using MoreMountains.Feedbacks;
using PartyGame.Scripts.MiniGame.Base;
using PartyGame.Scripts.MiniGame.QuizHowMany;
using PartyGame.Scripts.Shared.FloatingText;
using PartyGame.Scripts.Shared.Net; 
using System; 
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShepherdDogDirector : MiniGameDirectorBase
{
    [Serializable]
    public struct SpawnPoints
    {
        public Collider fromBound;
        public Collider toBound;
    }
    [Serializable]
    public struct SpawnTime
    {
        public float time;
        public float tickTime;
        public int maxCount;
        public int countPerTick;
    }
    [Header("In Game Content")]
    public GameObject sheepPrefab;
    public GameObject goatPrefab;
    public Vector3 modifiedGravity;
    public Vector3 topLeft;
    public Vector3 bottomright;
    public GameObject barn_Sheep;
    public GameObject barnTrigger_Sheep;
    public GameObject barn_Goat;
    public GameObject barnTrigger_Goat;
    public int maxSheepCount = 30;
    public int maxGoatCount = 20;
    public int initialSheepCount = 20;
    public int initialGoatCount = 10;
    public int maxSpawnCount = 40;
    public int spawnCountPerTick = 5;
    public int targetScore;
    public float sheepSpawnTime = 2.0f;
    public float sheepSpawnRate = 0.7f;
    public float goatSpawnRate = 0.3f;
    public SpawnPoints[] spawnPoints;
    public SpawnTime[] spawnTimes;
    public bool gameStart = true;

    Vector3 originalGravity = Vector3.zero;
    NetworkVariable<int> finalScore_Sheep = new ();
    NetworkVariable<int> finalScore_Goat = new ();
    List<NetworkObject> sheepNetworkObjects = new ();
    List<ShepherdDogSheep> listSheep = new ();
    Queue<float> queueSpawnTime = new Queue<float>();

    [Header("MMFeedbacks")]
    [SerializeField] private MMFeedbacks feedbacks_OnScored_Sheep = null;
    [SerializeField] private MMFeedbacks feedbacks_OnScored_Goat = null;

    protected override void Awake()
    {
        base.Awake();

        //양이 너무 느리게 떨어진다. 유니티 특유의 달에서 점프하는 느낌. 중력값 바꿔준다.
        originalGravity = Physics.gravity;
        Physics.gravity = modifiedGravity;

        finalScore_Sheep.OnValueChanged += OnSheepScoreValueChanged;
        finalScore_Goat.OnValueChanged += OnGoatScoreValueChanged;
    }
    public override void Update()
    {
        base.Update();

        if (!IsServer) return;
        if (CurrentMiniGameState != MiniGameDirectorBase.MiniGameState.Playing)
            return;

        SpawnTimer();
        SpawnSheep();
    }
    public override void OnDestroy()
    {
        base.OnDestroy();

        Physics.gravity = originalGravity;
    }
    public override void OnPlayerObjectsAllSpawned()
    {
    }
    public override void OnMiniGameStateValueChanged(MiniGameState oldValue, MiniGameState newValue)
    {
        switch (newValue)
        {
            case MiniGameState.PrePlaying:
                {
                    if (IsServer)
                    {
                        NetGameState.RunAfterNetworkSpawned((netGameState) =>
                        {
                            int currentMiniGameDifficulty = netGameState.CurrentMiniGameDifficulty.Value;
                            finalScore_Sheep.Value = 0;
                            finalScore_Goat.Value = 0;
                            targetScore = (int)((float)miniGameInfo.GetIntValue("TargetSheepCount") * miniGameInfo.GetDifficultyValue("TargetSheepCountRate", currentMiniGameDifficulty));
                            //scoreText.SetText("0 / " + targetScore.ToString());
                            InitSpawnCount();
                        });
                    }
                }
                break;
            case MiniGameState.OutroSuccess:
                {
                    if (IsServer)
                    {
                        DespawnAllSheeps();
                    }
                }
                break;
            case MiniGameState.OutroFailed:
                {
                    if (IsServer)
                    {
                        DespawnAllSheeps();
                    }
                }
                break;
        }
    }
    protected override void OnGameEndedValueChanged(bool previousValue, bool newValue)
    {
    }
    protected override bool CanFadeScreenInOnPrePlaying()
    {
        return true;
    }
    void SpawnTimer()
    {
        for (int i = 0; i < spawnTimes.Length; i++)
        {
            if (MiniGameTimer.GetTimeSpan() > spawnTimes[i].time)
            {
                maxSpawnCount = spawnTimes[i].maxCount;
                sheepSpawnTime = spawnTimes[i].tickTime;
                spawnCountPerTick = spawnTimes[i].countPerTick;
                break;
            }
        }
    }
    void SpawnSheep()
    {
        if (queueSpawnTime.Count < spawnCountPerTick)
        {
            if (UnityEngine.Random.value < sheepSpawnRate)
            {
                SpawnRandom(true);
            }
            else
            {
                SpawnRandom(false);
            }
            //SpawnRandom(spawnSheepCount);
        }
        else if (NetworkManager.Singleton.ServerTime.TimeAsFloat - queueSpawnTime.Peek() > sheepSpawnTime)
        {
            queueSpawnTime.Clear();
        }
    }
    void InitSpawnCount()
    {
        //DeactivateAllSheeps(); 
        DespawnAllSheeps();
        SpawnInstant(maxSheepCount, false);
        SpawnInstant(maxGoatCount, true);
        for (int i = 0; i < spawnCountPerTick; i++)
        {
            queueSpawnTime.Enqueue(0);
        }
        //for (int i = 0; i < maxSheepCount - initialSheepCount; i++)
        //{
        //    listSheep[i].Deactivate(GetOutOfBoundsPosition());
        //}
        //for (int i = 0; i < maxSheepCount - initialSheepCount; i++)
        //{
        //    listSheep[i].Deactivate(GetOutOfBoundsPosition());
        //}
        int sheepCounter = 0;
        int goatCounter = 0;
        for (int i = 0; i < listSheep.Count; i++)
        {
            if (listSheep[i].isBlackGoat)
            {
                goatCounter++;
                if (goatCounter > initialGoatCount)
                    listSheep[i].Deactivate(GetOutOfBoundsPosition());
            }
            else
            {
                sheepCounter++;
                if (sheepCounter > initialSheepCount)
                    listSheep[i].Deactivate(GetOutOfBoundsPosition());
            }
        }
    }
    void SpawnInstant(int maxCount, bool isBlackGoat)
    {
        if (spawnPoints.Length < 1)
        {
            Debug.LogError("양 스폰 포인트 지정 불가");
            return;
        }
        float radius = 0.75f;
        int count = 0;
        int breakPoint = 0;
        while (count < maxCount && breakPoint < maxCount * 100)
        {
            SpawnPoints sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            while (breakPoint < maxCount * 100)
            {
                Vector3 v = RandomPointInBounds(sp.toBound.bounds);
                int sheepCount = 0;
                foreach (Collider c in Physics.OverlapSphere(v, radius))
                {
                    if (c.CompareTag("Wall"))
                    {
                        sheepCount = -1;
                        break;
                    }
                    if (c.CompareTag("Enemy"))
                    {
                        sheepCount++;
                        break;
                    }
                }
                breakPoint++;
                if (sheepCount == 0)
                {
                    GameObject sheepObj = isBlackGoat == true ? Instantiate(goatPrefab, v, Quaternion.identity) : Instantiate(sheepPrefab, v, Quaternion.identity);
                    ShepherdDogSheep sheep = sheepObj.GetComponent<ShepherdDogSheep>();
                    NetworkObject networkObject = sheepObj.GetComponent<NetworkObject>();
                    networkObject.Spawn(true);
                    sheep.Init(v, v);
                    sheepNetworkObjects.Add(networkObject);
                    listSheep.Add(sheep);
                    count++;
                    break;
                }
            }
        }
    }
    void SpawnRandom(bool isBlackGoat)
    {
        if (spawnPoints.Length < 1)
        {
            Debug.LogError("양 스폰 포인트 지정 불가");
            return;
        }
        SpawnPoints sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        float radius = 3.0f;
        Vector3 fromPos = RandomPointInBounds(sp.fromBound.bounds);
        Vector3 toPos = Vector3.zero;
        int minSheepCount = int.MaxValue;
        for (int i = 0; i < 10; i++)
        {
            int sheepCount = 0;
            Vector3 v = RandomPointInBounds(sp.toBound.bounds);
            foreach (Collider c in Physics.OverlapSphere(v, radius))
            {
                if (c.CompareTag("Wall"))
                {
                    sheepCount = -1;
                    break;
                }
                if (c.CompareTag("Enemy"))
                {
                    sheepCount++;
                }
            }
            if (sheepCount < 0)
            {
                i--;
                radius = radius * 0.9f;
            }
            else if (sheepCount < minSheepCount)
            {
                minSheepCount = sheepCount;
                radius = radius * 1.1f;
                toPos = v;
            }
        }
        int count = 0;
        ShepherdDogSheep selected = null;
        foreach (ShepherdDogSheep sheep in listSheep)
        {
            if (sheep.isActive == false && sheep.isBlackGoat == isBlackGoat)
            {
                selected = sheep;

                //소환 생성 제한 코드
                selected.Init(fromPos, toPos);
                queueSpawnTime.Enqueue(NetworkManager.Singleton.ServerTime.TimeAsFloat);
                return;
            }
            if (sheep.isActive)
            {
                count++;
            }
        }
        if (count < maxSpawnCount)
        {
            if (selected != null)
            {
                selected.Init(fromPos, toPos);
                queueSpawnTime.Enqueue(NetworkManager.Singleton.ServerTime.TimeAsFloat);
            }
            else
            {
                GameObject sheepObj = isBlackGoat == true ? Instantiate(goatPrefab, fromPos, Quaternion.identity) : Instantiate(sheepPrefab, fromPos, Quaternion.identity);
                ShepherdDogSheep sheep = sheepObj.GetComponent<ShepherdDogSheep>();
                NetworkObject networkObject = sheepObj.GetComponent<NetworkObject>();
                networkObject.Spawn(true);
                listSheep.Add(sheep);
                sheep.Init(fromPos, toPos);
                sheepNetworkObjects.Add(networkObject);
                queueSpawnTime.Enqueue(NetworkManager.Singleton.ServerTime.TimeAsFloat);
            }
        }
    }
    void SpawnRandom(int maxCount)
    {
        if (spawnPoints.Length < 1)
        {
            Debug.LogError("양 스폰 포인트 지정 불가");
            return;
        }
        SpawnPoints sp = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        float radius = 3.0f;
        Vector3 fromPos = RandomPointInBounds(sp.fromBound.bounds);
        Vector3 toPos = Vector3.zero;
        int minSheepCount = int.MaxValue;
        for (int i = 0; i < 10; i++)
        {
            int sheepCount = 0;
            Vector3 v = RandomPointInBounds(sp.toBound.bounds);
            foreach (Collider c in Physics.OverlapSphere(v, radius))
            {
                if (c.CompareTag("Wall"))
                {
                    sheepCount = -1;
                    break;
                }
                if (c.CompareTag("Enemy"))
                {
                    sheepCount++;
                }
            }
            if (sheepCount < 0)
            {
                i--;
                radius = radius * 0.9f;
            }
            else if (sheepCount < minSheepCount)
            {
                minSheepCount = sheepCount;
                radius = radius * 1.1f;
                toPos = v;
            }
        }
        int count = 0;
        ShepherdDogSheep selectedSheep = null;
        foreach (ShepherdDogSheep sheep in listSheep)
        {
            if (sheep.isActive == false)
            {
                selectedSheep = sheep;
            }
            else
            {
                count++;
            }
        }
        if (selectedSheep != null && count < maxCount)
        {
            selectedSheep.Init(fromPos, toPos);
            queueSpawnTime.Enqueue(NetworkManager.Singleton.ServerTime.TimeAsFloat);
        }
        //bool isFoundSheep = false;
        //foreach (ShepherdDogSheep sheep in listSheep)
        //{
        //    if (sheep.isActive == false)
        //    {
        //        sheep.Init(fromPos, toPos);
        //        //isFoundSheep = true;
        //        queueSpawnTime.Enqueue(NetworkManager.Singleton.ServerTime.TimeAsFloat);
        //        break;
        //    }
        //}
        //if (isFoundSheep == false && listSheep.Count < maxCount)
        //{
        //    GameObject sheepObj = Instantiate(sheepPrefab, fromPos, Quaternion.identity);
        //    ShepherdDogSheep sheep = sheepObj.GetComponent<ShepherdDogSheep>(); 
        //    NetworkObject networkObject = sheepObj.GetComponent<NetworkObject>();
        //    networkObject.Spawn(true);
        //    listSheep.Add(sheep);
        //    sheep.Init(fromPos, toPos);
        //    queueSpawnTime.Enqueue(NetworkManager.Singleton.ServerTime.TimeAsFloat);
        //}
    }
    public Vector3 RandomPointInBounds(Bounds bounds)
    {
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            0.1f,
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );
    }
    public bool IsInGameBoundary(Vector3 pos, float radius)
    {
        if (pos.x > topLeft.x + radius && pos.x < bottomright.x - radius && pos.z > bottomright.z + radius && pos.z < topLeft.z - radius)
            return true;
        return false;
    }
    public bool IsInSpawnPoint(Vector3 pos, float radius)
    {
        foreach (SpawnPoints sp in spawnPoints)
        {
            if (sp.toBound.bounds.Contains(pos))
                return true;
        }
        return false;
    }
    void DeactivateAllSheeps()
    {
        foreach (ShepherdDogSheep sheep in listSheep)
        {
            sheep.Deactivate(GetOutOfBoundsPosition());
        }
    }
    void DespawnAllSheeps()
    {
        foreach (NetworkObject o in sheepNetworkObjects)
        {
            if (o != null)
                o.Despawn();
        }
        sheepNetworkObjects.Clear();
        listSheep.Clear();
    }
    public Vector3 GetOutOfBoundsPosition()
    {
        return bottomright * 2.0f + new Vector3(0, -10, 0);
    }
    public void AddScore(int score, bool isSheepScored)
    {
        if (isSheepScored) finalScore_Sheep.Value += score;
        else finalScore_Goat.Value += score;
    }
    void OnSheepScoreValueChanged(int oldScore, int newScore)
    {
        feedbacks_OnScored_Sheep?.PlayFeedbacks();
    }
    void OnGoatScoreValueChanged(int oldScore, int newScore)
    {
        feedbacks_OnScored_Goat?.PlayFeedbacks();
    }
    public void AddPlayerScore(NetGameState.PlayerId playerId, int score)
    {
        if (gameStart == false) return;
        if (!PlayerBases.ContainsKey(playerId)) return;
        var player = PlayerBases[playerId];
        if (player == null) return;
        ShepherdDogPlayer castedPlayer = player as ShepherdDogPlayer;
        castedPlayer.personalScore.Value += score;
    }
}
