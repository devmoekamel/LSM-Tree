using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.SSTable;

public class SSTableMetadata
{
    public  string  maxKey { get; set; }
    public  string  minKey { get; set; }
    public string Path { get; set; }

    public SSTableMetadata(string Path, int MetadataOffset)
    {
        using var fileStream = new FileStream(Path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream);
        fileStream.Seek(MetadataOffset, SeekOrigin.End);
        long metaStartOffset = reader.ReadInt64();
        fileStream.Seek(metaStartOffset, SeekOrigin.Begin);
        minKey = reader.ReadString();
        maxKey= reader.ReadString();
        this.Path = Path;
    }


    public bool IsKeyInRange(string key)
    {
        return string.CompareOrdinal(key, minKey) >= 0 && string.CompareOrdinal(key, maxKey) <= 0;
    }

}
