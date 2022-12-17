using Dungeonator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace newKatmod
{
    static class SimpleCompanionBehaviours
    {
        public class SimpleCompanionApproach : MovementBehaviorBase
        {
            public SimpleCompanionApproach(int desiredDistance)
            {
                DesiredDistance = desiredDistance;
            }

            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter) { base.Init(gameObject, aiActor, aiShooter); }
            public override void Upkeep()
            {
                base.Upkeep();
                base.DecrementTimer(ref this.repathTimer, false);
            }
            public override BehaviorResult Update()
            {
                if (Owner == null)
                {
                    if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                    {
                        Owner = this.m_aiActor.CompanionOwner;
                    }
                    else
                    {
                        Owner = GameManager.Instance.BestActivePlayer;
                    }
                }
                SpeculativeRigidbody overrideTarget = this.m_aiActor.OverrideTarget;
                BehaviorResult result;
                if (this.repathTimer > 0f)
                {
                    result = ((overrideTarget == null) ? BehaviorResult.Continue : BehaviorResult.SkipRemainingClassBehaviors);
                }
                else
                {
                    if (overrideTarget == null)
                    {
                        this.PickNewTarget();
                        result = BehaviorResult.Continue;
                    }
                    else
                    {
                        this.isInRange = (Vector2.Distance(this.m_aiActor.specRigidbody.UnitCenter, overrideTarget.UnitCenter) <= this.DesiredDistance);
                        if (overrideTarget != null && !this.isInRange)
                        {
                            this.m_aiActor.PathfindToPosition(overrideTarget.UnitCenter, null, false, null, null, null, false);
                            this.repathTimer = this.PathInterval;
                            result = BehaviorResult.SkipRemainingClassBehaviors;
                        }
                        else
                        {
                            if (overrideTarget != null && this.repathTimer >= 0f)
                            {
                                this.m_aiActor.ClearPath();
                                this.repathTimer = -1f;
                            }
                            result = BehaviorResult.Continue;
                        }
                    }
                }
                return result;
            }
            private void PickNewTarget()
            {
                if (this.m_aiActor != null)
                {
                    if (this.Owner == null)
                    {
                        if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                        {
                            Owner = this.m_aiActor.CompanionOwner;
                        }
                        else
                        {
                            Owner = GameManager.Instance.BestActivePlayer;
                        }
                    }
                    this.Owner.CurrentRoom.GetActiveEnemies(RoomHandler.ActiveEnemyType.RoomClear, ref this.roomEnemies);
                    for (int i = 0; i < this.roomEnemies.Count; i++)
                    {
                        AIActor aiactor = this.roomEnemies[i];
                        if (aiactor.IsHarmlessEnemy || !aiactor.IsNormalEnemy || aiactor.healthHaver.IsDead || aiactor == this.m_aiActor || aiactor.EnemyGuid == "ba928393c8ed47819c2c5f593100a5bc")
                        { this.roomEnemies.Remove(aiactor); }
                    }
                    if (this.roomEnemies.Count == 0) { this.m_aiActor.OverrideTarget = null; }
                    else
                    {
                        AIActor aiActor = this.m_aiActor;
                        AIActor aiactor2 = this.roomEnemies[UnityEngine.Random.Range(0, this.roomEnemies.Count)];
                        aiActor.OverrideTarget = ((aiactor2 != null) ? aiactor2.specRigidbody : null);
                    }
                }
            }
            public float PathInterval = 0.25f;
            public float DesiredDistance;
            private float repathTimer;
            private List<AIActor> roomEnemies = new List<AIActor>();
            private bool isInRange;
            private PlayerController Owner;
        }
        public class CompanionGoesToCursor : MovementBehaviorBase
        {
            public override void Init(GameObject gameObject, AIActor aiActor, AIShooter aiShooter) { base.Init(gameObject, aiActor, aiShooter); }
            public override BehaviorResult Update()
            {
                if (Owner == null)
                {
                    if (this.m_aiActor && this.m_aiActor.CompanionOwner)
                    {
                        Owner = this.m_aiActor.CompanionOwner;
                    }
                    else
                    {
                        Owner = GameManager.Instance.BestActivePlayer;
                    }
                }
                BehaviorResult result;
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if (mousePosition != null)
                {
                    this.m_aiActor.FakePathToPosition(mousePosition);
                    result = BehaviorResult.SkipRemainingClassBehaviors;
                }
                else
                {
                    result = BehaviorResult.Continue;
                }
                return result;
            }
            private PlayerController Owner;
        }

        public class PingPongAroundBehavior : MovementBehaviorBase
        {
            public PingPongAroundBehavior()
            {
                this.startingAngles = new float[]
                {
                    45f,
                    135f,
                    225f,
                    315f
                };
                this.motionType = PingPongAroundBehavior.MotionType.Diagonals;
            }

            private bool ReflectX
            {
                get
                {
                    return this.motionType == MotionType.Diagonals || this.motionType == MotionType.Horizontal;
                }
            }

            private bool ReflectY
            {
                get
                {
                    return this.motionType == MotionType.Diagonals || this.motionType == MotionType.Vertical;
                }
            }

            public override void Start()
            {
                base.Start();
                this.m_aiActor.specRigidbody.AddCollisionLayerOverride(CollisionMask.LayerToMask(CollisionLayer.EnemyBlocker));
                SpeculativeRigidbody specRigidbody = this.m_aiActor.specRigidbody;
                specRigidbody.OnTileCollision += OnCollision;
            }

            public override BehaviorResult Update()
            {
                this.m_startingAngle = BraveMathCollege.ClampAngle360(BraveUtility.RandomElement<float>(this.startingAngles));
                this.m_aiActor.BehaviorOverridesVelocity = true;
                this.m_aiActor.BehaviorVelocity = BraveMathCollege.DegreesToVector(this.m_startingAngle, this.m_aiActor.MovementSpeed);
                this.m_isBouncing = true;
                return BehaviorResult.RunContinuousInClass;
            }

            public override ContinuousBehaviorResult ContinuousUpdate()
            {
                base.ContinuousUpdate();
                return this.m_aiActor.BehaviorOverridesVelocity ? ContinuousBehaviorResult.Continue : ContinuousBehaviorResult.Finished;
            }

            public override void EndContinuousUpdate()
            {
                base.EndContinuousUpdate();
                this.m_isBouncing = false;
            }

            protected virtual void OnCollision(CollisionData collision)
            {
                if (!this.m_isBouncing)
                {
                    return;
                }
                if (collision.OtherRigidbody)
                {
                    return;
                }
                if (collision.CollidedX || collision.CollidedY)
                {
                    Vector2 vector = collision.MyRigidbody.Velocity;
                    if (collision.CollidedX && this.ReflectX)
                    {
                        vector.x *= -1f;
                    }
                    if (collision.CollidedY && this.ReflectY)
                    {
                        vector.y *= -1f;
                    }
                    if (this.motionType == MotionType.Horizontal)
                    {
                        vector.y = 0f;
                    }
                    if (this.motionType == MotionType.Vertical)
                    {
                        vector.x = 0f;
                    }
                    vector = vector.normalized * this.m_aiActor.MovementSpeed;
                    PhysicsEngine.PostSliceVelocity = new Vector2?(vector);
                    this.m_aiActor.BehaviorVelocity = vector;
                }
            }

            public float[] startingAngles;

            public MotionType motionType;

            private bool m_isBouncing;

            private float m_startingAngle;

            public enum MotionType
            {
                Diagonals = 10,
                Horizontal = 20,
                Vertical = 30
            }
        }
    }
}
