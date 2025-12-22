using LSMStorageEngine.memtable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.WAL;

public class WAL : IDisposable
{
    private string path;
    private FileStream? _fileStream;
    private BinaryWriter? _writer;
    private bool _disposed = false;
    public WAL(string path)
    {
        this.path = path;
        _fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        _writer = new BinaryWriter(_fileStream);
    }

    public void Append(string key, byte[] value, long seq, bool isDeleted)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(WAL));
        var folder = Path.GetDirectoryName(path);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);



        _writer.Write(key);
        _writer.Write(value?.Length ?? 0);           
        if (value != null && value.Length > 0)
            _writer.Write(value);
        _writer.Write(seq);
        _writer.Write(isDeleted);
        _writer.Flush();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _writer?.Dispose();
        _fileStream?.Dispose();
        _disposed = true;
    }

    public IEnumerable<Entry> Read()
    {
        if (!File.Exists(path))
             return Enumerable.Empty<Entry>(); ;

        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fileStream);

        List<Entry> entries = new List<Entry>();    
        while (fileStream.Position < fileStream.Length)
        {
            var key = reader.ReadString();
            var valueLength = reader.ReadInt32();
            var value = valueLength > 0 ? reader.ReadBytes(valueLength) : Array.Empty<byte>();
            var seq = reader.ReadInt64();
            var isDeleted = reader.ReadBoolean();
            
           entries.Add(new Entry(key,value,seq,isDeleted));
        }

        return entries;
    }
}