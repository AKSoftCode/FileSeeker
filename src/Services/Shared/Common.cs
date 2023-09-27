using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Shared;
public record FileSearchCriteria(string FileNamePattern, long? MinFileSizeBytes);

public record FileSearchResult(string FileName, string FilePath, long FileSizeBytes);
