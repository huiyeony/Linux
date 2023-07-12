using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public class Inventory
    {
        public Dictionary<int, Item> Items { get; private set; } = new Dictionary<int, Item>();
        object _lock = new object();

        //인벤토리에 아이템 추가 
        public void Add(Item item)
        {
            lock (_lock)
            {
                Items.Add(item.itemDbId,item);
            }
        }

        public Item Get(int itemDbId)
        {
            lock (_lock)
            {
                Item item = null;
                Items.TryGetValue(itemDbId, out item);
                if (item != null)
                    return item;
                return null;
            }
        }
        //조건에 맞는 아이템 리턴 
        public Item Find(Func<Item, bool> func)
        {
            lock (_lock)
            {
                foreach(Item item in Items.Values)
                {
                    if (func.Invoke(item) == true)
                        return item;
                   
                }
                return null;
            }
        }
        public int GetEmptySlot()
        {
            for(int slot = 0; slot < 12; slot++)
            {
                Item item = null;
                item = Items.Values.FirstOrDefault(i => i.slot == slot);
                if (item == null)
                    return slot;
            }
            return -1;//빈자리 없을 때 
        }
        public bool Remove(int itemDbId)
        {
            lock (_lock)
            {
                if (Items.Remove(itemDbId))
                    return true;
                return false;
            }
        }
    

        
    }
}

