using UnityEngine;
public class EnemyMove : MonoBehaviour
{ 
    public float speed = 10.0f;
    public float accelerationTime = 5.0f;
    public float collideDamage = 5.0f;
    public GameObject enemyObject;
    public GameObject shadowObject;
    public float attackInterval = 2.0f;
    public float dashInterval = 2.0f;
    public float moveAwayTime = 0.5f;
    public float dashTime = 3.0f;
    public float movementPanelty = 0.9f;
    public float dashCorrectionCoefficient = 1.2f;
    public float holdPosTime = 2.0f;
    public float turnAngle = 15.0f;
    public float initialSpeed = 5.0f;
    public float acceleration = 5.0f;
    public float flyingHeight;
    public int movePhase = 0;
    public Vector3 moveDir = Vector3.zero;
    public Vector3 knockbackDir = Vector3.one;

    protected EnemyState state;
    protected Transform playerTransform;
    protected CarControl playerControl;
    protected PlayerState playerState;
    protected Vector3 movePos = Vector3.zero;
    protected Vector3 toPlayerPos = Vector3.zero;
    protected Vector3 collideDir = Vector3.zero;
    protected Vector3 randomPos = Vector3.zero;
    protected Vector3 randomDir = Vector3.zero;
    protected Vector3 relativePos = Vector3.zero;
    protected Vector3 gravity = new Vector3(0, -9.8f, 0);
    protected Vector3 jumpHeight = new Vector3(0, 10.0f, 0);
    protected Vector3 followPos = Vector3.zero;
    protected Vector3 attackPos = Vector3.zero;
    protected Transform relativeTransform;
    protected float moveAwayTimer = 0;
    protected float attackTimer = 0;
    protected float dashTimer = 0;
    protected float accelTimer = 0;
    protected float holdPosTimer = 0;
    protected float prevDot = 1; 
    protected RaycastHit hit;

