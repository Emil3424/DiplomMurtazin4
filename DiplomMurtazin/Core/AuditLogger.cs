using System;
using System.Data.SqlClient;

namespace DiplomMurtazin.Core
{
    public static class AuditLogger
    {
        public static void Log(string actionType, string entityType, string details, string entityId = null, string metadata = null)
        {
            try
            {
                using (var context = new KPMurtazinEntities())
                {
                    var user = App.CurrentUser;
                    var employeeId = user?.EmployeeID;
                    var userLogin = user?.Login ?? "SYSTEM";

                    const string sql = @"
INSERT INTO dbo.AuditLog
(
    EventDate,
    UserLogin,
    EmployeeID,
    ActionType,
    EntityType,
    EntityID,
    Details,
    Metadata
)
VALUES
(
    @EventDate,
    @UserLogin,
    @EmployeeID,
    @ActionType,
    @EntityType,
    @EntityID,
    @Details,
    @Metadata
)";

                    context.Database.ExecuteSqlCommand(
                        sql,
                        new SqlParameter("@EventDate", DateTime.Now),
                        new SqlParameter("@UserLogin", (object)userLogin ?? DBNull.Value),
                        new SqlParameter("@EmployeeID", (object)employeeId ?? DBNull.Value),
                        new SqlParameter("@ActionType", (object)actionType ?? DBNull.Value),
                        new SqlParameter("@EntityType", (object)entityType ?? DBNull.Value),
                        new SqlParameter("@EntityID", (object)entityId ?? DBNull.Value),
                        new SqlParameter("@Details", (object)details ?? DBNull.Value),
                        new SqlParameter("@Metadata", (object)metadata ?? DBNull.Value)
                    );
                }
            }
            catch
            {
                // Не блокируем бизнес-операции, если аудит недоступен.
            }
        }
    }
}
