using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CarControl : MonoBehaviour
{

    [SerializeField] GameObject headLightR;
    [SerializeField] GameObject headLightL;
    [SerializeField] GameObject tailLightR;
    [SerializeField] GameObject tailLightL;
    [SerializeField] GameObject wheelFR;
    [SerializeField] GameObject wheelFL;
    [SerializeField] GameObject wheelRR;
    [SerializeField] GameObject wheelRL;
    [SerializeField] TrailRenderer wheelSkidRR;
    [SerializeField] TrailRenderer wheelSkidRL;
    [SerializeField] float frontWheelRadius;
    [SerializeField] float rearWheelRadius;
    [SerializeField] float suspensionDist = 0.2f;
    [SerializeField] float groundDrag = 20.0f;
    [SerializeField] float groundDragCoefficient = 50.0f;
    [SerializeField] float airDragCoefficient = 0.565f;
    [SerializeField] float pushbackForce = 10.0f;
    [SerializeField] float driftDecelerationRate = 0.8f;
    [SerializeField] float boostTime = 3.0f;
    [SerializeField] float uTurnTime = 1.0f; 
    float externalForceReductionRate = 0.8f;
    float testPower = 10.0f; 
    public FixedJoystick joyStick; 
    public OilGauge oilGauge;
    public DriftGauge driftGauge;
    public Rigidbody rb;
    public Transform floatFR;
    public Transform floatFL;
    public Transform floatRR;
    public Transform floatRL; 
    Vector3 velocity = Vector3.zero;
    public float floatForce = 1.0f;
    public float floatDist = 1.0f;
    public float acceleration = 10.0f;
    public float boostAcceleration = 10.0f;
    public float decelerationRate = 0.9f;
    public float maxSpeed = 23.0f;
    public float maxTurnAngle = 15.0f;
    public bool isRunning = true;
    public bool isDrift = false;
    public bool isUTurn = false;
    public bool isBoost = false;
    public bool isOnAir = false;
    PlayerState state; 
    Material matColor; 
    RaycastHit hit;
    Quaternion balanceRot = Quaternion.identity;
    Quaternion uTurnRot = Quaternion.identity;
    Vector3 wheelDefaultPosFR;
    Vector3 wheelDefaultPosFL;
    Vector3 wheelDefaultPosRR;
    Vector3 wheelDefaultPosRL;
    Vector3 contactNormal = Vector3.zero;
    Vector3 suspensionForce = Vector3.zero;
    Vector3 accelerationForce = Vector3.zero;
    Vector3 internalForce = Vector3.zero;
    Vector3 externalForce = Vector3.zero;
    Vector3 driftForce = Vector3.zero;
    Vector3 prevPos = Vector3.zero;
    Vector3 gravitationalAcceleration = new Vector3(0, -9.8f, 0);
    Vector3 uTurnDir = Vector3.zero;
    Vector3 actualSpeed = Vector3.zero;
    Vector3 limitSpeed = Vector3.zero;
    Vector3 rotationEuler = Vector3.zero;
    float currentSpeed;
    float uTurnSpeed;
    float frontWheelRotationAngle = 0;
    float rearWheelRotationAngle = 0;
    float driftDir = 1.0f;
    float inputHor = 0;
    float inputVer = 0;
    float slowRate = 1.0f;
    float slowRecoverRate = 0.5f;
    int layerMask_Self;
    bool blink = false;
    bool isExternalForce = false;
    bool isOnLimit = false;
    public bool forceStop = false;
    public bool isStart = false;
    bool useJoystick = false;
    public enum eVehicleSoundState
    {
        eNone,
        eStartup,
        eIdle,
        eLowOn,
        eLowOff,
        eMedOn,
        eMedOff,
        eHighOn,
        eHighOff,
        eMaxRPM
    }
    eVehicleSoundState soundState = new eVehicleSoundState();
    eVehicleSoundState prevSoundState = new eVehicleSoundState();

  
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        layerMask_Self = (-1) - (1 << LayerMask.NameToLayer("SelfMask") | 1 << LayerMask.NameToLayer("Enemy") | 1 << LayerMask.NameToLayer("Missile") | 1 << LayerMask.NameToLayer("StaticObject") | 1 << LayerMask.NameToLayer("EnemyBlocker") | 1 << LayerMask.NameToLayer("SelfMask_Enemy") | 1 << LayerMask.NameToLayer("Drone") | 1 << LayerMask.NameToLayer("EnemyBullet") | 1 << LayerMask.NameToLayer("Chest") | 1 << LayerMask.NameToLayer("Khopesh"));

        List<Material> listMat = new List<Material>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.Contains("Body"))
            {
                transform.GetChild(i).GetComponent<MeshRenderer>().GetMaterials(listMat);
                foreach(Material mat in listMat)
                {
                    if (mat.name.Contains("ColorVar"))
                    {
                        matColor = mat;
                    }
                }
                break;
            }
        } 

        state = GetComponent<PlayerState>();
        state.OnDamagePlayer = PlayerHit;
        state.OnGameOver = GameOver;
        state.OnResurection = Resurection;
        wheelDefaultPosFR = wheelFR.transform.localPosition;
        wheelDefaultPosFL = wheelFL.transform.localPosition;
        wheelDefaultPosRR = wheelRR.transform.localPosition;
        wheelDefaultPosRL = wheelRL.transform.localPosition;

        rb.centerOfMass =
            (wheelFR.transform.localPosition +
            wheelFL.transform.localPosition +
            wheelRR.transform.localPosition +
            wheelRL.transform.localPosition) * 0.25f;

        //BoxCollider boxCollider = GetComponent<BoxCollider>();
        //floatFR.localPosition = boxCollider.center + new Vector3(boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f, boxCollider.size.z * 0.5f);
        //floatFL.localPosition = boxCollider.center + new Vector3(-boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f, boxCollider.size.z * 0.5f);
        //floatRR.localPosition = boxCollider.center + new Vector3(boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f, -boxCollider.size.z * 0.5f);
        //floatRL.localPosition = boxCollider.center + new Vector3(-boxCollider.size.x * 0.5f, -boxCollider.size.y * 0.5f, -boxCollider.size.z * 0.5f);

        //float test001 = wheelFR.GetComponent<MeshFilter>().mesh.bounds.size.y * 0.5f;
        //float test002 = wheelRR.GetComponent<MeshFilter>().mesh.bounds.size.y * 0.5f;

        float minY = float.MaxValue;
        float maxY = float.MinValue;  
        List<Vector3> verticies = new List<Vector3>();
        wheelFR.GetComponent<MeshFilter>().mesh.GetVertices(verticies);
        foreach (Vector3 v in verticies)
        {
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y; 
        }
        frontWheelRadius = (maxY - minY) * 0.5f;
        minY = float.MaxValue;
        maxY = float.MinValue;
        wheelRR.GetComponent<MeshFilter>().mesh.GetVertices(verticies);
        foreach (Vector3 v in verticies)
        {
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y;
        }
        rearWheelRadius = (maxY - minY) * 0.5f; 

        TimerManager.I.AddTimer("CrashTimer", 0.5f);
        TimerManager.I.AddTimer("DriftTimer", 0.2f);
        TimerManager.I.AddTimer("DriftSoundTimer", 8.0f);
        TimerManager.I.SetTimer("DustEffect", 0.1f); 
        TimerManager.I.AddTimer("Boost", boostTime);
        TimerManager.I.SetOnFinishedCallback("Boost", BoostEnd);
        TimerManager.I.AddTimer("UTurnTimer", 0.2f);
        TimerManager.I.AddTimer("UTurnSpeed", uTurnTime);
        TimerManager.I.AddTimer("PlayerHit", 0.5f);
        TimerManager.I.AddTimer("BlinkInterval", 0.1f); 
        TimerManager.I.AddTimer("ExternalForceTimer", 0.3f);

        soundState = eVehicleSoundState.eStartup;
        prevSoundState = eVehicleSoundState.eNone;

        isStart = false;
    }
    private void Update()
    { 
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ObjectManager.I.ActivateChest(transform.position + new Vector3(UnityEngine.Random.value * 5.0f, 0, UnityEngine.Random.value * 5.0f));
        }
        Control();
    }
    // Update is called once per frame
    void FixedUpdate()
    { 
        if (isRunning)
        { 
            //Acceleration(joyStick.Vertical);
            AutoAcceleration();
            ExternalForceCheck();
            UTurn(inputVer);
            Drift(inputVer, inputHor);
        }
        Steering(inputHor);
        Suspension();
        PreventOverturn();
        CalculateVelocity();
        PlayEffect();
        //PlaySound();
    } 
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ImmovableObject"))
        {
            if (TimerManager.I.IsActive("CrashTimer") == false)
            {
                state.KnockbackStuckEnemies();
                contactNormal = Vector3.zero;
                EnemyMove em = collision.transform.parent.GetComponent<EnemyMove>();
                Rigidbody rb = collision.gameObject.GetComponent<Rigidbody>();
                if (em != null && em.isStatic == true)
                {
                    foreach (ContactPoint cp in collision.contacts)
                    { 
                        contactNormal += cp.normal;
                    }
                }
                else
                {
                    foreach (ContactPoint cp in collision.contacts)
                    {
                        if (rb != null && (state.isInvincible == true || isStart == false))
                        {
                            rb.AddExplosionForce(500000.0f, cp.point, 100.0f);
                        }
                        contactNormal += cp.normal;
                    }
                    if (state.isInvincible == true || isStart == false) return;
                }
                contactNormal.y = 0;
                internalForce = -0.5f * internalForce;
                if (Mathf.Abs(joyStick.Horizontal) > 0.1f)
                {
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0, joyStick.Horizontal * 30.0f, 0));
                }
                else
                {
                    float dot = Vector3.Dot(transform.right, contactNormal);
                    float angle = dot > 0.0f ? 30.0f : -30.0f;
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0, angle, 0));
                }
                externalForce = contactNormal.normalized * pushbackForce;
                currentSpeed = 0;
                isDrift = false;
                isUTurn = false;
                soundState = eVehicleSoundState.eIdle;
                TimerManager.I.DisableTimer("DriftTimer");
                SoundManager.I.PlayOnce("clash");
                if (gameObject.activeSelf == true) StartCoroutine(Deceleration());
            }
        } 
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("ImmovableObject"))
        {
            if (TimerManager.I.IsActive("CrashTimer") == false)
            {
                state.KnockbackStuckEnemies();
                contactNormal = Vector3.zero;
                Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
                if (state.isInvincible == true) return;
                contactNormal = transform.position - other.transform.position;
                contactNormal.y = 0;
                contactNormal.Normalize();
                internalForce = -0.5f * internalForce;
                if (Mathf.Abs(joyStick.Horizontal) > 0.1f)
                {
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0, joyStick.Horizontal * 30.0f, 0));
                }
                else
                {
                    float dot = Vector3.Dot(transform.right, contactNormal);
                    float angle = dot > 0.0f ? 30.0f : -30.0f;
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0, angle, 0));
                }
                externalForce = contactNormal * pushbackForce;
                currentSpeed = 0;
                isDrift = false;
                isUTurn = false;
                soundState = eVehicleSoundState.eIdle;
                TimerManager.I.DisableTimer("DriftTimer");
                if (gameObject.activeSelf == true) StartCoroutine(Deceleration());
            }
        }
    } 
    void Control()
    {
        if (joyStick != null)
        {
            inputHor = joyStick.Horizontal;
            inputVer = joyStick.Vertical;
        }
#if UNITY_EDITOR_WIN
#elif UNITY_STANDALONE_WIN
        inputHor = Input.GetAxis("Horizontal");
        inputVer = Input.GetAxis("Vertical");
#endif
        //if (Input.GetButtonDown("joystickbutton0"))
        //{
        //    Boost();
        //}
    }
    void Acceleration(float verticalAxis)
    {
        if (Mathf.Abs(verticalAxis) > 0.1f)
        {
            currentSpeed += acceleration * verticalAxis * Time.fixedDeltaTime;
            if (currentSpeed > maxSpeed) currentSpeed = maxSpeed;
            if (currentSpeed < -0.5f * maxSpeed) currentSpeed = -0.5f * maxSpeed;

            accelerationForce = transform.forward;
            accelerationForce = accelerationForce.normalized * currentSpeed;
            //accelerationForce.y = rb.velocity.y;
            internalForce = accelerationForce;
        }
    }
    void AutoAcceleration()
    {
        isOnLimit = false;
        slowRate += slowRecoverRate * 0.5f * Time.fixedDeltaTime;
        if (slowRate > 1.0f) slowRate = 1.0f;
        if (isUTurn == true)
        {
            state.KnockbackStuckEnemies();
            float timer = TimerManager.I.GetCurrentTimer("UTurnSpeed");
            if (TimerManager.I.IsFinished("UTurnSpeed") == true)
            {
                timer = uTurnTime;
                isUTurn = false;
            }
            currentSpeed = uTurnSpeed * (uTurnTime - timer) / uTurnTime;
            float turnAngle = 180.0f * timer / uTurnTime;
            transform.rotation = uTurnRot * Quaternion.Euler(new Vector3(0, -turnAngle, 0));
            wheelFR.transform.localRotation = Quaternion.Euler(new Vector3(0, maxTurnAngle, 0));
            wheelFL.transform.localRotation = Quaternion.Euler(new Vector3(0, maxTurnAngle, 0));
        }
        else if (isBoost == true)
        {
            state.KnockbackStuckEnemies();
            if (currentSpeed > maxSpeed * 3.0f) currentSpeed = currentSpeed + (maxSpeed * 3.0f - currentSpeed) * decelerationRate * Time.fixedDeltaTime;
            else
            {
                currentSpeed += boostAcceleration * Time.fixedDeltaTime;
                if (currentSpeed > maxSpeed * 3.0f) currentSpeed = maxSpeed * 3.0f;
            }
            if (isOnAir == true) soundState = eVehicleSoundState.eHighOff;
            else soundState = eVehicleSoundState.eHighOn;
        }
        else
        {
            if (currentSpeed > maxSpeed) currentSpeed = currentSpeed + (maxSpeed - currentSpeed) * decelerationRate * Time.fixedDeltaTime;
            else
            {
                currentSpeed += acceleration * Time.fixedDeltaTime;
                if (currentSpeed > maxSpeed)
                {
                    isOnLimit = true;
                    currentSpeed = maxSpeed;
                }
            }  
            //if (currentSpeed > maxSpeed * 0.8f && isOnAir == true) soundState = eVehicleSoundState.eMedOff;
            //else if (currentSpeed > maxSpeed * 0.8f && isOnAir == false) soundState = eVehicleSoundState.eMedOn;
            //else if (currentSpeed > maxSpeed * 0.4f && isOnAir == true) soundState = eVehicleSoundState.eLowOff;
            //else if (currentSpeed > maxSpeed * 0.4f && isOnAir == false) soundState = eVehicleSoundState.eLowOff;
        }
        if (currentSpeed < -0.5f * maxSpeed) currentSpeed = -0.5f * maxSpeed;
        accelerationForce = transform.forward;
        if (isUTurn == true) accelerationForce = uTurnDir;
        if (forceStop == true) currentSpeed = 0;
        accelerationForce = accelerationForce.normalized * currentSpeed * slowRate; 
        internalForce = accelerationForce; 
    }
    void ExternalForceCheck()
    {
        if (isExternalForce == true)
        {
            externalForce = Vector3.Lerp(externalForce, externalForce * externalForceReductionRate, Time.fixedDeltaTime);
            if (TimerManager.I.IsFinished("ExternalForceTimer"))
            {
                externalForce = Vector3.zero;
                isExternalForce = false;
            }
        }
    }
    void Steering(float horizontalAxis)
    {
        if (isUTurn == true) return;
        frontWheelRotationAngle += Mathf.Rad2Deg * Time.fixedDeltaTime * Vector3.Dot(transform.forward, rb.velocity) / frontWheelRadius;
        rearWheelRotationAngle += Mathf.Rad2Deg * Time.fixedDeltaTime * Vector3.Dot(transform.forward, rb.velocity) / rearWheelRadius;
        if (frontWheelRotationAngle > 360.0f) frontWheelRotationAngle -= 360.0f;
        if (frontWheelRotationAngle < 360.0f) frontWheelRotationAngle += 360.0f;
        if (rearWheelRotationAngle > 360.0f) rearWheelRotationAngle -= 360.0f;
        if (rearWheelRotationAngle < 360.0f) rearWheelRotationAngle += 360.0f;
        float turnAngle = maxTurnAngle * horizontalAxis;
        if (Mathf.Abs(horizontalAxis) > 0.1f)
        {
            if (isOnAir == false)
            {
                if (isDrift)
                {
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0, 4.0f * turnAngle * Time.fixedDeltaTime, 0));
                    turnAngle *= -1.0f;
                }
                else
                {
                    transform.rotation = transform.rotation * Quaternion.Euler(new Vector3(0, turnAngle * Time.fixedDeltaTime, 0));
                }
            }
        }
        wheelFR.transform.localRotation = Quaternion.Euler(new Vector3(frontWheelRotationAngle, turnAngle, 0));
        wheelFL.transform.localRotation = Quaternion.Euler(new Vector3(frontWheelRotationAngle, turnAngle, 0));
        wheelRR.transform.localRotation = Quaternion.Euler(new Vector3(rearWheelRotationAngle, 0, 0));
        wheelRL.transform.localRotation = Quaternion.Euler(new Vector3(rearWheelRotationAngle, 0, 0));
    }
    void Suspension()
    {
        float groundDist = 0;
        groundDist += WheelSuspension(floatFR, wheelFR, null, wheelDefaultPosFR, frontWheelRadius);
        groundDist += WheelSuspension(floatFL, wheelFL, null, wheelDefaultPosFL, frontWheelRadius);
        groundDist += WheelSuspension(floatRR, wheelRR, wheelSkidRR, wheelDefaultPosRR, rearWheelRadius, TimerManager.I.IsFinished("DustEffect"));//rontForce);// - sideForce);
        groundDist += WheelSuspension(floatRL, wheelRL, wheelSkidRL, wheelDefaultPosRL, rearWheelRadius, TimerManager.I.IsFinished("DustEffect"));//frontForce);// - sideForce);
        if (TimerManager.I.IsFinished("DustEffect") == true)
        {
            TimerManager.I.ResetTimer("DustEffect");
        }
        groundDist *= 0.25f;
        groundDist = floatDist - groundDist;
        if (groundDist > 0) groundDist = 0;
        rb.drag = groundDrag * Mathf.Exp(groundDragCoefficient * groundDist);
    }
    float WheelSuspension(Transform t, GameObject wheel, TrailRenderer skidMark, Vector3 defaultLocalPos, float wheelRadius, bool isEffect = false)
    {
        float force = 0;
        float wheelDist = 0;
        float dist = 0;
        bool isWater = false;
        if (Physics.Raycast(t.position + new Vector3(0, 10, 0), -transform.up, out hit, 100.0f, layerMask_Self) == true)
        {
            dist = hit.distance - 10;
            force = floatDist - dist;
            isWater = hit.transform.CompareTag("Water");
        }
        else
        {
            force = 0;
            dist = float.MaxValue;
        }
        Vector3 effectPosition = new Vector3(t.transform.position.x, TerrainManager.I.GetTerrainHeight(t.transform.position.x, t.transform.position.z) + 0.01f, t.transform.position.z);
        if (force < 0) force = 0;
        suspensionForce = transform.up * force * floatForce * Time.fixedDeltaTime;
        if (isWater == true) rb.AddForceAtPosition(suspensionForce * 0.9f, t.position);
        else rb.AddForceAtPosition(suspensionForce, t.position);

        isOnAir = false;
        if (dist > wheelRadius + suspensionDist)
        {
            wheelDist = suspensionDist;
            if (dist > wheelRadius + suspensionDist * 2.0f)
                isOnAir = true;
        }
        else if (dist < wheelRadius - suspensionDist)
        {
            wheelDist = -suspensionDist;
            if (isEffect == true)
            {
                if (isWater == true)
                {
                    if (isDrift == true) ProjectileManager.I.ActivateProjectileInstance("Water", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                    else ProjectileManager.I.ActivateProjectileInstance("WaterSmall", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                }
                else
                {
                    if (isDrift == true) ProjectileManager.I.ActivateProjectileInstance("Dust", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                    else ProjectileManager.I.ActivateProjectileInstance("DustSoft", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                }
            }
        }
        else
        {
            wheelDist = dist - wheelRadius;
            if (isEffect == true)
            {
                if (isWater == true)
                {
                    if (isDrift == true) ProjectileManager.I.ActivateProjectileInstance("Water", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                    else ProjectileManager.I.ActivateProjectileInstance("WaterSmall", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                }
                else
                {
                    if (isDrift == true) ProjectileManager.I.ActivateProjectileInstance("Dust", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                    else ProjectileManager.I.ActivateProjectileInstance("DustSoft", effectPosition, Vector3.zero, Vector3.zero, 1.0f);
                }
            } 
        }

        if(skidMark != null) skidMark.emitting = false;
        if (!isOnAir && (isDrift || isUTurn) && skidMark != null)
        {
            skidMark.emitting = true; 
            skidMark.transform.position = effectPosition + new Vector3(0, 0.01f, 0);
        }

        wheel.transform.localPosition = defaultLocalPos - transform.up.normalized * wheelDist;

        return dist;
    }
    void UTurn(float verticalAxis)
    {
        if (isDrift == false && isUTurn == false)
        {
            if (verticalAxis > 0.7f) TimerManager.I.ResetTimer("UTurnTimer");
            else if (TimerManager.I.IsFinished("UTurnTimer") == true) TimerManager.I.DisableTimer("UTurnTimer");
            else if (TimerManager.I.IsActive("UTurnTimer") == true && verticalAxis < -0.7f)
            {
                //if (oilGauge.UTurn() == true)
                {
                    SoundManager.I.PlayOnce("uturn");
                    TimerManager.I.SetTimer("UTurnSpeed", uTurnTime);
                    uTurnSpeed = currentSpeed;
                    uTurnRot = transform.rotation;
                    uTurnDir = transform.forward;
                    uTurnDir.y = 0;
                    isUTurn = true;
                }
                //else
                //{
                //    TimerManager.I.ResetTimer("UTurnTimer");
                //}
            }
        }
    }
    void Drift(float verticalAxis, float horizontalAxis)
    {
        //if (isBoost == true) return;
        driftForce = Vector3.zero; 
        if (isDrift == false && isUTurn == false)
        {
            if (TimerManager.I.IsActive("DriftSoundTimer") == true)
            {
                SoundManager.I.StopSound("drift");
                TimerManager.I.DisableTimer("DriftSoundTimer");
            }
            if (verticalAxis < -0.7f) TimerManager.I.ResetTimer("DriftTimer");
            else if (TimerManager.I.IsFinished("DriftTimer") == true) TimerManager.I.DisableTimer("DriftTimer");
            else if (TimerManager.I.IsActive("DriftTimer") == true &&
                Mathf.Abs(horizontalAxis) > 0.7f)
            {
                //if (driftGauge.Drift(true, Time.fixedDeltaTime) == true)
                {
                    driftDir = horizontalAxis > 0.0f ? -1.0f : 1.0f;
                    isDrift = true;
                }
            }
        }
        if (isDrift == true)
        {
            if (TimerManager.I.IsActive("DriftSoundTimer") == false)
            {
                SoundManager.I.PlayOnce("drift");
                TimerManager.I.ResetTimer("DriftSoundTimer");
            }
            float turnAngle = Mathf.Abs(maxTurnAngle * horizontalAxis * 4.0f);
            driftForce = driftDir * transform.right;
            driftForce.y = 0;
            float test = Mathf.Tan(Mathf.Deg2Rad * turnAngle);
            driftForce = driftForce.normalized * currentSpeed * Mathf.Tan(Mathf.Deg2Rad * turnAngle);
            internalForce = (accelerationForce + driftForce).normalized * currentSpeed * driftDecelerationRate;
            internalForce.y = rb.velocity.y;
            state.KnockbackStuckEnemies();
            if (Mathf.Abs(horizontalAxis) < 0.1f || driftDir * horizontalAxis > 0.0f)
            {
                isDrift = false;
                New_LevelUpManager.I.PageStackCheck();
            }
            //else if (driftGauge.Drift(false, Time.fixedDeltaTime) == false)
            //{
            //    isDrift = false;
            //}
        }
    }
    void Boost()
    {
        if (isUTurn == true) return; 
        if (isBoost == false && oilGauge.Boost() == true)
        {
            SoundManager.I.PlayOnce("booster");
            currentSpeed = maxSpeed;
            isBoost = true;
            isDrift = false;
            state.isInvincible = true;
            EffectManager.I.ActivateEffect("Aura", boostTime);
            TimerManager.I.SetTimer("Boost", boostTime);
            //if (boostCounter.UseBoost() == true)
            //{
            //}
        }
    }
    void ForceBoost()
    {
        SoundManager.I.PlayOnce("booster");
        currentSpeed = maxSpeed;
        isBoost = true;
        isDrift = false;
        state.isInvincible = true;
        EffectManager.I.ActivateEffect("Aura", boostTime);
        TimerManager.I.SetTimer("Boost", boostTime);
    }
    public void BoostEnd()
    {
        isBoost = false;
        state.isInvincible = false;
    }
    void CalculateVelocity()
    {
        velocity = internalForce * ((1.0f - airDragCoefficient) * rb.drag / groundDrag + airDragCoefficient) + externalForce;// + gravitationalAcceleration * Time.fixedDeltaTime;
        //transform.position += velocity * Time.fixedDeltaTime;
        velocity.y = rb.velocity.y; 
        rb.velocity = velocity;
        actualSpeed = (transform.position - prevPos) / Time.fixedDeltaTime;
        actualSpeed.y = 0;
        if (isOnLimit) limitSpeed = actualSpeed;  
        prevPos = transform.position;
    }
    void PreventOverturn()
    {
        rotationEuler = transform.rotation.eulerAngles;
        bool correction = false;
        if (rotationEuler.x > 45.0f && rotationEuler.x < 180.0f)
        {
            rotationEuler.x = 45.0f;
            correction = true;
        }
        if (rotationEuler.x < 315.0f && rotationEuler.x > 180.0f)
        {
            rotationEuler.x = 315.0f;
            correction = true;
        }
        if (rotationEuler.z > 45.0f && rotationEuler.z < 180.0f)
        {
            rotationEuler.z = 45.0f;
            correction = true;
        }
        if (rotationEuler.z < 315.0f && rotationEuler.z > 180.0f)
        {
            rotationEuler.z = 315.0f;
            correction = true;
        }
        if (rotationEuler.x < -45.0f && rotationEuler.x > -180.0f)
        {
            rotationEuler.x = -45.0f;
            correction = true;
        }
        if (rotationEuler.x > -315.0f && rotationEuler.x < -180.0f)
        {
            rotationEuler.x = -315.0f;
            correction = true;
        }
        if (rotationEuler.z < -45.0f && rotationEuler.z > -180.0f)
        {
            rotationEuler.z = -45.0f;
            correction = true;
        }
        if (rotationEuler.z > -315.0f && rotationEuler.z < -180.0f)
        {
            rotationEuler.z = -315.0f;
            correction = true;
        }
        if (correction == true)
        {
            transform.up = TerrainManager.I.GetTerrainNormal(transform.position.x, transform.position.z);
            //transform.rotation = Quaternion.Euler(rotationEuler);
        }
        //if (Physics.Raycast(transform.position, -transform.up, out hit, 100.0f, layerMask_Self) == true)
        //{  
        //    float dot = Vector3.Dot(transform.up, hit.normal);
        //    float angle = Mathf.Acos(dot);
        //    if (dot < 0.9f)
        //    {
        //        balanceRot = Quaternion.LookRotation(transform.forward, Vector3.up);
        //        transform.rotation = Quaternion.Lerp(transform.rotation, balanceRot, Time.fixedDeltaTime);
        //        //transform.rotation = transform.rotation * Quaternion.AngleAxis(angle * Time.fixedDeltaTime * 5.0f, Vector3.Cross(hit.normal, transform.up).normalized);
        //    }
        //}
    }
    public void GameOver()
    {
        ProjectileManager.I.ActivateProjectileInstance("NukeExplosion", transform.position, Vector3.zero, Vector3.zero, 2.0f);
        ProjectileManager.I.ActivateProjectileInstance("FireFieldRed", transform.position, Vector3.zero, Vector3.zero, 10.0f);
        gameObject.SetActive(false);
    }
    public void Resurection()
    { 
        gameObject.SetActive(true);
        ForceBoost();
    }
    public void PlayerHit(Vector3 pos)
    {
        pos = pos + new Vector3(0, Random.Range(0.5f, 1.5f), 0);
        Vector3 dir = transform.position - pos;
        Physics.Raycast(pos, dir, out hit, 100.0f, layerMask_Self);
        ShakeCar(transform.position + new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-0.5f, 0.5f)), Vector3.up, 50.0f);
        EffectManager.I.ActivateEffect("PlayerHit", 1.0f, hit.point - transform.position);
        TimerManager.I.ResetTimer("PlayerHit");
        TimerManager.I.ResetTimer("BlinkInterval");
        matColor.SetColor("_Color", Color.red);
    }
    void PlayEffect()
    {
        if (TimerManager.I.IsActive("PlayerHit") &&
            TimerManager.I.IsFinished("BlinkInterval"))
        {
            if (blink == true) matColor.SetColor("_Color", Color.red);
            else matColor.SetColor("_Color", Color.white);
            TimerManager.I.ResetTimer("BlinkInterval");
            blink = !blink;
        }
        if (TimerManager.I.IsFinished("PlayerHit"))
        {
            TimerManager.I.DisableTimer("PlayerHit");
            TimerManager.I.DisableTimer("BlinkInterval");
            blink = false;
            matColor.SetColor("_Color", Color.white);
        }
    }
    void PlaySound()
    {
        if (soundState == prevSoundState) return;
        switch (soundState)
        { 
            case eVehicleSoundState.eStartup:
                {
                    SoundManager.I.PlayOnce("Vehicle001_startup");
                    prevSoundState = soundState;
                    soundState = eVehicleSoundState.eIdle;
                }
                break;
            case eVehicleSoundState.eIdle:
                {
                    SoundManager.I.PlayLoop("Vehicle001_idle");
                    prevSoundState = eVehicleSoundState.eIdle;
                }
                break;
            case eVehicleSoundState.eLowOn:
                {
                    SoundManager.I.PlayLoop("Vehicle001_low_on");
                    prevSoundState = eVehicleSoundState.eLowOn;
                }
                break;
            case eVehicleSoundState.eLowOff:
                {
                    SoundManager.I.PlayLoop("Vehicle001_low_off");
                    prevSoundState = eVehicleSoundState.eLowOff;
                }
                break;
            case eVehicleSoundState.eMedOn:
                {
                    SoundManager.I.PlayLoop("Vehicle001_med_on");
                    prevSoundState = eVehicleSoundState.eMedOn;
                }
                break;
            case eVehicleSoundState.eMedOff:
                {
                    SoundManager.I.PlayLoop("Vehicle001_med_off");
                    prevSoundState = eVehicleSoundState.eMedOff;
                }
                break;
            case eVehicleSoundState.eHighOn:
                {
                    SoundManager.I.PlayLoop("Vehicle001_high_on");
                    prevSoundState = eVehicleSoundState.eHighOn;
                }
                break;
            case eVehicleSoundState.eHighOff:
                {
                    SoundManager.I.PlayLoop("Vehicle001_high_off");
                    prevSoundState = eVehicleSoundState.eHighOff;
                }
                break;
            case eVehicleSoundState.eMaxRPM:
                {
                    SoundManager.I.PlayLoop("Vehicle001_maxRPM");
                    prevSoundState = eVehicleSoundState.eMaxRPM;
                }
                break;
        }
    }
    public void SetJoyStick(FixedJoystick controller)
    {
        joyStick = controller; 
        joyStick.onDoubleClick += Boost;
    }
    public void ShakeCar(Vector3 pos, Vector3 dir, float power, float decelerationRate = 1.0f)
    {
        internalForce = internalForce * decelerationRate;
        rb.AddForceAtPosition(dir * power, pos);
    }
    public void PushCar(Vector3 dir, float power)
    {
        externalForce += dir * power;
        isExternalForce = true;
        TimerManager.I.ResetTimer("ExternalForceTimer");
    }
    public Vector3 GetActualVelocity()
    {
        return actualSpeed;
    }
    public Vector3 GetLimitSpeed()
    {
        if (isOnLimit == true) return limitSpeed;
        if (isBoost == false) return actualSpeed;
        return limitSpeed;
    }
    public float GetCurrentSpeed() { return currentSpeed; }
    public void SetMaxSpeed(int level)
    {
        //maxSpeed = 40.0f + 2.0f * (float)level;
        //maxSpeed = 13.0f + 5.0f * (float)level;
        maxSpeed = 15.0f + 7.0f * (float)level;
    }
    public void UpgradeMaxSpeed(float percent)
    {
        maxSpeed += maxSpeed * percent;
    }
    public void BoostAccelerationInit(float percent)
    {
        boostAcceleration = acceleration * 10 * (1.0f + percent); 
    }
    public void SlowCar(float rate)
    {
        slowRate -= rate;
        if (slowRate < 0.0f) slowRate = 0.0f;
        slowRecoverRate = 1.0f - slowRate;
    }
    IEnumerator Deceleration()
    {
        isRunning = false;
        TimerManager.I.ResetTimer("CrashTimer");
        float t = Mathf.Pow(TimerManager.I.GetSetTime("CrashTimer"), 2.0f);
        float dt = 0;
        float a = internalForce.magnitude / (-t);
        float b = externalForce.magnitude / (-t);
        while (TimerManager.I.IsFinished("CrashTimer") == false)
        {
            dt = Mathf.Pow(TimerManager.I.GetCurrentTimer("CrashTimer"), 2.0f);
            internalForce = internalForce.normalized * (a * (dt - t));
            externalForce = externalForce.normalized * (b * (dt - t));
            yield return null;
        }
        internalForce = Vector3.zero;
        externalForce = Vector3.zero;
        TimerManager.I.DisableTimer("CrashTimer");
        isRunning = true;
        yield return null;
    }
}
