﻿using Impulse;
using Impulse.Audio;
using Impulse.Cam;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A circle that pulls the player towards its center with a set force.
/// Force depends on the players distance to the center of the circle.
/// Can be used to elevate the player upwards if the attraction (pullForce) is greater than the gravity pulling the player downwards.
/// </summary>
namespace Impulse.LevelObjects
{
    public class Attractor : MonoBehaviour
    {
        public static bool playerCollidesWithAny = false;
        public static List<Attractor> attractors = new List<Attractor>();
        private static Attractor collidedAttractor;

        // attractor radius aka. area of effect, sets the scale of the gameObject
        public float pullRadius = 1000f;

        // maximum pull
        public float maxPullForce = 1000f;

        // attractor shader material
        public Material attractorMaterial;

        // amplifies the pull fprce by this value when the player comes closer
        public float pullAmplifier = 2F;

        // current force that gets applied to the object
        private float pullForce;
        private Rigidbody2D playerRb;
        private Vector2 center;

        private bool colliding = false;
        private bool shaking = false;

        [ExecuteInEditMode]
        public void SetScale()
        {
            transform.localScale = new Vector3(pullRadius * 2, pullRadius * 2, transform.localScale.z);
        }

        private void Awake()
        {
            attractors.Clear();
        }

        public void Start()
        {
            playerRb = Player._instance.GetComponent<Rigidbody2D>();
            center = transform.position;
            SetScale();

            attractors.Add(this);

            // reset shader input
            attractorMaterial.SetFloat("_PlayerDistance", pullRadius * 10);
            attractorMaterial.SetFloat("_AttractorRadius", pullRadius);
        }

        // draws an outline around the attractor to make it visible in the editor
        [ExecuteInEditMode]
        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.DrawWireSphere(transform.position, pullRadius);
#endif
        }

        public void FixedUpdate()
        {
            collidedAttractor = null;
            playerCollidesWithAny = false;
            foreach (Attractor a in attractors)
            {
                if (Physics2D.OverlapCircleAll(a.center, a.pullRadius, 1 << LayerMask.NameToLayer("Player")).Length > 0)
                {
                    // player is inside any attractor
                    collidedAttractor = a;
                    playerCollidesWithAny = true;
                    break;
                }
            }

            if (playerCollidesWithAny && collidedAttractor == this)
            {
                // calculate direction from player to center of this
                Vector2 forceDirection = center - new Vector2(Player._instance.transform.position.x, Player._instance.transform.position.y);

                // apply force on player towards center of this
                playerRb.AddForce(forceDirection.normalized * maxPullForce * Time.fixedDeltaTime * pullAmplifier);

                // calculate distance to the center of this
                float dist = Mathf.Abs(Vector3.Distance(Player._instance.transform.position, transform.position));

                // update shader
                attractorMaterial.SetFloat("_AttractorRadius", pullRadius);
                attractorMaterial.SetVector("_AttractionCenter", transform.InverseTransformPoint(transform.position));
                attractorMaterial.SetFloat("_PlayerDistance", dist);

                // update camera shake
                float shake = Mathf.InverseLerp(pullRadius, 0, dist);
                CamShake.attractorDistance = shake;
                SoundPlayer.rumbleAlive = true;
                SoundPlayer.rumbleVolume = shake;
            }

            if (playerCollidesWithAny == true && shaking == false && collidedAttractor == this)
            {
                SoundPlayer.rumbleAlive = true;
                SoundManager.PlayRumbleSound(transform.position);
                CamShake.AttractorShake();
                shaking = true;
            }
            else if (playerCollidesWithAny == false)
            {
                Debug.Log("rumble falssseeeeeeeeeeeeeee");
                SoundPlayer.rumbleAlive = false;
                CamShake.AttractorShakeBreak();
            }
        }
    }
}