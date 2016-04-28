﻿/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2016.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity
{
    /** A physics finger model for our rigid hand made out of various cube Unity Colliders. */
    public class MotorizedFinger : SkeletalFinger
    {
        public float filtering = 0.5f;
        public Rigidbody Palm;

        private bool dirty = false;
        private HingeJoint[] hinges;
        private Vector3 PalmPos;
        private Quaternion PalmRot;
        private float FingerStrength = 0f;
        private float FingerSpeed = 0f;

        void InitializeFingerJoints()
        {
            hinges = new HingeJoint[3];
            for (int i = 1; i < bones.Length - 1; ++i) {
                if (bones[i] != null) {
                    if (i == 1) {
                        HingeJoint Hinge = Palm.gameObject.AddComponent<HingeJoint>();
                        Hinge.enablePreprocessing = true;
                        Hinge.autoConfigureConnectedAnchor = false;
                        Hinge.connectedBody = bones[i].GetComponent<Rigidbody>();
                        Hinge.anchor = Palm.transform.InverseTransformPoint(bones[i].TransformPoint(new Vector3(0f, 0f, (bones[i].GetComponent<CapsuleCollider>().radius) - (bones[i].GetComponent<CapsuleCollider>().height / 2f))));
                        Hinge.connectedAnchor = new Vector3(0f, 0f, (bones[i].GetComponent<CapsuleCollider>().radius) - (bones[i].GetComponent<CapsuleCollider>().height / 2f));
                        Hinge.axis = Palm.transform.InverseTransformDirection(bones[i].transform.right);
                        Hinge.enableCollision = false;

                        Hinge.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInEditor;

                        Hinge.useMotor = true;
                        Hinge.useLimits = false;
                        JointLimits limit = new JointLimits();
                        limit.min = -80f;
                        limit.max = 5f;
                        Hinge.limits = limit;

                        hinges[0] = Hinge;
                    }

                    if (i + 1 < bones.Length) {
                        HingeJoint Hinge = bones[i].gameObject.AddComponent<HingeJoint>();
                        Hinge.enablePreprocessing = true;
                        Hinge.autoConfigureConnectedAnchor = false;
                        Hinge.connectedBody = bones[i + 1].gameObject.GetComponent<Rigidbody>();
                        Hinge.anchor = bones[i].InverseTransformPoint(bones[i + 1].TransformPoint(new Vector3(0f, 0f, (bones[i + 1].GetComponent<CapsuleCollider>().radius) - (bones[i + 1].GetComponent<CapsuleCollider>().height / 2f))));
                        Hinge.connectedAnchor = new Vector3(0f, 0f, (bones[i + 1].GetComponent<CapsuleCollider>().radius) - (bones[i + 1].GetComponent<CapsuleCollider>().height / 2f));
                        Hinge.axis = bones[i].InverseTransformDirection(bones[i + 1].transform.right);
                        Hinge.enableCollision = false;

                        Hinge.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInEditor;

                        Hinge.useMotor = true;
                        Hinge.useLimits = false;
                        JointLimits limit = new JointLimits();
                        limit.min = -70f;
                        limit.max = 15f;
                        Hinge.limits = limit;

                        hinges[i] = Hinge;
                    }
                }
            }
        }

        void RemoveFingerJoints()
        {
            HingeJoint[] palmhingelist = Palm.gameObject.GetComponents<HingeJoint>();
            foreach (HingeJoint hinge in palmhingelist) {
                Destroy(hinge);
            }

            for (int i = 0; i < bones.Length; ++i) {
                if (bones[i] != null) {
                    HingeJoint[] hingelist = bones[i].gameObject.GetComponents<HingeJoint>();
                    foreach (HingeJoint hinge in hingelist) {
                        Destroy(hinge);
                    }

                    bones[i].GetComponent<Rigidbody>().velocity = Vector3.zero;
                    bones[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                    bones[i].gameObject.SetActive(false);
                }
            }
        }

        void OnEnable()
        {
            //dirty ensures that the fingers get updated in "Update" before the Joints are applied
            dirty = true;
        }

        void OnDisable()
        {
            RemoveFingerJoints();
        }

        public void setPalmTransform(Vector3 pos, Quaternion rot)
        {
            PalmPos = pos;
            PalmRot = rot;
        }

        public void setParentofDigits(Transform parent, float strength, float speed, bool gravity)
        {
            FingerStrength = strength;
            FingerSpeed = speed;

            for (int i = 0; i < bones.Length; ++i) {
                if (bones[i] != null) {
                    bones[i].transform.parent = parent.transform;
                    bones[i].GetComponent<Rigidbody>().useGravity = gravity;
                }
            }
        }

        public override void UpdateFinger()
        {
            for (int i = 0; i < bones.Length; ++i) {
                if (bones[i] != null) {
                    // Set bone dimensions.
                    CapsuleCollider capsule = bones[i].GetComponent<CapsuleCollider>();
                    if (capsule != null) {
                        // Initialization
                        capsule.direction = 2;
                        bones[i].localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

                        // Update
                        capsule.radius = (GetBoneWidth(i) / 2f)*transform.lossyScale.x;
                        capsule.height = (GetBoneLength(i) + GetBoneWidth(i)) * transform.lossyScale.x;
                    }

                    Rigidbody boneBody = bones[i].GetComponent<Rigidbody>();

                    if (!bones[i].gameObject.activeSelf) {
                        bones[i].gameObject.SetActive(true);
                        if (boneBody) {
                            boneBody.velocity = Vector3.zero;
                            boneBody.angularVelocity = Vector3.zero;

                            bones[i].position = GetBoneCenter(i);
                            bones[i].rotation = GetBoneRotation(i);
                        } else {
                            bones[i].position = GetBoneCenter(i);
                            bones[i].rotation = GetBoneRotation(i);
                        }
                    } else {

                        if (hinges != null && hinges[i - 1] != null) {
                            Quaternion localRealFinger = (Quaternion.Inverse(PalmRot) * GetBoneRotation(i));
                            Quaternion localPhysicsFinger = (Quaternion.Inverse(Palm.transform.rotation) * bones[i].rotation);
                            float offset = (Quaternion.Inverse((Quaternion.Inverse(localRealFinger) * localPhysicsFinger))).eulerAngles.x;
                            if (offset > 180) {
                                offset -= 360f;
                            }
                            JointMotor mmotor = new JointMotor();
                            mmotor.force = 10000f;// FingerStrength;
                            mmotor.targetVelocity = offset * -50f;// FingerSpeed;
                            mmotor.freeSpin = false;
                            hinges[i - 1].motor = mmotor;
                        }
                    }
                }
            }

            if (dirty) {
                dirty = false;
                Palm.transform.position = PalmPos;
                Palm.transform.rotation = PalmRot;
                InitializeFingerJoints();
            }
        }
    }
}
