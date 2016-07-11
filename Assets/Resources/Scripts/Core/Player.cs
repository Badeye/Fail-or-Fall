﻿using Sliders.Levels;
using Sliders.UI;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sliders
{
    public class Player : MonoBehaviour
    {
        public enum PlayerState { alive, ready, dead };

        public static PlayerState playerState;

        public static PlayerStateChangeEvent onPlayerStateChange = new PlayerStateChangeEvent();

        public CameraMovement cm;
        public LayerMask finishMask;
        public LayerMask killMask;
        public GameObject trail;
        public GameObject trail2;
        public Text text;

        public float gravity = 15F;
        public float maxChargeVelocity;
        public float chargeForcePerTick;
        public float respawnTime = 1f;

        private Spawn spawn;
        private Quaternion spawnRotaion;

        public static float _playerZ;
        public static bool facingLeft = true;

        private bool charging = false;
        private Vector2 chargeVelocity;
        private bool firstChargeDone = false;

        private void Awake()
        {
            _playerZ = Constants.playerY;
        }

        private void Start()
        {
            spawn = LevelManager.GetSpawn();
            transform.position = spawn.position;
            spawnRotaion = transform.rotation;
            facingLeft = spawn.facingLeftOnSpawn;

            Debug.Log("Spawnpos: " + spawn.position + " Rot: " + spawnRotaion);
            MoveToSpawn();
            Game.onGameStateChange.AddListener(GameStateChanged);
        }

        private void GameStateChanged(Game.GameState gameState)
        {
            Debug.Log("GameState changed to: " + gameState);
            switch (gameState)
            {
                case Game.GameState.playing:
                    Spawn();
                    break;

                case Game.GameState.deathscreen:
                    Fail();
                    break;

                default:
                    break;
            }
        }

        private void SwitchFacingDirection()
        {
            facingLeft = !facingLeft;
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (1 << collider.gameObject.layer == killMask.value && IsAlive())
            {
                Game.SetGameState(Game.GameState.deathscreen);
            }
            else if (1 << collider.gameObject.layer == finishMask.value && IsAlive())
            {
                Game.SetGameState(Game.GameState.finishscreen);
            }
        }

        private void MoveToSpawn()
        {
            //Vector3 spawnPos = LevelManager.GetSpawn();
            transform.rotation = spawnRotaion;
            transform.position = spawn.position;

            GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            GetComponent<Rigidbody2D>().gravityScale = 0f;
            GetComponent<Rigidbody2D>().Sleep();

            SetPlayerState(PlayerState.ready);
        }

        private void Spawn()
        {
            SetPlayerState(PlayerState.alive);
            CameraMovement.SetCameraState(CameraMovement.CameraState.following);

            trail.GetComponent<TrailRenderer>().time = 0.5f;
            trail.GetComponent<TrailRenderer>().enabled = true;
            trail2.GetComponent<TrailRenderer>().time = 1f;
            trail2.GetComponent<TrailRenderer>().enabled = true;

            GetComponent<Rigidbody2D>().gravityScale = gravity;
            GetComponent<Rigidbody2D>().velocity = new Vector3(0f, -0.00001f, 0f);
            GetComponent<Rigidbody2D>().WakeUp();
        }

        private void Fail()
        {
            SetPlayerState(PlayerState.dead);
            charging = false;
            facingLeft = true;
            firstChargeDone = false;

            trail.GetComponent<TrailRenderer>().time = 0.0f;
            trail.GetComponent<TrailRenderer>().enabled = false;
            trail2.GetComponent<TrailRenderer>().time = 0.0f;
            trail2.GetComponent<TrailRenderer>().enabled = false;

            //to game
            cm.moveCamTo(new Vector3(spawn.position.x, spawn.position.y + Constants.cameraY, transform.position.z), respawnTime);

            MoveToSpawn();
        }

        //X-Achsen-Spiegelung der Figurenflugbahn
        private void Reflect()
        {
            GetComponent<Rigidbody2D>().velocity = new Vector2(GetComponent<Rigidbody2D>().velocity.x * (-1), GetComponent<Rigidbody2D>().velocity.y);
            SwitchFacingDirection();
        }

        //Gravitationsabbruch, FIgur wird auf der Ebene gehalten und beschleunigt
        private void Charge()
        {
            GetComponent<Rigidbody2D>().gravityScale = 0f;
            GetComponent<Rigidbody2D>().velocity = new Vector2(GetComponent<Rigidbody2D>().velocity.x, 0f);
            charging = true;
        }

        //Erneutes Hinzufügen der Gravitation
        private void Decharge()
        {
            charging = false;
            GetComponent<Rigidbody2D>().gravityScale = gravity;
        }

        //Geschwindigkeitszuwachs während die Figur gehalten wird
        private void FixedUpdate()
        {
            if (IsAlive())
            {
                Vector2 velocity = transform.GetComponent<Rigidbody2D>().velocity;
                //float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                //Quaternion quad = Quaternion.AngleAxis(angle, Vector3.forward);
                //transform.rotation = quad;

                if (charging && Mathf.Abs(GetComponent<Rigidbody2D>().velocity.x) < maxChargeVelocity)
                {
                    if (facingLeft)
                    {
                        velocity.x = velocity.x - chargeForcePerTick;
                    }
                    else
                    {
                        velocity.x = velocity.x + chargeForcePerTick;
                    }
                    GetComponent<Rigidbody2D>().velocity = velocity;
                }
            }
        }

        //Inputs, alles in den Input Manager!
        private void Update()
        {
            if (IsAlive())
            {
                //Keyboard
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    Fail();
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    Reflect();
                }
                else if (Input.GetKeyDown(KeyCode.Y) && !charging)
                {
                    Charge();
                }
                else if (Input.GetKeyUp(KeyCode.Y) && charging)
                {
                    Decharge();
                }
                /*
                //Touch
                foreach (Touch touch in Input.touches)
                {
                    Vector3 position = touch.position;

                    //Right Touch
                    if (position.x >= Camera.main.pixelWidth / 2 && touch.phase == TouchPhase.Began && firstChargeDone)
                        Reflect();

                    //Left Touch
                    else if (position.x < Camera.main.pixelWidth / 2)
                    {
                        if (touch.phase == TouchPhase.Began && !charging)
                        {
                            Charge();
                            firstChargeDone = true;
                        }
                        else if (touch.phase == TouchPhase.Ended && touch.phase != TouchPhase.Canceled && charging)
                            Decharge();
                    }
                }
                */
            }
        }

        public void SetPlayerState(PlayerState ps)
        {
            playerState = ps;
            onPlayerStateChange.Invoke(playerState);
        }

        public static bool IsAlive()
        {
            if (playerState == PlayerState.alive)
                return true;
            else
                return false;
        }

        public static bool IsReady()
        {
            if (playerState == PlayerState.ready)
                return true;
            else
                return false;
        }

        public class PlayerStateChangeEvent : UnityEvent<PlayerState> { }
    }
}