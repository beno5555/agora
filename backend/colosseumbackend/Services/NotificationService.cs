using colosseumbackend.Common;
using colosseumbackend.Dtos;
using colosseumbackend.Mappers;
using colosseumbackend.Repositories;

namespace colosseumbackend.Services;

public class NotificationService
{
    private readonly NotificationRepository _notificationRepository;

    public NotificationService(NotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<ListResponse<NotificationDto>> GetMineAsync(string wallet, PageQuery pageQuery)
    {
        var response      = new ListResponse<NotificationDto>();
        var notifications = await _notificationRepository.GetForWalletAsync(wallet, pageQuery.PageNumber, pageQuery.PageSize);

        response.Ok(notifications.Select(NotificationMapper.ToDto).ToList(), ResponseMessages.NotificationsRetrieved);
        return response;
    }

    public async Task<ApplicationResponse> MarkReadAsync(long id, string wallet)
    {
        var  response = new ApplicationResponse();
        bool updated  = await _notificationRepository.MarkReadAsync(id, wallet);

        if (updated)
        {
            response.Ok(ResponseMessages.NotificationRead);
        }
        else
        {
            response.Fail(ResponseMessages.NotificationNotFound);
        }

        return response;
    }
}
