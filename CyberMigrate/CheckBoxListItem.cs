namespace CyberMigrate
{
    public class CheckBoxListItem<T>
    {
        public bool IsSelected { get; set; }

        public T ObjectData { get; set;  }

        public CheckBoxListItem(T objectData)
        {
            ObjectData = objectData;
        }

        public CheckBoxListItem(T objectData, bool isSelected)
        {
            ObjectData = objectData;
            IsSelected = isSelected;
        }
    }
}
