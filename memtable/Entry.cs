using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.memtable;

internal class Entry
{
    public string Key { get; set; }
    public byte[] Value { get; set; }
    public long Seq { get; set; }

    public bool IsDeleted = false;
    public Entry(string key, byte[] value, long seq, bool isDeleted = false )
    {
        Key = key;
        Value = value;
        IsDeleted = isDeleted;
        Seq = seq;
    }

    public bool CompareTo(Entry other)
    {
        return this.Value == other.Value && this.IsDeleted;
    }

}
