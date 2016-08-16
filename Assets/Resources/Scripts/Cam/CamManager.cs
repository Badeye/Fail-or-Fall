﻿using System.Collections;
using UnityEngine;

namespace Sliders.Cam
{
    public enum InterpolationType { smoothstep, linear }

    public class CamManager : MonoBehaviour
    {
        public static CamManager _instance;
        public Player player;

        private void Awake()
        {
            _instance = this;
        }

        private void Start()
        {
            Game.onGameStateChange.AddListener(GameStateChanged);
            player.onPlayerAction.AddListener(PlayerAction);
            player.onPlayerStateChange.AddListener(PlayerStateChanged);
        }

        //Listener
        private void PlayerAction(Player.PlayerAction playerAction)
        {
            switch (playerAction)
            {
                case Player.PlayerAction.reflect:
                    break;

                case Player.PlayerAction.charge:
                    CamZoom.ZoomToVelocity(player);
                    break;

                case Player.PlayerAction.decharge:
                    break;

                default:
                    break;
            }
        }

        //Listener
        private void PlayerStateChanged(Player.PlayerState playerState)
        {
            switch (playerState)
            {
                case Player.PlayerState.alive:
                    CamMove.StartFollowing();
                    CamZoom.ZoomToVelocity(player);
                    break;

                case Player.PlayerState.dead:
                    Debug.Log("DEAD");
                    CamZoom.DeathZoom();
                    //CamShake.DeathShake();
                    //CamRotation.DeathRotation(); //change to camshake
                    //CamMove.StopFollowing();
                    break;

                case Player.PlayerState.ready:
                    break;

                default:
                    break;
            }
        }

        private void GameStateChanged(Game.GameState gameState)
        {
            switch (gameState)
            {
                case Game.GameState.playing:
                    break;

                case Game.GameState.scorescreen:
                    CamZoom.ZoomToMinimum();
                    break;

                case Game.GameState.finishscreen:
                    break;

                default:
                    break;
            }
        }
    }
}