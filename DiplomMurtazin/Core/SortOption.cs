namespace DiplomMurtazin.Core
{
    public class SortOption
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsAscending { get; set; }

        public override string ToString() => Name;
    }
}