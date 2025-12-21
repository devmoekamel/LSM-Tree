using LSMStorageEngine.memtable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSMStorageEngine.WAL;

public class WAL
{
    public WAL()
    {
    }


    public  void Append(string key , byte[] value,long seq,bool isDeleted)
    {
       
    }
