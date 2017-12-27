using Dto;

namespace Tasks.BuiltIn.Note
{
    public class NoteDto : CMTaskDataDtoBase
    {
        /// <summary>
        /// The note that is held by this task data
        /// </summary>
        public string Note { get; set; }
    }
}
