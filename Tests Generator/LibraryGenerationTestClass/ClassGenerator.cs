using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryGenerationTestClass
{
    public class ClassGenerator
    {
        internal int FilesReadCount;
        internal int FilesWriteCount;
        internal int ClassesGenerateCount;
        internal string Path;
        internal List<string> FilesTestClasssesPaths;
        
        public ClassGenerator(int filesreadcount, int fileswritecount, int classesgeneratecount, string path, List<string> filestestclassespaths) 
        {
            FilesReadCount = filesreadcount;
            FilesWriteCount = fileswritecount;
            ClassesGenerateCount = classesgeneratecount;
            Path = path;
            FilesTestClasssesPaths = filestestclassespaths;
        }
        
    }
}
