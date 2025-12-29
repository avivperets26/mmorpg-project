using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.CharacterCreator
{
    [System.Serializable]
    public class TransformConnectionData
    {
        public bool Active = false;
        public bool SoftFollow = true;
        [Range(1F,60F)]
        public float FollowSpeed = 10F;
        public bool Spring = true;
        [Range(0F, 1F)]
        public float SpringDampping = 0.5F;
        [HideInInspector]
        public Vector3 Velocity=new Vector3(0F,0F,0F);
        [HideInInspector]
        public Quaternion AngularVelocity = Quaternion.identity;
    }
    [DefaultExecutionOrder(50)]
    public class TransformConnection : MonoBehaviour
    {
        public Transform Target;
        public TransformConnectionData PositionConnection;
        public TransformConnectionData RotationConnection;
        public TransformConnectionData ScaleConnection;
        Vector3 pos;
        Quaternion rot;
        void Start()
        {
            pos = Target.position;
            rot = Target.rotation;
        }


        void LateUpdate()
        {
            if (PositionConnection.Active)
            {
                if (PositionConnection.SoftFollow)
                {
                    pos = Vector3.Lerp(pos, Target.position+ PositionConnection.Velocity, PositionConnection.FollowSpeed *Time.deltaTime);
                    if (PositionConnection.Spring) {
                        PositionConnection.Velocity += Vector3.Lerp((Target.position - pos) * PositionConnection.FollowSpeed * Time.deltaTime,Vector3.zero, PositionConnection.SpringDampping*0.8F);
                        PositionConnection.Velocity = Vector3.Lerp(PositionConnection.Velocity,Vector3.zero,Time.deltaTime* PositionConnection.SpringDampping *2F);
                    }
                    transform.position = pos;
                }
                else
                {
                    transform.position = Target.position;
                }
            }

            if (RotationConnection.Active)
            {
                if (RotationConnection.SoftFollow)
                {
                    rot = Quaternion.Lerp(rot, Target.rotation * RotationConnection.AngularVelocity, RotationConnection.FollowSpeed *1.5F* Time.deltaTime);
                    if (RotationConnection.Spring)
                    {
                        RotationConnection.AngularVelocity *= Quaternion.Lerp(Quaternion.Inverse(rot) *Target.rotation, Quaternion.identity,0.5F+RotationConnection.SpringDampping*0.45F);
                        RotationConnection.AngularVelocity =Quaternion.Lerp(RotationConnection.AngularVelocity,Quaternion.identity, Time.deltaTime * RotationConnection.SpringDampping * 2F);
                    }
                    transform.rotation = rot;
                }
                else
                {
                    transform.rotation = Target.rotation;
                }
            }

            if (ScaleConnection.Active)
            {
                if (ScaleConnection.SoftFollow)
                {
                    transform.localScale = Vector3.Lerp(transform.localScale, Target.localScale + ScaleConnection.Velocity, ScaleConnection.FollowSpeed * Time.deltaTime);
                    if (ScaleConnection.Spring)
                    {
                        ScaleConnection.Velocity += Vector3.Lerp((Target.localScale - transform.localScale) * ScaleConnection.FollowSpeed * Time.deltaTime, Vector3.zero, ScaleConnection.SpringDampping * 0.8F);
                        ScaleConnection.Velocity = Vector3.Lerp(ScaleConnection.Velocity, Vector3.zero, Time.deltaTime * ScaleConnection.SpringDampping * 2F);
                    }
                }
                else
                {
                    transform.localScale = Target.localScale;
                }
            }
        }
    }
}
