public static class EnemyHelpers
{
    public static void AddPermanentCharm(this AIActor actor, GameActorCharmEffect charm = null)
    {
        if (charm == null)
        {
            actor.ApplyEffect(GameManager.Instance.Dungeon.sharedSettingsPrefab.DefaultPermanentCharmEffect, 1f, null);
        }
        else
        {
            actor.ApplyEffect(charm, 1f, null);
        }
        actor.gameObject.AddComponent<KillOnRoomClear>();
        actor.IsHarmlessEnemy = true;
        actor.IgnoreForRoomClear = true;
        if (actor.gameObject.GetComponent<SpawnEnemyOnDeath>())
        {
            UnityEngine.Object.Destroy(actor.gameObject.GetComponent<SpawnEnemyOnDeath>());
        }
    }
}
