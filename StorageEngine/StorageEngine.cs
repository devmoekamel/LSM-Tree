using LSMStorageEngine.memtable;
using LSMStorageEngine.SSTable;
using LSMStorageEngine.WAL;
using System.Collections.Concurrent;
using System.Net.Sockets;
namespace LSMStorageEngine.StorageEngine;

public class StorageEngine : IDisposable
{
    public StorageEngineConfig Config { get; set; }
    private WAL.WAL _WAL;
    private MemoryTable _CurrentMemTable;
    private ConcurrentQueue<MemoryTable> _ReadOnlyMemTables;
    private SStableManager _stableManager;
    private long _Seq = 1;
    private object _Writelock = new object();
    private Task _flushBackgroundWorker;
    private CancellationTokenSource _cts = new();

    public StorageEngine(StorageEngineConfig config)
    {
        Config = config;
        if (!Directory.Exists(Config.Path))
        {
            Directory.CreateDirectory(Config.Path);
        }
        _CurrentMemTable = new MemoryTable(config.MaxMemTableSize);
        Recover();
        var newWalFilePath = Path.Combine(Config.Path, $"Wal_{++_Seq}.wal");
        _CurrentMemTable.setWalFilePath(newWalFilePath);
         _WAL = new WAL.WAL(newWalFilePath);
        _ReadOnlyMemTables = new ConcurrentQueue<MemoryTable>();
        _stableManager = new SStableManager(config.Path);
        _flushBackgroundWorker = Task.Run(FlushWorker);
    }


    public byte[]? Get(string key)
    {
        var _CurrentMemTableSnapShot = _CurrentMemTable;
       var result = _CurrentMemTableSnapShot.Get(key);
        if (result is not null)
            return result;
        foreach(var memTable in _ReadOnlyMemTables.Reverse() )
        {
          var res =  memTable.Get(key);
            if(res is not null)
                return res;
        }


        var finalresult = _stableManager.Search(key);

        return finalresult.found ? finalresult.value : null; 
    }

    public void Put(string key, byte[] value)
    {
        lock(_Writelock)
        {
            var seq = ++_Seq;
           _WAL.Append(key, value, seq, false);
            _CurrentMemTable.Put(key, value, seq);
            SwapMemTableIfNeeded();
        }
       
    }

    private void SwapMemTableIfNeeded()
    {
        if (_CurrentMemTable.ReadyToBeFlushed(Config.MaxMemTableSize))
        {
            _ReadOnlyMemTables.Enqueue(_CurrentMemTable);

            var newWalFilePath = Path.Combine(Config.Path, $"Wal_{++_Seq}.wal");
            _WAL = new WAL.WAL(newWalFilePath);
            _CurrentMemTable = new MemoryTable(Config.MaxMemTableSize);
            _CurrentMemTable.setWalFilePath(newWalFilePath);

        }
    }

 public void Delete(string key)
    {
        lock(_Writelock)
        {
            var seq = ++_Seq;
            _WAL.Append(key, Array.Empty<byte>(), seq, true);
            _CurrentMemTable.Delete(key, seq);
            SwapMemTableIfNeeded();
        }
       
    }

    public void Recover()
    {
        var files = Directory.GetFiles(Config.Path, "*.wal")
                       .OrderBy(f => f).ToList();

        List<Entry>  RecoverdEntries = new List<Entry>();
        foreach (var file in files)
        {
          var  WAL = new WAL.WAL(file);

           RecoverdEntries.AddRange(WAL.Read() ?? Enumerable.Empty<Entry>());
            
        }
        if(RecoverdEntries is not null && RecoverdEntries.Count >0)
            _CurrentMemTable.PutRange(RecoverdEntries);
     }

    private async Task FlushWorker()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            if (!_ReadOnlyMemTables.IsEmpty)
            {
                FlushToSSTable();
                Compact();
            }
            await Task.Delay(Config.FlushIntervalMS);
        }
    }
    public  void FlushToSSTable()
    {
        while (_ReadOnlyMemTables.TryDequeue(out var memtable))
        {
            string fileName = Path.Combine(Config.Path, $"Data_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.sst");
            var sst = new SSTable.SSTable(fileName);
            sst.WriteToDisk(memtable);
            _stableManager.RegisterNewTable(fileName);
            
           if(!string.IsNullOrEmpty(memtable.WalFilePath)&& File.Exists(memtable.WalFilePath))
            {
                File.Delete(memtable.WalFilePath);
            }
        }
    }
    public void ManualFlushToSSTable()
    {
       
            string fileName = Path.Combine(Config.Path, $"Data_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.sst");
            var sst = new SSTable.SSTable(fileName);
            sst.WriteToDisk(_CurrentMemTable);
            _stableManager.RegisterNewTable(fileName);
            


    }
    public void Compact()
    {
        var filesToCompact = Directory.GetFiles(Config.Path, "*.sst")
              .OrderBy(f => File.GetCreationTime(f)).ToList();
        if (filesToCompact.Count < Config.NumberOfFilesTOCompact) return;

        var mergedData = new SortedDictionary<string, Entry>();

        foreach (var file in filesToCompact)
        {
             var sst = new SSTable.SSTable(file);
            var entries = sst.ReadAll();

            foreach (var entry in entries)
            {
                if (!mergedData.ContainsKey(entry.Key) || entry.Seq > mergedData[entry.Key].Seq)
                {
                    mergedData[entry.Key] = entry;
                }
            }
        }

        string newFileName = Path.Combine(Config.Path, $"Merged_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.sst");
        var newSst = new SSTable.SSTable(newFileName);

        var tempMem = new MemoryTable(int.MaxValue);
        tempMem.PutRange(mergedData.Values.ToList());

        newSst.WriteToDisk(tempMem);

        _stableManager.ReplaceTables(filesToCompact, newFileName);
    }
    public void Dispose()
    {
        _cts.Cancel();
        _flushBackgroundWorker.Wait();
        FlushToSSTable();
        //_WAL.Dispose();
    }
}