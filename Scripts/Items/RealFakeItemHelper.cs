using UnityEngine;

namespace JuneLib.Items
{
    public static class RealFakeItemHelper
    {
        public static PassiveItem CreateFakeItem(PassiveItem item, PlayerController user, Transform parent, bool pickedUpThisRun = true)
        {
            PassiveItem pItem = UnityEngine.Object.Instantiate(item.gameObject).GetComponent<PassiveItem>();
            pItem.m_pickedUpThisRun = pickedUpThisRun;
            pItem.encounterTrackable.SuppressInInventory = true;
            pItem.encounterTrackable.IgnoreDifferentiator = true;
            pItem.Pickup(user);

            pItem.transform.SetParent(parent);
            pItem.transform.position = parent.position;
            pItem.gameObject.AddComponent<FakeRealItemBehaviour>();
            return pItem;
        }

        public static void RemoveFakeItem(PlayerController player, PassiveItem item)
        {
            if (player == null || item == null)
            {
                Debug.Log("junelib: trying to remove a fake item that either doesnt exist or doesnt have a player? ");
                return;
            }
            player.DropPassiveItem(item);
            Object.Destroy(item.gameObject);
        }
    }
    public class FakeRealItemBehaviour : BraveBehaviour
    {
        public PassiveItem parentItem;
        public PassiveItem item;

        public void Start()
        {
            item = this.GetComponent<PassiveItem>();
            if (gameObject.transform.parent != null)
            {
                GameObject pObject = gameObject.transform.parent.gameObject;
                if (pObject != null && pObject.GetComponent<PassiveItem>())
                {
                    parentItem = pObject.GetComponent<PassiveItem>();
                }
                else
                {
                    DontDestroyOnLoad(pObject);
                }
            }
        }

        protected void Update()
        {
            if (item != null)
                if ((parentItem != null && parentItem.Owner == null)
                    || item.Owner == null)
                {
                    if (parentItem && parentItem.Owner != null)
                    {

                        RealFakeItemHelper.RemoveFakeItem(item.Owner, item);
                    }
                    else
                    {
                        Destroy(item);
                    }
                }
        }
    }
}
