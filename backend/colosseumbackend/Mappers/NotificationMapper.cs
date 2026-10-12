using colosseumbackend.Data.Entities;
using colosseumbackend.Dtos;

namespace colosseumbackend.Mappers;

public static class NotificationMapper
{
    public static NotificationDto ToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id        = notification.Id,
            DealId    = notification.DealId,
            Kind      = notification.Kind,
            CreatedAt = notification.CreatedAt,
            IsRead    = notification.ReadAt is not null,
        };
    }
}
