using PartyGame.Scripts.MiniGame.Base;
using PartyGame.Scripts.Shared;
using PartyGame.Scripts.Shared.Net.Struct; 
using UnityEngine;
using Unity.Netcode; 
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using TMPro;
using Obi;
using UnityEngine.SocialPlatforms.Impl;
using System.Collections;
using PartyGame._Shared.Movement;
using PartyGame.Scripts.MiniGame.DinnersReady;

public class ShepherdDogPlayer : MiniGamePlayerBase
{
    [SerializeField] float barkRadius = 5.0f;
    [SerializeField] float barkCoolTime = 0.5f;
    [SerializeField] Transform startPos;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject AOE;
    [SerializeField] Animator animator;


    float lastBarkTime = -1.0f;
    Dictionary<ulong, ShepherdDogSheep> sheepHerdDictionary = new ();
    public NetworkVariable<int> personalScore = new NetworkVariable<int>();
    Material aoeMat;
    Rigidbody rb;
    Coroutine checkGameStartCoroutine;
    Player2DMovement player2DMovement = null;
    public bool gameStart = true;
    ShepherdDogDirector director;

    [Header("MMFeedbacks")]
    [SerializeField] private MMFeedbacks feedbacks_OnBark = null;
    [SerializeField] private MMFeedbacks feedbacks_OnScored = null;
    [SerializeField] private MMFeedbacks feedbacks_OnBarkFail = null;
    protected override void Awake()
    {
        base.Awake();

        personalScore.OnValueChanged += OnScoreValueChanged;

        rb = GetComponent<Rigidbody>();
        player2DMovement = GetComponent<Player2DMovement>();
    }
    protected override void Update()
    {
        base.Update();
        scoreText.transform.rotation = Quaternion.Euler(70, 0, 0);

        if (director == null)
            return;

        if (director.IsGameSceneState(MiniGameDirectorBase.MiniGameState.Playing) == false)
            return;

        if (IsOwner && playerInputController && playerInputController.ActionConfirmKeyDown)
        {
            OnBarkServerRpc();
        }
        if (IsOwner && aoeMat)
        {
            aoeMat.SetVector("_Position", transform.position);
            aoeMat.SetVector("_Direction", transform.forward);
        }
    }
    [ServerRpc]
    void OnBarkServerRpc()
    {
        if (NetworkManager.Singleton.ServerTime.TimeAsFloat - lastBarkTime > barkCoolTime)
        {
            foreach (Collider c in Physics.OverlapSphere(transform.position, barkRadius))
            {
                if (c.CompareTag("Enemy"))
                {
                    Vector3 dir = c.transform.position - transform.position;
                    dir.y = 0;
                    if (Vector3.Dot(transform.forward, dir.normalized) > 0.7071f)
                    {
                        ShepherdDogSheep sheep = c.transform.parent.GetComponent<ShepherdDogSheep>();
                        sheep.RunOnBark(transform.position, transform.forward, PlayerId);
                    }
                }
            }
            lastBarkTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
        }
        else
        {
            feedbacks_OnBarkFail?.PlayFeedbacks();
            return;
        }
        OnBarkClientRpc();
    }
    [ClientRpc]
    void OnBarkClientRpc()
    {
        feedbacks_OnBark?.PlayFeedbacks();
    }
    public override void OnPrePlaying()
    {
        lastBarkTime = -barkCoolTime;
        sheepHerdDictionary.Clear();
        transform.position = startPos.position;
        transform.forward = -transform.position.normalized;
        if (IsServer) personalScore.Value = 0;
        if (IsOwner)
        {
            aoeMat = AOE.GetComponent<MeshRenderer>().material;
            aoeMat.SetFloat("_Radius", barkRadius);

            director = GameContext.GetDirector<ShepherdDogDirector>();
            if (director && director.RealGame)
            {
                player2DMovement.SetIsInputEnabled_Contents(false);
                director.gameStart = false;
                if (checkGameStartCoroutine != null)
                {
                    StopCoroutine(checkGameStartCoroutine);
                    checkGameStartCoroutine = null;
                }
                checkGameStartCoroutine = StartCoroutine(CheckGameStart());
            }
        }
        else
        {
            AOE.SetActive(false);
        }
    }
    public override void OnPostPlaying()
    {
    }
    protected override void OnHealthValueChanged(int oldValue, int newValue)
    {
    }
    public override void OnDead()
    {

    }
    public override void OnIntro()
    {

    }
    public override void OnOutro(MiniGameDirectorBase.MiniGameState currentMiniGameState)
    {

    }
    public override NetGameResult MakePlayerGameResult(MiniGameDirectorBase miniGameDirector, bool? overrideSuccess)
    {
        base.MakePlayerGameResult(miniGameDirector, overrideSuccess);

        bool success = false;

        ShepherdDogDirector sheepDirector = GameContext.GetDirector<ShepherdDogDirector>();
        if (miniGameDirector)
        {
            success = personalScore.Value >= sheepDirector.targetScore ? true : false;
        }

        NetGameResult gameResult = default;
        gameResult.PlayerId = PlayerId;
        gameResult.SetSuccess(success);
        gameResult.NumberRecord = personalScore.Value;

        return gameResult;
    }
    void OnScoreValueChanged(int oldScore, int newScore)
    {
        scoreText.SetText(newScore.ToString());// + " / " + targetScore.ToString());
        if (newScore > 0) feedbacks_OnScored?.PlayFeedbacks();
    }
    IEnumerator CheckGameStart()
    {
        yield return new WaitUntil(() => MiniGameFramework.Instance.IsRealGameStartCountingPlaying());
        yield return new WaitUntil(() => !MiniGameFramework.Instance.IsRealGameStartCountingPlaying());
        player2DMovement.SetIsInputEnabled_Contents(true);
        director.gameStart = true;
        yield return null;
    }
}