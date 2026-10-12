using colosseumbackend.Data;
using colosseumbackend.Data.Entities;

namespace colosseumbackend.Repositories;

public class ActionLogRepository : BaseRepository<ActionLog>
{
    public ActionLogRepository(AgoraDbContext dbContext) : base(dbContext)
    {
    }
}
