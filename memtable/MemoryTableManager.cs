using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.memtable;

internal static class MemoryTableManager
{
    public static ConcurrentQueue<MemoryTable>  ReadOnlyMemtables { get;  set; } = new ConcurrentQueue<MemoryTable>();

    public static MemoryTable CurrentMemtable { get; set; } = new MemoryTable();
    public static int CountOfReadOnlyMemtables => ReadOnlyMemtables.Count;

 
    public static void AddReadOnlyMemtable(MemoryTable memtable)
    {
        ReadOnlyMemtables.Enqueue(memtable);
        // turn on Flush to SSTable

    }

    public static ConcurrentQueue<MemoryTable> GetReadOnlyMemtables()
    {
        return ReadOnlyMemtables;
    }


    public static MemoryTable CreateMemoryTable()
    {
               return new MemoryTable();
    }


    public static void Put(string key , byte[] value)
    {
        if (CurrentMemtable.isFull)
        {
            AddReadOnlyMemtable(CurrentMemtable);
            CurrentMemtable = CreateMemoryTable();
            CurrentMemtable.Put(key, value);
        }
        else
        {
            CurrentMemtable.Put(key, value);
        }
    }

    public static void  Delete  (string Key)
    {
               CurrentMemtable.Delete(Key);
    }

    public static (bool found,byte[]? value) Get (string Key)
    {
         CurrentMemtable.DataPairs.TryGetValue(Key,out var value);
        
        if(value is not null)
        {
            return (true,value);
        }
        var Snapshot = new ConcurrentQueue<MemoryTable>(ReadOnlyMemtables).ToList();

     for(int index = Snapshot.Count-1; index>=0; index--)
        {
           Snapshot[index].DataPairs.TryGetValue(Key, out var val);
            if(val is not null)
             {
                return (true,val);
            }
        }

        return (false,null);
    }
}
