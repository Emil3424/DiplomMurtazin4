using System.Collections.Generic;
using System.Linq;

namespace DiplomMurtazin
{
    public partial class UserViews
    {
        // Для удобства работы - список колонок
        public List<string> GetColumnList()
        {
            if (string.IsNullOrEmpty(SelectedColumns))
                return new List<string>();

            return SelectedColumns.Split(',').Select(c => c.Trim()).ToList();
        }

        public void SetColumnList(List<string> columns)
        {
            SelectedColumns = string.Join(",", columns);
        }
    }
}