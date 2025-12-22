using LSMStorageEngine.memtable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.SSTable;

public class SSTable
{
    public string FilePath { get; set; }

    const int _metadataSatartOffset = -12;
    bool _disposed = false;

    //FileStream _filestream;
    //BinaryWriter _binartwriter;
    public SSTable(string filePath)
    {
        FilePath = filePath;
 
    }


    public void WriteToDisk(MemoryTable memtable)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SSTable));

        // data block section     

        var sortedEntries = memtable.Entries.OrderBy(e => e.Key).ToList();

        var minkey = sortedEntries.First().Key;
        var maxkey = sortedEntries.Last().Key;
        var index = new List<(string Key, long Offset)>();

      using  var _filestream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
      using  var _binartwriter = new BinaryWriter(_filestream);
        foreach (var entry in sortedEntries)
        {
            long offset = _filestream.Position;
            index.Add((entry.Key, offset));

            _binartwriter.Write(entry.Key);
            _binartwriter.Write(entry.Value.Value?.Length ?? 0);
            if (!entry.Value.IsDeleted)
                _binartwriter.Write(entry.Value.Value);

            _binartwriter.Write(entry.Value.Seq);
            _binartwriter.Write(entry.Value.IsDeleted);
        }

        //index section 
        long indexStartOffset = _filestream.Position;
        foreach (var item in index)
        {
            _binartwriter.Write(item.Key);
            _binartwriter.Write(item.Offset);
        }
        //footer section
        long MetaStartOffset = _filestream.Position;
        _binartwriter.Write(minkey);
        _binartwriter.Write(maxkey);
        _binartwriter.Write(indexStartOffset);
        _binartwriter.Write(index.Count);
        _binartwriter.Write(MetaStartOffset);
        _binartwriter.Write(0x4C534D31);

    }

    public (bool found, byte[]? value) Searchkey(string key)
    {
        using var fileStream = new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        fileStream.Seek(_metadataSatartOffset, SeekOrigin.End);
        long metaStartOffset = binaryReader.ReadInt64();
        fileStream.Seek(metaStartOffset, SeekOrigin.Begin);
        string minKey = binaryReader.ReadString();
        string maxKey = binaryReader.ReadString();
        if (string.CompareOrdinal(key, minKey) < 0 || string.CompareOrdinal(key, maxKey) > 0)
        {
            return (false, Array.Empty<byte>());
        }
        long indexStartOffset = binaryReader.ReadInt64();
        int indexCount = binaryReader.ReadInt32();

        fileStream.Seek(indexStartOffset, SeekOrigin.Begin);
        long keyoffset = -1;
        for (int i = 0; i < indexCount; i++)
        {
            string currentkey = binaryReader.ReadString();
            long currentkeyoffset = binaryReader.ReadInt64();

            if (currentkey == key)
            {
                keyoffset = currentkeyoffset;
                break;
            }
            if (string.CompareOrdinal(currentkey, key) > 0) break;
        }
        if (keyoffset == -1) return (false, null);

        fileStream.Seek(keyoffset, SeekOrigin.Begin);
        binaryReader.ReadString();
        int valueLength = binaryReader.ReadInt32();
        byte[] valueData = binaryReader.ReadBytes(valueLength);
        long seq = binaryReader.ReadInt64();
        bool isDeleted = binaryReader.ReadBoolean();


        if (isDeleted) return (false, null);

        return (true, valueData);


    }


    public IEnumerable<Entry> ReadAll()
    {
        using var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fs);

        fs.Seek(_metadataSatartOffset, SeekOrigin.End);
        long metaOffset = reader.ReadInt64();
        fs.Seek(metaOffset, SeekOrigin.Begin);

        reader.ReadString();
        reader.ReadString();

        long indexOffset = reader.ReadInt64();
        int count = reader.ReadInt32();

        fs.Seek(0, SeekOrigin.Begin);
        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadString();
            int valLen = reader.ReadInt32();
            byte[] val = reader.ReadBytes(valLen);
            long seq = reader.ReadInt64();
            bool deleted = reader.ReadBoolean();
            yield return new Entry(key, val, seq, deleted);
        }
    }


    
}
