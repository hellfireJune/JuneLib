using Alexandria.ItemAPI;
using Alexandria.PrefabAPI;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
public abstract class BaseAdvancedGrappleModule
{
    [SerializeField]
    public BaseAdvancedGrappleModule()
    {
        grappleSpeed = 20;
    }

    public static GameObject GenerateHookPrefab(string name, IntVector2 hookDimensions, string hookSprite, string chainSprite, Assembly assembly = null)
    {
        if (assembly == null) { assembly = Assembly.GetCallingAssembly(); }
        GameObject hookObject = PrefabBuilder.BuildObject(name);

        SpeculativeRigidbody specRigidBody = hookObject.AddComponent<SpeculativeRigidbody>();
        PixelCollider collide = new PixelCollider
        {
            IsTrigger = false,
            ManualWidth = hookDimensions.x,
            ManualHeight = hookDimensions.y,
            ColliderGenerationMode = PixelCollider.PixelColliderGeneration.Manual,
            CollisionLayer = CollisionLayer.Projectile,
            ManualOffsetX = 0,
            ManualOffsetY = 0,
            BagleUseFirstFrameOnly = false
        };
        specRigidBody.PixelColliders = new List<PixelCollider>
            {
                collide
            };
        int hookSpriteID = SpriteBuilder.AddSpriteToCollection(hookSprite, ETGMod.Databases.Items.ProjectileCollection, assembly);
        tk2dSprite sprite = hookObject.gameObject.GetOrAddComponent<tk2dSprite>();
        sprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, hookSpriteID);

        GameObject chainObject = PrefabBuilder.BuildObject(name);
        chainObject.transform.parent = hookObject.transform;
        int spriteID = SpriteBuilder.AddSpriteToCollection(chainSprite, ETGMod.Databases.Items.ProjectileCollection, assembly);
        tk2dTiledSprite tiledSprite = chainObject.gameObject.GetOrAddComponent<tk2dTiledSprite>();

        tiledSprite.SetSprite(ETGMod.Databases.Items.ProjectileCollection, spriteID);
        tk2dSpriteDefinition def = tiledSprite.GetCurrentSpriteDef();
        def.ConstructOffsetsFromAnchor(tk2dBaseSprite.Anchor.MiddleLeft);

        //tiledSprite.dimensions = new Vector2(3, chainY);

        return hookObject;
    }
    public void Trigger(PlayerController user)
    {
        m_user = user;
        collidedBody = null;
        m_hitTile = false;
        m_hookObject = UnityEngine.Object.Instantiate(hookPrefab);
        m_hookObject.transform.position = user.CenterPosition.ToVector3ZUp(0f);
        currentCoroutine = user.StartCoroutine(UseGrapple(user));
    }

    protected bool isDone = false;
    protected IEnumerator UseGrapple(PlayerController user)
    {
        SpeculativeRigidbody hookRigidBody = m_hookObject.GetComponent<SpeculativeRigidbody>();
        OnPreFireHook(user, hookRigidBody);
        Vector2 startPoint = user.CenterPosition;
        Vector2 aimDirection = user.unadjustedAimPoint.XY() - startPoint;
        hookRigidBody.Velocity = aimDirection.normalized * this.grappleSpeed;
        hookRigidBody.Reinitialize();

        hookRigidBody.OnPreRigidbodyCollision += OnPreRigidBodyCollision;
        hookRigidBody.OnRigidbodyCollision += OnRigidBodyCollision;
        hookRigidBody.OnTileCollision += OnRigidBodyCollision;

        tk2dTiledSprite chainSprite = hookRigidBody.GetComponentInChildren<tk2dTiledSprite>();
        chainSprite.dimensions = new Vector2(3f, 3f);
        chainSprite.anchor = tk2dBaseSprite.Anchor.MiddleRight;

        while (!isDone)
        {
            if (!m_hookObject)
            {
                yield break;
            }
            Vector2 currentDirVec = hookRigidBody.UnitCenter - user.CenterPosition;
            int pixelsWide = Mathf.RoundToInt(currentDirVec.magnitude / 0.0625f);
            chainSprite.dimensions = new Vector2((float)pixelsWide, chainSprite.dimensions.y);
            float currentChainSpriteAngle = BraveMathCollege.Atan2Degrees(currentDirVec);
            hookRigidBody.transform.rotation = Quaternion.Euler(0f, 0f, currentChainSpriteAngle);

            UpdateHook(currentDirVec, hookRigidBody);
            yield return null;
        }
        this.m_user = null;
        UnityEngine.Object.Destroy(this.m_hookObject);
        this.m_hookObject = null;
        yield break;
    }

    protected virtual void OnPreRigidBodyCollision(SpeculativeRigidbody myRigidbody, PixelCollider myCollider, SpeculativeRigidbody otherRigidbody, PixelCollider otherCollider)
    {
        if (collidedBody || m_hitTile)
        {
            PhysicsEngine.SkipCollision = true;
            return;
        }
        if (otherRigidbody.GetComponent<PlayerController>() != null)
        {
            PhysicsEngine.SkipCollision = true;
            return;
        }
    }

    protected virtual void OnRigidBodyCollision(CollisionData rigidbodyCollision)
    {
        m_hitTile = true;
        rigidbodyCollision.MyRigidbody.Velocity = Vector2.zero;
    }

    protected virtual void UpdateHook(Vector2 Direction, SpeculativeRigidbody hookbody)
    {
        if (m_hitTile)
        {
            hookbody.Velocity = Direction.normalized * -grappleSpeed; //(hookbody.UnitCenter - m_user.specRigidbody.UnitCenter).normalized * grappleSpeed;
            if (Vector2.Distance(m_user.specRigidbody.UnitCenter, hookbody.UnitCenter) < 1.5f)
            {
                isDone = true;
            }
        }
    }

    protected bool m_hitTile;

    protected SpeculativeRigidbody collidedBody;

    protected abstract void OnPreFireHook(PlayerController user, SpeculativeRigidbody hook);

    public float grappleSpeed;

    public GameObject hookPrefab;

    protected GameObject m_hookObject;

    protected PlayerController m_user;

    private Coroutine currentCoroutine;
}
