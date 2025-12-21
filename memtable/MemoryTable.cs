using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.memtable;

public class MemoryTable
{

    public ConcurrentDictionary<string,Entry> Entries { get; private set; }
    public int MaxSize { get; set; } 

    public bool  IsReadyToBeFlushed => GetSize()>= MaxSize;
    public MemoryTable(int maxSize)
    {  
        Entries = new ConcurrentDictionary<string, Entry>();
        MaxSize = maxSize;
    }
    public void Put(string Key , byte[] Value,int seq)
    {
        Entries.TryGetValue(Key, out Entry? existingEntry);

        if (existingEntry is null)

        {
            Entries.TryAdd(Key, new Entry(Key, Value,seq, false));
            return;
        }
        bool sameValue = existingEntry.Value.SequenceEqual(Value) && !existingEntry.IsDeleted;

        if (!sameValue)
        {
            Entries[Key] = new Entry(Key, Value,seq , false);
        }


    }

    public byte[]? Get(string Key)
    {
        Entries.TryGetValue(Key, out Entry? entry);
        if (entry is null || entry.IsDeleted)
        {
            return null;
        }
        return entry.Value;
    }


    public void Delete (string Key, int seq)
    {
        Entries.TryGetValue(Key, out Entry? existingEntry);
        if (existingEntry is null)
        {
            Entries.TryAdd(Key, new Entry(Key, Array.Empty<byte>(),seq , true));
            return;
        }
        
        if (!existingEntry.IsDeleted)
        {
            Entries[Key] = new Entry(Key, Array.Empty<byte>(),seq , true);
        }
    }

    public int GetSize() {

        int size = 0;

        foreach (var entry in Entries)
        {
            size += Encoding.UTF8.GetByteCount(entry.Key);
            size += entry.Value.Value.Length ;
            size += sizeof(long); 
            size += sizeof(bool); 

        }

        return size;
    }
   

}
