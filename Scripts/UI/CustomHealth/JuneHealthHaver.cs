/*using Alexandria.Misc;
using System.Collections.Generic;
using UnityEngine;

namespace JuneLib.UI.CustomHealth
{
    public class JuneHealthHaver : BraveBehaviour
    {
        public event HealthHaver.OnDamagedEvent OnDamaged;
        public event HealthHaver.OnHealthChangedEvent OnHealthChanged;
        protected HealthHaver m_baseHealthHaver;
        protected PlayerController m_player;

        public void Start()
        {
            m_baseHealthHaver = gameObject.GetComponent<HealthHaver>();
            m_player = gameObject.GetComponent<PlayerController>();

            m_currentHealth = m_baseHealthHaver.currentHealth;
            m_maxHealth = m_baseHealthHaver.maximumHealth;
            m_curseHealthMaximum = m_baseHealthHaver.m_curseHealthMaximum;
            m_armor = m_baseHealthHaver.currentArmor;

            OnDamaged += JuneHealthHaver_OnDamaged;
            OnHealthChanged += JuneHealthHaver_OnHealthChanged;
            m_baseHealthHaver.OnDeath += M_baseHealthHaver_OnDeath;
            m_baseHealthHaver.OnPreDeath += M_baseHealthHaver_OnPreDeath;
        }

        private void M_baseHealthHaver_OnPreDeath(Vector2 obj)
        {
            /*m_baseHealthHaver.currentHealth = 0;
            m_baseHealthHaver.currentArmor = 0;
        }

        private void M_baseHealthHaver_OnDeath(Vector2 obj)
        {
            ETGModConsole.Log("I'm dying over here!");
        }

        private void JuneHealthHaver_OnHealthChanged(float resultValue, float maxValue)
        {
            HealthHaver.OnHealthChangedEvent healthChanged = ReflectionUtility.ReflectGetField<HealthHaver.OnHealthChangedEvent>(typeof(HealthHaver), "OnHealthChanged", m_baseHealthHaver);
            healthChanged.Invoke(resultValue, maxValue);
        }

        private void JuneHealthHaver_OnDamaged(float resultValue, float maxValue, CoreDamageTypes damageTypes, DamageCategory damageCategory, Vector2 damageDirection)
        {
            HealthHaver.OnDamagedEvent onDamaged = ReflectionUtility.ReflectGetField<HealthHaver.OnDamagedEvent>(typeof(HealthHaver), "OnDamaged", m_baseHealthHaver);
            onDamaged.Invoke(resultValue, maxValue, damageTypes, damageCategory, damageDirection);
        }

        public void SetHealthMaximum(float targetValue, float? amountOfHealthToGain = null, bool keepHealthPercentage = false)
        {
            if (targetValue == this.m_maxHealth)
            {
                return;
            }
            float currentHealthPercentage = this.GetCurrentHealthPercentage();
            if (!keepHealthPercentage)
            {
                if (amountOfHealthToGain != null)
                {
                    this.m_currentHealth += amountOfHealthToGain.Value;
                }
                else if (targetValue > this.m_maxHealth)
                {
                    this.m_currentHealth += targetValue - this.m_maxHealth;
                }
            }
            this.m_maxHealth = targetValue;
            if (keepHealthPercentage)
            {
                this.m_currentHealth = currentHealthPercentage * this.AdjustedMaxHealth;
                if (amountOfHealthToGain != null)
                {
                    this.m_currentHealth += amountOfHealthToGain.Value;
                }
            }
            this.m_currentHealth = Mathf.Min(this.m_currentHealth, this.AdjustedMaxHealth);
            if (m_baseHealthHaver.quantizeHealth)
            {
                this.m_currentHealth = BraveMathCollege.QuantizeFloat(this.m_currentHealth, m_baseHealthHaver.quantizedIncrement);
                this.m_maxHealth = BraveMathCollege.QuantizeFloat(this.m_maxHealth, m_baseHealthHaver.quantizedIncrement);
            }
            if (this.OnHealthChanged != null)
            {
                this.OnHealthChanged(this.m_currentHealth, this.AdjustedMaxHealth);
            }
        }

        public float GetCurrentHealthPercentage()
        {
            return this.m_currentHealth / this.AdjustedMaxHealth;
        }

        public float Armor
        {
            get { return m_armor; }
            set
            {
                if (this.m_player && !this.m_player.ForceZeroHealthState && m_baseHealthHaver.IsDead)
                {
                    return;
                }
                this.m_armor = value;
                if (this.OnHealthChanged != null)
                {
                    this.OnHealthChanged(this.m_currentHealth, this.AdjustedMaxHealth);
                }
            }
        }
        internal float AdjustedMaxHealth
        {
            get
            {
                return this.GetMaxHealth();
            }
            set
            {
                this.m_maxHealth = value;
            }
        }
        public float GetMaxHealth()
        {
            return Mathf.Min(this.CursedMaximum, this.m_maxHealth);
        }


        public float CursedMaximum
        {
            get
            {
                return this.m_curseHealthMaximum;
            }
            set
            {
                this.m_curseHealthMaximum = value;
                this.m_currentHealth = Mathf.Min(this.m_currentHealth, this.AdjustedMaxHealth);
                if (this.OnHealthChanged != null)
                {
                    this.OnHealthChanged(this.GetCurrentHealth(), this.GetMaxHealth());
                }
            }
        }
        public float GetCurrentHealth()
        {
            //ETGModConsole.Log($"Getting Health: {this.m_currentHealth}");
            return this.m_currentHealth;
        }

        public void FullHeal()
        {
            this.m_currentHealth = this.AdjustedMaxHealth;
            if (this.OnHealthChanged != null)
            {
                this.OnHealthChanged(this.m_currentHealth, this.AdjustedMaxHealth);
            }
        }

        protected float m_curseHealthMaximum;
        protected float m_currentHealth;
        protected float m_maxHealth;
        protected float m_armor;

        public void ApplyHealing(float healing)
        {
            if (!this.m_player || this.m_player.IsGhost)
            {
                return;
            }
            if (m_baseHealthHaver.ModifyHealing != null)
            {
                HealthHaver.ModifyHealingEventArgs modifyHealingEventArgs = new HealthHaver.ModifyHealingEventArgs
                {
                    InitialHealing = healing,
                    ModifiedHealing = healing
                };
                m_baseHealthHaver.ModifyHealing(m_baseHealthHaver, modifyHealingEventArgs);
                healing = modifyHealingEventArgs.ModifiedHealing;
            }
            this.m_currentHealth += healing;
            if (m_baseHealthHaver.quantizeHealth)
            {
                this.m_currentHealth = BraveMathCollege.QuantizeFloat(this.m_currentHealth, m_baseHealthHaver.quantizedIncrement);
            }
            if (this.m_currentHealth > this.AdjustedMaxHealth)
            {
                this.m_currentHealth = this.AdjustedMaxHealth;
            }
            if (this.OnHealthChanged != null)
            {
                this.OnHealthChanged(this.m_currentHealth, this.AdjustedMaxHealth);
            }
        }

        internal void ApplyDamageDirectional(float damage, Vector2 direction, string damageSource, CoreDamageTypes damageTypes, DamageCategory damageCategory = DamageCategory.Normal, bool ignoreInvulnerabilityFrames = false, PixelCollider hitPixelCollider = null, bool ignoreDamageCaps = false)
        {
            if (m_baseHealthHaver.PreventAllDamage && damageCategory != DamageCategory.Unstoppable)
            {
                return;
            }
            if (this.m_player && this.m_player.IsGhost)
            {
                return;
            }
            if (hitPixelCollider != null && m_baseHealthHaver.DamageableColliders != null && !m_baseHealthHaver.DamageableColliders.Contains(hitPixelCollider))
            {
                return;
            }
            if (m_baseHealthHaver.isFirstFrame)
            {
                return;
            }
            if (ignoreInvulnerabilityFrames)
            {
                if (!m_baseHealthHaver.vulnerable)
                {
                    return;
                }
            }
            else if (!m_baseHealthHaver.IsVulnerable)
            {
                return;
            }
            if (damage <= 0f)
            {
                return;
            }
            damage *= m_baseHealthHaver.GetDamageModifierForType(damageTypes);
            damage *= m_baseHealthHaver.AllDamageMultiplier;
            if (m_baseHealthHaver.OnlyAllowSpecialBossDamage && (damageTypes & CoreDamageTypes.SpecialBossDamage) != CoreDamageTypes.SpecialBossDamage)
            {
                damage = 0f;
            }
            if (this.m_player && !ignoreInvulnerabilityFrames)
            {
                damage = Mathf.Min(damage, 0.5f);
            }
            if (this.m_player && damageCategory == DamageCategory.BlackBullet)
            {
                damage = 1f;
            }
            if (m_baseHealthHaver.ModifyDamage != null)
            {
                HealthHaver.ModifyDamageEventArgs modifyDamageEventArgs = new HealthHaver.ModifyDamageEventArgs
                {
                    InitialDamage = damage,
                    ModifiedDamage = damage
                };
                m_baseHealthHaver.ModifyDamage(m_baseHealthHaver, modifyDamageEventArgs);
                damage = modifyDamageEventArgs.ModifiedDamage;
            }
            if (damage <= 0f)
            {
                return;
            }
            if (m_baseHealthHaver.NextShotKills)
            {
                damage = 100000f;
            }
            if (m_baseHealthHaver.HasCrest)
            {
                m_baseHealthHaver.HasCrest = false;
            }
            if (m_baseHealthHaver.healthIsNumberOfHits)
            {
                damage = 1f;
            }
            if (!m_baseHealthHaver.NextDamageIgnoresArmor && !m_baseHealthHaver.NextShotKills)
            {
                bool flag = this.Armor > 0f;
                if (flag)
                {
                    this.Armor -= 1f;
                    damage = 0f;
                    this.m_player.OnLostArmor();
                }
            }
            m_baseHealthHaver.NextDamageIgnoresArmor = false;
            float num = damage;
            if (num > 999f)
            {
                num = 0f;
            }
            num = Mathf.Min(this.m_currentHealth, num);
            if (m_baseHealthHaver.TrackPixelColliderDamage)
            {
                if (hitPixelCollider != null)
                {
                    float num2;
                    if (m_baseHealthHaver.PixelColliderDamage.TryGetValue(hitPixelCollider, out num2))
                    {
                        m_baseHealthHaver.PixelColliderDamage[hitPixelCollider] = num2 + damage;
                    }
                }
                else if (damage <= 999f)
                {
                    float num3 = damage * m_baseHealthHaver.GlobalPixelColliderDamageMultiplier;
                    List<PixelCollider> list = new List<PixelCollider>(m_baseHealthHaver.PixelColliderDamage.Keys);
                    for (int i = 0; i < list.Count; i++)
                    {
                        PixelCollider pixelCollider = list[i];
                        Dictionary<PixelCollider, float> pixelColliderDamage;
                        PixelCollider key;
                        (pixelColliderDamage = m_baseHealthHaver.PixelColliderDamage)[key = pixelCollider] = pixelColliderDamage[key] + num3;
                    }
                }
            }
            this.m_currentHealth -= damage;
            UnityEngine.Debug.Log(this.m_currentHealth + "||" + damage);
            if (m_baseHealthHaver.quantizeHealth)
            {
                this.m_currentHealth = BraveMathCollege.QuantizeFloat(this.m_currentHealth, m_baseHealthHaver.quantizedIncrement);
            }
            this.m_currentHealth = Mathf.Clamp(this.m_currentHealth, m_baseHealthHaver.minimumHealth, this.AdjustedMaxHealth);
            if (m_baseHealthHaver.flashesOnDamage && base.spriteAnimator != null && !m_baseHealthHaver.m_isFlashing)
            {
                if (m_baseHealthHaver.m_flashOnHitCoroutine != null)
                {
                    base.StopCoroutine(m_baseHealthHaver.m_flashOnHitCoroutine);
                }
                m_baseHealthHaver.m_flashOnHitCoroutine = null;
                if (m_baseHealthHaver.materialsToFlash == null)
                {
                    m_baseHealthHaver.materialsToFlash = new List<Material>();
                    m_baseHealthHaver.outlineMaterialsToFlash = new List<Material>();
                    m_baseHealthHaver.sourceColors = new List<Color>();
                }
                if (base.gameActor)
                {
                    for (int k = 0; k < m_baseHealthHaver.materialsToFlash.Count; k++)
                    {
                        m_baseHealthHaver.materialsToFlash[k].SetColor("_OverrideColor", base.gameActor.CurrentOverrideColor);
                    }
                }
                if (m_baseHealthHaver.outlineMaterialsToFlash != null)
                {
                    for (int l = 0; l < m_baseHealthHaver.outlineMaterialsToFlash.Count; l++)
                    {
                        if (l >= m_baseHealthHaver.sourceColors.Count)
                        {
                            UnityEngine.Debug.LogError("NOT ENOUGH SOURCE COLORS");
                            break;
                        }
                        m_baseHealthHaver.outlineMaterialsToFlash[l].SetColor("_OverrideColor", m_baseHealthHaver.sourceColors[l]);
                    }
                }
                m_baseHealthHaver.m_flashOnHitCoroutine = base.StartCoroutine(m_baseHealthHaver.FlashOnHit(damageCategory, hitPixelCollider));
            }
            if (m_baseHealthHaver.incorporealityOnDamage && !m_baseHealthHaver.m_isIncorporeal)
            {
                m_baseHealthHaver.StartCoroutine("IncorporealityOnHit");
            }
            m_baseHealthHaver.lastIncurredDamageSource = damageSource;
            m_baseHealthHaver.lastIncurredDamageDirection = direction;
            if (m_baseHealthHaver.shakesCameraOnDamage)
            {
                GameManager.Instance.MainCameraController.DoScreenShake(m_baseHealthHaver.cameraShakeOnDamage, new Vector2?(base.specRigidbody.UnitCenter), false);
            }
            if (m_baseHealthHaver.NextShotKills)
            {
                this.Armor = 0f;
            }
            if (this.OnDamaged != null)
            {
                this.OnDamaged(this.m_currentHealth, this.AdjustedMaxHealth, damageTypes, damageCategory, direction);
            }
            if (this.OnHealthChanged != null)
            {
                this.OnHealthChanged(this.m_currentHealth, this.AdjustedMaxHealth);
            }
            if (this.m_currentHealth <= 0f && this.Armor <= 0f)
            {
                m_baseHealthHaver.NextShotKills = false;
                if (!m_baseHealthHaver.SuppressDeathSounds)
                {
                    AkSoundEngine.PostEvent("Play_ENM_death", base.gameObject);
                    AkSoundEngine.PostEvent(string.IsNullOrEmpty(m_baseHealthHaver.overrideDeathAudioEvent) ? "Play_CHR_general_death_01" : m_baseHealthHaver.overrideDeathAudioEvent, base.gameObject);
                }
                m_baseHealthHaver.Die(direction);
            }
            else if (m_baseHealthHaver.usesInvulnerabilityPeriod)
            {
                base.StartCoroutine(m_baseHealthHaver.HandleInvulnerablePeriod(-1f));
            }
            if (damageCategory == DamageCategory.Normal || damageCategory == DamageCategory.Collision)
            {
                if (this.m_currentHealth <= 0f && this.Armor <= 0f)
                {
                    if (!m_baseHealthHaver.DisableStickyFriction)
                    {
                        StickyFrictionManager.Instance.RegisterDeathStickyFriction();
                    }
                }
                else
                {
                    StickyFrictionManager.Instance.RegisterPlayerDamageStickyFriction(damage);
                }
            }
        }
    }
}*/
