using AutoLetterbox;
using CatlikeCoding.SDFToolkit.Examples;
using MoreMountains.Feedbacks;
using PartyGame.Scripts.Shared;
using PartyGame.Scripts.Shared.Net;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SampleCode_Sheep : NetworkBehaviour
{
    Vector3 referenceForward = new Vector3(0f, 0f, 1.0f);
    Vector3 referenceRight = new Vector3(1.0f, 0f, 0f);
    int operMi = 0b00000000000000001000000000000000;
    int operP1 = 0b00000000000000000111111100000000;
    int operP2 = 0b00000000000000000000000011111111;
    int operHt = 0b00000000000000000001111111111111;
    int operAg = 0b00000000000000000000000111111111;
    int operSt = 0b00000000000000000000001111111111;

    public enum eSheepState
    {
        idle,
        onSpawn,
        isJump,
        isWalk,
        isRun,
        isRunOnBark,
        isStun,
        onGoal,
    }
    public struct MoveData : INetworkSerializable
    {
        //public float time;
        public int xzPos;
        public int posYnAnglenState;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            //serializer.SerializeValue(ref time);
            serializer.SerializeValue(ref xzPos);
            serializer.SerializeValue(ref posYnAnglenState);
        }
    }
    public struct LerpData
    {
        public float lerpTime;
        public Vector3 lerpPos;
        public Vector3 lerpDir;
        public eSheepState state;
    }

    [Header("Sheep Control")]
    public float sheepRadius = 0.5f;
    public float runAwayDetectRadius = 2.0f;
    public float jumpDetectRadius = 1.0f;
    public float jumpPowerUp = 2.0f;
    public float jumpPower = 500.0f;
    public float runAwaySpeed = 7.0f;
    public float runAwayDistance = 5.0f;
    public float runAwayTime = 2.0f;
    public float runAwayTimer = 0f;
    public float barkRunAwayDistance = 10.0f;
    public float barkRunAwayTime = 4.0f;
    public float herdRegisterMaxTime = 3.0f;
    public float randomWalkTimer = 0;
    public float randomWalkStartTime = 0;
    public float minWalkStartTime = 2.0f;
    public float maxWalkStartTime = 5.0f;
    public float walkSpeed = 2.0f;
    public float walkDistance = 1.0f;
    public float stunDuration = 1.0f;
    public float turnSpeed = 0.0174533f;

    public int scoreGoal;
    public int scoreAssist;
    public int scorePenalty;

    [Header("Collider & Trigger Event")]
    [SerializeField] SphereCollider bodyCollider;
    [SerializeField] SphereCollider runAwayCollider;
    [SerializeField] SphereCollider jumpCollider;
    [SerializeField] EventTrigger runAwayEvent;
    [SerializeField] EventTrigger jumpEvent;
    [SerializeField] Animator animator = null;

    [Header("Sheep State")]
    public bool isActive = false;
    public eSheepState moveState = eSheepState.idle;
    public eSheepState prevMoveState = eSheepState.idle;
    public float contactRecordTime = 10.0f;
    public float positionLerpTime = 0.1f;
    public float penaltyLimitTime = 3.0f;


    NetworkVariable<MoveData> moveData = new ();
    NetworkVariable<int> pointScored = new ();
    LerpData nextLerpData = new ();
    Vector3 targetPos = Vector3.zero;
    Vector3 prevPos = Vector3.zero;
    Vector3 direction = Vector3.zero;
    Vector3 lookDir = Vector3.zero;
    Vector3 randomWalkDirection = Vector3.zero;
    [SerializeField] Vector3 deactivePos = Vector3.zero;
    float jumpTimer = 0f;
    float stunTimer = 0f;
    float lerpTime = -1;
    float stunTime = 0f;
    float penaltyTime = 0f;
    int prevX = int.MinValue;
    int prevY = int.MinValue;
    int prevZ = int.MinValue;
    Rigidbody rb;
    int animParameter_AnimState = 0;
    float animParameter_Speed = 1.0f;
    Dictionary<NetGameState.PlayerId, float> dicLastContactTime = new Dictionary<NetGameState.PlayerId, float>();


    Vector3 lerpStart = Vector3.zero;

    [Header("Physics Based Variables")]
    //public Vector3 velocity = Vector3.zero;
    //public float speed;
    //public float deceleration = -2.5f;
    public float seeRadius = 3.0f;
    //public float alignRadius = 3.0f;
    ////public float dogDetectRadius = 2.0f;
    public float coherenceFactor = 1.0f;
    //public float seperationFactor = 1.0f;
    //public float alignmentFactor = 1.0f;

    //public bool selectedTestSheep = false;
    [Header("Black Goat")]
    public bool isBlackGoat = false;


    [Header("MMFeedbacks")]
    [SerializeField] private MMFeedbacks feedbacks_OnMove = null;
    [SerializeField] private MMFeedbacks feedbacks_OnBarkDetected = null;
    [SerializeField] private MMFeedbacks feedbacks_OnJumpStart = null;
    [SerializeField] private MMFeedbacks feedbacks_OnLand = null;
    [SerializeField] private MMFeedbacks feedbacks_OnStunned = null;
    [SerializeField] private MMFeedbacks feedbacks_OnSheepEnterBarn = null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        jumpEvent.onTrigger += Jump;
        runAwayEvent.onTrigger += RunAway;
        moveData.OnValueChanged += OnMoveValueChanged;
        bodyCollider.radius = sheepRadius;
        runAwayCollider.radius = runAwayDetectRadius;
        jumpCollider.radius = jumpDetectRadius;
    }

    private void FixedUpdate()
    {
        if (!NetworkManager.Singleton) return;
        if (IsServer)
        {
            MoveStateCheck();
            UpdateNetworkMove();
            OnChangeState();
        }
        else
        {
            if (transform.position != nextLerpData.lerpPos)
            {
                lerpStart = transform.position;
                lerpTime = 0f;
            }
            lerpTime += Time.fixedDeltaTime;
            if (lerpTime > positionLerpTime)
            {
                lerpTime = positionLerpTime;
            }
            transform.position = Vector3.Lerp(lerpStart, nextLerpData.lerpPos, lerpTime / positionLerpTime);
            direction = Vector3.Lerp(direction, nextLerpData.lerpDir, lerpTime / positionLerpTime);
            if ((nextLerpData.lerpPos - transform.position).magnitude > 5.0f)
            {
                transform.position = nextLerpData.lerpPos;
                direction = nextLerpData.lerpDir;
            }

            //if (lerpTime > 0.0f && lerpTime < nextLerpData.lerpTime)
            //{
            //    lerpTime += Time.fixedDeltaTime;
            //    float lerpRate = lerpTime / nextLerpData.lerpTime;
            //    if (lerpRate > 1.0f)
            //    {
            //        //moveState = nextLerpData.state;
            //        lerpRate = 1.0f;
            //    }
            //    transform.position = Vector3.Lerp(transform.position, nextLerpData.lerpPos, lerpRate);
            //    direction = Vector3.Lerp(direction, nextLerpData.lerpDir, lerpRate);
            //}
        }

        //if (direction != Vector3.zero) 
        if (transform.position - prevPos != Vector3.zero)
        {
            lookDir = transform.position - prevPos;
            lookDir.y = 0;
            //transform.forward = lookDir.normalized;
            prevPos = transform.position;
        }
        transform.forward = Vector3.RotateTowards(transform.forward, lookDir.normalized, turnSpeed, 0.0f);
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") == true && moveState != eSheepState.idle && moveState != eSheepState.isWalk)
        {
            SampleCode_Sheep s = collision.gameObject.GetComponent<SampleCode_Sheep>();
            //if (s && s.moveState == eSheepState.idle)
            {
                NetGameState.PlayerId latestPlayer = NetGameState.PlayerId.Invalid;
                float t = 0;
                foreach (var p in dicLastContactTime)
                {
                    if (p.Value > t)
                    {
                        t = p.Value;
                        latestPlayer = p.Key;
                    }
                }
                s.ChangeDirection(targetPos, direction, s.runAwayTimer, latestPlayer);
            }
        }
        else if (collision.gameObject.CompareTag("Wall") == true)
        {
            if (moveState == eSheepState.isRun || moveState == eSheepState.isRunOnBark)
            {
                foreach (ContactPoint cp in collision.contacts)
                {
                    direction = direction - cp.normal * (Vector3.Dot(direction, cp.normal) - 0.1f);
                    direction.y = 0;
                    direction.Normalize();
                }
            }
            else if (moveState == eSheepState.onSpawn)
            {
                direction = Vector3.zero;
                rb.velocity = Vector3.zero;
                moveState = eSheepState.idle;
                rb.useGravity = true;
                bodyCollider.enabled = true;
            }
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("Finish"))
        {
            SampleCode_Director sheepDirector = GameContext.GetDirector<SampleCode_Director>();
            if (isBlackGoat == true)
            {
                if (other.gameObject == sheepDirector.barnTrigger_Goat)
                {
                    OnGoal(sheepDirector.barn_Goat.transform.position);
                }
                else if (moveState != eSheepState.isJump && moveState != eSheepState.isStun)
                {
                    Jump(sheepDirector.barn_Goat.transform.position, Vector3.zero, NetGameState.PlayerId.Invalid);
                    AddPenalty();
                }
            }
            else if (isBlackGoat == false)
            {
                if (other.gameObject == sheepDirector.barnTrigger_Sheep)
                {
                    OnGoal(sheepDirector.barn_Sheep.transform.position);
                }
                else if (moveState != eSheepState.isJump && moveState != eSheepState.isStun)
                {
                    Jump(sheepDirector.barn_Sheep.transform.position, Vector3.zero, NetGameState.PlayerId.Invalid);
                    AddPenalty();
                }
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            rb.useGravity = false;
            bodyCollider.enabled = false;
        }
    }
    public void Init(Vector3 fromPos, Vector3 toPos)
    {
        lerpTime = -1.0f;
        jumpTimer = 0f;
        targetPos = Vector3.zero;
        direction = Vector3.zero;
        transform.position = fromPos;
        prevPos = transform.position;
        targetPos = toPos;
        prevX = int.MinValue;
        prevY = int.MinValue;
        prevZ = int.MinValue;
        moveState = eSheepState.onSpawn;
        isActive = true;
        rb.useGravity = false;
        bodyCollider.enabled = false;
        dicLastContactTime.Clear();
    }
    public void Deactivate(Vector3 toPos)
    {
        isActive = false;
        rb.useGravity = false;
        bodyCollider.enabled = false;
        transform.position = toPos;
        moveState = eSheepState.idle;
    }
    void OnMoveValueChanged(MoveData oldMove, MoveData newMove)
    {
        if (!NetworkManager.Singleton) return;
        if (IsServer) return;
        int n = newMove.xzPos;
        int h = newMove.posYnAnglenState;
        int nx = ((n >> 16) & operP1) + ((n >> 16) & operP2);
        int ny = (h >> 19) & operHt;
        int nz = (n & operP1) + (n & operP2);
        int na = (h >> 10) & operAg;
        int ns = h & operSt;
        if ((n & (operMi << 16)) != 0) nx = -nx;
        if ((n & operMi) != 0) nz = -nz;

        nextLerpData.lerpPos = new Vector3((float)nx / 512.0f, (float)ny / 512.0f, (float)nz / 512.0f);
        nextLerpData.lerpDir = na > 360 ? Vector3.zero : Quaternion.AngleAxis((float)na, Vector3.up) * referenceForward;
        nextLerpData.state = (eSheepState)ns;
        //nextLerpData.lerpTime = newMove.time;
        moveState = nextLerpData.state;

        OnChangeState();

        //if (lerpTime < 0)
        //{ 
        //    transform.position = nextLerpData.lerpPos;
        //    direction = nextLerpData.lerpDir;
        //    //moveState = nextLerpData.state;
        //    lerpTime = newMove.time;
        //} 
    }
    public void Jump(Vector3 pos, Vector3 dir, NetGameState.PlayerId playerId)
    {
        if (!IsServer) return;
        if (moveState == eSheepState.isJump) return;
        if (moveState == eSheepState.onGoal) return;
        if (moveState == eSheepState.isStun) return;
        if (bodyCollider.enabled == false) return;
        if (NetworkManager.Singleton.ServerTime.TimeAsFloat - jumpTimer < 0.5f) return;
        direction = pos - transform.position;
        direction.y = 0;
        direction.Normalize();
        direction = direction + new Vector3(0, jumpPowerUp, 0);
        rb.AddForce(direction * jumpPower);
        jumpTimer = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        moveState = eSheepState.isJump;
        if (dicLastContactTime.ContainsKey(playerId)) dicLastContactTime[playerId] = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        else dicLastContactTime.Add(playerId, NetworkManager.Singleton.ServerTime.TimeAsFloat);
    }
    public void RunAway(Vector3 pos, Vector3 dir, NetGameState.PlayerId playerId)
    {
        if (!IsServer) return;
        if (moveState == eSheepState.isJump) return;
        if (moveState == eSheepState.onGoal) return;
        if (moveState == eSheepState.isStun) return;
        if (bodyCollider.enabled == false) return;

        direction = (transform.position - pos).normalized;
        //direction += dir;
        direction.y = 0;
        direction.Normalize();
        targetPos = pos;// transform.position; 
        runAwayTimer = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        moveState = eSheepState.isRun;
        if (dicLastContactTime.ContainsKey(playerId)) dicLastContactTime[playerId] = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        else dicLastContactTime.Add(playerId, NetworkManager.Singleton.ServerTime.TimeAsFloat);
    }
    public void ChangeDirection(Vector3 pos, Vector3 dir, float serverTime, NetGameState.PlayerId playerId)
    {
        if (!IsServer) return;
        if (moveState == eSheepState.isJump) return;
        if (moveState == eSheepState.isRun) return;
        if (moveState == eSheepState.isRunOnBark) return;
        if (moveState == eSheepState.isStun) return;
        if (moveState == eSheepState.onGoal) return;

        //direction = (transform.position - pos).normalized;
        //direction += dir;
        direction = dir + Vector3.Cross(Vector3.up, dir) * UnityEngine.Random.Range(-0.5f, 0.5f);
        direction.y = 0;
        direction.Normalize();
        targetPos = pos;
        runAwayTimer = serverTime;
        moveState = eSheepState.isRun;
        if (dicLastContactTime.ContainsKey(playerId)) dicLastContactTime[playerId] = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        else dicLastContactTime.Add(playerId, NetworkManager.Singleton.ServerTime.TimeAsFloat);
    }
    public void RunOnBark(Vector3 pos, Vector3 dir, NetGameState.PlayerId playerId)
    {
        if (!IsServer) return;
        if (moveState == eSheepState.isJump) return;
        if (moveState == eSheepState.onGoal) return;
        if (bodyCollider.enabled == false) return;
        if (isBlackGoat)
        {
            stunTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
            moveState = eSheepState.isStun;
            return;
        }
        //direction += (transform.position - pos).normalized;
        direction = dir;
        direction.y = 0;
        direction.Normalize();
        targetPos = transform.position;
        runAwayTimer = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        moveState = eSheepState.isRunOnBark;
        if (dicLastContactTime.ContainsKey(playerId)) dicLastContactTime[playerId] = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        else dicLastContactTime.Add(playerId, NetworkManager.Singleton.ServerTime.TimeAsFloat);
    }
    int CalculateIntAngle(Vector3 dir)
    {
        if (dir == Vector3.zero) return 400;
        float angle = Vector3.SignedAngle(Vector3.forward, dir, Vector3.up);
        if (angle < 0) angle += 360.0f;
        return (int)angle;
    }
    void UpdateNetworkMove()
    {
        if (isActive == false) return;

        MoveData data = new MoveData();
        int nx = (int)(transform.position.x * 512.0f);
        int ny = (int)(transform.position.y * 512.0f);
        int nz = (int)(transform.position.z * 512.0f);

        if (prevX == nx && prevY == ny && prevZ == nz && prevMoveState == moveState)
        {
            return;
        }
        else
        {
            prevX = nx;
            prevY = ny;
            prevZ = nz;
        }

        int n = 0;
        int angle = CalculateIntAngle(direction);
        if (nx < 0)
        {
            n = operMi << 16;
            nx = -nx;
        }
        if (ny < 0)
        {
            ny = 0;
        }
        if (nz < 0)
        {
            n = n | operMi;
            nz = -nz;
        }
        data.xzPos = n + ((nx & operP1) << 16) + ((nx & operP2) << 16) + (nz & operP1) + (nz & operP2);
        data.posYnAnglenState = ((ny & operHt) << 19) + ((angle & operAg) << 10) + (int)moveState;
        //data.time = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        moveData.Value = data;
    }
    void OnGoal(Vector3 toPos)
    {
        targetPos = toPos;
        moveState = eSheepState.onGoal;
        rb.useGravity = false;
        bodyCollider.enabled = false;
    }
    void ScorePoint()
    {
        SampleCode_Director sheepDirector = GameContext.GetDirector<SampleCode_Director>();
        sheepDirector.AddScore(1, !isBlackGoat);
        float latestTime = 10000f;
        NetGameState.PlayerId lastContact = NetGameState.PlayerId.Invalid;
        foreach (var p in dicLastContactTime)
        {
            if (p.Key == NetGameState.PlayerId.Invalid) continue;
            float contactTime = NetworkManager.Singleton.ServerTime.TimeAsFloat - p.Value;
            if (contactTime < latestTime)
            {
                latestTime = contactTime;
                lastContact = p.Key;
            }
        }
        if (lastContact != NetGameState.PlayerId.Invalid)
        {
            foreach (var p in dicLastContactTime)
            {
                if (p.Key == NetGameState.PlayerId.Invalid) continue;
                float contactTime = NetworkManager.Singleton.ServerTime.TimeAsFloat - p.Value;
                if (contactTime < contactRecordTime && p.Key != lastContact)
                {
                    sheepDirector.AddPlayerScore(p.Key, scoreAssist);
                }
            }
            sheepDirector.AddPlayerScore(lastContact, scoreGoal);
        }
        Deactivate(sheepDirector.GetOutOfBoundsPosition());
    }
    void AddPenalty()
    {
        if (NetworkManager.Singleton.ServerTime.TimeAsFloat - penaltyTime < penaltyLimitTime) return;
        penaltyTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        SampleCode_Director sheepDirector = GameContext.GetDirector<SampleCode_Director>();
        float latestTime = 10000f;
        NetGameState.PlayerId lastContact = NetGameState.PlayerId.Invalid;
        foreach (var p in dicLastContactTime)
        {
            if (p.Key == NetGameState.PlayerId.Invalid) continue;
            float contactTime = NetworkManager.Singleton.ServerTime.TimeAsFloat - p.Value;
            if (contactTime < latestTime)
            {
                latestTime = contactTime;
                lastContact = p.Key;
            }
        }
        if (lastContact != NetGameState.PlayerId.Invalid)
        {
            sheepDirector.AddPlayerScore(lastContact, scorePenalty);
        }
    }
    void MoveStateCheck()
    {
        switch (moveState)
        {
            case eSheepState.idle:
                {
                    //SheepPhysics(Time.fixedDeltaTime);
                    randomWalkTimer += Time.fixedDeltaTime;
                    if (randomWalkTimer > randomWalkStartTime)
                    {
                        moveState = eSheepState.isWalk;
                    }
                }
                break;
            case eSheepState.isWalk:
                {
                    rb.velocity = direction * walkSpeed;
                    //SheepPhysics(Time.fixedDeltaTime);
                    if ((transform.position - targetPos).magnitude > walkDistance || direction == Vector3.zero)
                    {
                        moveState = eSheepState.idle;
                    }
                }
                break;
            case eSheepState.onSpawn:
                {
                    direction = targetPos - transform.position;
                    direction.y = 0;
                    direction.Normalize();
                    rb.velocity = direction * runAwaySpeed;
                    if ((transform.position - targetPos).magnitude < 0.2f)
                    {
                        rb.useGravity = true;
                        bodyCollider.enabled = true;
                        moveState = eSheepState.idle;
                    }
                    SampleCode_Director sheepDirector = GameContext.GetDirector<SampleCode_Director>();
                    if (sheepDirector && sheepDirector.IsInSpawnPoint(transform.position, sheepRadius))
                    {
                        rb.useGravity = true;
                        bodyCollider.enabled = true;
                    }
                }
                break;
            case eSheepState.isRun:
                {
                    Vector3 dir = Vector3.zero;
                    Vector3 dist = Vector3.zero;
                    foreach (Collider c in Physics.OverlapSphere(transform.position, seeRadius))
                    {
                        if (c.CompareTag("Enemy"))
                        {
                            SampleCode_Sheep s = c.transform.parent.GetComponent<SampleCode_Sheep>();
                            if (s && s.isBlackGoat == isBlackGoat)
                            {
                                dist = c.transform.position - transform.position;
                                dist.y = 0;
                                if (dist.magnitude < seeRadius && dist.magnitude > sheepRadius && Vector3.Dot(dist, direction) > 0)
                                {
                                    dir += dist;
                                }
                            }
                        }
                    }
                    direction = (direction + dir.normalized * coherenceFactor).normalized;
                    rb.velocity = direction * runAwaySpeed;
                    //if ((transform.position - targetPos).magnitude > runAwayDistance)
                    if (NetworkManager.Singleton.ServerTime.TimeAsFloat - runAwayTimer > runAwayTime)
                    {
                        moveState = eSheepState.idle;
                    }
                }
                break;
            case eSheepState.isRunOnBark:
                {
                    Vector3 dir = Vector3.zero;
                    Vector3 dist = Vector3.zero;
                    foreach (Collider c in Physics.OverlapSphere(transform.position, seeRadius))
                    {
                        if (c.CompareTag("Enemy"))
                        {
                            SampleCode_Sheep s = c.transform.parent.GetComponent<SampleCode_Sheep>();
                            if (s && s.isBlackGoat == isBlackGoat)
                            {
                                dist = c.transform.position - transform.position;
                                dist.y = 0;
                                if (dist.magnitude < seeRadius && dist.magnitude > sheepRadius && Vector3.Dot(dist, direction) > 0)
                                {
                                    dir += dist;
                                }
                            }
                        }
                    }
                    direction = (direction + dir.normalized * coherenceFactor).normalized;
                    rb.velocity = direction * runAwaySpeed;
                    //if ((transform.position - targetPos).magnitude > barkRunAwayDistance)
                    if (NetworkManager.Singleton.ServerTime.TimeAsFloat - runAwayTimer > barkRunAwayTime)
                    {
                        moveState = eSheepState.idle;
                    }
                }
                break;
            case eSheepState.isJump:
                {
                    if (transform.position.y < 0.1f && NetworkManager.Singleton.ServerTime.TimeAsFloat - jumpTimer > 0.5f)
                    {
                        moveState = eSheepState.idle;
                    }
                }
                break;
            case eSheepState.isStun:
                {
                    if (NetworkManager.Singleton.ServerTime.TimeAsFloat - stunTime > stunDuration)
                    {
                        moveState = eSheepState.idle;
                    }
                }
                break;
            case eSheepState.onGoal:
                {
                    direction = targetPos - transform.position;
                    direction.y = 0;
                    direction.Normalize();
                    if (transform.position.y > 0)
                    {
                        transform.position += Vector3.down * Time.fixedDeltaTime * 4.2f;
                    }
                    rb.velocity = direction * runAwaySpeed;
                    if ((transform.position - targetPos).magnitude < 0.2f)
                    {
                        moveState = eSheepState.idle;
                        ScorePoint();
                    }
                }
                break;
        }
    }
    void OnChangeState()
    {
        if (moveState == prevMoveState) return;

        if (prevMoveState == eSheepState.onSpawn)
        {
            direction = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.0f));
            direction.Normalize();
        }
        else if (prevMoveState == eSheepState.isJump)
        {
            feedbacks_OnLand?.PlayFeedbacks();
        }
        switch (moveState)
        {
            case eSheepState.idle:
                {
                    animParameter_AnimState = 0;
                    animParameter_Speed = 1.0f;
                    randomWalkTimer = 0;
                    randomWalkStartTime = UnityEngine.Random.Range(minWalkStartTime, maxWalkStartTime);
                    rb.velocity = Vector3.zero;
                }
                break;
            case eSheepState.onSpawn:
                {
                    animParameter_AnimState = 0;
                    animParameter_Speed = 1.0f;
                }
                break;
            case eSheepState.isJump:
                {
                    animParameter_AnimState = 3;
                    animParameter_Speed = 1.0f;
                    feedbacks_OnJumpStart?.PlayFeedbacks();
                }
                break;
            case eSheepState.isWalk:
                {
                    animParameter_AnimState = 1;
                    animParameter_Speed = 1.0f;
                    targetPos = transform.position;
                    direction = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.0f));
                    direction.Normalize();
                }
                break;
            case eSheepState.isRun:
                {
                    animParameter_AnimState = 2;
                    animParameter_Speed = 2.0f;
                }
                break;
            case eSheepState.isRunOnBark:
                {
                    animParameter_AnimState = 2;
                    animParameter_Speed = 2.0f;
                    feedbacks_OnBarkDetected?.PlayFeedbacks();
                }
                break;
            case eSheepState.isStun:
                {
                    animParameter_AnimState = 4;
                    animParameter_Speed = 1.0f;
                    rb.velocity = Vector3.zero;
                    feedbacks_OnStunned?.PlayFeedbacks();
                }
                break;
            case eSheepState.onGoal:
                {
                    animParameter_AnimState = 1;
                    animParameter_Speed = 1.0f;
                }
                break;
        }
        if (animator)
        {
            animator.SetInteger("AnimState", animParameter_AnimState);
            animator.SetFloat("Speed", animParameter_Speed);
        }
        prevMoveState = moveState;
    }
    public void CallOnMoveFeedback()
    {
        feedbacks_OnMove?.PlayFeedbacks();
    }
    /*
    void SheepPhysics(float time)
    {
        Vector3 dist = Vector3.zero;
        Vector3 coherence = Vector3.zero;
        Vector3 seperation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        float count = 0;
        speed = walkSpeed;//rb.velocity.magnitude;
        //speed += time * deceleration;
        if (speed < 0) { speed = 0; }
        foreach (Collider c in Physics.OverlapSphere(transform.position, seeRadius))
        {
            //if (c.CompareTag("Player"))
            //{
            //    dist = c.transform.position - transform.position;
            //    dist.y = 0;
            //    if (dist.magnitude < jumpDetectRadius)
            //    {
            //        Jump(c.transform.position, c.transform.forward);
            //    }
            //    else if (dist.magnitude > runAwayDetectRadius)
            //    {
            //        speed = runAwaySpeed;
            //        velocity = -dist.normalized * speed;
            //    }
            //    break;
            //}
            if (c.CompareTag("Enemy"))
            {
                dist = transform.position - c.transform.position;
                dist.y = 0;
                if (dist.magnitude < seeRadius)
                {
                    count += 1.0f;
                    coherence += c.transform.position;
                    seperation = dist - seperation;
                    alignment += c.transform.parent.GetComponent<ShepherdDogSheep>().velocity;
                }
                if (coherence != Vector3.zero)
                {
                    coherence = coherence * coherenceFactor / count;
                }
                if (seperation != Vector3.zero)
                {
                    seperation = seperation * seperationFactor;
                }
                if (alignment != Vector3.zero)
                {
                    alignment = alignment * alignmentFactor / count;
                }
                velocity += coherence + seperation + alignment;
                velocity = velocity.normalized * speed;
            }
        }
        velocity.y = 0;
        velocity.Normalize();
        rb.velocity = velocity * speed;
        if (velocity != Vector3.zero) direction = velocity;
    } 
    */
}

