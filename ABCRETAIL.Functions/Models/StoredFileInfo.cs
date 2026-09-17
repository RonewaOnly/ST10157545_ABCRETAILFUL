using System;
using System.Collections.Generic;
using System.Text;

namespace ABCRETAIL.Functions.Models
{
    public class StoredFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }
}
