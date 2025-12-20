using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.memtable;

internal class MemoryTable
{
    public SortedDictionary<string, byte[]> DataPairs { get;private set; }

    public int SizeInbytes = 100;

    public bool isFull => calculateSizeInBytes() >= SizeInbytes;

    public MemoryTable()
    {
        DataPairs = new SortedDictionary<string, byte[]>();
    }
    public void Put(string Key , byte[] Value)
    {
        DataPairs.TryGetValue(Key, out byte[]? value);
        if(value is null)
            DataPairs.Add(Key, Value);
        else 
            DataPairs[Key] = Value;

    }

    public byte[]? Get(string Key)
    {
       DataPairs.TryGetValue(Key, out byte[]?  value);
        return value;

    }


    public void Delete (string Key)
    {
        DataPairs.Remove(Key);
    }


    private int calculateSizeInBytes()
    { int size = 0;
       
        foreach(var pair in DataPairs)
        {
            size += Encoding.UTF8.GetByteCount( pair.Key);
            size += pair.Value.Length;
        }
        

        return size;
    }


}