    public bool isAttackPlayer = false;
    public bool isDash = false;
    public bool isDamaged = false;
    public bool isCollide = false;
    public bool isTeleport = false;
    public bool isPlayerFollow = false;
    public bool isHeavy = false;
    public bool isJump = false;
    public bool isStickToPlayer = false;
    public bool isKnockback = false;
    public bool isStatic = false;
    public bool isSuicide = false;
    public bool isUnderground = false;
    public bool isFlying = false;
    public bool useShadow = true;
    public static float cosineQuaterPI = 1.0f / Mathf.Pow(2.0f, 0.5f);
    public static float gravitationalAcceleration = -9.8f;
    private void Awake()
    {
        state = GetComponent<EnemyState>();
        Init(); 
    }
    private void OnEnable()
    {
        playerTransform = PlayerManager.I.player.transform;
        playerControl = PlayerManager.I.player.GetComponent<CarControl>();
        playerState = PlayerManager.I.player.GetComponent<PlayerState>();

        moveDir = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.0f)).normalized;
        movePos = transform.position;
        movePhase = 0;
        attackTimer = 0;
        accelTimer = 0;
        moveAwayTimer = 0;
        dashTimer = 0;
        isAttackPlayer = false;
        isDash = false;
        isDamaged = false;
        isCollide = false; 
        isPlayerFollow = false; 
        isJump = false; 
        isKnockback = false;  
        OnActive();
    }
    void FixedUpdate()
    {
        if (state.isDead == false)
        {
            CalculatePlayerDistance();
            OnAttackPlayer();
            Move();
            StickToGround();
        }
    }
    private void OnDisable()
    {
        OnDeactive(); 
    }
    private void OnDestroy()
    {
        Final();
    }
    protected virtual void Init() { }
    protected virtual void OnActive() { }
    protected virtual void CalculatePlayerDistance()
    {
        toPlayerPos = playerTransform.position - transform.position;
        toPlayerPos.y = 0;
        if (enemyObject.activeSelf == true && toPlayerPos.magnitude > TimerManager.I.viewDistance)
        {
            enemyObject.SetActive(false);
        }
        else if (enemyObject.activeSelf == false && toPlayerPos.magnitude < TimerManager.I.viewDistance)
        {
            enemyObject.SetActive(true);
        }
    }
    protected virtual void OnAttackPlayer()
    {
        if (isAttackPlayer == true)
        {
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
    }
    protected virtual void Move() { } 
    protected virtual void StickToGround()
    {
        float terrainHeight = TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z);
        if (isJump == false && isKnockback == false && isFlying == false) transform.position = new Vector3(movePos.x, terrainHeight, movePos.z);
        else transform.position = movePos;
        if (isJump == false && moveDir.magnitude > 0) transform.rotation = Quaternion.LookRotation(moveDir.normalized, TerrainManager.I.GetTerrainNormal(movePos.x, movePos.z));
        //transform.up = TerrainManager.I.GetTerrainNormal(movePos.x, movePos.z); 
        //transform.forward = moveDir.normalized; 
        float shadowHeight = terrainHeight - transform.position.y;
        if (shadowObject != null)
        {
            if (useShadow == true && shadowHeight <= 0)
            {
                if (shadowObject.activeSelf == false)
                    shadowObject.SetActive(true);
                shadowObject.transform.localPosition = new Vector3(0, shadowHeight, 0);
            }
            else if (shadowObject.activeSelf == true)
            {
                shadowObject.SetActive(false);
            }
        }

    }
    protected virtual void OnDeactive() { }
    protected virtual void Final() { }   
    private void OnTriggerStay(Collider other)
    {
        collideDir = transform.position - other.transform.position;
        collideDir.y = 0;
        if (other.gameObject.CompareTag("Enemy") == true)
        {
            float r = other.GetComponent<EnemyState>().radius;
            transform.position = transform.position + collideDir.normalized * (state.radius + r - collideDir.magnitude); 
        }
        if (other.gameObject.CompareTag("Player"))
        {
            if (isSuicide == true)
            {
                playerState.DamagePlayer(state.damage, transform.position);
                ProjectileManager.I.ActivateProjectileInstance("GasExplosionFire", transform.position, Vector3.zero, Vector3.zero, 1.0f); 
                state.DestroySelf();
            }
            if (isAttackPlayer == false)
            {
                float dot = Vector3.Dot(collideDir.normalized, playerControl.GetActualVelocity().normalized);
                if (isStatic == false && dot > cosineQuaterPI && movePhase < 4 && playerState.isInvincible == true)
                {
                    Knockback((collideDir + Vector3.up).normalized * Mathf.Abs(dot * playerControl.GetActualVelocity().magnitude));
                }
                else if (isStatic == true && playerState.isInvincible == true)
                {
                    Knockback((collideDir + Vector3.up).normalized * Mathf.Abs(dot * playerControl.GetActualVelocity().magnitude));
                    state.DisableBlocker();
                }
                if (playerState.isInvincible == false)
                {
                    if (isHeavy == true) playerControl.PushCar(moveDir.normalized, 10.0f);
                    if (isStickToPlayer == true) playerControl.SlowCar(0.16f);
                    //근접공격 데미지
                    playerState.DamagePlayer(state.damage, transform.position); 
                }
                ProjectileManager.I.ActivateProjectileInstance("EnemyHit", transform.position, Vector3.zero, Vector3.zero, 1.0f);
                //근접공격 반사 데미지
                float meleeDamage = playerState.life * 0.1f + playerState.level;
                if (dot > 0) state.DamageMonster(meleeDamage, Color.red);
                isAttackPlayer = true;
                attackTimer = 0;
                accelTimer = 0;
                isPlayerFollow = false;
            }
        } 
        else if (other.gameObject.CompareTag("Enemy") == false && other.gameObject.CompareTag("Missile") == false)
        {
            moveDir = transform.position - other.transform.position;
            moveDir.y = 0;
            transform.position = transform.position + moveDir.normalized * speed * Time.deltaTime;
            if (moveDir.magnitude > state.radius * 0.9f)
            {
                moveAwayTimer = 0;
                dashTimer = 0;
                isDash = false;
                isCollide = true;
            }
        }
    } 
    //void Move()
    //{ 
    //    switch (state.type)
    //    {
    //        case eEnemyType.eType001: MoveType001(); break;
    //        case eEnemyType.eType002: MoveType002(); break;
    //        case eEnemyType.eType003: MoveType003(); break;
    //        case eEnemyType.eType004: MoveType004(); break;
    //        case eEnemyType.eType005: MoveType005(); break;
    //        case eEnemyType.eType006: MoveType006(); break;
    //        case eEnemyType.eType007: MoveType007(); break;
    //        case eEnemyType.eType008: MoveType008(); break;
    //    }
    //}
    //플레이어 좌우 방향으로 랜덤거리를 향해 돌진하는 패턴. 
    void MoveType001() 
    {
        if (isAttackPlayer == true)
        {
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        if (moveDir == Vector3.zero) moveDir = transform.forward;
        float dot = Vector3.Dot(playerTransform.forward.normalized, -toPlayerPos.normalized);
        Vector3 toPlayerRight = Vector3.Cross(Vector3.up, toPlayerPos.normalized);

        if (isDash == true)
        {
            dashTimer += Time.fixedDeltaTime;
            if (toPlayerPos.magnitude > 40.0f)
            {
                if (dashTimer > 7.0f || dot > 0)
                {
                    isDash = false;
                    moveDir = toPlayerPos;
                    dashTimer = 0;
                }
            }
        }
        if (isDash == true)
        {
            if (speed < playerControl.GetLimitSpeed().magnitude + 12.0f)
                speed += 12.0f * Time.fixedDeltaTime;
        }
        else if (isDash == false && Mathf.Abs(dot) < cosineQuaterPI && toPlayerPos.magnitude > 40.0f)
        {
            isDash = true;
            dashTimer = 0;
            moveDir = toPlayerPos + (toPlayerRight + playerControl.GetLimitSpeed().normalized) * UnityEngine.Random.Range(-10.0f, 10.0f);
            //moveDir = toPlayerPos + playerTransform.right * UnityEngine.Random.Range(-10.0f, 10.0f);
        }
        else if (isDash == false && dot < -cosineQuaterPI && toPlayerPos.magnitude > 40.0f)
        {
            isDash = true;
            dashTimer = 0;
            moveDir = toPlayerPos + playerTransform.right * UnityEngine.Random.Range(-10.0f, 10.0f);
        }
        else
        {
            moveDir = Vector3.RotateTowards(moveDir, toPlayerPos, turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
            //moveDir = toPlayerPos;
            speed = 10.0f;
        }



        //if (isDash == false && dashTimer > dashInterval)
        //{
        //    isDash = true;
        //    dashTimer = 0;
        //    moveDir = toPlayerPos + playerTransform.right * UnityEngine.Random.Range(-10.0f, 10.0f);
        //}

        //else if (dot >= 0)
        //{
        //    moveDir = Vector3.RotateTowards(moveDir, toPlayerPos, turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
        //    speed = 10.0f;
        //}

        if (isCollide == true)
        {
            moveAwayTimer += Time.fixedDeltaTime;
            movePos = transform.position + moveDir.normalized * speed * 0.5f * Time.fixedDeltaTime;
            if (moveAwayTimer > moveAwayTime)
            {
                isCollide = false;
            }
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            } 
        }
        else
        {
            movePos = transform.position + moveDir.normalized * speed * Time.fixedDeltaTime;
        }
    }
    //안움직인다.
    void MoveType002()
    {
        if (isAttackPlayer == true)
        {
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        float dot = Vector3.Dot(playerTransform.forward, -toPlayerPos);
        //if (movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
        //{
        //
        //}
        if (dot < 0 && toPlayerPos.magnitude > 35.0f && isTeleport == true)
        {
            movePos = new Vector3(UnityEngine.Random.Range(-35.0f, 35.0f), 0, UnityEngine.Random.Range(-35.0f, 35.0f));
            movePos = PlayerManager.I.player.transform.position +
                PlayerManager.I.player.transform.forward * 150.0f +
                movePos;
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            }
        }
        moveDir = toPlayerPos;
    }
    //플레이어 뒤를 쫓아오면서 통과하는 패턴
    void MoveType003()
    {
        if (isAttackPlayer == true)
        {
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        if (moveDir == Vector3.zero) moveDir = transform.forward;
        float dot = Vector3.Dot(playerTransform.forward, -toPlayerPos);

        if (isPlayerFollow == false && (dot < 0 || toPlayerPos.magnitude > 40.0f))
        {
            isPlayerFollow = true;
        }
        if (isPlayerFollow == true && dot >= 0 && toPlayerPos.magnitude > 40.0f)
        {
            isPlayerFollow = false;
        }

        if (isPlayerFollow == true)
        {
            if (speed < playerControl.GetLimitSpeed().magnitude + 20.0f)
                speed += acceleration * Time.fixedDeltaTime;
            moveDir = Vector3.RotateTowards(moveDir, toPlayerPos, turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
        }
        else
        { 
            speed -= 8.0f * acceleration * Time.fixedDeltaTime;
            moveDir = Vector3.RotateTowards(moveDir, toPlayerPos, 5.0f * turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
            if (speed < initialSpeed)
            {
                speed = initialSpeed;
                isPlayerFollow = true;
                moveDir = toPlayerPos.normalized;
            }
        }
        //if (dot < 0 || toPlayerPos.magnitude > 20.0f)
        //{
        //    if (speed < playerControl.velocity.magnitude * 1.1f)
        //        speed += acceleration * Time.fixedDeltaTime;
        //}
        //else
        //{
        //    if (speed > initialSpeed)
        //        speed -= acceleration * Time.fixedDeltaTime;
        //}
        if (isCollide == true)
        {
            moveAwayTimer += Time.fixedDeltaTime;
            movePos = transform.position + moveDir.normalized * speed * 0.5f * Time.fixedDeltaTime;
            if (moveAwayTimer > moveAwayTime)
            {
                isCollide = false;
            }
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            }
        }
        else
        {
            movePos = transform.position + moveDir.normalized * speed * Time.fixedDeltaTime;
        }
    }
    //플레이어를 앞질러나가 플레이어 전방에서 방향을 틀어 플레이어를 향해 돌진하는 패턴
    void MoveType004()
    {
        if (isAttackPlayer == true)
        {
            isCollide = true;
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        if (moveDir == Vector3.zero) moveDir = transform.forward;
        float dot = Vector3.Dot(playerTransform.forward, -toPlayerPos);

        switch (movePhase)
        {
            case 0:
                {
                    if (speed < playerControl.GetLimitSpeed().magnitude * 1.1f)
                        speed += acceleration * Time.fixedDeltaTime;
                    //moveDir = toPlayerPos + playerControl.velocity * 10.0f;
                    moveDir = Vector3.RotateTowards(moveDir, toPlayerPos + playerControl.GetLimitSpeed() * 10.0f, 5.0f * turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
                    if (dot > 0 && toPlayerPos.magnitude > 100.0f)
                    {
                        movePhase = 1;
                        speed = initialSpeed * 2.0f;
                        moveDir = toPlayerPos;
                    }
                }
                break;
            case 1:
                {
                    if (speed < playerControl.GetLimitSpeed().magnitude * 1.1f)
                        speed += acceleration * Time.fixedDeltaTime;
                    moveDir = Vector3.RotateTowards(moveDir, toPlayerPos, turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
                    if (dot < 0 && toPlayerPos.magnitude > 20.0f)
                    {
                        movePhase = 0;
                        speed = initialSpeed;
                        moveDir = toPlayerPos;
                    }
                }
                break; 
        }

        //if (dot * prevDot < 0) speed = initialSpeed;
        //if (movePhase == 0)
        //{
        //    if (speed < playerControl.velocity.magnitude * 1.1f)
        //        speed += acceleration * Time.fixedDeltaTime;
        //    moveDir = toPlayerPos + playerControl.velocity * 10.0f;
        //}
        //else
        //{
        //    if (speed < playerControl.velocity.magnitude)
        //        speed += acceleration * Time.fixedDeltaTime;
        //    moveDir = Vector3.RotateTowards(moveDir, toPlayerPos, turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
        //
        //}
        //prevDot = dot;

        if (isCollide == true)
        {
            moveAwayTimer += Time.fixedDeltaTime;
            movePos = transform.position + moveDir.normalized * speed * 0.5f * Time.fixedDeltaTime;
            if (moveAwayTimer > moveAwayTime)
            {
                isCollide = false;
            }
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            }
        }
        else
        {
            movePos = transform.position + moveDir.normalized * speed * Time.fixedDeltaTime;
        }
    }
    //플레이어를 앞질러나가 플레이어 전방에서 방향을 틀어 플레이어를 향해 돌진하는 패턴
    void MoveType005()
    {
        if (isAttackPlayer == true)
        { 
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        //if (moveDir == Vector3.zero) moveDir = transform.forward;
        float dot = Vector3.Dot(playerTransform.forward, -toPlayerPos);
        Vector3 toPlayerRight = Vector3.Cross(Vector3.up, toPlayerPos.normalized);

        //if (isPlayerFollow == false && (dot < 0 || toPlayerPos.magnitude > 40.0f))
        //{
        //    movePhase = 0;
        //    isPlayerFollow = true;
        //    isDash = true;
        //    dashTimer = 0;
        //}
        //if (isPlayerFollow == true)
        //{
        //    dashTimer += Time.fixedDeltaTime;
        //    float waitTime = isDash ? 7.0f : 1.0f;
        //    if (dashTimer > waitTime)
        //    {
        //        isDash = !isDash;
        //        if (isDash)
        //        {
        //            moveDir = toPlayerPos +
        //                (PlayerManager.I.player.transform.forward +
        //                PlayerManager.I.player.transform.right * UnityEngine.Random.Range(-1.0f, 1.0f)).normalized;
        //            //moveDir = PlayerManager.I.player.transform.position + 
        //            //    (PlayerManager.I.player.transform.forward + 
        //            //    PlayerManager.I.player.transform.right * UnityEngine.Random.Range(-1.0f, 1.0f)).normalized *
        //            //    UnityEngine.Random.Range(25.0f, 40.0f) - transform.position;
        //            moveDir.y = 0;
        //            speed = 0;
        //        }
        //        else
        //        {
        //            moveDir = toPlayerPos;
        //        }
        //        dashTimer = 0;
        //    }
        //}


        if (isKnockback == false)
        {
            switch (movePhase)
            {
                case 0:
                    {
                        moveDir = toPlayerPos; 
                        if (toPlayerPos.magnitude < 5.0f)
                        {
                            movePhase = 4;
                        }
                        else if (dot < 0 && toPlayerPos.magnitude > 40.0f)
                        {
                            movePhase = 1;
                            isDash = false;
                            isJump = true;
                            moveDir = toPlayerPos;
                            dashTimer = 0; 
                        }
                        else
                        {
                            movePos = transform.position + moveDir.normalized * 10.0f * Time.fixedDeltaTime;
                        }




                        //isJump = true;
                        //transform.LookAt(transform.position - new Vector3(0, -10.0f, 0), -toPlayerPos.normalized);
                        //
                        //randomPos = playerTransform.position + 
                        //    (playerTransform.forward +
                        //    playerTransform.right * UnityEngine.Random.Range(-1.0f, 1.0f)).normalized *
                        //    UnityEngine.Random.Range(40.0f, 100.0f);
                        //moveDir = randomPos - transform.position;
                        //movePhase = 1;
                    }
                    break;
                case 1:
                    {
                        transform.LookAt(transform.position + new Vector3(0, -10.0f, 0), -toPlayerPos.normalized);
                        movePos = transform.position + transform.forward * 5.0f * Time.fixedDeltaTime;
                        if (TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z) - movePos.y > 10.0f)
                        {
                            state.isInvincible = true;
                            isJump = true;
                            movePhase = 2;
                            randomDir = Quaternion.AngleAxis(UnityEngine.Random.Range(-45.0f, 45.0f), Vector3.up) * playerTransform.forward;
                            movePos = playerTransform.position + randomDir * UnityEngine.Random.Range(playerControl.GetLimitSpeed().magnitude * 5.0f, playerControl.GetLimitSpeed().magnitude * 7.5f);// UnityEngine.Random.Range(playerControl.GetCurrentSpeed() * 1.5f, playerControl.GetCurrentSpeed() * 3.0f);
                            movePos.y = TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z) - 10.0f; 
                        }
                        //dashTimer += Time.fixedDeltaTime;
                        //if (dashTimer > 0.5f)
                        //{
                        //    dashTimer = 0.5f;
                        //    movePos = Vector3.Lerp(movePos, randomPos, dashTimer * 2.0f) + Vector3.Lerp(Vector3.zero, jumpHeight, dashTimer * 2.0f);
                        //    randomPos = new Vector3(movePos.x, TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z), movePos.z);
                        //    dashTimer = 0.0f;
                        //    movePhase = 2;
                        //}
                        //else
                        //{
                        //    movePos = Vector3.Lerp(movePos, randomPos, dashTimer * 2.0f) + Vector3.Lerp(Vector3.zero, jumpHeight, dashTimer * 2.0f);
                        //}
                    }
                    break;
                case 2:
                    {
                        transform.LookAt(transform.position + new Vector3(0, 10.0f, 0), -toPlayerPos.normalized);
                        movePos = transform.position + transform.forward * 5.0f * Time.fixedDeltaTime;
                        if (TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z) - movePos.y < 0.0f)
                        {
                            state.isInvincible = false;
                            isJump = false;
                            movePhase = 0; 
                            transform.LookAt(playerTransform.position, TerrainManager.I.GetTerrainNormal(movePos.x, movePos.z));
                            moveDir = toPlayerPos;
                        }

                        //dashTimer += Time.fixedDeltaTime;
                        //if (dashTimer > 0.5f)
                        //{
                        //    dashTimer = 0.5f; 
                        //    dashTimer = 0.0f;
                        //    isJump = false;
                        //    movePhase = 3;
                        //}
                        //movePos = Vector3.Lerp(movePos, randomPos, dashTimer * 2.0f);
                    }
                    break;
                case 3:
                    {
                        moveDir = toPlayerPos;
                        if (toPlayerPos.magnitude < 5.0f)
                        {
                            movePhase = 4;
                        }
                        else if (dot < 0 && toPlayerPos.magnitude > 40.0f)
                        {
                            movePhase = 0;
                            isDash = false;
                            moveDir = toPlayerPos;
                            dashTimer = 0;
                            speed = 0;
                        }
                    }
                    break;
                case 4:
                    {
                        randomPos = playerTransform.position + new Vector3(UnityEngine.Random.Range(-2.0f, 2.0f), 2.0f, UnityEngine.Random.Range(-2.0f, 2.0f));
                        if (Physics.Raycast(randomPos, playerTransform.position - randomPos, out hit, 100.0f))
                        {
                            relativePos = 0.5f * (hit.point - playerTransform.position);
                            relativeTransform = playerState.GetStickPosition(this);
                            isJump = true;
                            movePhase = 5;
                        }
                    }
                    break;
                case 5:
                    {
                        dashTimer += Time.fixedDeltaTime * 0.5f;
                        if (dashTimer > 1.0f)
                        {
                            dashTimer = 1.0f;
                            isStickToPlayer = true;
                        }
                        movePos = Vector3.Lerp(movePos, relativePos + playerTransform.position, dashTimer);
                        moveDir = movePos - transform.position;
                        moveDir.y = 0;
                        if (isStickToPlayer == true)
                        {
                            relativeTransform.localPosition = relativePos;
                            relativeTransform.forward = transform.forward;
                            relativeTransform.up = relativePos.normalized;
                            //transform.up = relativePos.normalized;
                            moveDir = Vector3.zero;
                            movePhase = 6;
                            speed = 0;
                        }
                    }
                    break;
                case 6:
                    {
                        moveDir = Vector3.zero;
                        movePos = relativeTransform.position;
                        transform.rotation = relativeTransform.rotation;
                        //movePos = relativePos + PlayerManager.I.player.transform.position; 
                    }
                    break;
            }
        }  

        if (isCollide == true)
        {
            moveAwayTimer += Time.fixedDeltaTime;
            movePos = transform.position + moveDir.normalized * speed * 0.5f * Time.fixedDeltaTime;
            if (moveAwayTimer > moveAwayTime)
            {
                isCollide = false;
            }
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            }
            //movePos = transform.position + moveDir.normalized * speed * Time.fixedDeltaTime;
        }
    }
    //플레이어를 일정 속도로 따라가는 패턴
    void MoveType006()
    {
        if (isAttackPlayer == true)
        {
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        if (moveDir == Vector3.zero) moveDir = transform.forward;
        moveDir = toPlayerPos;
        //float dot = Vector3.Dot(playerTransform.forward.normalized, -toPlayerPos.normalized);
        //Vector3 toPlayerRight = Vector3.Cross(Vector3.up, toPlayerPos.normalized);

        if (isCollide == true)
        {
            moveAwayTimer += Time.fixedDeltaTime;
            movePos = transform.position + moveDir.normalized * state.speed * 0.5f * Time.fixedDeltaTime;
            if (moveAwayTimer > moveAwayTime)
            {
                isCollide = false;
            }
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            }
        }
        else
        {
            movePos = transform.position + moveDir.normalized * state.speed * Time.fixedDeltaTime;
        }
    }
    //플레이어를 치고 빠지는 패턴
    void MoveType007()
    {
        if (isAttackPlayer == true)
        {
            isDash = false;
            speed = 10.0f;
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        if (moveDir == Vector3.zero) moveDir = transform.forward;
        float dot = Vector3.Dot(playerTransform.forward.normalized, -toPlayerPos.normalized);

        if (isKnockback == false)
        {
            if (Mathf.Abs(dot) < cosineQuaterPI)
            {
                if (speed < playerControl.GetLimitSpeed().magnitude * 1.2f)
                {
                    speed += acceleration * Time.fixedDeltaTime;
                }
                if (toPlayerPos.magnitude > 40.0f)
                {
                    isDash = false;
                    moveDir = Vector3.RotateTowards(moveDir.normalized, toPlayerPos.normalized, turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
                }
                else if (isDash == false)
                {
                    isDash = true;
                    moveDir = toPlayerPos;
                }
            }
            else if (dot > 0)
            {
                speed = 10.0f;
                moveDir = toPlayerPos;
            }
            else
            {
                if (speed < playerControl.GetLimitSpeed().magnitude * 1.2f)
                {
                    speed += acceleration * Time.fixedDeltaTime;
                }
                if (toPlayerPos.magnitude > 40.0f)
                {
                    isDash = false;
                    moveDir = Vector3.RotateTowards(moveDir.normalized, toPlayerPos.normalized, turnAngle * Mathf.Deg2Rad * Time.fixedDeltaTime, 0.0f);
                }
                else if (isDash == false)
                {
                    isDash = true;
                    moveDir = toPlayerPos;
                }
            } 
        } 

        if (isCollide == true)
        {
            moveAwayTimer += Time.fixedDeltaTime;
            movePos = transform.position + moveDir.normalized * speed * 0.5f * Time.fixedDeltaTime;
            if (moveAwayTimer > moveAwayTime)
            {
                isCollide = false;
            }
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            }
        }
        else
        {
            movePos = transform.position + moveDir.normalized * speed * Time.fixedDeltaTime;
        }
    }
    //완전 정적. 로테이셩 안함
    void MoveType008()
    {
        if (isAttackPlayer == true)
        {
            attackTimer += Time.fixedDeltaTime;
            if (attackTimer > attackInterval)
            {
                isAttackPlayer = false;
            }
        }
        float dot = Vector3.Dot(playerTransform.forward, -toPlayerPos); 
        if (dot < 0 && toPlayerPos.magnitude > 35.0f && isTeleport == true)
        {
            movePos = new Vector3(UnityEngine.Random.Range(-35.0f, 35.0f), 0, UnityEngine.Random.Range(-35.0f, 35.0f));
            movePos = PlayerManager.I.player.transform.position +
                PlayerManager.I.player.transform.forward * 150.0f +
                movePos;
        }
        else if (isKnockback == true)
        {
            knockbackDir = new Vector3(knockbackDir.x * 0.9f, knockbackDir.y + gravitationalAcceleration * Time.fixedDeltaTime, knockbackDir.z * 0.9f);
            movePos += knockbackDir;
            if (knockbackDir.y < 0 && movePos.y < TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z))
            {
                isKnockback = false;
            }
        }
        moveDir = Vector3.zero;
    }
    //void StickToGround()
    //{ 
    //    if (isJump == false && isKnockback == false) transform.position = new Vector3(movePos.x, TerrainManager.I.GetTerrainHeight(movePos.x, movePos.z), movePos.z);
    //    else transform.position = movePos;
    //    if (isJump == false && moveDir.magnitude > 0) transform.rotation = Quaternion.LookRotation(moveDir.normalized, TerrainManager.I.GetTerrainNormal(movePos.x, movePos.z));
    //    //transform.up = TerrainManager.I.GetTerrainNormal(movePos.x, movePos.z); 
    //    //transform.forward = moveDir.normalized;
    //}
    public void Pushback(Vector3 power)
    {
        transform.position += power;
        movePos = transform.position;
    }
    public void Knockback(Vector3 power)
    {
        movePhase = 0;
        dashTimer = 0;
        knockbackDir = power;
        moveDir = -power;
        isJump = false;
        isKnockback = true;
        isPlayerFollow = false;
        isDash = false;
    }
}
