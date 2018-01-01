using DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tasks.BuiltIn.Note
{

    public class NoteTaskDataCRUD : CMTaskDataCRUD<NoteDto>
    {
        public NoteTaskDataCRUD(string collectionName) : base(collectionName)
        {
        }
    }
}
